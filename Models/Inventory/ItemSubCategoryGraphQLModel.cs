using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class ItemSubCategoryGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ItemCategoryGraphQLModel ItemCategory { get; set; } = new();
        public IEnumerable<ItemGraphQLModel> Items { get; set; }
    }
}
