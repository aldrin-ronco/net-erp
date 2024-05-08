using System.Windows;
using System.Windows.Controls;

namespace NetErp.Billing.Sellers.Views
{
    /// <summary>
    /// Lógica de interacción para SellerDetailView.xaml
    /// </summary>
    public partial class SellerDetailView : UserControl
    {
        public SellerDetailView()
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
