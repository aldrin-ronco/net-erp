using DevExpress.Xpf.Core;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.Reports.EntityVsAccount.Views
{
    /// <summary>
    /// Lógica de interacción para EntityVsAccountReportView.xaml
    /// </summary>
    public partial class EntityVsAccountReportView : UserControl
    {
        public EntityVsAccountReportView()
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
