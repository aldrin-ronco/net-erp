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
    public class ItemGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameTemplate { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool AllowFraction { get; set; }
        public bool HasExtendedInformation { get; set; }
        public bool AiuBasedService { get; set; }
        public bool Billable { get; set; }
        public bool AmountBasedOnWeight { get; set; }
        public CompanyGraphQLModel Company { get; set; } = new();
        public MeasurementUnitGraphQLModel MeasurementUnit { get; set; } = new();
        public ItemBrandGraphQLModel Brand { get; set; } = new();
        public ItemSizeCategoryGraphQLModel SizeCategory { get; set; } = new();
        public AccountingGroupGraphQLModel AccountingGroup { get; set; } = new();
        public ItemSubCategoryGraphQLModel SubCategory { get; set; } = new();
        public AccountingAccountGraphQLModel ReceivableAccount { get; set; } = new();
        public IEnumerable<ComponentsByItemGraphQLModel> Components { get; set; } = [];
        public SystemAccountGraphQLModel CreatedBy { get; set; } = new();
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public IEnumerable<EanCodeByItemGraphQLModel> EanCodes { get; set; } = [];
        public IEnumerable<ImageByItemGraphQLModel> Images { get; set; } = [];
        public IEnumerable<StockGraphQLModel> Stocks { get; set; } = [];
    }

    public class ItemCreateMessage
    {
        public UpsertResponseType<ItemGraphQLModel> CreatedItem { get; set; } = new();
    }

    public class ItemUpdateMessage
    {
        public UpsertResponseType<ItemGraphQLModel> UpdatedItem { get; set; } = new();
    }

    public class ItemDeleteMessage
    {
        public DeleteResponseType DeletedItem { get; set; } = new();
    }


}
