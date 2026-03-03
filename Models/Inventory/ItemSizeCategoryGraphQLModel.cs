using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Inventory
{
    public class ItemSizeCategoryGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public IEnumerable<ItemSizeValueGraphQLModel> ItemSizeValues { get; set; } = [];
        public CompanyGraphQLModel Company { get; set; } = new();
        public SystemAccountGraphQLModel CreatedBy { get; set; } = new();
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ItemSizeCategoryCreateMessage
    {
        public UpsertResponseType<ItemSizeCategoryGraphQLModel> CreatedItemSizeCategory { get; set; } = new();
    }

    public class ItemSizeCategoryUpdateMessage
    {
        public UpsertResponseType<ItemSizeCategoryGraphQLModel> UpdatedItemSizeCategory { get; set; } = new();
    }

    public class ItemSizeCategoryDeleteMessage
    {
        public DeleteResponseType DeletedItemSizeCategory { get; set; } = new();
    }
}
