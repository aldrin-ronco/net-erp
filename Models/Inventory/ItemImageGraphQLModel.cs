using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class ItemImageGraphQLModel
    {
        public string Id { get; set; } = string.Empty;
        public int ItemId { get; set; }
        public string S3Bucket { get; set; } = string.Empty;
        public string S3BucketDirectory {  get; set; } = string.Empty;
        public string S3FileName {  get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
