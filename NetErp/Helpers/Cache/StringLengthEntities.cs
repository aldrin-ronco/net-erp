using Models.Billing;
using Models.Books;
using Models.Global;
using Models.Inventory;
using Models.Suppliers;
using System;
using System.Collections.Generic;

namespace NetErp.Helpers.Cache
{
    /// <summary>
    /// Defines entity groupings per module for StringLengthCache loading.
    /// Each array contains the GraphQL model types whose string field lengths
    /// should be loaded together when a module initializes.
    /// </summary>
    public static class StringLengthEntities
    {
        /// <summary>
        /// Entities whose database tables have NO varchar/string columns.
        /// These are excluded from API requests (no point sending them) and
        /// marked as loaded with zero fields in the cache.
        ///
        /// IMPORTANT: If a varchar column is ever added to one of these tables,
        /// the corresponding type MUST be removed from this list so that
        /// StringLengthCache starts fetching its constraints from the API.
        /// </summary>
        public static readonly HashSet<Type> NoStringFieldEntities = [typeof(SellerGraphQLModel)];

        /// <summary>
        /// Explicit entity name overrides for models whose class name does not match
        /// the API entity name by convention (strip "GraphQLModel" + ToSnakeCase).
        /// Example: WithholdingCertificateConfigGraphQLModel → convention produces
        /// "withholding_certificate_config" but the API entity is "withholding_certificate".
        ///
        /// IMPORTANT: Only add entries here when the convention fails. If the model
        /// class is renamed to match the convention, remove the override.
        /// </summary>
        public static readonly Dictionary<Type, string> EntityNameOverrides = new()
        {
            [typeof(WithholdingCertificateConfigGraphQLModel)] = "withholding_certificate"
        };

        // Billing
        public static readonly Type[] Customer = [typeof(CustomerGraphQLModel), typeof(AccountingEntityGraphQLModel), typeof(EmailGraphQLModel)];
        public static readonly Type[] Seller = [typeof(SellerGraphQLModel), typeof(AccountingEntityGraphQLModel)];
        public static readonly Type[] Zone = [typeof(ZoneGraphQLModel)];
        public static readonly Type[] PriceList = [typeof(PriceListGraphQLModel)];

        // Books
        public static readonly Type[] AccountingEntity = [typeof(AccountingEntityGraphQLModel), typeof(EmailGraphQLModel)];
        public static readonly Type[] WithholdingCertificateConfig = [typeof(WithholdingCertificateConfigGraphQLModel)];
        public static readonly Type[] IdentificationType = [typeof(IdentificationTypeGraphQLModel)];
        public static readonly Type[] AccountingAccount = [typeof(AccountingAccountGraphQLModel)];
        public static readonly Type[] AccountingBook = [typeof(AccountingBookGraphQLModel)];
        public static readonly Type[] AccountingPresentation = [typeof(AccountingPresentationGraphQLModel)];
        public static readonly Type[] TaxCategory = [typeof(TaxCategoryGraphQLModel)];
        public static readonly Type[] Tax = [typeof(TaxGraphQLModel)];
        public static readonly Type[] AccountingGroup = [typeof(AccountingGroupGraphQLModel)];

        // Global
        public static readonly Type[] AccessProfile = [typeof(AccessProfileGraphQLModel)];
        public static readonly Type[] PermissionDefinition = [typeof(PermissionDefinitionGraphQLModel)];
        public static readonly Type[] Email = [typeof(EmailGraphQLModel), typeof(SmtpGraphQLModel)];
        public static readonly Type[] Smtp = [typeof(SmtpGraphQLModel)];
        public static readonly Type[] AwsS3Config = [typeof(AwsS3ConfigGraphQLModel)];
        public static readonly Type[] S3StorageLocation = [typeof(S3StorageLocationGraphQLModel)];

        // Suppliers
        public static readonly Type[] Supplier = [typeof(SupplierGraphQLModel), typeof(AccountingEntityGraphQLModel)];

        // CostCenters
        public static readonly Type[] CostCenters = [typeof(CompanyGraphQLModel), typeof(CompanyLocationGraphQLModel), typeof(CostCenterGraphQLModel), typeof(StorageGraphQLModel)];

        // Inventory
        public static readonly Type[] CatalogItem = [typeof(CatalogGraphQLModel), typeof(ItemGraphQLModel), typeof(ItemTypeGraphQLModel), typeof(ItemCategoryGraphQLModel), typeof(ItemSubCategoryGraphQLModel)];
        public static readonly Type[] MeasurementUnit = [typeof(MeasurementUnitGraphQLModel)];

        // Global
        public static readonly Type[] MenuItem = [typeof(MenuItemGraphQLModel)];
    }
}
