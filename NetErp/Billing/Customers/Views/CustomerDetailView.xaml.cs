using System.Windows;
using System.Windows.Controls;

namespace NetErp.Billing.Customers.Views
{
    /// <summary>
    /// Lógica de interacción para CustomerDetailView.xaml
    /// </summary>
    public partial class CustomerDetailView : UserControl
    {
        public CustomerDetailView()
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
