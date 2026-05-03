using Caliburn.Micro;
using DevExpress.Xpf.Grid;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace NetErp.Helpers.Shortcuts
{
    public static class GridFocusHelper
    {
        public static bool FocusFirstRow(this IViewAware screen, string gridName)
        {
            if (screen.GetView() is not UserControl view) return false;
            FrameworkElement? element = FindChildByName(view, gridName);
            return element switch
            {
                GridControl dx => FocusFirstRowDx(dx),
                DataGrid dg => FocusFirstRowWpf(dg),
                FrameworkElement fe => fe.Focus(),
                _ => false
            };
        }

        private static bool FocusFirstRowWpf(DataGrid dg)
        {
            if (dg.Items.Count == 0) return false;
            object first = dg.Items[0];
            DataGridColumn col = dg.Columns.FirstOrDefault(c => c.Visibility == Visibility.Visible) ?? dg.Columns[0];
            dg.UpdateLayout();
            dg.ScrollIntoView(first, col);
            dg.SelectedIndex = 0;
            dg.CurrentCell = new DataGridCellInfo(first, col);
            dg.UpdateLayout();
            DataGridCell? cell = GetCell(dg, 0, dg.Columns.IndexOf(col));
            if (cell != null)
            {
                cell.Focus();
                Keyboard.Focus(cell);
                return true;
            }
            if (dg.ItemContainerGenerator.ContainerFromIndex(0) is DataGridRow row)
            {
                row.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
                return true;
            }
            return dg.Focus();
        }

        private static DataGridCell? GetCell(DataGrid dg, int rowIndex, int columnIndex)
        {
            if (dg.ItemContainerGenerator.ContainerFromIndex(rowIndex) is not DataGridRow row) return null;
            DataGridCellsPresenter? presenter = FindVisualChild<DataGridCellsPresenter>(row);
            return presenter?.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match) return match;
                T? found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        private static bool FocusFirstRowDx(GridControl grid)
        {
            if (grid.VisibleRowCount == 0) return false;
            grid.View?.MoveFirstRow();
            grid.CurrentItem = grid.GetRow(0);
            grid.Focus();
            if (grid.View is TableView tv)
            {
                Keyboard.Focus(tv);
                return true;
            }
            return true;
        }

        private static FrameworkElement? FindChildByName(DependencyObject parent, string name)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement fe && fe.Name == name) return fe;
                FrameworkElement? found = FindChildByName(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
