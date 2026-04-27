using NetErp.Inventory.StockMovementsIn.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NetErp.Inventory.StockMovementsIn.Views
{
    public partial class StockMovementInDetailView : UserControl
    {
        public StockMovementInDetailView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            _ = Dispatcher.BeginInvoke(new System.Action(() =>
            {
                if (DataContext is StockMovementInDetailViewModel vm)
                    vm.Editor?.FocusSearch();
            }), DispatcherPriority.ContextIdle);
        }

        private void LinesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete) return;
            if (e.OriginalSource is TextBox) return;
            if (DataContext is not StockMovementInDetailViewModel vm) return;
            // SelectionUnit=CellOrRowHeader: clicar celda no setea SelectedItem.
            // Tomar la fila desde CurrentItem (fila de la celda enfocada).
            if (LinesGrid.CurrentItem is not NetErp.Inventory.StockMovementsIn.DTO.StockMovementLineDTO row) return;
            vm.SelectedLine = row;
            e.Handled = true;
            _ = vm.RemoveLineAsync();
        }

        // CellEditingTemplate con CurrencyTextBox: el TextBox interno NO recibe foco
        // automático cuando WPF DataGrid entra a edit (F2/click). Buscamos el TextBox
        // descendiente, le damos foco y seleccionamos todo.
        private void LinesGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (e.EditingElement is not FrameworkElement editingElement) return;
            _ = Dispatcher.BeginInvoke(new System.Action(() =>
            {
                TextBox? tb = FindDescendant<TextBox>(editingElement);
                if (tb != null)
                {
                    tb.Focus();
                    tb.SelectAll();
                }
            }), DispatcherPriority.Input);
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) return match;
                T? result = FindDescendant<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
