using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private readonly IRepository<CountryGraphQLModel> _countryService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly CountryCache _countryCache;

        private CostCenterMasterViewModel? _costCenterMasterViewModel;
        public CostCenterMasterViewModel CostCenterMasterViewModel
        {
            get
            {
                if (_costCenterMasterViewModel is null)
                    _costCenterMasterViewModel = new CostCenterMasterViewModel(this, _companyService, _companyLocationService, _costCenterService, _storageService, _countryService, _dialogService, _notificationService, _countryCache);
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
            IRepository<CountryGraphQLModel> countryService,
            Helpers.IDialogService dialogService,
            Helpers.Services.INotificationService notificationService,
            CountryCache countryCache)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _companyService = companyService;
            _companyLocationService = companyLocationService;
            _costCenterService = costCenterService;
            _storageService = storageService;
            _countryService = countryService;
            _dialogService = dialogService;
            _notificationService = notificationService;
            _countryCache = countryCache;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await ActivateMasterViewAsync();
                }
                catch (Exception ex)
                {
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ThemedMessageBox.Show(title: "Error de inicialización", text: $"Error al activar vista de centro de costo: {ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
            });
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(CostCenterMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
