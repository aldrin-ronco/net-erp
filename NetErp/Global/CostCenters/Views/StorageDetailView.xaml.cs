using NetErp.Global.CostCenters.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Global.CostCenters.Views
{
    public partial class StorageDetailView : UserControl
    {
        public StorageDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is StorageDetailViewModel)
            {
                Name.Focus();
            }
        }
    }
}
