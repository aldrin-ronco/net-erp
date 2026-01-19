using Models.Books;
using Models.Global;
using System;
using System.Collections.ObjectModel;
using static Models.Global.GraphQLResponseTypes;

namespace Common.Helpers
{
    /// <summary>
    /// Caché estática global para datos comunes utilizados en múltiples módulos.
    /// Carga los datos una vez al inicio de la aplicación y los mantiene en memoria.
    /// </summary>
    public static class GlobalDataCache
    {
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Tipos de identificación disponibles en el sistema
        /// </summary>
        public static ObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes { get; private set; } = [];

        /// <summary>
        /// Países con sus departamentos y ciudades
        /// </summary>
        public static ObservableCollection<CountryGraphQLModel> Countries { get; private set; } = [];
        public static ObservableCollection<WithholdingTypeGraphQLModel> WithholdingTypes { get; private set; } = [];
        
        /// <summary>
        /// Indica si el caché ya ha sido inicializado
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                lock (_lock)
                {
                    return _isInitialized;
                }
            }
        }

        /// <summary>
        /// Inicializa el caché con los datos comunes.
        /// Este método debe ser llamado al inicio de la aplicación después del login.
        /// </summary>
        /// <param name="identificationTypes">Lista de tipos de identificación</param>
        /// <param name="countries">Lista de países con departamentos y ciudades</param>
        /// /// <param name="withholdingTypes">Lista de tipos de retenciones</param>
        public static void Initialize(
            ObservableCollection<IdentificationTypeGraphQLModel> identificationTypes,
            ObservableCollection<CountryGraphQLModel> countries,
            ObservableCollection<WithholdingTypeGraphQLModel> withholdingTypes)
        {
            lock (_lock)
            {
                if (_isInitialized)
                {
                    throw new InvalidOperationException("GlobalDataCache ya ha sido inicializado. Use Clear() antes de reinicializar.");
                }

                IdentificationTypes = identificationTypes ?? [];
                Countries = countries ?? [];
                WithholdingTypes = withholdingTypes ?? [];
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Limpia el caché y permite reinicializar.
        /// Útil para cambios de sesión o actualizaciones de datos maestros.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                IdentificationTypes.Clear();
                Countries.Clear();
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Refresca los datos del caché sin necesidad de cerrar sesión.
        /// </summary>
        /// <param name="identificationTypes">Nueva lista de tipos de identificación</param>
        /// <param name="countries">Nueva lista de países</param>
        public static void Refresh(
            ObservableCollection<IdentificationTypeGraphQLModel> identificationTypes,
            ObservableCollection<CountryGraphQLModel> countries)
        {
            lock (_lock)
            {
                IdentificationTypes.Clear();
                Countries.Clear();

                foreach (var item in identificationTypes ?? [])
                {
                    IdentificationTypes.Add(item);
                }

                foreach (var item in countries ?? [])
                {
                    Countries.Add(item);
                }

                _isInitialized = true;
            }
        }
    }
}
