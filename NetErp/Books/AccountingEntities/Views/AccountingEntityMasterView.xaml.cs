using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using Models.Books;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.AccountingEntities.Views
{
    /// <summary>
    /// Interaction logic for AccountingEntityMasterView.xaml
    /// </summary>
    public partial class AccountingEntityMasterView : UserControl
    {
        public AccountingEntityMasterView()
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

        private void BooleanPropCheckEdit_Checked(object sender, RoutedEventArgs e)
        {
            
        }
        private void BooleanPropCheckEdit_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void TableView_CurrentPageIndexChanged(object sender, DevExpress.Xpf.Editors.DataPager.DataPagerPageIndexChangedEventArgs e)
        {

        }
    }
}
