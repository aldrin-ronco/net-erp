using System.Windows;
using System.Windows.Controls;

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
            Number.Focus();
        }
    }
}
