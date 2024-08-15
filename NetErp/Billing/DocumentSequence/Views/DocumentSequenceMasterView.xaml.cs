using System.Windows;
using System.Windows.Controls;

namespace NetErp.Billing.DocumentSequence.Views
{
    /// <summary>
    /// Lógica de interacción para DocumentSequenceMasterView.xaml
    /// </summary>
    public partial class DocumentSequenceMasterView : UserControl
    {
        public DocumentSequenceMasterView()
        {
            InitializeComponent();
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            if (toolBar.Template.FindName("OverflowGrid", toolBar) is FrameworkElement overflowGrid)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
        }
    }
}
