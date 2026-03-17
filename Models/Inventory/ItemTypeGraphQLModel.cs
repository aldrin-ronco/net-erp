using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

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
        public IEnumerable<ItemCategoryGraphQLModel> ItemCategories { get; set; }
    }

    public class ItemTypeCreateMessage
    {
        public UpsertResponseType<ItemTypeGraphQLModel> CreatedItemType { get; set; } = new();
    }

    public class ItemTypeUpdateMessage
    {
        public UpsertResponseType<ItemTypeGraphQLModel> UpdatedItemType { get; set; } = new();
    }

    public class ItemTypeDeleteMessage
    {
        public DeleteResponseType DeletedItemType { get; set; } = new();
    }

}
