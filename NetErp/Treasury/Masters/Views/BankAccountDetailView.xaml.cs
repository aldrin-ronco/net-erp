using System.Windows;
using System.Windows.Controls;
using NetErp.Treasury.Masters.ViewModels;

namespace NetErp.Treasury.Masters.Views
{
    public partial class BankAccountDetailView : UserControl
    {
        public BankAccountDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is BankAccountDetailViewModel vm && vm.IsDigitalWallet)
            {
                NumberWallet.Focus();
            }
            else
            {
                Number.Focus();
            }
        }
    }
}
