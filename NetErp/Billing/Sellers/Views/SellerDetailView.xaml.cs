using NetErp.Billing.Sellers.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Billing.Sellers.Views
{
    public partial class SellerDetailView : UserControl
    {
        public SellerDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is SellerDetailViewModel vm)
            {
                if (vm.IsNewRecord)
                    IdentificationNumber.Focus();
                else
                    FirstName.Focus();
            }
        }
    }
}
