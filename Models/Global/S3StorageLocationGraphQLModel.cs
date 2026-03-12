namespace Models.Global
{
    public class S3StorageLocationGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Key { get; set; } = string.Empty;
        public string Bucket { get; set; } = string.Empty;
        public string Directory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AwsS3ConfigGraphQLModel? AwsS3Config { get; set; }
    }
}
