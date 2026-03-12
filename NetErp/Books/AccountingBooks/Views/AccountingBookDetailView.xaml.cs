using System.Windows.Controls;

namespace NetErp.Books.AccountingBooks.Views
{
    public partial class AccountingBookDetailView : UserControl
    {
        public AccountingBookDetailView()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (AccountingBookName != null) AccountingBookName.Focus();
            };
        }
    }
}
