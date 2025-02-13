﻿using System;
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
        public ItemTypeGraphQLModel ItemType { get; set; } = new();
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
