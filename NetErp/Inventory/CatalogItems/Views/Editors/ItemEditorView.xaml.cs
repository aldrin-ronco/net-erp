using System.Windows.Controls;
using System.Windows.Threading;

namespace NetErp.Inventory.CatalogItems.Views.Editors
{
    public partial class ItemEditorView : UserControl
    {
        public ItemEditorView()
        {
            InitializeComponent();
        }

        private void OnFormIsEnabledChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true)
            {
                Dispatcher.BeginInvoke(() => NameTextEdit.Focus(), DispatcherPriority.Render);
            }
        }
    }
}
