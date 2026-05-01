using DevExpress.Xpf.Editors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NetErp.UserControls.ItemDimensionEditor.Views
{
    public partial class SizesDimensionDialogView : UserControl
    {
        public SizesDimensionDialogView()
        {
            InitializeComponent();
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
            int idx = SizesGrid.ItemContainerGenerator.IndexFromContainer(row);
            int next = e.Key == Key.Up ? idx - 1 : idx + 1;
            e.Handled = true;
            if (next < 0 || next >= SizesGrid.Items.Count) return;
            FocusQuantityAt(next);
        }

        private void FocusQuantityAt(int index)
        {
            if (index < 0 || index >= SizesGrid.Items.Count) return;
            SizesGrid.ScrollIntoView(SizesGrid.Items[index]);
            SizesGrid.UpdateLayout();
            DataGridRow? row = SizesGrid.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
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
