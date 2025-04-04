using Caliburn.Micro;
using Common.Interfaces;
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
    class AccountingPresentationsViewModel : Conductor<Caliburn.Micro.Screen>.Collection.OneActive
    {

        public IGenericDataAccess<AccountingPresentationGraphQLModel> AccountingPresentationService { get; set; } = IoC.Get<IGenericDataAccess<AccountingPresentationGraphQLModel>>();

        public IEventAggregator EventAggregator { get; set; }

        private AccountingPresentationsMasterViewModel _accountingPresentationsMasterViewModel;
        public AccountingPresentationsMasterViewModel AccountingPresentationsMasterViewModel
        {
            get
            {
                if (_accountingPresentationsMasterViewModel is null) _accountingPresentationsMasterViewModel = new AccountingPresentationsMasterViewModel(this);
                return _accountingPresentationsMasterViewModel;
            }
        }

        private AccountingPresentationsDetailViewModel _accountingPresentationsDetailViewModel;
        public AccountingPresentationsDetailViewModel AccountingPresentationsDetailViewModel
        {
            get
            {
                if (_accountingPresentationsDetailViewModel is null) _accountingPresentationsDetailViewModel = new AccountingPresentationsDetailViewModel(this);
                return _accountingPresentationsDetailViewModel;
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
        public async Task ActivateDetailForNewAsync(ObservableCollection<AccountingBookGraphQLModel> Books)
        {
            try
            {
                AccountingPresentationsDetailViewModel Instance = new(this)
                {
                    AccountingBooks = Books
                };
                Instance.UpdateAccountingBookClosure();
                await ActivateItemAsync(Instance, new System.Threading.CancellationToken());
            }
            catch
            {
                throw;
            }
        }
        public async Task ActivateDetailViewForEditAsync(AccountingPresentationGraphQLModel Presentation, ObservableCollection<AccountingBookGraphQLModel> Books)
        {
            try
            {
                foreach (var book in Books)
                {
                    book.IsChecked = Presentation.AccountingBooks.Any(p => p.Id == book.Id);
                }
                AccountingPresentationsDetailViewModel Instance = new(this)
                {
                    PresentationId = Presentation.Id,
                    PresentationName = Presentation.Name,
                    PresentationAllowClosure = Presentation.AllowsAccountingClosure,
                    AccountingBookClosure = Presentation.AccountingBookClosure,
                    PresentationAccountingBooks = Presentation.AccountingBooks,
                    AccountingBooks = Books 
                };
                Instance.UpdateAccountingBookClosure();

                await ActivateItemAsync(Instance, new System.Threading.CancellationToken());
            }
            catch
            {
                throw;
            }       
        }
        public AccountingPresentationsViewModel(IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
            _accountingPresentationsMasterViewModel = new AccountingPresentationsMasterViewModel(this); 
            _ = Task.Run(() => ActivateMasterViewAsync());
        }
    }
}
