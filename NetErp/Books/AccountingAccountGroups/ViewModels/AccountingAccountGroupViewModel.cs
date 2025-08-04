using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Xpf.Core;
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

        public IEventAggregator EventAggregator { get; private set; }

        private AccountingAccountGroupMasterViewModel _accountingAccountGroupMasterViewModel;

		public AccountingAccountGroupMasterViewModel AccountingAccountGroupMasterViewModel
		{
            get
            {
                if (_accountingAccountGroupMasterViewModel is null) _accountingAccountGroupMasterViewModel = new AccountingAccountGroupMasterViewModel(this);
                return _accountingAccountGroupMasterViewModel;
            }
        }

        public AccountingAccountGroupViewModel(IMapper mapper, IEventAggregator eventAggregator)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
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
