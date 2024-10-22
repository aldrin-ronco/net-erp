using Models.Books;
using Models.Inventory;
using NetErp.Global.Modals.ViewModels;
using NetErp.Global.Modals.Views;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.ViewModels;
using NetErp.Inventory.CatalogItems.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Helpers
{
    public interface IDialogService 
    {
        void ShowDialog(dynamic viewModel, string tittle);
    }
    public class DialogService : IDialogService
    {

        public void ShowDialog(dynamic viewModel, string tittle)
        {
            if (viewModel is SearchItemModalViewModel<ItemDTO, ItemGraphQLModel>)
            {
                var view = new SearchItemModalView
                {
                    DataContext = viewModel
                };

                var window = new Window
                {
                    Content = view,
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize,
                    Title = tittle
                };
                viewModel.DialogWindow = window;
                window.ShowDialog();
            }

            if(viewModel is SearchWithTwoColumnsGridViewModel<AccountingEntityGraphQLModel>)
            {
                var view = new SearchWithTwoColumnsGridView
                {
                    DataContext = viewModel
                };

                var window = new Window
                {
                    Content = view,
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize,
                    Title = tittle
                };
                viewModel.DialogWindow = window;
                window.ShowDialog();
            }
        }
    }
}
