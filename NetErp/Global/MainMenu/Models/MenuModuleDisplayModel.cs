using System.Collections.ObjectModel;
using System.Windows.Input;

namespace NetErp.Global.MainMenu.Models
{
    public class MenuModuleDisplayModel
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public ObservableCollection<MenuItemDisplayModel> Items { get; set; } = [];
    }

    public class MenuItemDisplayModel
    {
        public string Name { get; set; } = string.Empty;
        public string ItemKey { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsSeparator { get; set; }
        public ICommand? Command { get; set; }
    }
}
