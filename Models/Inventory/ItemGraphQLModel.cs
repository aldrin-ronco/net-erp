using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class ItemGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool AllowFraction { get; set; }
        public bool HasExtendedInformation { get; set; }
        public MeasurementUnitGraphQLModel MeasurementUnit { get; set; } = new();
        public bool AiuBasedService { get; set; }
        public bool Billable { get; set; }
        public bool AmountBasedOnWeight { get; set; }
        public BrandGraphQLModel Brand { get; set; } = new();
        public ItemSizeMasterGraphQLModel Size { get; set; } = new();
        public AccountingGroupGraphQLModel AccountingGroup { get; set; } = new();
        public ItemSubCategoryGraphQLModel SubCategory { get; set; } = new();
        public IEnumerable<ItemDetailGraphQLModel> RelatedProducts { get; set; } = [];
        public IEnumerable<EanCodeGraphQLModel> EanCodes { get; set; } = [];
        public IEnumerable<ItemImageGraphQLModel> Images { get; set; } = [];
    }

    public class ItemCreateMessage
    {
        public ItemGraphQLModel CreatedItem { get; set; }
    }

    public class ItemUpdateMessage
    {
        public ItemGraphQLModel UpdatedItem { get; set; }
    }

    public class ItemDeleteMessage
    {
        public ItemGraphQLModel DeletedItem { get; set; }
    }


}
