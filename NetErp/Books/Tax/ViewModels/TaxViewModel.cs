using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Books;
using NetErp.Books.Tax.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Books.Tax.ViewModels
{
    public class TaxViewModel : Conductor<object>.Collection.OneActive
    {
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<TaxGraphQLModel> _taxService;

        public IEventAggregator EventAggregator { get; private set; }
        public IMapper AutoMapper { get; private set; }

        private TaxMasterViewModel _TaxMasterViewModel;

        public TaxMasterViewModel TaxMasterViewModel
        {
            get
            {
                if (_TaxMasterViewModel is null) _TaxMasterViewModel = new TaxMasterViewModel(this, _notificationService, _taxService);
                return _TaxMasterViewModel;
            }
        }
        
       
        public TaxViewModel(IMapper mapper, IEventAggregator eventAggregator,  Helpers.Services.INotificationService notificationService, IRepository<TaxGraphQLModel> taxService)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _taxService = taxService ?? throw new ArgumentNullException(nameof(taxService));
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
        }
        public async Task ActivateMasterViewModelAsync()
        {
            try
            {
                await ActivateItemAsync(TaxMasterViewModel, new CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public async Task ActivateDetailViewForEditAsync(int taxId, ObservableCollection<TaxCategoryGraphQLModel> TaxCategories, ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts)
        {
            TaxDetailViewModel instance = new(this,  _taxService, TaxCategories, AccountingAccounts );
            
            await instance.InitializeAsync();
            TaxGraphQLModel tax = await instance.LoadDataForEditAsync(taxId);
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
        public async Task ActivateDetailViewForNewAsync( ObservableCollection<TaxCategoryGraphQLModel> TaxCategories, ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts)
        {
            TaxDetailViewModel instance = new(this, _taxService, TaxCategories, AccountingAccounts);
            await instance.InitializeAsync();
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
    }
}
