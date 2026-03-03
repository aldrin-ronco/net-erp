using Models.Books;
using Models.Global;
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
        public CompanyGraphQLModel Company { get; set; } = new();
        public CatalogGraphQLModel Catalog { get; set; }
        public MeasurementUnitGraphQLModel DefaultMeasurementUnit { get; set; }
        public AccountingGroupGraphQLModel DefaultAccountingGroup { get; set; }
        public SystemAccountGraphQLModel CreatedBy { get; set; } = new();
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
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
