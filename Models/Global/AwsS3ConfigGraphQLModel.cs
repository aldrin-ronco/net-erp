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
        public string Region { get; set; } = "us-east-1"; // cambiar por el primero del dictionaries
        public override string ToString()
        {
            return Description;
        }
    }
}
