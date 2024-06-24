using DotNetKit.Windows.Controls;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.AccountingEntries.Views
{
    /// <summary>
    /// Interaction logic for AccountingEntriesDetailView.xaml
    /// </summary>
    public partial class AccountingEntriesDetailView : UserControl
    {
        public AccountingEntriesDetailView()
        {
            InitializeComponent();
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness();
            }
        }

        private void SetFocusOnAutoCompleteComboBox(object sender, RoutedEventArgs e)
        {
            AutoCompleteComboBox.Focus();
        }
    }
}
