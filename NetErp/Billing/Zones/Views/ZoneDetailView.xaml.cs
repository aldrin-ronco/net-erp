using System.Windows;
using System.Windows.Controls;

namespace NetErp.Billing.Zones.Views
{
    public partial class ZoneDetailView : UserControl
    {
        public ZoneDetailView()
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
