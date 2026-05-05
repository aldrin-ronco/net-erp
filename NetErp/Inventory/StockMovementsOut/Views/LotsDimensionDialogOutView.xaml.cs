using DevExpress.Xpf.Editors;
using NetErp.Inventory.StockMovementsOut.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NetErp.Inventory.StockMovementsOut.Views
{
    public partial class LotsDimensionDialogOutView : UserControl
    {
        private LotsDimensionDialogOutViewModel? _wiredVm;

        public LotsDimensionDialogOutView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_wiredVm != null) _wiredVm.RequestFocusGrid -= HandleFocusGrid;
            _wiredVm = e.NewValue as LotsDimensionDialogOutViewModel;
            if (_wiredVm != null) _wiredVm.RequestFocusGrid += HandleFocusGrid;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_wiredVm != null) _wiredVm.RequestFocusGrid -= HandleFocusGrid;
            _wiredVm = null;
        }

        private void HandleFocusGrid()
        {
            if (LotsGrid.Items.Count == 0) return;
            FocusQuantityAt(0);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            if (DataContext is LotsDimensionDialogOutViewModel vm)
                _ = vm.CancelAsync();
        }

        private void OnGridLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Action(() => FocusQuantityAt(0)), DispatcherPriority.Loaded);
        }

        private void OnGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Up && e.Key != Key.Down && e.Key != Key.Enter) return;

            DependencyObject? focused = Keyboard.FocusedElement as DependencyObject;
            DataGridRow? row = FindAncestor<DataGridRow>(focused);
            if (row == null) return;
            int idx = LotsGrid.ItemContainerGenerator.IndexFromContainer(row);
            int next = e.Key == Key.Up ? idx - 1 : idx + 1;
            e.Handled = true;
            if (next < 0 || next >= LotsGrid.Items.Count) return;
            FocusQuantityAt(next);
        }

        private void FocusQuantityAt(int index)
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

        private static T? FindAncestor<T>(DependencyObject? d) where T : DependencyObject
        {
            while (d != null && d is not T) d = VisualTreeHelper.GetParent(d);
            return d as T;
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
