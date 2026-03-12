using Models.Billing;
using Models.Books;
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
        public static readonly Type[] Customer = [typeof(CustomerGraphQLModel), typeof(AccountingEntityGraphQLModel)];
        public static readonly Type[] Seller = [typeof(SellerGraphQLModel)];

        // Inventory
        public static readonly Type[] CatalogItem = [typeof(CatalogGraphQLModel), typeof(ItemGraphQLModel), typeof(ItemTypeGraphQLModel), typeof(ItemCategoryGraphQLModel), typeof(ItemSubCategoryGraphQLModel)];
    }
}
