using Caliburn.Micro;
using DevExpress.Mvvm;
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
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Helpers
{
    public interface IDialogService 
    {
        Task<bool?> ShowDialogAsync<T>(T viewModel, string tittle, CancellationToken cancellationToken = default) where T : Screen;
        Task CloseDialogAsync(Screen viewModel, bool? dialogResult = null);
    }
    public class DialogService : IDialogService
    {
        private readonly IWindowManager _windowManager;

        public DialogService(IWindowManager windowManager)
        {
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        }

        public async Task<bool?> ShowDialogAsync<T>(T viewModel, string tittle = null, CancellationToken cancellationToken = default) where T : Screen
        {
            var settings = new Dictionary<string, object>
            {
                { "WindowStartupLocation", WindowStartupLocation.CenterOwner },
                { "SizeToContent", SizeToContent.WidthAndHeight },
                { "ResizeMode", ResizeMode.NoResize },
            };

            if (!string.IsNullOrEmpty(tittle))
            {
                settings.Add("Title", tittle);
            }

            bool? result = await _windowManager.ShowDialogAsync(viewModel, null, settings);
            return result;
        }

        public async Task CloseDialogAsync(Screen viewModel, bool? dialogResult = null)
        {
            await viewModel.TryCloseAsync(dialogResult);
        }
    }
}
