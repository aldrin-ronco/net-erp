using AutoMapper;
using Caliburn.Micro;
using Models.Books;
using Models.DTO.Global;
using Models.Inventory;
using NetErp.Books.AccountingEntities.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dictionaries.BooksDictionaries;

namespace NetErp.Inventory.MeasurementUnits.ViewModels
{
    public class MeasurementUnitViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }

        public IEventAggregator EventAggregator { get; set; }

        private MeasurementUnitMasterViewModel _measurementUnitMasterViewModel;
        public MeasurementUnitMasterViewModel MeasurementUnitMasterViewModel
        {
            get
            {
                if (_measurementUnitMasterViewModel is null) _measurementUnitMasterViewModel = new MeasurementUnitMasterViewModel(this);
                return _measurementUnitMasterViewModel;
            }
        }

        public MeasurementUnitViewModel(IMapper mapper, IEventAggregator eventAggregator)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            Task.Run(ActivateMasterView);
        }

        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(MeasurementUnitMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task ActivateDetailViewForEdit(MeasurementUnitGrahpQLModel selectedItem)
        {
            MeasurementUnitDetailViewModel instance = new(this);
            instance.Id = selectedItem.Id;
            instance.Name = selectedItem.Name;
            instance.Abbreviation = selectedItem.Abbreviation;
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForNew()
        {
            try
            {
                MeasurementUnitDetailViewModel instance = new(this);
                instance.CleanUpControls();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
