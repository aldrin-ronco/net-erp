using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Treasury;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NetErp.Treasury.Masters.Validators;
using static Models.Global.GraphQLResponseTypes;
using IDialogService = NetErp.Helpers.IDialogService;

namespace NetErp.Treasury.Masters.ViewModels
{
    /// <summary>
    /// ViewModel modal para crear/editar un <see cref="BankGraphQLModel"/>.
    /// Publica <see cref="BankCreateMessage"/> / <see cref="BankUpdateMessage"/> al guardar.
    /// </summary>
    public class BankDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<BankGraphQLModel> _bankService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogService _dialogService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly BankValidator _validator;

        #endregion

        #region MaxLength Properties

        public int CodeMaxLength => _stringLengthCache.GetMaxLength<BankGraphQLModel>(nameof(BankGraphQLModel.Code));
        public int PaymentMethodPrefixMaxLength => _stringLengthCache.GetMaxLength<BankGraphQLModel>(nameof(BankGraphQLModel.PaymentMethodPrefix));

        #endregion

        #region Dialog Size

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

        public double DialogHeight
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogHeight));
                }
            }
        } = 350;

        #endregion

        #region Properties

        private readonly Dictionary<string, List<string>> _errors = [];

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

        public bool IsNewRecord => Id == 0;

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

        public string Code
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Code));
                    ValidateProperty(nameof(BankValidationContext.Code));
                    this.TrackChange(nameof(Code), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string PaymentMethodPrefix
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PaymentMethodPrefix));
                    ValidateProperty(nameof(BankValidationContext.PaymentMethodPrefix));
                    this.TrackChange(nameof(PaymentMethodPrefix), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = "Z";

        public int AccountingEntityId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingEntityId));
                    this.TrackChange(nameof(AccountingEntityId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public string AccountingEntityName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingEntityName));
                    ValidateProperty(nameof(BankValidationContext.AccountingEntityName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public bool CanSave => _validator.CanSave(BuildContext(), this.HasChanges(), HasErrors);

        private BankValidationContext BuildContext() => new()
        {
            Code = Code,
            PaymentMethodPrefix = PaymentMethodPrefix,
            AccountingEntityName = AccountingEntityName,
            AccountingEntityId = AccountingEntityId
        };

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand => _saveCommand ??= new AsyncCommand(SaveAsync);

        private ICommand? _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new AsyncCommand(CancelAsync);

        private ICommand? _searchAccountingEntityCommand;
        public ICommand SearchAccountingEntityCommand =>
            _searchAccountingEntityCommand ??= new AsyncCommand(SearchAccountingEntityAsync);

        #endregion

        #region Constructor

        public BankDetailViewModel(
            IRepository<BankGraphQLModel> bankService,
            IEventAggregator eventAggregator,
            IDialogService dialogService,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            BankValidator validator)
        {
            _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _joinableTaskFactory = joinableTaskFactory;
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));

            Messenger.Default.Register<ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel>>(
                this,
                SearchWithTwoColumnsGridMessageToken.BankAccountingEntity,
                false,
                OnAccountingEntitySelected);
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Messenger.Default.Unregister(this);
                this.AcceptChanges();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            Id = 0;
            Code = string.Empty;
            AccountingEntityId = 0;
            AccountingEntityName = string.Empty;
            PaymentMethodPrefix = "Z";
            SeedDefaultValues();
        }

        public void SetForEdit(BankGraphQLModel bank)
        {
            Id = bank.Id;
            Code = bank.Code;
            AccountingEntityId = bank.AccountingEntity?.Id ?? 0;
            AccountingEntityName = bank.AccountingEntity?.SearchName ?? string.Empty;
            PaymentMethodPrefix = bank.PaymentMethodPrefix;
            SeedCurrentValues();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(PaymentMethodPrefix), PaymentMethodPrefix);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Code), Code);
            this.SeedValue(nameof(AccountingEntityId), AccountingEntityId);
            this.SeedValue(nameof(PaymentMethodPrefix), PaymentMethodPrefix);
            this.AcceptChanges();
        }

        #endregion

        #region Search AccountingEntity modal

        public async Task SearchAccountingEntityAsync()
        {
            string query = GetSearchAccountingEntityQuery();
            SearchWithTwoColumnsGridViewModel<AccountingEntityGraphQLModel> viewModel = new(
                query,
                fieldHeader1: "NIT",
                fieldHeader2: "Nombre o razón social",
                fieldData1: "IdentificationNumberWithVerificationDigit",
                fieldData2: "SearchName",
                variables: null,
                messageToken: SearchWithTwoColumnsGridMessageToken.BankAccountingEntity,
                dialogService: _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Búsqueda de terceros");
        }

        private void OnAccountingEntitySelected(ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel> message)
        {
            if (message.ReturnedData is null) return;
            AccountingEntityId = message.ReturnedData.Id;
            AccountingEntityName = message.ReturnedData.SearchName;
        }

        private static string GetSearchAccountingEntityQuery()
        {
            var fields = FieldSpec<PageType<AccountingEntityGraphQLModel>>
                .Create()
                .Field(f => f.PageNumber)
                .Field(f => f.TotalEntries)
                .Field(f => f.PageSize)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.IdentificationNumber)
                    .Field(e => e.VerificationDigit)
                    .Field(e => e.SearchName))
                .Build();

            GraphQLQueryFragment fragment = new("accountingEntitiesPage",
                [new("filters", "AccountingEntityFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<BankGraphQLModel> result = await ExecuteSaveAsync();

                if (!result.Success)
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new BankCreateMessage { CreatedBank = result }
                        : new BankUpdateMessage { UpdatedBank = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<BankGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _bankService.CreateAsync<UpsertResponseType<BankGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _bankService.UpdateAsync<UpsertResponseType<BankGraphQLModel>>(query, variables);
                }
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task CancelAsync() => await TryCloseAsync(false);

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<BankGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "bank", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Code)
                    .Field(e => e.PaymentMethodPrefix)
                    .Select(e => e.AccountingEntity, ae => ae
                        .Field(a => a.Id)
                        .Field(a => a.IdentificationNumber)
                        .Field(a => a.VerificationDigit)
                        .Field(a => a.SearchName)
                        .Field(a => a.CaptureType)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            GraphQLQueryFragment fragment = new("createBank",
                [new("input", "CreateBankInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<BankGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "bank", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Code)
                    .Field(e => e.PaymentMethodPrefix)
                    .Select(e => e.AccountingEntity, ae => ae
                        .Field(a => a.Id)
                        .Field(a => a.IdentificationNumber)
                        .Field(a => a.VerificationDigit)
                        .Field(a => a.SearchName)
                        .Field(a => a.CaptureType)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            GraphQLQueryFragment fragment = new("updateBank",
                [new("data", "UpdateBankInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Validation (INotifyDataErrorInfo)

        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? errors))
                return Enumerable.Empty<string>();
            return errors;
        }

        private void ValidateProperty(string propertyName)
        {
            BankValidationContext context = BuildContext();
            object? value = propertyName switch
            {
                nameof(BankValidationContext.Code) => context.Code,
                nameof(BankValidationContext.PaymentMethodPrefix) => context.PaymentMethodPrefix,
                nameof(BankValidationContext.AccountingEntityName) => context.AccountingEntityName,
                _ => null
            };
            IReadOnlyList<string> errors = _validator.Validate(propertyName, value, context);
            SetPropertyErrors(propertyName, errors);
        }

        private void ValidateProperties()
        {
            Dictionary<string, IReadOnlyList<string>> all = _validator.ValidateAll(BuildContext());
            foreach (string prop in new[] { nameof(BankValidationContext.Code), nameof(BankValidationContext.PaymentMethodPrefix), nameof(BankValidationContext.AccountingEntityName) })
            {
                SetPropertyErrors(prop, all.TryGetValue(prop, out IReadOnlyList<string>? errors) ? errors : []);
            }
        }

        private void SetPropertyErrors(string propertyName, IReadOnlyList<string> errors)
        {
            bool hadErrors = _errors.ContainsKey(propertyName);
            if (errors.Count > 0)
                _errors[propertyName] = [.. errors];
            else if (hadErrors)
                _errors.Remove(propertyName);

            if (hadErrors || errors.Count > 0)
                RaiseErrorsChanged(propertyName);
        }

        #endregion
    }
}
