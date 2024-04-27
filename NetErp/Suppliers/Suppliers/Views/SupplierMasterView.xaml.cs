using System.Windows;
using System.Windows.Controls;

namespace NetErp.Suppliers.Suppliers.Views
{
    /// <summary>
    /// Lógica de interacción para SupplierMasterView.xaml
    /// </summary>
    public partial class SupplierMasterView : UserControl
    {
        public SupplierMasterView()
        {
            InitializeComponent();
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            if (toolBar.Template.FindName("OverflowGrid", toolBar) is FrameworkElement overflowGrid)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
        }
    }
}
