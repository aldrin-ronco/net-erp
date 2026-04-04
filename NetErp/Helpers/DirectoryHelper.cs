using System;
using System.IO;

namespace NetErp.Helpers
{
    /// <summary>
    /// Helper para verificar la existencia de carpetas en la ruta de instalación.
    /// </summary>
    public static class DirectoryHelper
    {
        private static readonly string BasePath = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// Verifica si una carpeta existe en la ruta de instalación.
        /// </summary>
        /// <param name="relativePath">Ruta relativa desde el directorio de instalación (ej: "Reports/Books")</param>
        /// <returns>true si la carpeta existe, false si no</returns>
        public static bool Exists(string relativePath)
        {
            return Directory.Exists(Path.Combine(BasePath, relativePath));
        }

        /// <summary>
        /// Retorna la ruta absoluta de una carpeta relativa al directorio de instalación.
        /// </summary>
        public static string GetFullPath(string relativePath)
        {
            return Path.Combine(BasePath, relativePath);
        }

        /// <summary>
        /// Crea la carpeta en la ruta de instalación.
        /// </summary>
        public static void Create(string relativePath)
        {
            Directory.CreateDirectory(Path.Combine(BasePath, relativePath));
        }
    }
}
