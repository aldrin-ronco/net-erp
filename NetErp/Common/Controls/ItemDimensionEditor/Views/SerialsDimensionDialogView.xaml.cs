using NetErp.UserControls.ItemDimensionEditor.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace NetErp.UserControls.ItemDimensionEditor.Views
{
    public partial class SerialsDimensionDialogView : UserControl
    {
        public SerialsDimensionDialogView()
        {
            InitializeComponent();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            if (DataContext is SerialsDimensionDialogViewModel vm) _ = vm.CancelAsync();
        }
    }
}
