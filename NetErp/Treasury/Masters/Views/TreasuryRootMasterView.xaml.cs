using DevExpress.Xpf.Grid;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Treasury.Masters.Views
{
    public partial class TreasuryRootMasterView : UserControl
    {
        public TreasuryRootMasterView()
        {
            InitializeComponent();
        }

        private void TreeViewControlView_Loaded(object sender, RoutedEventArgs e)
        {
            var control = sender as TreeViewControlView;
            control.VisibleColumns[0].Width = new GridColumnWidth(1, GridColumnUnitType.Star);
        }
    }
}
