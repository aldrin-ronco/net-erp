using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NetErp.Inventory.CatalogItems.Views.Editors
{
    public partial class ItemSubCategoryEditorView : UserControl
    {
        public ItemSubCategoryEditorView()
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
