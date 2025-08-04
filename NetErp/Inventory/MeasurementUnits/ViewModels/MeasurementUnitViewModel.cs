using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
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
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<MeasurementUnitGraphQLModel> _measurementUnitService;

        private MeasurementUnitMasterViewModel _measurementUnitMasterViewModel;
        public MeasurementUnitMasterViewModel MeasurementUnitMasterViewModel
        {
            get
            {
                _measurementUnitMasterViewModel ??= new MeasurementUnitMasterViewModel(this, _measurementUnitService, _notificationService);
                return _measurementUnitMasterViewModel;
            }
        }

        public MeasurementUnitViewModel(IMapper mapper, 
            IEventAggregator eventAggregator,
            Helpers.Services.INotificationService notificationService,
            IRepository<MeasurementUnitGraphQLModel> measuremetunitService)
        {
            AutoMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _measurementUnitService = measuremetunitService ?? throw new ArgumentNullException(nameof(measuremetunitService));
            _ = Task.Run(async () =>
            {
                try
                {
                    await ActivateMasterViewAsync();
                }
                catch (AsyncException ex)
                {
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
                catch(Exception ex)
                {
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.ActivateMasterViewAsync \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
            });
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(MeasurementUnitMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public async Task ActivateDetailViewForEdit(MeasurementUnitGraphQLModel selectedItem)
        {
            MeasurementUnitDetailViewModel instance = new(this, _measurementUnitService);
            instance.Id = selectedItem.Id;
            instance.Name = selectedItem.Name;
            instance.Abbreviation = selectedItem.Abbreviation;
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForNew()
        {
            try
            {
                MeasurementUnitDetailViewModel instance = new(this, _measurementUnitService);
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
