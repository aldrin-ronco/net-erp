namespace Models.Global
{
    public class GlobalConfigGraphQLModel
    {
        public int Id { get; set; } = 0;
        public AwsS3ConfigGraphQLModel DefaultAwsS3Config { get; set; } = new();
        public CompanyGraphQLModel Company { get; set; } = new();
        public SystemAccountGraphQLModel CreatedBy { get; set; } = new();
        public string InsertedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }
}
