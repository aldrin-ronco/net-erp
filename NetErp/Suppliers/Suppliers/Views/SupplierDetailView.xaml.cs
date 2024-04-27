using System.Windows;
using System.Windows.Controls;

namespace NetErp.Suppliers.Suppliers.Views
{
    /// <summary>
    /// Lógica de interacción para SupplierDetailView.xaml
    /// </summary>
    public partial class SupplierDetailView : UserControl
    {
        public SupplierDetailView()
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
