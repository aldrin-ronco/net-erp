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
            if (DataContext is not StockMovementInDetailViewModel vm) return;
            if (LinesGrid.CurrentItem is not NetErp.Inventory.StockMovementsIn.DTO.StockMovementLineDTO row) return;

            // Ctrl+Enter → abrir modal de dimensiones (si la línea es dimensionada)
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.OriginalSource is TextBox) return;
                if (!row.HasDimensions) return;
                e.Handled = true;
                _ = vm.EditLineDimensionsAsync(row);
                return;
            }

            // Delete → eliminar línea
            if (e.Key == Key.Delete)
            {
                if (e.OriginalSource is TextBox) return;
                vm.SelectedLine = row;
                e.Handled = true;
                _ = vm.RemoveLineAsync();
            }
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

        // Bloquea edición de Cantidad cuando la línea es dimensionada (la cantidad sale
        // de la suma de preselecciones — se edita via doble-click → dialog de dimensiones).
        private void LinesGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is not NetErp.Inventory.StockMovementsIn.DTO.StockMovementLineDTO row) return;
            if (!ReferenceEquals(e.Column, QuantityColumn)) return;
            if (row.HasDimensions) e.Cancel = true;
        }

        // Doble-click sobre fila dimensionada → abre dialog de edición de dimensiones.
        // Si está en edit-mode (CurrentCell.IsEditing), no interferir.
        private void LinesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TextBox) return;
            if (LinesGrid.CurrentItem is not NetErp.Inventory.StockMovementsIn.DTO.StockMovementLineDTO row) return;
            if (!row.HasDimensions) return;
            if (DataContext is not StockMovementInDetailViewModel vm) return;
            e.Handled = true;
            _ = vm.EditLineDimensionsAsync(row);
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
