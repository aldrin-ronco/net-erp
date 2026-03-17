using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

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


    

        public class AwsS3ConfigCreateMessage
    {
        public UpsertResponseType<AwsS3ConfigGraphQLModel> CreatedAwsS3Config { get; set; } = new();
    }

    public class AwsS3ConfigUpdateMessage
    {
        public UpsertResponseType<AwsS3ConfigGraphQLModel> UpdatedAwsS3Config { get; set; } = new();
    }

    public class AwsS3ConfigDeleteMessage
    {
        public DeleteResponseType DeletedAwsS3Config { get; set; } = new();
    }
}
