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
            TreeViewControlView? control = sender as TreeViewControlView;
            if (control is null) return;
            control.VisibleColumns[0].Width = new GridColumnWidth(1, GridColumnUnitType.Star);
            TreasuryTree.Focus();
        }
    }
}
