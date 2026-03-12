using NetErp.Books.IdentificationTypes.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.IdentificationTypes.Views
{
    public partial class IdentificationTypeDetailView : UserControl
    {
        public IdentificationTypeDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is IdentificationTypeDetailViewModel vm)
            {
                if (vm.IsNewRecord)
                    IdentificationTypeCode.Focus();
                else
                    IdentificationTypeName.Focus();
            }
        }
    }
}
