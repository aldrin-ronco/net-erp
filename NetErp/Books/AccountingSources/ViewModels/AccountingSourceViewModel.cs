using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingEntities.ViewModels;
using NetErp.Books.Tax.ViewModels;
using NetErp.Helpers.Cache;
using Services.Books.DAL.PostgreSQL;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NetErp.Books.AccountingSources.ViewModels.AccountingSourceDetailViewModel;

namespace NetErp.Books.AccountingSources.ViewModels
{
    public class AccountingSourceViewModel : Conductor<object>.Collection.OneActive
    {
        private readonly Helpers.Services.INotificationService _notificationService;

        private readonly IRepository<AccountingSourceGraphQLModel> _accountingSourceService;
        private AccountingSourceMasterViewModel _accountingSourceMasterViewModel;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly ProcessTypeCache _processTypeCache;
        private readonly ModuleCache _moduleCache;
        public AccountingSourceMasterViewModel AccountingSourceMasterViewModel 
        {
            get
            {
                
                    if (_accountingSourceMasterViewModel is null) _accountingSourceMasterViewModel = new(this, _accountingSourceService, _notificationService, _moduleCache);
                    return _accountingSourceMasterViewModel;
                }
        }

        public IMapper AutoMapper { get; set; }
        public IEventAggregator EventAggregator { get; set; }

       

        private bool _enableOnViewReady = true;

        public bool EnableOnViewReady
        {
            get { return _enableOnViewReady; }
            set
            {
                _enableOnViewReady = value;
            }
        }
        public AccountingSourceViewModel(IMapper mapper,
                                         IEventAggregator eventAggregator,  IRepository<AccountingSourceGraphQLModel> accountingSourceService, Helpers.Services.INotificationService notificationService, AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache, ProcessTypeCache processTypeCache, ModuleCache moduleCache)
        {
            this._notificationService = notificationService;
            this._accountingSourceService = accountingSourceService;
            this._auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
                this._moduleCache = moduleCache;
            this._processTypeCache = processTypeCache;
            this.EventAggregator = eventAggregator;
            this.AutoMapper = mapper;
            _ = Task.Run(ActivateMasterViewAsync);
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(this.AccountingSourceMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateDetailViewForNewAsync()
        {
            
            AccountingSourceDetailViewModel instance = new(this, _accountingSourceService,  _auxiliaryAccountingAccountCache, _processTypeCache);
            //instance.Id = 0; // Necesario para saber que es un nuevo registro
            instance.CleanUpControls();
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForEditAsync(AccountingSourceDTO selectedItem)
        {
            AccountingSourceDetailViewModel instance = new(this, _accountingSourceService,  _auxiliaryAccountingAccountCache, _processTypeCache);
            AccountingSourceGraphQLModel entity = await instance.LoadDataForEditAsync(selectedItem.Id);
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());

           

           
        }
    }
}
