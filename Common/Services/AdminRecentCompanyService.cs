using Common.Interfaces;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Common.Services
{
    public class AdminRecentCompanyService : IAdminRecentCompanyService
    {
        private readonly string _dbPath;

        public AdminRecentCompanyService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appData, "NetErp");
            Directory.CreateDirectory(appFolder);
            _dbPath = Path.Combine(appFolder, "neterp.db");
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using SqliteConnection connection = new($"Data Source={_dbPath}");
            connection.Open();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS RecentCompanies (
                    AccountId INTEGER NOT NULL,
                    CompanyId INTEGER NOT NULL,
                    CompanyData TEXT NOT NULL,
                    DisplayName TEXT NOT NULL,
                    OrganizationName TEXT NOT NULL DEFAULT '',
                    LastAccessedAt TEXT NOT NULL,
                    PRIMARY KEY (AccountId, CompanyId)
                )";
            command.ExecuteNonQuery();
        }

        public async Task<List<AdminRecentCompanyEntry>> GetRecentCompaniesAsync(int accountId, int limit = 10)
        {
            using SqliteConnection connection = new($"Data Source={_dbPath}");
            await connection.OpenAsync();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
                SELECT AccountId, CompanyId, CompanyData, DisplayName, OrganizationName, LastAccessedAt
                FROM RecentCompanies
                WHERE AccountId = @accountId
                ORDER BY LastAccessedAt DESC
                LIMIT @limit";
            command.Parameters.AddWithValue("@accountId", accountId);
            command.Parameters.AddWithValue("@limit", limit);

            List<AdminRecentCompanyEntry> entries = [];
            using SqliteDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                entries.Add(new AdminRecentCompanyEntry
                {
                    AccountId = reader.GetInt32(reader.GetOrdinal("AccountId")),
                    CompanyId = reader.GetInt32(reader.GetOrdinal("CompanyId")),
                    CompanyData = reader.GetString(reader.GetOrdinal("CompanyData")),
                    DisplayName = reader.GetString(reader.GetOrdinal("DisplayName")),
                    OrganizationName = reader.GetString(reader.GetOrdinal("OrganizationName")),
                    LastAccessedAt = reader.GetString(reader.GetOrdinal("LastAccessedAt"))
                });
            }
            return entries;
        }

        private const int MaxRecentEntries = 20;

        public async Task SaveRecentCompanyAsync(int accountId, int companyId, string companyData, string displayName, string organizationName)
        {
            using SqliteConnection connection = new($"Data Source={_dbPath}");
            await connection.OpenAsync();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO RecentCompanies (AccountId, CompanyId, CompanyData, DisplayName, OrganizationName, LastAccessedAt)
                VALUES (@accountId, @companyId, @companyData, @displayName, @organizationName, @lastAccessedAt)";
            command.Parameters.AddWithValue("@accountId", accountId);
            command.Parameters.AddWithValue("@companyId", companyId);
            command.Parameters.AddWithValue("@companyData", companyData);
            command.Parameters.AddWithValue("@displayName", displayName);
            command.Parameters.AddWithValue("@organizationName", organizationName);
            command.Parameters.AddWithValue("@lastAccessedAt", DateTime.UtcNow.ToString("o"));

            await command.ExecuteNonQueryAsync();

            // Purge oldest entries beyond limit
            SqliteCommand purgeCommand = connection.CreateCommand();
            purgeCommand.CommandText = @"
                DELETE FROM RecentCompanies
                WHERE AccountId = @accountId
                AND CompanyId NOT IN (
                    SELECT CompanyId FROM RecentCompanies
                    WHERE AccountId = @accountId
                    ORDER BY LastAccessedAt DESC
                    LIMIT @maxEntries
                )";
            purgeCommand.Parameters.AddWithValue("@accountId", accountId);
            purgeCommand.Parameters.AddWithValue("@maxEntries", MaxRecentEntries);
            await purgeCommand.ExecuteNonQueryAsync();
        }
    }
}
