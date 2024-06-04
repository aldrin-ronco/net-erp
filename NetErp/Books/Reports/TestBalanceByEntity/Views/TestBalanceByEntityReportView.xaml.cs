using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.Reports.TestBalanceByEntity.Views
{
    /// <summary>
    /// Lógica de interacción para TestBalanceByEntityReportView.xaml
    /// </summary>
    public partial class TestBalanceByEntityReportView : UserControl
    {
        public TestBalanceByEntityReportView()
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
