using System.Collections.ObjectModel;

namespace Models.Global
{
    public class MenuItemGroupGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public MenuModuleGraphQLModel? MenuModule { get; set; }
        public ObservableCollection<MenuItemGraphQLModel> MenuItems { get; set; } = [];
    }
}
