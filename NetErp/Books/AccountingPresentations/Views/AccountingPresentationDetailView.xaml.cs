using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.AccountingPresentations.Views
{
    public partial class AccountingPresentationDetailView : UserControl
    {
        public AccountingPresentationDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            PresentationName.Focus();
        }
    }
}
