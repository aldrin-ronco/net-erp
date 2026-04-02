using NetErp.Books.AccountingBooks.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.AccountingBooks.Views
{
    public partial class AccountingBookDetailView : UserControl
    {
        public AccountingBookDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            AccountingBookName.Focus();
        }
    }
}
