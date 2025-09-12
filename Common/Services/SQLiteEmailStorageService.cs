using Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Common.Services
{
    //Servicio creado para manejar el almacenamiento de emails en SQLite (local), se usa en el login para mostrar los emails guardados y agilizar el login
    public class SQLiteEmailStorageService: ISQLiteEmailStorageService
    {
        private readonly string _dbPath;

        public SQLiteEmailStorageService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appData, "NetErp");
            Directory.CreateDirectory(appFolder);
            _dbPath = Path.Combine(appFolder, "emails.db");
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
              CREATE TABLE IF NOT EXISTS SavedEmails (
                  Email TEXT PRIMARY KEY,
                  LastUsed DATETIME NOT NULL,
                  UseCount INTEGER NOT NULL DEFAULT 1
              )";
            command.ExecuteNonQuery();
        }

        public async Task<List<string>> GetSavedEmailsAsync()
        {
            // Retorna emails ordenados por frecuencia de uso y fecha
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT Email FROM SavedEmails ORDER BY UseCount DESC, LastUsed DESC LIMIT 10";

            List<string> emails = [];
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                emails.Add(reader.GetString(reader.GetOrdinal("Email")));
            }
            return emails;
        }

        public async Task SaveEmailAsync(string email)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
              INSERT OR REPLACE INTO SavedEmails (Email, LastUsed, UseCount)
              VALUES (@email, @lastUsed,
                  COALESCE((SELECT UseCount FROM SavedEmails WHERE Email = @email), 0) + 1)";
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@lastUsed", DateTime.Now);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> ShouldPromptToSaveEmailAsync(string email)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM SavedEmails WHERE Email = @email";
            command.Parameters.AddWithValue("@email", email);

            int count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count == 0; // Solo preguntar si es nuevo
        }

        public async Task RemoveEmailAsync(string email)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM SavedEmails WHERE Email = @email";
            command.Parameters.AddWithValue("@email", email);

            await command.ExecuteNonQueryAsync();
        }
    }
}
