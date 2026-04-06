using System.Windows;
using System.Windows.Controls;

namespace NetErp.Global.DianCertificate.Views
{
    public partial class DianCertificateDetailView : UserControl
    {
        public DianCertificateDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            BrowseButton.Focus();
        }
    }
}
