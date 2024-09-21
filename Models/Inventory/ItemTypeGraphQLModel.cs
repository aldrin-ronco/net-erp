using Models.Books;
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
        public MeasurementUnitGraphQLModel MeasurementUnitByDefault { get; set; }
        public AccountingGroupGraphQLModel AccountingGroupByDefault { get; set; }
        public IEnumerable<ItemCategoryGraphQLModel> ItemsCategories { get; set; }
    }

    public class ItemTypeCreateMessage
    {
        public ItemTypeGraphQLModel CreatedItemType { get; set; }
    }
    public class ItemTypeUpdateMessage 
    {
        public ItemTypeGraphQLModel UpdatedItemType { get; set; }
    }

    public class ItemTypeDeleteMessage
    {
        public ItemTypeGraphQLModel DeletedItemType { get; set; }
    }

}
