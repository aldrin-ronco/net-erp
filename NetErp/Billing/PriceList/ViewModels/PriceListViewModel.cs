using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Global;
using Models.Inventory;
using NetErp.Billing.CreditLimit.ViewModels;
using NetErp.Helpers.Services;
using Common.Services;
using NetErp.Billing.PriceList.PriceListHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class PriceListViewModel: Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; set; }
        public IEventAggregator EventAggregator { get; set; }
        private readonly IRepository<PriceListDetailGraphQLModel> _priceListDetailService;
        private readonly IBackgroundQueueService _backgroundQueueService;
        private readonly INotificationService _notificationService;
        private readonly NetErp.Helpers.IDialogService _dialogService;
        private readonly IParallelBatchProcessor _parallelBatchProcessor;
        private readonly IPriceListCalculatorFactory _calculatorFactory;
        private readonly IRepository<PriceListGraphQLModel> _priceListService;
        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly IRepository<TempRecordGraphQLModel> _tempRecordService;
        private readonly IRepository<StorageGraphQLModel> _storageService;

        private PriceListMasterViewModel _priceListMasterViewModel;

        public PriceListMasterViewModel PriceListMasterViewModel
        {
            get 
            {
                if (_priceListMasterViewModel is null) _priceListMasterViewModel = new PriceListMasterViewModel(this, _priceListDetailService, _backgroundQueueService, _notificationService, _calculatorFactory, _dialogService, _priceListService, _storageService);
                return _priceListMasterViewModel; 
            }
        }


        public PriceListViewModel(
            IMapper autoMapper, 
            IEventAggregator eventAggregator,
            IRepository<PriceListDetailGraphQLModel> priceListDetailService,
            IBackgroundQueueService backgroundQueueService,
            INotificationService notificationService,
            NetErp.Helpers.IDialogService dialogService,
            IParallelBatchProcessor parallelBatchProcessor,
            IPriceListCalculatorFactory calculatorFactory,
            IRepository<PriceListGraphQLModel> priceListService,
            IRepository<ItemGraphQLModel> itemService,
            IRepository<TempRecordGraphQLModel> tempRecordService,
            IRepository<StorageGraphQLModel> storageService)
        {
            AutoMapper = autoMapper;
            EventAggregator = eventAggregator;
            _priceListDetailService = priceListDetailService;
            _backgroundQueueService = backgroundQueueService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _parallelBatchProcessor = parallelBatchProcessor;
            _calculatorFactory = calculatorFactory;
            _priceListService = priceListService;
            _itemService = itemService;
            _tempRecordService = tempRecordService;
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
            });
            _storageService = storageService;
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(PriceListMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }

        }

        public async Task ActivateUpdatePromotionViewAsync(PriceListGraphQLModel promotion)
        {
            try
            {
                UpdatePromotionViewModel instance = new(this, _notificationService, _priceListDetailService, _dialogService, _parallelBatchProcessor, _itemService, _tempRecordService, _priceListService);
                instance.Id = promotion.Id;
                instance.Name = promotion.Name;
                instance.StartDate = promotion.StartDate;
                instance.EndDate = promotion.EndDate;
                await instance.InitializeAsync();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }
    }
}
