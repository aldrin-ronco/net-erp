using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class ItemSizeValueGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ItemSizeCategoryGraphQLModel ItemSizeCategory { get; set; } = new();
        public int DisplayOrder {  get; set; }
        public CompanyGraphQLModel Company { get; set; } = new();
        public DateTime InsertedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class ItemSizeDetailCreateMessage
    {
        public ItemSizeValueGraphQLModel CreatedItemSizeDetail { get; set; }
    }

    public class ItemSizeDetailUpdateMessage
    {
        public ItemSizeValueGraphQLModel UpdatedItemSizeDetail { get; set; }

    }

    public class ItemSizeDetailDeleteMessage
    {
        public ItemSizeValueGraphQLModel DeletedItemSizeDetail { get; set; }
    }
}
