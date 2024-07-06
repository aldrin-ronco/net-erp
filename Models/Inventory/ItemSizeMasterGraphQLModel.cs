using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class ItemSizeMasterGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public IEnumerable<ItemSizeDetailGraphQLModel> Sizes { get; set; }
    }

    public class ItemSizeMasterCreateMessage
    {
        public ItemSizeMasterGraphQLModel CreatedItemSizeMaster { get; set; }
    }

    public class ItemSizeMasterUpdateMessage
    {
        public ItemSizeMasterGraphQLModel UpdatedItemSizeMaster { get; set; }

    }

    public class ItemSizeMasterDeleteMessage
    {
        public ItemSizeMasterGraphQLModel DeletedItemSizeMaster { get; set; }
    }
}
