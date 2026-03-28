using DevExpress.Xpf.Grid;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Global.AccessProfile.Views
{
    public partial class AccessProfileView : UserControl
    {
        public AccessProfileView()
        {
            InitializeComponent();
            MenuTreeList.ItemsSourceChanged += OnItemsSourceChanged;
        }

        private void OnItemsSourceChanged(object sender, ItemsSourceChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(() => MenuTreeView.ExpandAllNodes(),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
}
