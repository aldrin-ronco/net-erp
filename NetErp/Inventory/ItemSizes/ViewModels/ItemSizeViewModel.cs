using AutoMapper;
using Caliburn.Micro;
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

        private ItemSizeMasterViewModel _itemSizeMasterViewModel;

        public ItemSizeMasterViewModel ItemSizeMasterViewModel
        {
            get
            {
                if (_itemSizeMasterViewModel is null) _itemSizeMasterViewModel = new ItemSizeMasterViewModel(this);
                return _itemSizeMasterViewModel;
            }
        }


        public ItemSizeViewModel(IMapper mapper,
                                         IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            Task.Run(ActivateMasterViewModel);
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
