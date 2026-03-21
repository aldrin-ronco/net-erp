using NetErp.Global.MenuItem.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Global.MenuItem.Views
{
    public partial class MenuItemDetailView : UserControl
    {
        public MenuItemDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is MenuItemDetailViewModel vm)
            {
                if (vm.IsNewRecord) ItemKey.Focus();
                else Name.Focus();
            }
        }
    }
}
