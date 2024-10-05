using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class ItemDetailGraphQLModel
    {
        public string Id { get; set; }
        public decimal Quantity { get; set; }
        public ItemGraphQLModel Parent { get; set; } = new();
        public ItemGraphQLModel Item { get; set; } = new();
    }
}
