using NetErp.Books.IdentificationTypes.ViewModels;
using NetErp.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.IdentificationTypes.Views
{
    /// <summary>
    /// Interaction logic for IdentificationTypeMasterView.xaml
    /// </summary>
    public partial class IdentificationTypeMasterView : UserControl
    {
        public IdentificationTypeMasterView()
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
    }
}
