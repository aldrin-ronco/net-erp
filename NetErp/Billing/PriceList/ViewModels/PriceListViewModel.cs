using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Common.Services;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Global;
using Models.Inventory;
using NetErp.Billing.PriceList.PriceListHelpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.Services;
using System;
using System.Threading;
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
        private readonly CatalogCache _catalogCache;
        private readonly StorageCache _storageCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly PaymentMethodCache _paymentMethodCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly PermissionCache _permissionCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        public PriceListMasterViewModel PriceListMasterViewModel
        {
            get
            {
                field ??= new PriceListMasterViewModel(this, _priceListItemService, _backgroundQueueService, _notificationService, _calculatorFactory, _dialogService, _priceListService, _catalogCache, _storageCache, _costCenterCache, _paymentMethodCache, _stringLengthCache, _permissionCache, _graphQLClient, _joinableTaskFactory);
                return field;
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
            CatalogCache catalogCache,
            StorageCache storageCache,
            CostCenterCache costCenterCache,
            PaymentMethodCache paymentMethodCache,
            StringLengthCache stringLengthCache,
            PermissionCache permissionCache,
            JoinableTaskFactory joinableTaskFactory,
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
            _catalogCache = catalogCache;
            _storageCache = storageCache;
            _costCenterCache = costCenterCache;
            _paymentMethodCache = paymentMethodCache;
            _stringLengthCache = stringLengthCache;
            _permissionCache = permissionCache;
            _joinableTaskFactory = joinableTaskFactory;
            _graphQLClient = graphQLClient;
        }

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.PriceList);
                await ActivateMasterViewAsync();
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnViewReady)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
                await TryCloseAsync();
            }
        }

        public async Task ActivateMasterViewAsync()
        {
            await ActivateItemAsync(PriceListMasterViewModel, default);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Items.Clear();
            }

            return base.OnDeactivateAsync(close, cancellationToken);
        }

        public async Task ActivateUpdatePromotionViewAsync(PriceListGraphQLModel promotion)
        {
            try
            {
                UpdatePromotionViewModel instance = new(this, _notificationService, _priceListItemService, _dialogService, _itemService, _tempRecordService, _priceListService, _stringLengthCache, _permissionCache, new NetErp.Helpers.DebouncedAction(), _joinableTaskFactory);
                instance.Id = promotion.Id;
                instance.Name = promotion.Name;
                instance.IsPromotionActive = promotion.IsActive;
                instance.StartDate = promotion.StartDate;
                instance.EndDate = promotion.EndDate;
                await instance.InitializeAsync();
                await ActivateItemAsync(instance, default);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(ActivateUpdatePromotionViewAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
        }
    }
}
