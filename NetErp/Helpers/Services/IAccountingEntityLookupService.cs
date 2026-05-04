using Models.Books;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Helpers.Services
{
    /// <summary>
    /// Servicio reutilizable para buscar un tercero (AccountingEntity) por número de
    /// identificación al mismo tiempo que se verifica si ya está vinculado al tipo
    /// que se está creando (Customer/Seller/Supplier). Cada método ejecuta una sola
    /// request GraphQL combinada.
    /// </summary>
    public interface IAccountingEntityLookupService
    {
        Task<AccountingEntityLookupResult> LookupForCustomerAsync(
            string identificationNumber,
            CancellationToken cancellationToken = default);

        Task<AccountingEntityLookupResult> LookupForSellerAsync(
            string identificationNumber,
            CancellationToken cancellationToken = default);

        Task<AccountingEntityLookupResult> LookupForSupplierAsync(
            string identificationNumber,
            CancellationToken cancellationToken = default);
    }
}
