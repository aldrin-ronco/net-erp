namespace Models.Global
{
    public class UserPermissionGraphQLModel
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;
        public string? ExpiresAt { get; set; }
        public PermissionDefinitionGraphQLModel? PermissionDefinition { get; set; }
    }
}
