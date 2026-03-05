using NetErp.Books.TaxCategory.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.TaxCategory.Views
{
    public partial class TaxCategoryDetailView : UserControl
    {
        public TaxCategoryDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is TaxCategoryDetailViewModel vm)
            {
                if (vm.IsNewRecord)
                    TaxCategoryCode.Focus();
                else
                    TaxCategoryName.Focus();
            }
        }
    }
}
