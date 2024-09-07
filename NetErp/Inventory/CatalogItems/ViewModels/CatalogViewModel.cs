using AutoMapper;
using Caliburn.Micro;
using NetErp.Inventory.MeasurementUnits.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    public class CatalogViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }

        public IEventAggregator EventAggregator { get; set; }

        private CatalogMasterViewModel _catalogMasterViewModel;

        public CatalogMasterViewModel CatalogMasterViewModel
        {
            get
            {
                if (_catalogMasterViewModel is null) _catalogMasterViewModel = new CatalogMasterViewModel(this);
                return _catalogMasterViewModel;
            }
        }

        public CatalogViewModel(IMapper mapper, IEventAggregator eventAggregator)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            Task.Run(ActivateMasterView);
        }

        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(new CatalogMasterViewModel(this), new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
