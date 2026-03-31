namespace Models.Global
{
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
