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
        
        private readonly IRepository<ItemSizeMasterGraphQLModel> _itemSizeMasterService;
        private readonly IRepository<ItemSizeDetailGraphQLModel> _itemSizeDetailService;
        private readonly Helpers.Services.INotificationService _notificationService;

        private ItemSizeMasterViewModel _itemSizeMasterViewModel;

        public ItemSizeMasterViewModel ItemSizeMasterViewModel
        {
            get
            {
                if (_itemSizeMasterViewModel is null) _itemSizeMasterViewModel = new ItemSizeMasterViewModel(this, _itemSizeMasterService, _itemSizeDetailService, _notificationService);
                return _itemSizeMasterViewModel;
            }
        }


        public ItemSizeViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<ItemSizeMasterGraphQLModel> itemSizeMasterService,
            IRepository<ItemSizeDetailGraphQLModel> itemSizeDetailService,
            Helpers.Services.INotificationService notificationService)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _itemSizeMasterService = itemSizeMasterService;
            _itemSizeDetailService = itemSizeDetailService;
            _notificationService = notificationService;
            _ = ActivateMasterViewModel();
        }


        public async Task ActivateMasterViewModel()
        {
            try
            {
                await ActivateItemAsync(ItemSizeMasterViewModel, new CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
