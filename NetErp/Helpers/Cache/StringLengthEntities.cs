using Models.Billing;
using Models.Books;
using Models.Global;
using Models.Inventory;
using System;

namespace NetErp.Helpers.Cache
{
    /// <summary>
    /// Defines entity groupings per module for StringLengthCache loading.
    /// Each array contains the GraphQL model types whose string field lengths
    /// should be loaded together when a module initializes.
    /// </summary>
    public static class StringLengthEntities
    {
        // Billing
        public static readonly Type[] Customer = [typeof(CustomerGraphQLModel), typeof(AccountingEntityGraphQLModel), typeof(EmailGraphQLModel)];
        public static readonly Type[] Seller = [typeof(SellerGraphQLModel), typeof(AccountingEntityGraphQLModel)];
        public static readonly Type[] Zone = [typeof(ZoneGraphQLModel)];
        public static readonly Type[] PriceList = [typeof(PriceListGraphQLModel)];

        // Books
        public static readonly Type[] AccountingEntity = [typeof(AccountingEntityGraphQLModel), typeof(EmailGraphQLModel)];
        public static readonly Type[] WithholdingCertificateConfig = [typeof(WithholdingCertificateConfigGraphQLModel)];
        public static readonly Type[] IdentificationType = [typeof(IdentificationTypeGraphQLModel)];
        public static readonly Type[] TaxCategory = [typeof(TaxCategoryGraphQLModel)];
        public static readonly Type[] Tax = [typeof(TaxGraphQLModel)];

        // Inventory
        public static readonly Type[] CatalogItem = [typeof(CatalogGraphQLModel), typeof(ItemGraphQLModel), typeof(ItemTypeGraphQLModel), typeof(ItemCategoryGraphQLModel), typeof(ItemSubCategoryGraphQLModel)];
    }
}
