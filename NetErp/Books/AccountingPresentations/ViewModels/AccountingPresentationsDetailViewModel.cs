using Caliburn.Micro;
using Common.Interfaces;
using DevExpress.Mvvm;
using Models.Billing;
using Models.Books;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Books.AccountingPresentations.ViewModels
{
    class AccountingPresentationsDetailViewModel : Screen
    {
        public AccountingPresentationsViewModel Context { get; set; }

        public IGenericDataAccess<AccountingPresentationGraphQLModel> PresentationService = IoC.Get<IGenericDataAccess<AccountingPresentationGraphQLModel>>();

        private AccountingPresentationGraphQLModel _selectedItem;

        public bool IsNewPresentation => PresentationId == 0;

        private int _presentationId;
        public int PresentationId
        {
            get { return _presentationId; }
            set
            {
                if (_presentationId != value)
                {
                    _presentationId = value;
                    NotifyOfPropertyChange(nameof(PresentationId));
                }
            }
        }

        private string _presentationName;
        public string PresentationName
        {
            get { return _presentationName; }
            set
            {
                if (_presentationName != value)
                {
                    _presentationName = value;
                    NotifyOfPropertyChange(nameof(PresentationName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _presentationAllowClosure;
        public bool PresentationAllowClosure
        {
            get { return _presentationAllowClosure; }
            set
            {
                if (_presentationAllowClosure != value)
                {
                    _presentationAllowClosure = value;
                    NotifyOfPropertyChange(nameof(PresentationAllowClosure));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private AccountingBookGraphQLModel? _accountingBookClosure;
        public AccountingBookGraphQLModel? AccountingBookClosure
        {
            get { return _accountingBookClosure; }
            set
            {
                if(_accountingBookClosure != value)
                {
                    _accountingBookClosure = value;
                    NotifyOfPropertyChange(nameof(AccountingBookClosure));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<AccountingBookGraphQLModel>? _presentationAccountingBooks;
        public ObservableCollection<AccountingBookGraphQLModel>? PresentationAccountingBooks
        {
            get { return _presentationAccountingBooks; }
            set
            {
                if(_presentationAccountingBooks != value)
                {
                    _presentationAccountingBooks = value;
                    NotifyOfPropertyChange(nameof(PresentationAccountingBooks));
                }
            }
        }

        private ObservableCollection<AccountingBookGraphQLModel>? _accountingBooks;
        public ObservableCollection<AccountingBookGraphQLModel>? AccountingBooks
        {
            get { return _accountingBooks; }
            set
            {
                if (_accountingBooks != value)
                {
                    _accountingBooks = value;
                    NotifyOfPropertyChange(nameof(AccountingBooks));
                    

                }
            }
        }

        private ICommand _goBackCommand;
        public ICommand? GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBackAsync);
                return _goBackCommand;
            }
        }

        public bool CanSave
        {
            get
            {
                return !string.IsNullOrEmpty(PresentationName) &&
                    ((!PresentationAllowClosure || AccountingBookClosure != null))  ;
            }
        }

        private ICommand _saveCommand;

        public ICommand? SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }
        public async Task SaveAsync()
        {
            try
            { 

                var SavedBooks = (AccountingBooks.Where(book => book.IsChecked).Select(book => new { id = book.Id }).ToList());
                string query = IsNewPresentation ? @"mutation($data : CreateAccountingPresentationInput!){
              CreateResponse: createAccountingPresentation(data: $data){
                name
                id
                accountingBookClosure{
                  id
                  name
                }
                accountingBooks{
                  name
                }
              }
            }" : @"mutation($data: UpdateAccountingPresentationInput!, $id: Int!){
              updateAccountingPresentation(data: $data, id: $id){
                id
                name
                allowsAccountingClosure
                accountingBookClosure{
                  id
                  name
                }
                accountingBooks{
                  id
                  name
                }
              }
            }";
                dynamic variables = new ExpandoObject();
                variables.data = new ExpandoObject();
                if (!IsNewPresentation) variables.id = PresentationId;
                variables.data.name = PresentationName;
                variables.data.allowsAccountingClosure = PresentationAllowClosure;
                if (PresentationAllowClosure) variables.data.accountingBookClosureId = AccountingBookClosure?.Id;
                else
                {
                    variables.data.accountingBookClosureId = 0;
                }
                variables.data.accountingBooks = SavedBooks;
                var result = IsNewPresentation ? await PresentationService.Create(query, variables) : await PresentationService.Update(query, variables);
                if (IsNewPresentation)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new PresentationCreateMessage() { CreatePresentation = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new PresentationUpdateMessage() { UpdatePresentation = result });
                }
                await GoBackAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task GoBackAsync()
        {
            
            await Context.ActivateMasterViewAsync();
            this.PresentationName = null;
            this.PresentationAllowClosure = false;
            this.PresentationAccountingBooks = null;
            this.AccountingBookClosure = null;
            foreach (var book in AccountingBooks)
            {
                book.IsChecked = false;
            }
            this.AccountingBooks = null;
        }

        public void UpdateAccountingBookClosure()
        {
            if (AccountingBooks == null || PresentationAccountingBooks == null)
                return;

            var matchingBook = AccountingBooks.FirstOrDefault(book => book.Id == AccountingBookClosure?.Id);
            if (matchingBook != null)
            {
                AccountingBookClosure = matchingBook;
                NotifyOfPropertyChange(nameof(AccountingBookClosure));
            }
            else
            {
                AccountingBookClosure = AccountingBooks.FirstOrDefault();
            }
        }

        public AccountingPresentationsDetailViewModel(AccountingPresentationsViewModel context)
        {
            Context = context;
            AccountingBooks = new ObservableCollection<AccountingBookGraphQLModel>();
            PresentationAccountingBooks = new ObservableCollection<AccountingBookGraphQLModel>();
        }
    }
}
