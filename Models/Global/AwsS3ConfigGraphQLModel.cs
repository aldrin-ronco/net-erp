using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class AwsS3ConfigGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Description { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Region { get; set; } = "us-east-1";
        public CompanyGraphQLModel Company { get; set; } = new();
        public SystemAccountGraphQLModel CreatedBy { get; set; } = new();
        public string InsertedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;

        public override string ToString() => Description;
    }
}
