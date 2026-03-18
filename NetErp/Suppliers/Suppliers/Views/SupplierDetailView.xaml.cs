using NetErp.Suppliers.Suppliers.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Suppliers.Suppliers.Views
{
    public partial class SupplierDetailView : UserControl
    {
        public SupplierDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is SupplierDetailViewModel vm)
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
