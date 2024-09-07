using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class CatalogGraphQLModel
    {
        public int Id {  get; set; }
        public string Name { get; set; } = string.Empty;
        public IEnumerable<ItemTypeGraphQLModel> ItemsTypes { get; set; }
    }

}
