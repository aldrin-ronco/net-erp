using Caliburn.Micro;
using Common.Extensions;
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

        //private readonly IEventAggregator _eventAggregator;
        public CatalogMasterView()
        {
            InitializeComponent();
            //_eventAggregator = IoC.Get<IEventAggregator>();
            //_eventAggregator.SubscribeOnUIThread(this);
        }

        //private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        //{
        //    if(DataContext is CatalogMasterViewModel)
        //    {

        //        if(CatalogTreeView.SelectedItem != null)
        //        {
             
        //            TreeViewItem selectedItemContainer = (TreeViewItem)CatalogTreeView.ItemContainerGenerator.ContainerFromItem(CatalogTreeView.SelectedItem);

        //            if(selectedItemContainer != null)
        //            {
        //                selectedItemContainer.IsSelected = true;
        //                selectedItemContainer.Focus();
        //            }
        //        }
        //    }
        //}


        //private void SelectNewItemAfterAddition(TreeView treeView, object newItem)
        //{
        //    // Usamos el Dispatcher para asegurarnos de que el nuevo ítem esté renderizado antes de seleccionarlo
        //    _ = treeView.Dispatcher.InvokeAsync(() =>
        //    {
        //        SelectedItemFocus(treeView, newItem);
        //    }, System.Windows.Threading.DispatcherPriority.Background); // Ejecutar después de que la UI haya actualizado los elementos visuales
        //}
        //private void SelectedItemFocus(ItemsControl parent, object itemToSelect)
        //{

        //    foreach (var item in parent.Items)
        //    {
        //        var container = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

        //        if (container != null) 
        //        {
        //            if (item == itemToSelect)
        //            {
        //                container.IsSelected = true;
        //                container.Focus();
        //                container.BringIntoView();
        //                break;
        //            }
        //            if (container.Items.Count > 0)
        //            {
        //                container.UpdateLayout();
        //                SelectedItemFocus(container, itemToSelect);
        //            }
        //        }
        //    }
        //}

        //public Task HandleAsync(CatalogItemFocusMessage message, CancellationToken cancellationToken)
        //{
        //    SelectNewItemAfterAddition(CatalogTreeView,message.CatalogItem);
        //    return Task.CompletedTask;
        //}
    }
}
