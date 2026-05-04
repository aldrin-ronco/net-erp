using Models.Billing;
using Models.Suppliers;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    /// <summary>
    /// Resultado público devuelto por <c>IAccountingEntityLookupService</c>.
    /// </summary>
    public class AccountingEntityLookupResult
    {
        public AccountingEntityGraphQLModel? AccountingEntity { get; init; }
        public bool AlreadyExistsAsCurrentType { get; init; }
    }

    /// <summary>
    /// Contexto GraphQL para deserializar una request combinada
    /// (accountingEntityByIdentificationNumber + customersPage) en una sola operación.
    /// </summary>
    public class AccountingEntityCustomerLookupContext
    {
        public AccountingEntityGraphQLModel? AccountingEntity { get; set; }
        public PageType<CustomerGraphQLModel>? Existing { get; set; }
    }

    public class AccountingEntitySellerLookupContext
    {
        public AccountingEntityGraphQLModel? AccountingEntity { get; set; }
        public PageType<SellerGraphQLModel>? Existing { get; set; }
    }

    public class AccountingEntitySupplierLookupContext
    {
        public AccountingEntityGraphQLModel? AccountingEntity { get; set; }
        public PageType<SupplierGraphQLModel>? Existing { get; set; }
    }
}
