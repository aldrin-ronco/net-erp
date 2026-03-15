using NetErp.Billing.Customers.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Billing.Customers.Views
{
    public partial class CustomerDetailView : UserControl
    {
        public CustomerDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is CustomerDetailViewModel vm)
            {
                if (vm.IsNewRecord)
                    IdentificationNumber.Focus();
                else if (vm.CaptureInfoAsPN)
                    FirstName.Focus();
                else
                    BusinessName.Focus();
            }
        }
    }
}
