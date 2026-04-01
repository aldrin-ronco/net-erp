using DevExpress.Xpf.Grid;
using System.Windows.Controls;

namespace NetErp.Global.UserPermission.Views
{
    public partial class BatchUserPermissionView : UserControl
    {
        public BatchUserPermissionView()
        {
            InitializeComponent();
            PermissionTreeList.ItemsSourceChanged += OnItemsSourceChanged;
        }

        private void OnItemsSourceChanged(object sender, ItemsSourceChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(() => PermissionTreeView.ExpandAllNodes(),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
}
