using Common.Interfaces;
using Models.Billing;
using Models.Books;
using Models.Suppliers;
using NetErp.Helpers.GraphQLQueryBuilder;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Helpers.Services
{
    /// <summary>
    /// Implementación que ejecuta una sola request GraphQL por consulta combinando:
    /// (1) accountingEntityByIdentificationNumber(...) para traer el tercero, y
    /// (2) &lt;type&gt;sPage(filters: matching=number) para verificar duplicado del tipo
    /// que se está creando. Validación exacta client-side para descartar falsos positivos
    /// del filtro `matching` (que es búsqueda parcial).
    /// </summary>
    public class AccountingEntityLookupService(IRepository<AccountingEntityGraphQLModel> repo)
        : IAccountingEntityLookupService
    {
        private readonly IRepository<AccountingEntityGraphQLModel> _repo = repo;

        public async Task<AccountingEntityLookupResult> LookupForCustomerAsync(
            string identificationNumber,
            CancellationToken cancellationToken = default)
        {
            (GraphQLQueryFragment entityFragment, GraphQLQueryFragment existingFragment, string query) = _customerLookupQuery.Value;
            object variables = BuildVariables(entityFragment, existingFragment, identificationNumber);
            AccountingEntityCustomerLookupContext? ctx = await _repo.GetDataContextAsync<AccountingEntityCustomerLookupContext>(query, variables, cancellationToken);
            return new AccountingEntityLookupResult
            {
                AccountingEntity = ctx?.AccountingEntity,
                AlreadyExistsAsCurrentType = (ctx?.Existing?.TotalEntries ?? 0) > 0
            };
        }

        public async Task<AccountingEntityLookupResult> LookupForSellerAsync(
            string identificationNumber,
            CancellationToken cancellationToken = default)
        {
            (GraphQLQueryFragment entityFragment, GraphQLQueryFragment existingFragment, string query) = _sellerLookupQuery.Value;
            object variables = BuildVariables(entityFragment, existingFragment, identificationNumber);
            AccountingEntitySellerLookupContext? ctx = await _repo.GetDataContextAsync<AccountingEntitySellerLookupContext>(query, variables, cancellationToken);
            return new AccountingEntityLookupResult
            {
                AccountingEntity = ctx?.AccountingEntity,
                AlreadyExistsAsCurrentType = (ctx?.Existing?.TotalEntries ?? 0) > 0
            };
        }

        public async Task<AccountingEntityLookupResult> LookupForSupplierAsync(
            string identificationNumber,
            CancellationToken cancellationToken = default)
        {
            (GraphQLQueryFragment entityFragment, GraphQLQueryFragment existingFragment, string query) = _supplierLookupQuery.Value;
            object variables = BuildVariables(entityFragment, existingFragment, identificationNumber);
            AccountingEntitySupplierLookupContext? ctx = await _repo.GetDataContextAsync<AccountingEntitySupplierLookupContext>(query, variables, cancellationToken);
            return new AccountingEntityLookupResult
            {
                AccountingEntity = ctx?.AccountingEntity,
                AlreadyExistsAsCurrentType = (ctx?.Existing?.TotalEntries ?? 0) > 0
            };
        }

        private static object BuildVariables(
            GraphQLQueryFragment entityFragment,
            GraphQLQueryFragment existingFragment,
            string identificationNumber)
        {
            return new GraphQLVariables()
                .For(entityFragment, "identificationNumber", identificationNumber)
                .For(existingFragment, "filters", new { identificationNumber })
                .For(existingFragment, "pagination", new { Page = 1, PageSize = 1 })
                .Build();
        }

        private static GraphQLQueryFragment BuildEntityFragment()
        {
            var fields = FieldSpec<AccountingEntityGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.IdentificationNumber)
                .Field(f => f.VerificationDigit)
                .Field(f => f.CaptureType)
                .Field(f => f.Regime)
                .Field(f => f.BusinessName)
                .Field(f => f.FirstName)
                .Field(f => f.MiddleName)
                .Field(f => f.FirstLastName)
                .Field(f => f.MiddleLastName)
                .Field(f => f.TradeName)
                .Field(f => f.Address)
                .Field(f => f.PrimaryPhone)
                .Field(f => f.SecondaryPhone)
                .Field(f => f.PrimaryCellPhone)
                .Field(f => f.SecondaryCellPhone)
                .Select(f => f.IdentificationType, idType => idType
                    .Field(t => t.Id)
                    .Field(t => t.Code))
                .Select(f => f.Country, country => country.Field(c => c.Id))
                .Select(f => f.Department, dept => dept.Field(d => d.Id))
                .Select(f => f.City, city => city.Field(c => c.Id))
                .SelectList(f => f.Emails, emails => emails
                    .Field(e => e.Id)
                    .Field(e => e.Email)
                    .Field(e => e.Description)
                    .Field(e => e.IsElectronicInvoiceRecipient)
                    .Field(e => e.IsCorporate))
                .Build();

            return new GraphQLQueryFragment(
                "accountingEntityByIdentificationNumber",
                [new("identificationNumber", "String!")],
                fields,
                "accountingEntity");
        }

        private static readonly Lazy<(GraphQLQueryFragment EntityFragment, GraphQLQueryFragment ExistingFragment, string Query)> _customerLookupQuery = new(() =>
        {
            GraphQLQueryFragment entityFragment = BuildEntityFragment();

            var customerFields = FieldSpec<PageType<CustomerGraphQLModel>>
                .Create()
                .Field(p => p.TotalEntries)
                .Build();

            GraphQLQueryFragment customerFragment = new(
                "customersPage",
                [new("filters", "CustomerFilters"), new("pagination", "Pagination")],
                customerFields,
                "existing");

            string query = new QueryBuilder([entityFragment, customerFragment]).GetQuery();
            return (entityFragment, customerFragment, query);
        });

        private static readonly Lazy<(GraphQLQueryFragment EntityFragment, GraphQLQueryFragment ExistingFragment, string Query)> _sellerLookupQuery = new(() =>
        {
            GraphQLQueryFragment entityFragment = BuildEntityFragment();

            var sellerFields = FieldSpec<PageType<SellerGraphQLModel>>
                .Create()
                .Field(p => p.TotalEntries)
                .Build();

            GraphQLQueryFragment sellerFragment = new(
                "sellersPage",
                [new("filters", "SellerFilters"), new("pagination", "Pagination")],
                sellerFields,
                "existing");

            string query = new QueryBuilder([entityFragment, sellerFragment]).GetQuery();
            return (entityFragment, sellerFragment, query);
        });

        private static readonly Lazy<(GraphQLQueryFragment EntityFragment, GraphQLQueryFragment ExistingFragment, string Query)> _supplierLookupQuery = new(() =>
        {
            GraphQLQueryFragment entityFragment = BuildEntityFragment();

            var supplierFields = FieldSpec<PageType<SupplierGraphQLModel>>
                .Create()
                .Field(p => p.TotalEntries)
                .Build();

            GraphQLQueryFragment supplierFragment = new(
                "suppliersPage",
                [new("filters", "SupplierFilters"), new("pagination", "Pagination")],
                supplierFields,
                "existing");

            string query = new QueryBuilder([entityFragment, supplierFragment]).GetQuery();
            return (entityFragment, supplierFragment, query);
        });
    }
}
