using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Books.AccountingAccounts.ViewModels
{
    public class AccountPlanViewModel : Conductor<object>.Collection.OneActive
    {

        private AccountPlanMasterViewModel _accountPlanMasterViewModel;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingAccountGraphQLModel> _accountingAccountService;
        public AccountPlanMasterViewModel AccountPlanMasterViewModel
        {
            get 
            {
                if (_accountPlanMasterViewModel is null) _accountPlanMasterViewModel = new AccountPlanMasterViewModel(this, _notificationService, _accountingAccountService);
                return _accountPlanMasterViewModel; 
            }
        }


        public AccountPlanViewModel(Helpers.Services.INotificationService notificationService,
            IRepository<AccountingAccountGraphQLModel> accountingAccountService)
        {
            this._accountingAccountService = accountingAccountService;
            this._notificationService = notificationService;
            _ = Task.Run(async () => 
            {
                try
                {
                    await ActivateMasterViewModel();
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


        public async Task ActivateMasterViewModel()
        {
            try
            {            
                await ActivateItemAsync(AccountPlanMasterViewModel, new CancellationToken());
            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
            }
        }

        public async Task ActivateDetailViewModel(string accountCode)
        {
            try
            {
                AccountPlanDetailViewModel instance = new(this, _notificationService, _accountingAccountService)
                {
                    Code = accountCode
                };
                instance.CleanUpControls();
                await ActivateItemAsync(instance, new CancellationToken());
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message ?? ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
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
