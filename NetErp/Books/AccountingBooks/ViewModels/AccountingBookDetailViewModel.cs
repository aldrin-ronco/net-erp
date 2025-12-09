using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
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
using static Models.Global.GraphQLResponseTypes;
using Extensions.Global;
using Common.Helpers;
using Services.Books.DAL.PostgreSQL;
using NetErp.Helpers.GraphQLQueryBuilder;

namespace NetErp.Books.AccountingBooks.ViewModels
{
    public class AccountingBookDetailViewModel: Screen, INotifyDataErrorInfo
    {
        public AccountingBookViewModel Context { get; set; }
        private readonly IRepository<AccountingBookGraphQLModel> _accountingBookService;

        public AccountingBookDetailViewModel(AccountingBookViewModel context, IRepository<AccountingBookGraphQLModel> accountingBookService)
        {
            Context = context;
            _errors = new Dictionary<string, List<string>>();
            _accountingBookService = accountingBookService;

        }
        public async Task GoBackAsync()
        {
            await Context.ActivateMasterViewAsync();
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
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBackAsync);
                return _goBackCommand;
            }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                    ValidateProperty(nameof(Name), value);
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }

        public bool IsNewRecord => AccountingBookId == 0;

        public bool CanSave
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return false;
                if (!this.HasChanges()) return false;
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
                Refresh();
                UpsertResponseType<AccountingBookGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new AccountingBookCreateMessage() { CreatedAccountingBook = result }
                        : new AccountingBookUpdateMessage() { UpdatedAccountingBook = result }
                );
                await Context.ActivateMasterViewAsync();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
           
            
            
        }

        public async Task<UpsertResponseType<AccountingBookGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    string query = GetCreateQuery();

                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");

                    UpsertResponseType<AccountingBookGraphQLModel> accountingBookCreated = await _accountingBookService.CreateAsync<UpsertResponseType<AccountingBookGraphQLModel>>(query, variables);
                    return accountingBookCreated;
                }
                else
                {
                    string query = GetUpdateQuery();

                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = AccountingBookId;

                    UpsertResponseType<AccountingBookGraphQLModel> updatedAccountingBook = await _accountingBookService.UpdateAsync<UpsertResponseType<AccountingBookGraphQLModel>>(query, variables);
                    return updatedAccountingBook;
                }
            }
            catch (Exception)
            {
                throw;
            }
           
        }
        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AccountingBookGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingBook", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateAccountingBookInput!");

            var fragment = new GraphQLQueryFragment("createAccountingBook", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AccountingBookGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingBook", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateAccountingBookInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateAccountingBook", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

       

        public void CleanUpControls()
        {
            AccountingBookId = 0;
            Name = string.Empty;
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => Name);
            ValidateProperties();
            this.AcceptChanges();
        }



        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return new List<object>();
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
                    case nameof(Name):
                        if (string.IsNullOrEmpty(Name)) AddError(propertyName, "El campo 'Nombre del libro contable' no puede estar vacío.");
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
            ValidateProperty(nameof(Name), Name);
        }

    }
}
