using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class EanCodeGraphQLModel
    {
        public string Id { get; set; } = string.Empty;
        public ItemGraphQLModel Item { get; set; } = new();
        public string EanCode { get; set; } = string.Empty;
    }
}
