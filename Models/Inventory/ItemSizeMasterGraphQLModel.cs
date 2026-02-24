using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class ItemSizeCategoryGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public IEnumerable<ItemSizeValueGraphQLModel> ItemSizeValues { get; set; } = [];
        public CompanyGraphQLModel Company { get; set; } = new();
        //TODO: añadir createdBy que es de tipo Account 
        public DateTime InsertedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class ItemSizeMasterCreateMessage
    {
        public ItemSizeCategoryGraphQLModel CreatedItemSizeMaster { get; set; }
    }

    public class ItemSizeMasterUpdateMessage
    {
        public ItemSizeCategoryGraphQLModel UpdatedItemSizeMaster { get; set; }

    }

    public class ItemSizeMasterDeleteMessage
    {
        public ItemSizeCategoryGraphQLModel DeletedItemSizeMaster { get; set; }
    }
}
