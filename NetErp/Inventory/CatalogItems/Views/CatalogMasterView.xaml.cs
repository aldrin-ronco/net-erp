using Caliburn.Micro;
using Common.Extensions;
using Models.Inventory;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace NetErp.Inventory.CatalogItems.Views
{
    /// <summary>
    /// Lógica de interacción para CatalogMasterView.xaml
    /// </summary>
    public partial class CatalogMasterView : UserControl /*IHandle<CatalogItemFocusMessage>*/
    {
        public CatalogMasterView()
        {
            InitializeComponent();
        }
    }
}
