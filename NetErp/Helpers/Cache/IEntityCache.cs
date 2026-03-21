using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NetErp.Helpers.Cache
{
    /// <summary>
    /// Interfaz no-genérica base para limpieza centralizada de caches.
    /// Permite que ShellViewModel limpie todos los caches al salir de una empresa.
    /// </summary>
    public interface IEntityCache
    {
        /// <summary>
        /// Indica si el cache ya fue inicializado
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Limpia el cache y marca como no inicializado
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Interfaz genérica para caches de entidades con lazy loading.
    /// Cada implementación maneja su propia carga, CRUD y suscripción a mensajes.
    /// </summary>
    /// <typeparam name="T">Tipo de la entidad</typeparam>
    public interface IEntityCache<T> : IEntityCache where T : class
    {
        /// <summary>
        /// Colección de items cacheados (solo lectura para consumidores externos).
        /// Usar los métodos Add/Update/Remove para modificar el cache.
        /// </summary>
        ReadOnlyObservableCollection<T> Items { get; }

        /// <summary>
        /// Asegura que los datos estén cargados. Si no están inicializados, los carga.
        /// </summary>
        Task EnsureLoadedAsync();

        /// <summary>
        /// Agrega un item al cache
        /// </summary>
        void Add(T item);

        /// <summary>
        /// Actualiza un item existente en el cache
        /// </summary>
        void Update(T item);

        /// <summary>
        /// Remueve un item del cache por su Id
        /// </summary>
        void Remove(int id);
    }
}
