using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Books;
using NetErp.Books.AccountingAccountGroups.ViewModels;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers.Cache;
using Ninject.Activation;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Books.WithholdingCertificateConfig.ViewModels
{
    public class WithholdingCertificateConfigViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<WithholdingCertificateConfigGraphQLModel> _withholdingCertificateConfigService;
        private readonly CtasRestVtasAccountingAccountGroupCache _ctasRestVtasAccountingAccountGroupCache;
        private readonly CostCenterCache _costCenterCache;

        public IEventAggregator EventAggregator { get; private set; }

        private WithholdingCertificateConfigMasterViewModel _withholdingCertificateConfigMasterViewModel;

        public WithholdingCertificateConfigMasterViewModel WithholdingCertificateConfigMasterViewModel
        {
            get
            {
                if (_withholdingCertificateConfigMasterViewModel is null) _withholdingCertificateConfigMasterViewModel = new WithholdingCertificateConfigMasterViewModel(this, _notificationService, _withholdingCertificateConfigService);
                return _withholdingCertificateConfigMasterViewModel;
            }
        }

        public WithholdingCertificateConfigViewModel(IMapper mapper, IEventAggregator eventAggregator,  Helpers.Services.INotificationService notificationService, IRepository<WithholdingCertificateConfigGraphQLModel> withholdingCertificateConfigService, CtasRestVtasAccountingAccountGroupCache ctasRestVtasAccountingAccountGroupCache, CostCenterCache costCenterCache)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _withholdingCertificateConfigService = withholdingCertificateConfigService ?? throw new ArgumentNullException(nameof(withholdingCertificateConfigService));
            _ctasRestVtasAccountingAccountGroupCache = ctasRestVtasAccountingAccountGroupCache ?? throw new ArgumentNullException(nameof(ctasRestVtasAccountingAccountGroupCache));
            _ = Task.Run(async () =>
            {
                try
                {
                    await ActivateMasterViewModelAsync();
                }
                catch (AsyncException ex)
                {
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message ?? ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
            });
            EventAggregator = eventAggregator;
            _costCenterCache = costCenterCache;
        }

        public async Task ActivateMasterViewModelAsync()
        {
            try
            {
                await ActivateItemAsync(WithholdingCertificateConfigMasterViewModel, new CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public async Task ActivateDetailViewForEdit(WithholdingCertificateConfigGraphQLModel selectedItem)
        {
            WithholdingCertificateConfigDetailViewModel instance = new(this,  _withholdingCertificateConfigService, _ctasRestVtasAccountingAccountGroupCache, _costCenterCache);
          
            WithholdingCertificateConfigGraphQLModel certificate = await instance.LoadDataForEditAsync(selectedItem.Id);
            await instance.InitializeAsync();
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
        public async Task ActivateDetailViewForNew()
        {
            WithholdingCertificateConfigDetailViewModel instance = new(this,  _withholdingCertificateConfigService, _ctasRestVtasAccountingAccountGroupCache, _costCenterCache);
            await instance.InitializeAsync();

            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
    }
}
