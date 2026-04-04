using Models.Billing;
using Models.Books;
using NetErp.Helpers.Cache;
using System;

namespace NetErp.Helpers
{
    /// <summary>
    /// Typed constants for permission codes. Values must match exactly
    /// the codes seeded in the permission_definitions table.
    /// ACTION codes are const strings. FIELD codes use nameof() for refactor safety
    /// and Lazy for single computation.
    /// Add new constants here when new permissions are seeded in the database.
    /// </summary>
    public static class PermissionCodes
    {
        public static class Customer
        {
            private const string Prefix = "customer";

            // ACTION
            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";

            // FIELD — derived from model properties (refactor-safe, computed once)
            private static readonly Lazy<string> _phone = new(() => BuildField(nameof(AccountingEntityGraphQLModel.PrimaryPhone)));
            private static readonly Lazy<string> _cellPhone = new(() => BuildField(nameof(AccountingEntityGraphQLModel.PrimaryCellPhone)));
            private static readonly Lazy<string> _address = new(() => BuildField(nameof(AccountingEntityGraphQLModel.Address)));
            private static readonly Lazy<string> _city = new(() => BuildField(nameof(AccountingEntityGraphQLModel.City)));
            private static readonly Lazy<string> _tradeName = new(() => BuildField(nameof(AccountingEntityGraphQLModel.TradeName)));
            private static readonly Lazy<string> _commercialCode = new(() => BuildField(nameof(AccountingEntityGraphQLModel.CommercialCode)));
            private static readonly Lazy<string> _zone = new(() => BuildField(nameof(CustomerGraphQLModel.Zone)));

            public static string Phone => _phone.Value;
            public static string CellPhone => _cellPhone.Value;
            public static string Address => _address.Value;
            public static string City => _city.Value;
            public static string TradeName => _tradeName.Value;
            public static string CommercialCode => _commercialCode.Value;
            public static string Zone => _zone.Value;
            public static string Email => $"{Prefix}.email";

            private static string BuildField(string propertyName) => $"{Prefix}.{StringLengthCache.ToSnakeCase(propertyName)}";
        }

        public static class Catalog
        {
            private const string Prefix = "catalog";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class MeasurementUnit
        {
            private const string Prefix = "measurement_unit";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class ItemSize
        {
            private const string Prefix = "item_size";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class AccountingEntity
        {
            private const string Prefix = "accounting_entity";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class AccountingSource
        {
            private const string Prefix = "accounting_source";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class AccountingPresentation
        {
            private const string Prefix = "accounting_presentation";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class AccountingBook
        {
            private const string Prefix = "accounting_book";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class IdentificationType
        {
            private const string Prefix = "identification_type";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class AccountingGroup
        {
            private const string Prefix = "accounting_group";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class Tax
        {
            private const string Prefix = "tax";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class TaxCategory
        {
            private const string Prefix = "tax_category";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class WithholdingCertificate
        {
            private const string Prefix = "withholding_certificate";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class AuthorizationSequence
        {
            private const string Prefix = "authorization_sequence";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class DianCertificate
        {
            private const string Prefix = "dian_certificate";

            public const string Create = $"{Prefix}.create";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class Smtp
        {
            private const string Prefix = "smtp";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class AwsS3Config
        {
            private const string Prefix = "aws_s3_config";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class Email
        {
            private const string Prefix = "email";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
        }

        public static class MenuItem
        {
            private const string Prefix = "menu_item";

            public const string Create = $"{Prefix}.create";
            public const string Edit = $"{Prefix}.edit";
            public const string Delete = $"{Prefix}.delete";
            public const string Reorder = $"{Prefix}.reorder";
        }
    }
}
