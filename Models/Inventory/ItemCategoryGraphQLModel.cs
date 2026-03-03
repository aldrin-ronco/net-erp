using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class ItemCategoryGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ItemTypeGraphQLModel ItemType { get; set; } = new();
        public CompanyGraphQLModel Company { get; set; } = new();
        public SystemAccountGraphQLModel CreatedBy { get; set; } = new();
        public IEnumerable<ItemSubCategoryGraphQLModel> ItemsSubCategories { get; set; }
    }

    public class ItemCategoryCreateMessage
    {
        public ItemCategoryGraphQLModel CreatedItemCategory { get; set; }
    }
    public class ItemCategoryUpdateMessage
    {
        public ItemCategoryGraphQLModel UpdatedItemCategory { get; set; }
    }
    public class ItemCategoryDeleteMessage
    {
        public ItemCategoryGraphQLModel DeletedItemCategory { get; set; }
    }
}
