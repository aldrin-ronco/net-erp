namespace Models.Global
{
    /// <summary>
    /// Published when company permission defaults are modified (create/update/delete).
    /// PermissionCache listens to this to reload resolved permissions.
    /// </summary>
    public class CompanyPermissionDefaultChangedMessage { }

    /// <summary>
    /// Published when user permissions are modified (individual or batch).
    /// PermissionCache listens to this to reload resolved permissions.
    /// </summary>
    public class UserPermissionChangedMessage { }

    /// <summary>
    /// Published by PermissionCache after it reloads.
    /// Business ViewModels listen to this to refresh their HasXPermission properties.
    /// </summary>
    public class PermissionsCacheRefreshedMessage { }

    public class PermissionDefinitionGraphQLModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PermissionType { get; set; } = string.Empty;
        public string SystemDefault { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public MenuItemGraphQLModel? MenuItem { get; set; }
    }
}
