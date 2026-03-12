using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingPresentations.ViewModels
{
    public class AccountingPresentationDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<AccountingPresentationGraphQLModel> _accountingPresentationService;
        private readonly IEventAggregator _eventAggregator;
        private readonly AccountingBookCache _accountingBookCache;

        #endregion

        #region State

        public bool IsNewRecord => Id == 0;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        #endregion

        #region Form Properties

        public int Id { get; set; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
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

        private List<int> _accountingBookIds = [];
        public List<int> AccountingBookIds
        {
            get => _accountingBookIds;
            set
            {
                if (_accountingBookIds != value)
                {
                    _accountingBookIds = value;
                    NotifyOfPropertyChange(nameof(AccountingBookIds));
                    this.TrackChange(nameof(AccountingBookIds));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _allowsClosure;
        public bool AllowsClosure
        {
            get => _allowsClosure;
            set
            {
                if (_allowsClosure != value)
                {
                    _allowsClosure = value;
                    ClosureAccountingBookId = null;
                    NotifyOfPropertyChange(nameof(AllowsClosure));
                    this.TrackChange(nameof(AllowsClosure));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int? _closureAccountingBookId;
        public int? ClosureAccountingBookId
        {
            get => _closureAccountingBookId;
            set
            {
                if (_closureAccountingBookId != value)
                {
                    _closureAccountingBookId = value;
                    NotifyOfPropertyChange(nameof(ClosureAccountingBookId));
                    this.TrackChange(nameof(ClosureAccountingBookId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region Collections

        private ObservableCollection<AccountingBookDTO> _accountingBooks = [];
        public ObservableCollection<AccountingBookDTO> AccountingBooks
        {
            get => _accountingBooks;
            set
            {
                if (_accountingBooks != value)
                {
                    _accountingBooks = value;
                    NotifyOfPropertyChange(nameof(AccountingBooks));
                }
            }
        }

        private ObservableCollection<AccountingBookGraphQLModel> _accountingBooksToClosure = [];
        public ObservableCollection<AccountingBookGraphQLModel> AccountingBooksToClosure
        {
            get => _accountingBooksToClosure;
            set
            {
                if (_accountingBooksToClosure != value)
                {
                    _accountingBooksToClosure = value;
                    NotifyOfPropertyChange(nameof(AccountingBooksToClosure));
                }
            }
        }

        #endregion

        #region Validation (INotifyDataErrorInfo)

        private readonly Dictionary<string, List<string>> _errors = new();

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null!;
            return _errors[propertyName];
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
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
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(Name):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "El nombre es requerido");
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(Name), Name);
        }

        #endregion

        #region Button States

        public bool CanSave => !HasErrors && this.HasChanges()
                               && !string.IsNullOrEmpty(Name)
                               && AccountingBookIds.Count > 0
                               && (!AllowsClosure || ClosureAccountingBookId.HasValue);

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        private ICommand? _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand ??= new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        #endregion

        #region Constructor

        public AccountingPresentationDetailViewModel(
            IRepository<AccountingPresentationGraphQLModel> accountingPresentationService,
            IEventAggregator eventAggregator,
            AccountingBookCache accountingBookCache)
        {
            _accountingPresentationService = accountingPresentationService;
            _eventAggregator = eventAggregator;
            _accountingBookCache = accountingBookCache;
        }

        #endregion

        #region Initialization

        public async Task InitializeAsync()
        {
            await _accountingBookCache.EnsureLoadedAsync();

            var books = _accountingBookCache.Items
                .Select(b => new AccountingBookDTO { Id = b.Id, Name = b.Name, IsChecked = false })
                .ToList();

            AccountingBooks = [.. books];
            AccountingBooksToClosure = [.. _accountingBookCache.Items];
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            if (IsNewRecord)
            {
                this.SeedValue(nameof(AllowsClosure), false);
            }
        }

        #endregion

        #region Load for Edit

        public async Task LoadDataForEditAsync(int id)
        {
            string query = _loadByIdQuery.Value;
            dynamic variables = new ExpandoObject();
            variables.singleItemResponseId = id;

            var entity = await _accountingPresentationService.FindByIdAsync(query, variables);
            PopulateFromEntity(entity);
        }

        private void PopulateFromEntity(AccountingPresentationGraphQLModel entity)
        {
            var checkedIds = entity.AccountingBooks.Select(p => p.Id).ToHashSet();

            var books = _accountingBookCache.Items
                .Select(b => new AccountingBookDTO { Id = b.Id, Name = b.Name, IsChecked = checkedIds.Contains(b.Id) })
                .ToList();

            AccountingBooks = [.. books];
            AccountingBooksToClosure = [.. _accountingBookCache.Items];

            Id = entity.Id;
            Name = entity.Name;
            AllowsClosure = entity.AllowsClosure;
            ClosureAccountingBookId = entity.ClosureAccountingBook?.Id;
            AccountingBookIds = checkedIds.ToList();
            NotifyOfPropertyChange(nameof(IsNewRecord));
            this.AcceptChanges();
        }

        #endregion

        #region Checkbox Toggle

        public void ToggleActive(RoutedEventArgs args)
        {
            List<int> selectedIds = AccountingBooks
                .Where(b => b.IsChecked)
                .Select(b => b.Id!.Value)
                .ToList();

            if (!selectedIds.SequenceEqual(AccountingBookIds))
            {
                AccountingBookIds = [.. selectedIds];
            }
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();

                var excludes = !AllowsClosure ? new[] { nameof(ClosureAccountingBookId) } : null;

                UpsertResponseType<AccountingPresentationGraphQLModel> result = await ExecuteSaveAsync(excludes);
                if (!result.Success)
                {
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new AccountingPresentationCreateMessage { CreatedAccountingPresentation = result }
                        : new AccountingPresentationUpdateMessage { UpdatedAccountingPresentation = result }
                );

                await TryCloseAsync(true);
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!);
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{currentMethod!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<AccountingPresentationGraphQLModel>> ExecuteSaveAsync(string[]? excludes)
        {
            if (IsNewRecord)
            {
                string query = _createQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput", excludeProperties: excludes);
                return await _accountingPresentationService.CreateAsync<UpsertResponseType<AccountingPresentationGraphQLModel>>(query, variables);
            }
            else
            {
                string query = _updateQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData", excludeProperties: excludes);
                variables.updateResponseId = Id;
                return await _accountingPresentationService.UpdateAsync<UpsertResponseType<AccountingPresentationGraphQLModel>>(query, variables);
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<string> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccountingPresentationGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingPresentation", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateAccountingPresentationInput!");
            var fragment = new GraphQLQueryFragment("createAccountingPresentation", [parameter], fields, "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        private static readonly Lazy<string> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccountingPresentationGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingPresentation", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateAccountingPresentationInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateAccountingPresentation", parameters, fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        private static readonly Lazy<string> _loadByIdQuery = new(() =>
        {
            var fields = FieldSpec<AccountingPresentationGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Name)
                .Field(e => e.AllowsClosure)
                .SelectList(e => e.AccountingBooks, acc => acc
                    .Field(c => c.Id)
                    .Field(c => c.Name))
                .Select(e => e.ClosureAccountingBook, acc => acc
                    .Field(c => c.Id)
                    .Field(c => c.Name))
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("accountingPresentation", [parameter], fields, "SingleItemResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });

        #endregion
    }
}
