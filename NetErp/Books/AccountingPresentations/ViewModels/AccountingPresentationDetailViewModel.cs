using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
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
    public class AccountingPresentationDetailViewModel(
        IRepository<AccountingPresentationGraphQLModel> accountingPresentationService,
        IEventAggregator eventAggregator,
        AccountingBookCache accountingBookCache,
        JoinableTaskFactory joinableTaskFactory,
        StringLengthCache stringLengthCache) : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<AccountingPresentationGraphQLModel> _accountingPresentationService = accountingPresentationService;
        private readonly IEventAggregator _eventAggregator = eventAggregator;
        private readonly AccountingBookCache _accountingBookCache = accountingBookCache;
        private readonly JoinableTaskFactory _joinableTaskFactory = joinableTaskFactory;
        private readonly StringLengthCache _stringLengthCache = stringLengthCache;

        #endregion

        #region State

        public bool IsNewRecord => Id == 0;

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public double DialogWidth
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogWidth));
                }
            }
        } = 500;

        #endregion

        #region Form Properties

        public int Id
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Name));
                    ValidateProperty(nameof(Name), value);
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public List<int> AccountingBookIds
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingBookIds));
                    this.TrackChange(nameof(AccountingBookIds));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = [];

        public bool AllowsClosure
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ClosureAccountingBookId = null;
                    NotifyOfPropertyChange(nameof(AllowsClosure));
                    this.TrackChange(nameof(AllowsClosure));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public int? ClosureAccountingBookId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ClosureAccountingBookId));
                    this.TrackChange(nameof(ClosureAccountingBookId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region Collections

        public ObservableCollection<AccountingBookDTO> AccountingBooks
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingBooks));
                }
            }
        } = [];

        public ObservableCollection<AccountingBookGraphQLModel> AccountingBooksToClosure
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingBooksToClosure));
                }
            }
        } = [];

        #endregion

        #region StringLength Properties

        public int NameMaxLength => _stringLengthCache.GetMaxLength<AccountingPresentationGraphQLModel>(nameof(AccountingPresentationGraphQLModel.Name));

        #endregion

        #region Validation (INotifyDataErrorInfo)

        private readonly Dictionary<string, List<string>> _errors = [];

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? value))
                return Enumerable.Empty<string>();
            return value;
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = [];

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
                RaiseErrorsChanged(propertyName);
            }
            _errors.Remove(propertyName);
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

        #region Initialization

        public async Task InitializeAsync()
        {
            await _accountingBookCache.EnsureLoadedAsync();

            List<AccountingBookDTO> books = [.. _accountingBookCache.Items.Select(b => new AccountingBookDTO { Id = b.Id, Name = b.Name, IsChecked = false })];

            AccountingBooks = [.. books];
            AccountingBooksToClosure = [.. _accountingBookCache.Items];
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(AllowsClosure), AllowsClosure);
            this.AcceptChanges();
        }

        public void SetForEdit(AccountingPresentationGraphQLModel entity)
        {
            HashSet<int> checkedIds = entity.AccountingBooks.Select(p => p.Id).ToHashSet();

            List<AccountingBookDTO> books = [.. _accountingBookCache.Items.Select(b => new AccountingBookDTO { Id = b.Id, Name = b.Name, IsChecked = checkedIds.Contains(b.Id) })];

            AccountingBooks = [.. books];
            AccountingBooksToClosure = [.. _accountingBookCache.Items];

            Id = entity.Id;
            Name = entity.Name;
            AllowsClosure = entity.AllowsClosure;
            ClosureAccountingBookId = entity.ClosureAccountingBook?.Id;
            AccountingBookIds = [.. checkedIds];
            NotifyOfPropertyChange(nameof(IsNewRecord));

            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(AllowsClosure), AllowsClosure);
            this.SeedValue(nameof(ClosureAccountingBookId), ClosureAccountingBookId);
            this.SeedValue(nameof(AccountingBookIds), AccountingBookIds);
            this.AcceptChanges();
        }

        #endregion

        #region Load for Edit

        public async Task LoadDataForEditAsync(int id)
        {
            var (fragment, query) = _loadByIdQuery.Value;
            ExpandoObject variables = new GraphQLVariables()
                .For(fragment, "id", id)
                .Build();

            AccountingPresentationGraphQLModel entity = await _accountingPresentationService.FindByIdAsync(query, variables);
            SetForEdit(entity);
        }

        #endregion

        #region Checkbox Toggle

        public void ToggleActive(RoutedEventArgs args)
        {
            List<int> selectedIds = [.. AccountingBooks
                .Where(b => b.IsChecked)
                .Select(b => b.Id!.Value)];

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

                string[]? excludes = !AllowsClosure ? [nameof(ClosureAccountingBookId)] : null;

                UpsertResponseType<AccountingPresentationGraphQLModel> result = await ExecuteSaveAsync(excludes);
                if (!result.Success)
                {
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
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
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<UpsertResponseType<AccountingPresentationGraphQLModel>> ExecuteSaveAsync(string[]? excludes)
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

            List<GraphQLQueryParameter> parameters =
            [
                new("data", "UpdateAccountingPresentationInput!"),
                new("id", "ID!")
            ];
            var fragment = new GraphQLQueryFragment("updateAccountingPresentation", parameters, fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadByIdQuery = new(() =>
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
                    .Field(c => c!.Id)
                    .Field(c => c!.Name))
                .Build();

            var fragment = new GraphQLQueryFragment("accountingPresentation",
                [new("id", "ID!")], fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
    }
}
