using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class ItemTypeGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PrefixChar {  get; set; } = string.Empty;
        public bool StockControl { get; set; }
        public CatalogGraphQLModel Catalog { get; set; }
        public IEnumerable<ItemCategoryGraphQLModel> ItemsCategories { get; set; }
    }
}
