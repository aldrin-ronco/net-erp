using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Books;
using NetErp.Books.AccountingAccounts.ViewModels;
using System;
using System.Collections.Generic;
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
        public IEventAggregator EventAggregator { get; private set; }

        private AccountingAccountGroupMasterViewModel _accountingAccountGroupMasterViewModel;

		public AccountingAccountGroupMasterViewModel AccountingAccountGroupMasterViewModel
		{
            get
            {
                if (_accountingAccountGroupMasterViewModel is null) _accountingAccountGroupMasterViewModel = new AccountingAccountGroupMasterViewModel(this, _notificationService, _accountingAccountGroupService);
                return _accountingAccountGroupMasterViewModel;
            }
        }

        public AccountingAccountGroupViewModel(IMapper mapper, IEventAggregator eventAggregator, Helpers.Services.INotificationService notificationService, IRepository<AccountingAccountGroupGraphQLModel> accountingAccountGroupService)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _notificationService = notificationService;
            _accountingAccountGroupService = accountingAccountGroupService;
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
