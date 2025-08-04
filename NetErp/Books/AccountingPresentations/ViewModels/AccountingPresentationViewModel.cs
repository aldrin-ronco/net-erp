using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using DevExpress.Mvvm.Native;
using Models.Books;
using NetErp.Billing.Zones.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetErp.Books.AccountingPresentations.ViewModels
{
    class AccountingPresentationViewModel : Conductor<Caliburn.Micro.Screen>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }

        private AccountingPresentationMasterViewModel _accountingPresentationsMasterViewModel;
        public AccountingPresentationMasterViewModel AccountingPresentationsMasterViewModel
        {
            get
            {
                if (_accountingPresentationsMasterViewModel is null) _accountingPresentationsMasterViewModel = new AccountingPresentationMasterViewModel(this);
                return _accountingPresentationsMasterViewModel;
            }
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(AccountingPresentationsMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task ActivateDetailForNewAsync(ObservableCollection<AccountingBookDTO> accountingBooks)
        {
            try
            {
                AccountingPresentationDetailViewModel instance = new(this)
                {
                    AccountingBooks = accountingBooks,
                    AccountingBookClosure = accountingBooks.FirstOrDefault()
                };
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch
            {
                throw;
            }
        }
        public async Task ActivateDetailViewForEditAsync(AccountingPresentationGraphQLModel accountingPresentation, ObservableCollection<AccountingBookDTO> accountingBooks)
        {
            try
            {
                var checkedIds = accountingPresentation.AccountingBooks.Select(p => p.Id).ToHashSet();
                foreach (var accountingBook in accountingBooks)
                {
                    accountingBook.IsChecked = checkedIds.Contains(accountingBook.Id);
                }

                AccountingPresentationDetailViewModel instance = new(this)
                {
                    AccountingPresentationId = accountingPresentation.Id,
                    AccountingPresentationName = accountingPresentation.Name,
                    AccountingPresentationAllowClosure = accountingPresentation.AllowsAccountingClosure,
                    AccountingBooks = accountingBooks,
                    AccountingBookClosure = accountingPresentation.AccountingBookClosure is null ? accountingBooks.FirstOrDefault() : accountingBooks.FirstOrDefault(accountingBook => accountingPresentation.AccountingBookClosure.Id == accountingBook.Id),
                    AccountingPresentationAccountingBooks = accountingPresentation.AccountingBooks,
                };
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch
            {
                throw;
            }       
        }
        public AccountingPresentationViewModel(IEventAggregator eventAggregator, IMapper mapper)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _accountingPresentationsMasterViewModel = new AccountingPresentationMasterViewModel(this); 
            _ = Task.Run(() => ActivateMasterViewAsync());
        }
    }
}
