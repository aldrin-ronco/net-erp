using Models.Global;
using static Models.Global.GraphQLResponseTypes;

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

    public class ItemSizeValueCreateMessage
    {
        public UpsertResponseType<ItemSizeValueGraphQLModel> CreatedItemSizeValue { get; set; } = new();
    }

    public class ItemSizeValueUpdateMessage
    {
        public UpsertResponseType<ItemSizeValueGraphQLModel> UpdatedItemSizeValue { get; set; } = new();
    }

    public class ItemSizeValueDeleteMessage
    {
        public DeleteResponseType DeletedItemSizeValue { get; set; } = new();
    }
}
