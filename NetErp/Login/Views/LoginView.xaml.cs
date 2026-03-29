using System.Windows;
using System.Windows.Controls;

namespace NetErp.Login.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            EmailBox.Focus();
        }
    }
}
