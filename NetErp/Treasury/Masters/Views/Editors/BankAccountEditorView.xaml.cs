using DevExpress.Xpf.Grid.LookUp;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NetErp.Treasury.Masters.Views.Editors
{
    public partial class BankAccountEditorView : UserControl
    {
        public BankAccountEditorView()
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
    }
}
