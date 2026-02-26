using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Models.Inventory;
using NetErp.Books.AccountingAccounts.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Inventory.ItemSizes.ViewModels
{
    public class ItemSizeViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }
        
        private readonly IRepository<ItemSizeCategoryGraphQLModel> _itemSizeCategoryService;
        private readonly IRepository<ItemSizeValueGraphQLModel> _itemSizeValueService;
        private readonly Helpers.Services.INotificationService _notificationService;

        private ItemSizeCategoryViewModel _itemSizeCategoryViewModel;

        public ItemSizeCategoryViewModel ItemSizeCategoryViewModel
        {
            get
            {
                if (_itemSizeCategoryViewModel is null) _itemSizeCategoryViewModel = new ItemSizeCategoryViewModel(this, _itemSizeCategoryService, _itemSizeValueService, _notificationService);
                return _itemSizeCategoryViewModel;
            }
        }


        public ItemSizeViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<ItemSizeCategoryGraphQLModel> itemSizeCategoryService,
            IRepository<ItemSizeValueGraphQLModel> itemSizeValueService,
            Helpers.Services.INotificationService notificationService)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _itemSizeCategoryService = itemSizeCategoryService;
            _itemSizeValueService = itemSizeValueService;
            _notificationService = notificationService;
            _ = ActivateCategoryViewModel();
        }


        public async Task ActivateCategoryViewModel()
        {
            try
            {
                await ActivateItemAsync(ItemSizeCategoryViewModel, new CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
