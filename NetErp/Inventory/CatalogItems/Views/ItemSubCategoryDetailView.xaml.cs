using NetErp.Inventory.CatalogItems.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Inventory.CatalogItems.Views
{
    public partial class ItemSubCategoryDetailView : UserControl
    {
        public ItemSubCategoryDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is ItemSubCategoryDetailViewModel)
            {
                Name.Focus();
            }
        }
    }
}
