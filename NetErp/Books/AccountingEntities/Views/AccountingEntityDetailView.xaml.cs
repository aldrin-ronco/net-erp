using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.AccountingEntities.Views
{
    /// <summary>
    /// Interaction logic for AccountingEntityDetailView.xaml
    /// </summary>
    public partial class AccountingEntityDetailView : UserControl
    {
        public AccountingEntityDetailView()
        {
            InitializeComponent();
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness();
            }
        }
    }
}
