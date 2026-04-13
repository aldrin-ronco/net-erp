using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NetErp.Global.AuthorizationSequence.Views
{
    public partial class AuthorizationSequenceDetailView : UserControl
    {
        public AuthorizationSequenceDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, () =>
            {
                Number.Focus();
                Keyboard.Focus(Number);
            });
        }
    }
}
