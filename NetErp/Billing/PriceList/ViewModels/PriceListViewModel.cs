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
using NetErp.Helpers.Cache;
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
        private readonly IRepository<PriceListItemGraphQLModel> _priceListItemService;
        private readonly IBackgroundQueueService _backgroundQueueService;
        private readonly INotificationService _notificationService;
        private readonly NetErp.Helpers.IDialogService _dialogService;
        private readonly IParallelBatchProcessor _parallelBatchProcessor;
        private readonly IPriceListCalculatorFactory _calculatorFactory;
        private readonly IRepository<PriceListGraphQLModel> _priceListService;
        private readonly IRepository<ItemGraphQLModel> _itemService;
        private readonly IRepository<TempRecordGraphQLModel> _tempRecordService;
        private readonly IGraphQLClient _graphQLClient;
        private readonly StorageCache _storageCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly PaymentMethodCache _paymentMethodCache;

        private PriceListMasterViewModel _priceListMasterViewModel;

        public PriceListMasterViewModel PriceListMasterViewModel
        {
            get 
            {
                if (_priceListMasterViewModel is null) _priceListMasterViewModel = new PriceListMasterViewModel(this, _priceListItemService, _backgroundQueueService, _notificationService, _calculatorFactory, _dialogService, _priceListService, _storageCache, _costCenterCache, _paymentMethodCache, _graphQLClient);
                return _priceListMasterViewModel; 
            }
        }


        public PriceListViewModel(
            IMapper autoMapper,
            IEventAggregator eventAggregator,
            IRepository<PriceListItemGraphQLModel> priceListItemService,
            IBackgroundQueueService backgroundQueueService,
            INotificationService notificationService,
            NetErp.Helpers.IDialogService dialogService,
            IParallelBatchProcessor parallelBatchProcessor,
            IPriceListCalculatorFactory calculatorFactory,
            IRepository<PriceListGraphQLModel> priceListService,
            IRepository<ItemGraphQLModel> itemService,
            IRepository<TempRecordGraphQLModel> tempRecordService,
            StorageCache storageCache,
            CostCenterCache costCenterCache,
            PaymentMethodCache paymentMethodCache,
            IGraphQLClient graphQLClient)
        {
            AutoMapper = autoMapper;
            EventAggregator = eventAggregator;
            _priceListItemService = priceListItemService;
            _backgroundQueueService = backgroundQueueService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _parallelBatchProcessor = parallelBatchProcessor;
            _calculatorFactory = calculatorFactory;
            _priceListService = priceListService;
            _itemService = itemService;
            _tempRecordService = tempRecordService;
            _storageCache = storageCache;
            _costCenterCache = costCenterCache;
            _paymentMethodCache = paymentMethodCache;
            _graphQLClient = graphQLClient;
        }

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                await ActivateMasterViewAsync();
            }
            catch (AsyncException ex)
            {
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(OnViewReady)}: {ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
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
                UpdatePromotionViewModel instance = new(this, _notificationService, _priceListItemService, _dialogService, _itemService, _tempRecordService, _priceListService);
                instance.Id = promotion.Id;
                instance.Name = promotion.Name;
                instance.IsPromotionActive = promotion.IsActive;
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
