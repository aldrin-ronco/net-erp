using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.LookUp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetErp.Treasury.Masters.Views
{
    /// <summary>
    /// Lógica de interacción para TreasuryRootMasterView.xaml
    /// </summary>
    public partial class TreasuryRootMasterView : UserControl
    {
        public TreasuryRootMasterView()
        {
            InitializeComponent();
        }

        private void LookUpEdit_GotFocus(object sender, RoutedEventArgs e)
        {
            var lookUpEdit = sender as LookUpEdit;
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                lookUpEdit?.SelectAll();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void LookUpEdit_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var lookUpEdit = sender as LookUpEdit;
            if (lookUpEdit != null && lookUpEdit.IsKeyboardFocusWithin)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    lookUpEdit.SelectAll();
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void TreeViewControlView_Loaded(object sender, RoutedEventArgs e)
        {
            var control = sender as TreeViewControlView;
            control.VisibleColumns[0].Width = new GridColumnWidth(1, GridColumnUnitType.Star);
        }
    }
}
