using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Books;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Books.AccountingPresentations.ViewModels
{
    class AccountingPresentationDetailViewModel : Screen, INotifyDataErrorInfo
    {
        public AccountingPresentationViewModel Context { get; set; }

        Dictionary<string, List<string>> _errors;

       
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingPresentationGraphQLModel> _accountingPresentationService;

        private bool _isBusy = false;

        public bool IsBusy
        {
            get { return _isBusy; }
            set 
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public bool IsNewRecord => AccountingPresentationId == 0;

        private int _accountingPresentationId;
        public int AccountingPresentationId
        {
            get { return _accountingPresentationId; }
            set
            {
                if (_accountingPresentationId != value)
                {
                    _accountingPresentationId = value;
                    NotifyOfPropertyChange(nameof(AccountingPresentationId));
                }
            }
        }

        private string _accountingPresentationName;
        public string AccountingPresentationName
        {
            get { return _accountingPresentationName; }
            set
            {
                if (_accountingPresentationName != value)
                {
                    _accountingPresentationName = value;
                    NotifyOfPropertyChange(nameof(AccountingPresentationName));
                    ValidateProperty(nameof(AccountingPresentationName), _accountingPresentationName);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _accountingPresentationAllowClosure;
        public bool AccountingPresentationAllowClosure
        {
            get { return _accountingPresentationAllowClosure; }
            set
            {
                if (_accountingPresentationAllowClosure != value)
                {
                    _accountingPresentationAllowClosure = value;
                    NotifyOfPropertyChange(nameof(AccountingPresentationAllowClosure));
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

        private ObservableCollection<AccountingBookGraphQLModel> _accountingPresentationAccountingBooks;
        public ObservableCollection<AccountingBookGraphQLModel> AccountingPresentationAccountingBooks
        {
            get { return _accountingPresentationAccountingBooks; }
            set
            {
                if(_accountingPresentationAccountingBooks != value)
                {
                    _accountingPresentationAccountingBooks = value;
                    NotifyOfPropertyChange(nameof(AccountingPresentationAccountingBooks));
                }
            }
        }

        private ObservableCollection<AccountingBookDTO> _accountingBooks;
        public ObservableCollection<AccountingBookDTO> AccountingBooks
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
                return !string.IsNullOrEmpty(AccountingPresentationName) &&
                    ((!AccountingPresentationAllowClosure || AccountingBookClosure != null) && _errors.Count <= 0);
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
                IsBusy = true;
                if (!AccountingBooks.Any(accountingBook => accountingBook.IsChecked))
                {
                    _notificationService.ShowInfo("Por favor seleccione como mínimo un libro contable.");
                    return;
                }
                List<int> savedBooksIds = [.. AccountingBooks.Where(accountingBook => accountingBook.IsChecked == true).Select(x => x.Id)];
                string query = IsNewRecord ? @"mutation($data : CreateAccountingPresentationInput!){
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
                    UpdateResponse: updateAccountingPresentation(data: $data, id: $id){
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
                if (!IsNewRecord) variables.id = AccountingPresentationId;
                variables.data.name = AccountingPresentationName.Trim().RemoveExtraSpaces();
                variables.data.allowsAccountingClosure = AccountingPresentationAllowClosure;
                variables.data.accountingBookClosureId = AccountingPresentationAllowClosure ? AccountingBookClosure?.Id : 0;
                variables.data.accountingBooksIds = savedBooksIds;
                var result = IsNewRecord ? await _accountingPresentationService.CreateAsync(query, variables) : await _accountingPresentationService.UpdateAsync(query, variables);
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new AccountingPresentationCreateMessage() { CreatedAccountingPresentation = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new AccountingPresentationUpdateMessage() { UpdatedAccountingPresentation = result });
                }
                await GoBackAsync();
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperty(nameof(AccountingPresentationName), AccountingPresentationName);
        }

        public async Task GoBackAsync()
        {
            
            await Context.ActivateMasterViewAsync();
            this.AccountingPresentationName = "";
            this.AccountingPresentationAllowClosure = false;
            this.AccountingPresentationAccountingBooks = [];
            this.AccountingBookClosure = null;
            this.AccountingBooks = [];
        }

        public AccountingPresentationDetailViewModel(AccountingPresentationViewModel context, Helpers.Services.INotificationService notificationService,
            IRepository<AccountingPresentationGraphQLModel> accountingPresentationService)
        {
            Context = context;
            _notificationService = notificationService;
            _accountingPresentationService = accountingPresentationService;
            _errors = [];
            AccountingBooks = [];
            AccountingPresentationAccountingBooks = [];
        }


        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(AccountingPresentationName):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "El nombre no puede estar vacío");
                        break;

                }
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }
    }
}
