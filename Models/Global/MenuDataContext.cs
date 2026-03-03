using static Models.Global.GraphQLResponseTypes;

namespace Models.Global
{
    public class MenuDataContext
    {
        public PageType<MenuModuleGraphQLModel> MenuModules { get; set; } = new();
    }
}
