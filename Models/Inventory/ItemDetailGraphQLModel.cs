using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class ComponentsByItemGraphQLModel
    {
        public string Id { get; set; }
        public decimal Quantity { get; set; }
        public ItemGraphQLModel Parent { get; set; } = new();
        public ItemGraphQLModel Component { get; set; } = new();
    }
}
