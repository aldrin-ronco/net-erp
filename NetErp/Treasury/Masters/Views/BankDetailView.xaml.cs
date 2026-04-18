using System.Windows;
using System.Windows.Controls;

namespace NetErp.Treasury.Masters.Views
{
    public partial class BankDetailView : UserControl
    {
        public BankDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            Code.Focus();
        }
    }
}
