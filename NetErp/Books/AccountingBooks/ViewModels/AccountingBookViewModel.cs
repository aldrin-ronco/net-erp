using AutoMapper;
using Caliburn.Micro;
using Models.Books;
using NetErp.Books.AccountingEntities.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Books.AccountingBooks.ViewModels
{
    public class AccountingBookViewModel: Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }
        private AccountingBookMasterViewModel _accountingBookMasterViewModel;
        private AccountingBookMasterViewModel AccountingBookMasterViewModel
        {
            get
            {
                if (_accountingBookMasterViewModel is null) _accountingBookMasterViewModel = new AccountingBookMasterViewModel(this);
                return _accountingBookMasterViewModel;

            }
        }
        public AccountingBookViewModel(IMapper mapper, IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _ = Task.Run(ActivateMasterView);
        }

        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(AccountingBookMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task ActivateDetailViewForEdit(AccountingBookGraphQLModel accountingBook)
        {
            try
            {
                AccountingBookDetailViewModel instance = new(this);
                instance.AccountingBookId = accountingBook.Id;
                instance.AccountingBookName = accountingBook.Name;
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch
            {

            }
        }
        public async Task ActivateDetailViewForNew()
        {
            try
            {
                AccountingBookDetailViewModel instance = new(this);
                instance.CleanUpControls();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
