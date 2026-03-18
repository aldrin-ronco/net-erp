using System.Windows;
using System.Windows.Controls;

namespace NetErp.Global.AwsS3Config.Views
{
    public partial class AwsS3ConfigDetailView : UserControl
    {
        public AwsS3ConfigDetailView()
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
