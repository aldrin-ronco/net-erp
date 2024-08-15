using System.Windows;
using System.Windows.Controls;

namespace NetErp.Billing.DocumentSequence.Views
{
    /// <summary>
    /// Lógica de interacción para DocumentSequenceDetailView.xaml
    /// </summary>
    public partial class DocumentSequenceDetailView : UserControl
    {
        public DocumentSequenceDetailView()
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
