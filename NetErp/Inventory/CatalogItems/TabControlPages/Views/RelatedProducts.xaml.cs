using DevExpress.Xpf.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetErp.Inventory.CatalogItems.TabControlPages.Views
{
    /// <summary>
    /// Lógica de interacción para RelatedProducts.xaml
    /// </summary>
    public partial class RelatedProducts : UserControl
    {
        public RelatedProducts()
        {
            InitializeComponent();
        }

        private void RelatedProductName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(RelatedProductName.Text)) 
            {
                RelatedProductQuantity.IsEnabled = true;
                RelatedProductQuantity.Focusable = true;

                _ = Dispatcher.BeginInvoke(new Action(() =>
                {
                    RelatedProductQuantity.Focus();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }
    }
}
