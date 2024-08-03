using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NetErp.Books.IdentificationTypes.Views
{
    /// <summary>
    /// Interaction logic for IdentificationTypeDetailView.xaml
    /// </summary>
    public partial class IdentificationTypeDetailView : UserControl
    {
        public IdentificationTypeDetailView()
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

        private void txtCode_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(e.Text, "[^0-9]+"))
            {
                e.Handled = true;
            }
        }
    }
}
