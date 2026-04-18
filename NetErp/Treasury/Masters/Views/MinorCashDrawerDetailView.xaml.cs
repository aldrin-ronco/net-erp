using System.Windows;
using System.Windows.Controls;

namespace NetErp.Treasury.Masters.Views
{
    public partial class MinorCashDrawerDetailView : UserControl
    {
        public MinorCashDrawerDetailView()
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
