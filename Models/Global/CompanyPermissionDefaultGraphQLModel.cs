namespace Models.Global
{
    public class CompanyPermissionDefaultGraphQLModel
    {
        public int Id { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public PermissionDefinitionGraphQLModel? PermissionDefinition { get; set; }
    }
}
