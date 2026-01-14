using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Books;
using Models.DTO.Global;
using Models.Suppliers;
using NetErp.Billing.Sellers.ViewModels;
using NetErp.Global.CostCenters.DTO;
using Ninject.Activation;
using Services.Billing.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static Dictionaries.BooksDictionaries;

namespace NetErp.Suppliers.Suppliers.ViewModels
{
    public class SupplierViewModel : Conductor<Screen>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; private set; }
        
        private readonly IRepository<SupplierGraphQLModel> _supplierService;
        private readonly Helpers.Services.INotificationService _notificationService;

        private SupplierMasterViewModel _supplierMasterViewModel;

        public SupplierMasterViewModel SupplierMasterViewModel
        {
            get 
            {
                if (_supplierMasterViewModel is null) _supplierMasterViewModel = new(this, _supplierService, _notificationService);
                return _supplierMasterViewModel; 
            }
            
        }

        public SupplierViewModel(
            IEventAggregator eventAggregator,
            IMapper mapper,
            IRepository<SupplierGraphQLModel> supplierService,
            Helpers.Services.INotificationService notificationService)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _supplierService = supplierService;
            _notificationService = notificationService;
            _ = ActivateMasterView();
        }

        private bool _enableOnViewReady = true;

        public bool EnableOnViewReady
        {
            get { return _enableOnViewReady; }
            set
            {
                _enableOnViewReady = value;
            }
        }
        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(SupplierMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateDetailViewForNewAsync(ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts)
        {
            SupplierDetailViewModel instance = new(this, _supplierService, AccountingAccounts);
           
            instance.CleanUpControlsForNew();
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForEditAsync(int supplierId, ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts)
        {
            try
            {
                SupplierDetailViewModel instance = new(this, _supplierService, AccountingAccounts);
                await instance.InitializeAsync();
              
                SupplierGraphQLModel supplier = await instance.LoadDataForEditAsync(supplierId);
                
                instance.AcceptChanges();
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

                throw new AsyncException(innerException: ex);
            }
        }
       
    

    }
}
