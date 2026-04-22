namespace Models.Global
{
    public class GlobalConfigGraphQLModel
    {
        public int Id { get; set; }
        public AwsS3ConfigGraphQLModel DefaultAwsS3Config { get; set; } = new();
        public CompanyGraphQLModel Company { get; set; } = new();
        public SystemAccountGraphQLModel CreatedBy { get; set; } = new();
        public DianCertificateGraphQLModel DefaultDianCertificate {  get; set; } = new();
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; } 
    }
}
