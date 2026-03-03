using System.Collections.ObjectModel;

namespace Models.Global
{
    public class MenuModuleGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public ObservableCollection<MenuItemGroupGraphQLModel> MenuItemGroups { get; set; } = [];
    }
}
