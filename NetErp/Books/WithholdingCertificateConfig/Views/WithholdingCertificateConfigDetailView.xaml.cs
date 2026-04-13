using NetErp.Books.WithholdingCertificateConfig.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.WithholdingCertificateConfig.Views
{
    public partial class WithholdingCertificateConfigDetailView : UserControl
    {
        public WithholdingCertificateConfigDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is WithholdingCertificateConfigDetailViewModel vm)
            {
                if (vm.IsNewRecord) Name.Focus();
                else Description.Focus();
            }
        }
    }
}
