using System.Windows.Controls;

namespace NetErp.Books.AccountingPresentations.Views
{
    public partial class AccountingPresentationDetailView : UserControl
    {
        public AccountingPresentationDetailView()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (PresentationName != null) PresentationName.Focus();
            };
        }
    }
}
