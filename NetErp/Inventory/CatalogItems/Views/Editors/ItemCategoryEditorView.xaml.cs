using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NetErp.Inventory.CatalogItems.Views.Editors
{
    public partial class ItemCategoryEditorView : UserControl
    {
        public ItemCategoryEditorView()
        {
            InitializeComponent();
        }

        private void OnFormIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                Dispatcher.BeginInvoke(() => NameTextEdit.Focus(), DispatcherPriority.Render);
            }
        }
    }
}
