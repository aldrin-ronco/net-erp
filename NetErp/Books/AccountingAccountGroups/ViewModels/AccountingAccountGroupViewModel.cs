using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Books;
using NetErp.Billing.CreditLimit.ViewModels;
using NetErp.Books.AccountingAccounts.ViewModels;
using NetErp.Helpers.Cache;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Books.AccountingAccountGroups.ViewModels
{
    public class AccountingAccountGroupViewModel: Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingAccountGroupGraphQLModel> _accountingAccountGroupService;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;

        public IEventAggregator EventAggregator { get; private set; }

        private AccountingAccountGroupMasterViewModel _accountingAccountGroupMasterViewModel;

		public AccountingAccountGroupMasterViewModel AccountingAccountGroupMasterViewModel
		{
            get
            {
                if (_accountingAccountGroupMasterViewModel is null) _accountingAccountGroupMasterViewModel = new AccountingAccountGroupMasterViewModel(this, _notificationService, _accountingAccountGroupService, _auxiliaryAccountingAccountCache);
                return _accountingAccountGroupMasterViewModel;
            }
        }

        public AccountingAccountGroupViewModel(IMapper mapper,
            IEventAggregator eventAggregator,
            Helpers.Services.INotificationService notificationService,
            IRepository<AccountingAccountGroupGraphQLModel> accountingAccountGroupService,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache
)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _notificationService = notificationService;
            _accountingAccountGroupService = accountingAccountGroupService;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;

            EventAggregator = eventAggregator;
        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = InitializeAsync().ConfigureAwait(false);
        }

        private async Task InitializeAsync()
        {
            try
            {
                await ActivateMasterViewModelAsync();
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(
                        title: "Error Inesperado!",
                        text: $"{this.GetType().Name}.InitializeAsync \r\n{ex.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }
        public async Task ActivateMasterViewModelAsync()
        {
            try
            {
                await ActivateItemAsync(AccountingAccountGroupMasterViewModel, new CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
          
        }
    }
}
