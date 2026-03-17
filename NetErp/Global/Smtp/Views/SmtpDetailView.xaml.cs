using System.Windows;
using System.Windows.Controls;

namespace NetErp.Global.Smtp.Views
{
    public partial class SmtpDetailView : UserControl
    {
        public SmtpDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            SmtpName.Focus();
        }
    }
}
