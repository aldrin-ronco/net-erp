using NetErp.Global.Email.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Global.Email.Views
{
    public partial class EmailDetailView : UserControl
    {
        public EmailDetailView()
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
