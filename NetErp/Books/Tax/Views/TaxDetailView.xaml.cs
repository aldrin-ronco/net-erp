using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.Tax.Views
{
    public partial class TaxDetailView : UserControl
    {
        public TaxDetailView()
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
