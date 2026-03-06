using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.AccountingSources.Views
{
    public partial class AccountingSourceDetailView : UserControl
    {
        public AccountingSourceDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            ShortCode.Focus();
        }
    }
}
