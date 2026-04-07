using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Global.CostCenters.Validators;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp.Global.CostCenters.ViewModels
{
    public class CostCenterViewModel : Conductor<object>
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }

        private readonly IRepository<CompanyGraphQLModel> _companyService;
        private readonly IRepository<CompanyLocationGraphQLModel> _companyLocationService;
        private readonly IRepository<CostCenterGraphQLModel> _costCenterService;
        private readonly IRepository<StorageGraphQLModel> _storageService;
        private readonly NetErp.Helpers.IDialogService _dialogService;
        private readonly NetErp.Helpers.Services.INotificationService _notificationService;
        private readonly CountryCache _countryCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly PermissionCache _permissionCache;
        private readonly AuthorizationSequenceCache _authorizationSequenceCache;
        private readonly IGraphQLClient _graphQLClient;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly CompanyValidator _companyValidator;
        private readonly CompanyLocationValidator _companyLocationValidator;
        private readonly CostCenterValidator _costCenterValidator;
        private readonly StorageValidator _storageValidator;

        private CostCenterMasterViewModel? _costCenterMasterViewModel;
        public CostCenterMasterViewModel CostCenterMasterViewModel
        {
            get
            {
                _costCenterMasterViewModel ??= new CostCenterMasterViewModel(
                    this,
                    _companyService,
                    _companyLocationService,
                    _costCenterService,
                    _storageService,
                    _dialogService,
                    _notificationService,
                    _countryCache,
                    _stringLengthCache,
                    _permissionCache,
                    _authorizationSequenceCache,
                    _graphQLClient,
                    _joinableTaskFactory,
                    _companyValidator,
                    _companyLocationValidator,
                    _costCenterValidator,
                    _storageValidator);
                return _costCenterMasterViewModel;
            }
        }

        public CostCenterViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<CompanyGraphQLModel> companyService,
            IRepository<CompanyLocationGraphQLModel> companyLocationService,
            IRepository<CostCenterGraphQLModel> costCenterService,
            IRepository<StorageGraphQLModel> storageService,
            NetErp.Helpers.IDialogService dialogService,
            NetErp.Helpers.Services.INotificationService notificationService,
            CountryCache countryCache,
            StringLengthCache stringLengthCache,
            PermissionCache permissionCache,
            AuthorizationSequenceCache authorizationSequenceCache,
            IGraphQLClient graphQLClient,
            JoinableTaskFactory joinableTaskFactory,
            CompanyValidator companyValidator,
            CompanyLocationValidator companyLocationValidator,
            CostCenterValidator costCenterValidator,
            StorageValidator storageValidator)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _companyService = companyService;
            _companyLocationService = companyLocationService;
            _costCenterService = costCenterService;
            _storageService = storageService;
            _dialogService = dialogService;
            _notificationService = notificationService;
            _countryCache = countryCache;
            _stringLengthCache = stringLengthCache;
            _permissionCache = permissionCache;
            _authorizationSequenceCache = authorizationSequenceCache;
            _graphQLClient = graphQLClient;
            _joinableTaskFactory = joinableTaskFactory;
            _companyValidator = companyValidator;
            _companyLocationValidator = companyLocationValidator;
            _costCenterValidator = costCenterValidator;
            _storageValidator = storageValidator;
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);
            try
            {
                await ActivateItemAsync(CostCenterMasterViewModel, cancellationToken);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Error de inicialización",
                    text: $"{GetType().Name}.{nameof(OnActivateAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
        }
    }
}
