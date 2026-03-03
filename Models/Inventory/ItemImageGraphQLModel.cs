using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class ImageByItemGraphQLModel
    {
        public int DisplayOrder { get; set; }
        public ItemGraphQLModel Item { get; set; } = new();
        public string S3Bucket { get; set; } = string.Empty;
        public string S3BucketDirectory {  get; set; } = string.Empty;
        public string S3FileName {  get; set; } = string.Empty;
    }
}
