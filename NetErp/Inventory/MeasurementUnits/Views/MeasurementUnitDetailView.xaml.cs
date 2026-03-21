using NetErp.Inventory.MeasurementUnits.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Inventory.MeasurementUnits.Views
{
    public partial class MeasurementUnitDetailView : UserControl
    {
        public MeasurementUnitDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is MeasurementUnitDetailViewModel vm)
            {
                if (vm.IsNewRecord) Name.Focus();
                else Name.Focus();
            }
        }
    }
}
