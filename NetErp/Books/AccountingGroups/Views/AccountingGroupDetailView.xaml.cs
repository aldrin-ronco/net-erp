using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.AccountingGroups.Views
{
    public partial class AccountingGroupDetailView : UserControl
    {
        public AccountingGroupDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            Name.Focus();
        }
    }
}
