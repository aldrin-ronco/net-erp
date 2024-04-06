using Caliburn.Micro;
using Common.Interfaces;
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

        public AccountPlanMasterViewModel AccountPlanMasterViewModel
        {
            get 
            {
                if (_accountPlanMasterViewModel is null) _accountPlanMasterViewModel = new AccountPlanMasterViewModel(this);
                return _accountPlanMasterViewModel; 
            }
        }


        public AccountPlanViewModel()
        {
            Task.Run(ActivateMasterViewModel);
        }


        public async Task ActivateMasterViewModel()
        {
            try
            {                
                await ActivateItemAsync(AccountPlanMasterViewModel, new CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateDetailViewModel(string accountCode)
        {
            try
            {
                AccountPlanDetailViewModel instance = new(this)
                {
                    Code = accountCode
                };
                instance.CleanUpControls();
                await ActivateItemAsync(instance, new CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
