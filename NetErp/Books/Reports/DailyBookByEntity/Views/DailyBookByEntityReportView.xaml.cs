using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.Reports.DailyBook.Views
{
    /// <summary>
    /// Lógica de interacción para DailyBookByEntityReportView.xaml
    /// </summary>
    public partial class DailyBookByEntityReportView : UserControl
    {
        public DailyBookByEntityReportView()
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
