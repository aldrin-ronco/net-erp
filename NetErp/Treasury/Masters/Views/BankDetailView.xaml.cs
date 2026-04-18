using System.Windows;
using System.Windows.Controls;
using NetErp.Treasury.Masters.ViewModels;

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
            if (DataContext is BankDetailViewModel vm && !vm.IsNewRecord)
            {
                // Edit: foco en Code (único campo realmente editable sin buscar entidad).
                Code.Focus();
            }
            else
            {
                Code.Focus();
            }
        }
    }
}
