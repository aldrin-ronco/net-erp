using NetErp.Inventory.CatalogItems.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Inventory.CatalogItems.Views
{
    public partial class ItemDetailContentView : UserControl
    {
        public ItemDetailContentView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            // Focus inicial solo aplica en modo modal (nuevo item).
            // En modo panel (read-only por defecto) no tiene sentido robarle el foco al árbol.
            if (DataContext is ItemDetailViewModel vm && vm.IsModal && vm.IsEditing)
            {
                NameEdit.Focus();
            }
        }
    }
}
