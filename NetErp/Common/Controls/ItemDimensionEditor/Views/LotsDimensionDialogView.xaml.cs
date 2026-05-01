using DevExpress.Xpf.Editors;
using NetErp.UserControls.ItemDimensionEditor.ViewModels;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NetErp.UserControls.ItemDimensionEditor.Views
{
    public partial class LotsDimensionDialogView : UserControl
    {
        private bool _isLoaded;
        private LotsDimensionDialogViewModel? _vm;

        public LotsDimensionDialogView()
        {
            InitializeComponent();
            Unloaded += OnUnloaded;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            if (_vm == null) return;

            // 1. Si hay fila incompleta (lote vacío o cantidad <= 0), removerla y enfocar la previa.
            LotEntryRow? incomplete = _vm.Rows.FirstOrDefault(r =>
                string.IsNullOrWhiteSpace(r.LotNumber) || r.Quantity <= 0);
            if (incomplete != null)
            {
                int idx = _vm.Rows.IndexOf(incomplete);
                _vm.RemoveRow(incomplete);

                if (_vm.Rows.Count == 0)
                {
                    _ = _vm.TryCloseAsync(false);
                    return;
                }

                int focusIdx = idx > 0 ? idx - 1 : 0;
                Dispatcher.BeginInvoke(new System.Action(() => FocusEditorAt(focusIdx)), DispatcherPriority.Loaded);
                return;
            }

            // 2. Sin filas incompletas → flujo normal de cancelación con prompt si dirty.
            _ = _vm.CancelAsync();
        }

        private void OnGridLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            if (DataContext is LotsDimensionDialogViewModel vm)
            {
                _vm = vm;
                vm.Rows.CollectionChanged += OnRowsChanged;
                vm.RequestFocusRow += OnRequestFocusRow;
            }
            Dispatcher.BeginInvoke(new System.Action(() => FocusEditorAt(0)), DispatcherPriority.Loaded);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_vm != null)
            {
                _vm.Rows.CollectionChanged -= OnRowsChanged;
                _vm.RequestFocusRow -= OnRequestFocusRow;
            }
            _vm = null;
            _isLoaded = false;
        }

        private void OnRequestFocusRow(int idx)
        {
            Dispatcher.BeginInvoke(new System.Action(() => FocusEditorAt(idx)), DispatcherPriority.Loaded);
        }

        private void OnGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab && e.Key != Key.Enter) return;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            DependencyObject? focused = Keyboard.FocusedElement as DependencyObject;
            TextEdit? te = FindAncestor<TextEdit>(focused);

            // Cantidad de última fila → AddRow (Tab/Enter sin Shift)
            if (!shift && te != null && te.Tag as string == "Quantity" && _vm != null
                && te.DataContext is LotEntryRow row
                && _vm.Rows.IndexOf(row) == _vm.Rows.Count - 1)
            {
                e.Handled = true;
                _vm.AddRow();
                return;
            }

            // Enter → simular Tab (avanzar/retroceder foco)
            if (e.Key == Key.Enter && Keyboard.FocusedElement is UIElement el)
            {
                e.Handled = true;
                FocusNavigationDirection dir = shift ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next;
                el.MoveFocus(new TraversalRequest(dir));
            }
        }

        private static T? FindAncestor<T>(DependencyObject? d) where T : DependencyObject
        {
            while (d != null && d is not T) d = VisualTreeHelper.GetParent(d) ?? LogicalTreeHelper.GetParent(d);
            return d as T;
        }

        private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (e.Action != NotifyCollectionChangedAction.Add) return;
            if (e.NewStartingIndex < 0) return;
            int idx = e.NewStartingIndex;
            Dispatcher.BeginInvoke(new System.Action(() => FocusEditorAt(idx)), DispatcherPriority.Loaded);
        }

        private void FocusEditorAt(int index)
        {
            if (index < 0 || index >= LotsGrid.Items.Count) return;
            LotsGrid.ScrollIntoView(LotsGrid.Items[index]);
            LotsGrid.UpdateLayout();
            DataGridRow? row = LotsGrid.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
            if (row == null) return;
            TextEdit? editor = FindDescendant<TextEdit>(row);
            if (editor == null) return;
            editor.Focus();
            editor.SelectAll();
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) return match;
                T? deep = FindDescendant<T>(child);
                if (deep != null) return deep;
            }
            return null;
        }
    }
}
