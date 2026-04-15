using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
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
        private readonly IPriceListCalculator _calculator;
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
        private readonly Func<NetErp.Helpers.DebouncedAction> _debouncedActionFactory;

        public PriceListMasterViewModel PriceListMasterViewModel
        {
            get
            {
                field ??= new PriceListMasterViewModel(this, _priceListItemService, _backgroundQueueService, _notificationService, _calculator, _dialogService, _priceListService, _catalogCache, _storageCache, _costCenterCache, _paymentMethodCache, _stringLengthCache, _permissionCache, _debouncedActionFactory(), _graphQLClient, _joinableTaskFactory);
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
            IPriceListCalculator calculator,
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
            IGraphQLClient graphQLClient,
            Func<NetErp.Helpers.DebouncedAction> debouncedActionFactory)
        {
            AutoMapper = autoMapper ?? throw new ArgumentNullException(nameof(autoMapper));
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _priceListItemService = priceListItemService ?? throw new ArgumentNullException(nameof(priceListItemService));
            _backgroundQueueService = backgroundQueueService ?? throw new ArgumentNullException(nameof(backgroundQueueService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _priceListService = priceListService ?? throw new ArgumentNullException(nameof(priceListService));
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _tempRecordService = tempRecordService ?? throw new ArgumentNullException(nameof(tempRecordService));
            _catalogCache = catalogCache ?? throw new ArgumentNullException(nameof(catalogCache));
            _storageCache = storageCache ?? throw new ArgumentNullException(nameof(storageCache));
            _costCenterCache = costCenterCache ?? throw new ArgumentNullException(nameof(costCenterCache));
            _paymentMethodCache = paymentMethodCache ?? throw new ArgumentNullException(nameof(paymentMethodCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _permissionCache = permissionCache ?? throw new ArgumentNullException(nameof(permissionCache));
            _joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
            _graphQLClient = graphQLClient ?? throw new ArgumentNullException(nameof(graphQLClient));
            _debouncedActionFactory = debouncedActionFactory ?? throw new ArgumentNullException(nameof(debouncedActionFactory));
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
                await _joinableTaskFactory.SwitchToMainThreadAsync();
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
                UpdatePromotionViewModel instance = new(this, _notificationService, _priceListItemService, _dialogService, _itemService, _tempRecordService, _priceListService, _stringLengthCache, _permissionCache, _catalogCache, _debouncedActionFactory(), _joinableTaskFactory, _debouncedActionFactory);
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
