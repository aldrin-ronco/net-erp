using DevExpress.Xpf.Grid;
using System.Windows.Controls;

namespace NetErp.Global.CompanyPermissionDefault.Views
{
    public partial class CompanyPermissionDefaultView : UserControl
    {
        public CompanyPermissionDefaultView()
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
