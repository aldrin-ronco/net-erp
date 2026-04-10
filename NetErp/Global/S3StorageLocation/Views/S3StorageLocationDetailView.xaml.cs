using NetErp.Global.S3StorageLocation.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Global.S3StorageLocation.Views
{
    public partial class S3StorageLocationDetailView : UserControl
    {
        public S3StorageLocationDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            Description.Focus();
        }
    }
}
