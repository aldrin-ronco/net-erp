using NetErp.Inventory.CatalogItems.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Inventory.CatalogItems.Views
{
    public partial class CatalogDetailView : UserControl
    {
        public CatalogDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is CatalogDetailViewModel)
            {
                Name.Focus();
            }
        }
    }
}
