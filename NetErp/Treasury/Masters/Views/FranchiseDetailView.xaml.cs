using System.Windows;
using System.Windows.Controls;

namespace NetErp.Treasury.Masters.Views
{
    public partial class FranchiseDetailView : UserControl
    {
        public FranchiseDetailView()
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
