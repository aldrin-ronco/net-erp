using static Models.Global.GraphQLResponseTypes;

namespace Models.Global
{
    public class MenuItemGraphQLModel
    {
        public int Id { get; set; }
        public string ItemKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsLockable { get; set; }
        public bool IsActive { get; set; } = true;
        public MenuItemGroupGraphQLModel? MenuItemGroup { get; set; }
    }

    public class MenuItemCreateMessage
    {
        public required UpsertResponseType<MenuItemGraphQLModel> CreatedMenuItem { get; set; }
    }

    public class MenuItemUpdateMessage
    {
        public required UpsertResponseType<MenuItemGraphQLModel> UpdatedMenuItem { get; set; }
    }

    public class MenuItemDeleteMessage
    {
        public required DeleteResponseType DeletedMenuItem { get; set; }
    }
}
