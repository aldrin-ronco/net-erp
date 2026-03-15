using NetErp.Books.AccountingEntities.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.AccountingEntities.Views
{
    public partial class AccountingEntityDetailView : UserControl
    {
        public AccountingEntityDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is AccountingEntityDetailViewModel vm)
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
