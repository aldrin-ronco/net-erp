using Models.Inventory;
using NetErp.Inventory.CatalogItems.DTO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Helpers.Services
{
    /// <summary>
    /// Provee imágenes (BitmapImage cargado lazy) para items, con cache en sesión
    /// y descarga local-first desde S3. Se registra como singleton — el cache vive
    /// hasta logout/cambio de empresa, beneficiando todos los módulos que muestran
    /// items con imagen (entradas/salidas por concepto, transferencias, compras,
    /// catálogo, etc).
    /// </summary>
    public interface IItemImageProvider
    {
        /// <summary>True cuando hay configuración S3 + cache local válidos.</summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Resuelve imágenes del item indicado. Retorna colección — la UI puede
        /// bindearse y los <c>BitmapImage</c> se asignan async via
        /// <c>NotifyOfPropertyChange</c> en cada DTO al completar descarga.
        /// </summary>
        Task<IReadOnlyList<ImageByItemDTO>> GetImagesAsync(ItemGraphQLModel item, CancellationToken token = default);

        /// <summary>Limpia el cache. Invocado en logout/company switch vía IEntityCache.</summary>
        void Clear();
    }
}
