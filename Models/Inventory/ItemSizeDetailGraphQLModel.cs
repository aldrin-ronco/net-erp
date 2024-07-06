using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class ItemSizeDetailGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ItemSizeMasterId { get; set; }
        public int PresentationOrder {  get; set; }
    }

    public class ItemSizeDetailCreateMessage
    {
        public ItemSizeDetailGraphQLModel CreatedItemSizeDetail { get; set; }
    }

    public class ItemSizeDetailUpdateMessage
    {
        public ItemSizeDetailGraphQLModel UpdatedItemSizeDetail { get; set; }

    }

    public class ItemSizeDetailDeleteMessage
    {
        public ItemSizeDetailGraphQLModel DeletedItemSizeDetail { get; set; }
    }
}
