using System.Windows;
using System.Windows.Controls;

namespace NetErp.Global.AccessProfile.Views
{
    public partial class AccessProfileDetailView : UserControl
    {
        public AccessProfileDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            ProfileName.Focus();
        }
    }
}
