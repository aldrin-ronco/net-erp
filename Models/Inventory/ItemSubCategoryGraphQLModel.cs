using Models.Global;
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
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ItemCategoryGraphQLModel ItemCategory { get; set; } = new();
        public CompanyGraphQLModel Company { get; set; } = new();
        public SystemAccountGraphQLModel CreatedBy { get; set; } = new();
        public IEnumerable<ItemGraphQLModel> Items { get; set; }
    }

    public class ItemSubCategoryCreateMessage
    {
        public ItemSubCategoryGraphQLModel CreatedItemSubCategory { get; set; }
    }
    public class ItemSubCategoryUpdateMessage
    {
        public ItemSubCategoryGraphQLModel UpdatedItemSubCategory { get; set; }
    }
    public class ItemSubCategoryDeleteMessage
    {
        public ItemSubCategoryGraphQLModel DeletedItemSubCategory { get; set; }
    }
}
