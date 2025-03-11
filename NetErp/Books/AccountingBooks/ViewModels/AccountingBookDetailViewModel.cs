using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Books.AccountingBooks.ViewModels
{
    public class AccountingBookDetailViewModel: Screen, INotifyDataErrorInfo
    {
        IGenericDataAccess<AccountingBookGraphQLModel> AccountingBookService = IoC.Get<IGenericDataAccess<AccountingBookGraphQLModel>>();
        public AccountingBookViewModel Context { get; set; }
        public AccountingBookDetailViewModel(AccountingBookViewModel context)
        {
            Context = context;
            _errors = new Dictionary<string, List<string>>();
        }
        public async Task GoBack()
        {
            await Context.ActivateMasterView();
        }

        public int _accountingBookId;
        public int AccountingBookId
        {
          get { return _accountingBookId; }
          set
            {
                if (_accountingBookId != value)
                {
                    _accountingBookId = value;
                    NotifyOfPropertyChange(nameof(AccountingBookId));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        private ICommand _goBackCommand;

        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBack);
                return _goBackCommand;
            }
        }

        private string _accountingBookName;

        public string AccountingBookName
        {
            get { return _accountingBookName; }
            set
            {
                if (_accountingBookName != value)
                {
                    _accountingBookName = value;
                    NotifyOfPropertyChange(nameof(AccountingBookName));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(AccountingBookName), value);
                }
            }
        }

        public bool IsNewRecord => AccountingBookId == 0;

        public bool CanSave
        {
            get
            {
                if (string.IsNullOrEmpty(AccountingBookName)) return false;
                return true;
            }
        }
        public ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }


        private bool _isBusy;

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

        Dictionary<string, List<string>> _errors;

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                AccountingBookGraphQLModel result = await ExecuteSaveAsync();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new AccountingBookCreateMessage() { CreatedAccountingBook = result });
                }
                else
                {
                    //Pasamos el mensaje a un escuchador.
                    await Context.EventAggregator.PublishOnUIThreadAsync(new AccountingBookUpdateMessage() { UpdatedAccountingBook = result });
                }
                await Context.ActivateMasterView();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                IsBusy = false;
            }
            
            
        }

        public async Task<AccountingBookGraphQLModel> ExecuteSaveAsync()
        {
            dynamic variables = new ExpandoObject();
            variables.data = new ExpandoObject();
            if (!IsNewRecord)
            {
                variables.id = AccountingBookId; // Sigue enviando el id fuera de data                
            }
            variables.data.name = AccountingBookName; // Asegurar que name esté dentro de data

            string query = IsNewRecord ? @"
                mutation($data:CreateAccountingBookInput!){
                  CreateResponse: createAccountingBook(data: $data){
                    id
                    name
                  }
                }" :
                @"
                mutation($id: Int!, $data: UpdateAccountingBookInput!) {
                  UpdateResponse: updateAccountingBook(id: $id, data: $data) {
                    id
                    name
                  }
                }";

            LogToFile("⏳ Ejecutando Mutación...");
            LogToFile($"Query: {query}");
            LogToFile($"Variables: {Newtonsoft.Json.JsonConvert.SerializeObject(variables)}");

            AccountingBookGraphQLModel result = null;
            try
            {
                result = IsNewRecord
                    ? await AccountingBookService.Create(query, variables)
                    : await AccountingBookService.Update(query, variables);
            }
            catch (Exception ex)
            {
                LogToFile($"❌ ERROR en GraphQL Mutation: {ex.Message}");
            }

            LogToFile($"Resultado: {Newtonsoft.Json.JsonConvert.SerializeObject(result)}");

            return result;
        }
        private void LogToFile(string message)
        {
            string path = "C:\\Temp\\log.txt";  // Asegúrate de que la carpeta existe
            File.AppendAllText(path, $"{DateTime.Now}: {message}\n");
        }

        public void CleanUpControls()
        {
            AccountingBookId = 0;
            AccountingBookName = string.Empty;
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => AccountingBookName);
            ValidateProperties();
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
                    case nameof(AccountingBookName):
                        if (string.IsNullOrEmpty(AccountingBookName)) AddError(propertyName, "El campo 'Nombre del libro contable' no puede estar vacío.");
                        break;                    
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(AccountingBookName), AccountingBookName);
        }

    }
}
