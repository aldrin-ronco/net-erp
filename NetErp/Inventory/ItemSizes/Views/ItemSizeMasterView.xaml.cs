using NetErp.Helpers;
using NetErp.Inventory.ItemSizes.DTO;
using NetErp.Inventory.ItemSizes.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit.Primitives;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace NetErp.Inventory.ItemSizes.Views
{
    /// <summary>
    /// Lógica de interacción para ItemSizeMasterView.xaml
    /// </summary>
    public partial class ItemSizeMasterView : System.Windows.Controls.UserControl
    {
        public ItemSizeMasterView()
        {
            InitializeComponent();
        }

        private void TreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is ItemSizeMasterViewModel context)
            {
                if (e.OldValue is null) return;

                if (e.OldValue is ItemSizeValueDTO)
                {
                    ((ItemSizeValueDTO)e.OldValue).IsEditing = false;
                    if(e.OldValue is ItemSizeValueDTO item && item.Id == 0)
                    {
                        ItemSizeCategoryDTO parent = context.ItemSizesMaster.FirstOrDefault(x => x.Id == item.ItemSizeCategoryId);
                        parent.ItemSizeValues.Remove(item);
                        if (parent.ItemSizeValues.Count == 0) parent.IsExpanded = false;
                    }
                }

                if (e.OldValue is ItemSizeCategoryDTO)
                {
                    ((ItemSizeCategoryDTO)e.OldValue).IsEditing = false;
                    if(((ItemSizeCategoryDTO)e.OldValue).Id == 0)
                    {
                        context.ItemSizesMaster.Remove(((ItemSizeCategoryDTO)e.OldValue));
                    }
                }
            }
        }

        private void CreateItemSize(object sender, RoutedEventArgs e)
        {
            if(DataContext is ItemSizeMasterViewModel context)
            {
                if(SizesTreeView.SelectedItem is ItemSizeValueDTO selectedNode)
                {
                    context.IsUpdate = false;
                    ItemSizeCategoryDTO parent = context.ItemSizesMaster.FirstOrDefault(x => x.Id == selectedNode.ItemSizeCategoryId);
                    var newNode = new ItemSizeValueDTO { Name = "NUEVO TALLAJE", IsEditing = true, Id = 0 ,ItemSizeCategoryId = parent.Id};
                    context.ItemSizesMaster.Where(x => x.Id == selectedNode.ItemSizeCategoryId).First().ItemSizeValues.Add(newNode);
                    TreeViewItem itemContainer = SizesTreeView.ItemContainerGenerator.ContainerFromItem(parent) as TreeViewItem;
                    if (itemContainer.ItemContainerGenerator.ContainerFromItem(newNode) is TreeViewItem newContainer)
                    {
                        newContainer.Focus();
                        //_ = context.SetFocus(nameof(context.TextBoxName));
                    }
                    else
                    {
                        itemContainer.UpdateLayout();
                        newContainer = itemContainer.ItemContainerGenerator.ContainerFromItem(newNode) as TreeViewItem;
                        if (newContainer != null)
                        {
                            newContainer.Focus();
                            //_ = context.SetFocus(nameof(context.TextBoxName));
                        }
                    }
                    context.SelectedItem = (ItemSizeType)newNode;
                    context.TextBoxName = ((ItemSizeValueDTO)context.SelectedItem).Name;
                    ((ItemSizeValueDTO)context.SelectedItem).IsEditing = true;
                }
                else
                {
                    context.IsUpdate = false;
                    var newParent = new ItemSizeCategoryDTO { Name = "NUEVO GRUPO DE TALLAJE", IsEditing = true, Id = 0};
                    context.ItemSizesMaster.Add(newParent);
                    TreeViewItem parentContainer = SizesTreeView.ItemContainerGenerator.ContainerFromItem(newParent) as TreeViewItem;
                    if(parentContainer != null)
                    {
                        parentContainer.Focus();
                    }
                    else
                    {
                        SizesTreeView.UpdateLayout();
                        parentContainer = SizesTreeView.ItemContainerGenerator.ContainerFromItem(newParent) as TreeViewItem;
                        if (parentContainer != null)
                        {
                            parentContainer.Focus();
                        }
                    }
                    context.SelectedItem = (ItemSizeType)newParent;
                    context.TextBoxName = ((ItemSizeCategoryDTO)context.SelectedItem).Name;
                    ((ItemSizeCategoryDTO)context.SelectedItem).IsEditing = true;
                }
            }
        }

        private void CreateItemSizeDetailFromMaster(object sender, RoutedEventArgs e)
        {
            if (DataContext is ItemSizeMasterViewModel context)
            {
                if (SizesTreeView.SelectedItem is ItemSizeCategoryDTO selectedNode)
                {
                    context.IsUpdate = false;
                    var newNode = new ItemSizeValueDTO { Name = "NUEVO TALLAJE", IsEditing = true, Id = 0, ItemSizeCategoryId = selectedNode.Id };
                    if (selectedNode.ItemSizeValues is null) selectedNode.ItemSizeValues = [];
                    selectedNode.ItemSizeValues.Add(newNode);
                    if (selectedNode.IsExpanded is false) selectedNode.IsExpanded = true;
                    TreeViewItem itemContainer = SizesTreeView.ItemContainerGenerator.ContainerFromItem(selectedNode) as TreeViewItem;
                    if (itemContainer.ItemContainerGenerator.ContainerFromItem(newNode) is TreeViewItem newContainer)
                    {
                        newContainer.Focus();
                    }
                    else
                    {
                        itemContainer.UpdateLayout();
                        newContainer = itemContainer.ItemContainerGenerator.ContainerFromItem(newNode) as TreeViewItem;
                        if (newContainer != null)
                        {
                            newContainer.Focus();
                        }
                    }
                    context.SelectedItem = (ItemSizeType)newNode;
                    context.TextBoxName = ((ItemSizeValueDTO)context.SelectedItem).Name;
                    ((ItemSizeValueDTO)context.SelectedItem).IsEditing = true;
                }
            }
        }
    }
}
