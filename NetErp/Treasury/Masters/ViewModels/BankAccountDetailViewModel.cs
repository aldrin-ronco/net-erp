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
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Treasury.Masters.Validators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Treasury.Masters.ViewModels
{
    /// <summary>
    /// ViewModel modal para crear/editar una <see cref="BankAccountGraphQLModel"/>.
    /// El parent Bank se pasa vía <see cref="SetForNew"/>; en edición viene dentro del
    /// propio modelo. La modalidad (bancaria tradicional vs NEQUI/DAVIPLATA) depende
    /// del <see cref="CaptureTypeEnum"/> del Bank padre.
    /// </summary>
    public class BankAccountDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<BankAccountGraphQLModel> _bankAccountService;
        private readonly IEventAggregator _eventAggregator;
        private readonly AuxiliaryAccountingAccountCache _accountingAccountCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly BankAccountValidator _validator;

        #endregion

        #region MaxLength Properties

        public int NumberMaxLength => _stringLengthCache.GetMaxLength<BankAccountGraphQLModel>(nameof(BankAccountGraphQLModel.Number));
        public int ReferenceMaxLength => _stringLengthCache.GetMaxLength<BankAccountGraphQLModel>(nameof(BankAccountGraphQLModel.Reference));

        #endregion

        #region Dialog Size

        public double DialogWidth
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(DialogWidth)); } }
        } = 650;

        public double DialogHeight
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(DialogHeight)); } }
        } = 550;

        #endregion

        #region LookUp Sources

        public ReadOnlyObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts => _accountingAccountCache.Items;

        #endregion

        #region Properties

        private readonly Dictionary<string, List<string>> _errors = [];

        public bool IsBusy
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(IsBusy)); } }
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
                    NotifyOfPropertyChange(nameof(ShowAutoCreateDescription));
                    NotifyOfPropertyChange(nameof(ShowAccountingAccountLookUp));
                }
            }
        }

        public string BankName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(BankName));
                    NotifyOfPropertyChange(nameof(Description));
                    NotifyOfPropertyChange(nameof(PaymentMethodName));
                }
            }
        } = string.Empty;

        [ExpandoPath("bankId")]
        public int BankId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(BankId));
                    this.TrackChange(nameof(BankId), value);
                }
            }
        }

        public CaptureTypeEnum BankCaptureType
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(BankCaptureType));
                    NotifyOfPropertyChange(nameof(IsTraditionalBank));
                    NotifyOfPropertyChange(nameof(IsDigitalWallet));
                }
            }
        } = CaptureTypeEnum.PJ;

        public bool IsTraditionalBank => BankCaptureType == CaptureTypeEnum.PJ;
        public bool IsDigitalWallet => BankCaptureType == CaptureTypeEnum.PN;

        public string Type
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Type));
                    NotifyOfPropertyChange(nameof(Description));
                    NotifyOfPropertyChange(nameof(PaymentMethodName));
                    this.TrackChange(nameof(Type), value);
                    this.TrackChange(nameof(Description), Description);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = "A";

        public string Number
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Number));
                    NotifyOfPropertyChange(nameof(Description));
                    NotifyOfPropertyChange(nameof(PaymentMethodName));
                    ValidateProperty(nameof(BankAccountValidationContext.Number));
                    this.TrackChange(nameof(Number), value);
                    this.TrackChange(nameof(Description), Description);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public new bool IsActive
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.TrackChange(nameof(IsActive), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = true;

        public string Reference
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Reference));
                    NotifyOfPropertyChange(nameof(Description));
                    this.TrackChange(nameof(Reference), value);
                    this.TrackChange(nameof(Description), Description);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public int DisplayOrder
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DisplayOrder));
                    this.TrackChange(nameof(DisplayOrder), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public string Provider
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Provider));
                    NotifyOfPropertyChange(nameof(Description));
                    NotifyOfPropertyChange(nameof(PaymentMethodName));
                    this.TrackChange(nameof(Provider), value);
                    this.TrackChange(nameof(Description), Description);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public bool EnablePaymentMethod
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(EnablePaymentMethod));
                    this.TrackChange(nameof(EnablePaymentMethod), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public string PaymentMethodAbbreviation
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(PaymentMethodAbbreviation)); } }
        } = string.Empty;

        public bool AccountingAccountAutoCreate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountAutoCreate));
                    NotifyOfPropertyChange(nameof(ShowAutoCreateDescription));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = true;

        public bool AccountingAccountSelectExisting
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountSelectExisting));
                    NotifyOfPropertyChange(nameof(ShowAccountingAccountLookUp));
                    if (!value) SelectedAccountingAccount = null;
                    ValidateProperty(nameof(BankAccountValidationContext.AccountingAccountId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool ShowAutoCreateDescription => IsNewRecord && AccountingAccountAutoCreate;
        public bool ShowAccountingAccountLookUp => !IsNewRecord || AccountingAccountSelectExisting;

        [ExpandoPath("accountingAccountId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedAccountingAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingAccount));
                    this.TrackChange(nameof(SelectedAccountingAccount), value);
                    ValidateProperty(nameof(BankAccountValidationContext.AccountingAccountId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public string Description
        {
            get
            {
                if (IsTraditionalBank)
                {
                    string typeLabel = Type == "A" ? "CTA. DE AHORROS" : "CTA. CORRIENTE";
                    string reference = string.IsNullOrEmpty(Reference) ? "" : $" - RF. {Reference}";
                    return $"{BankName} [{typeLabel} No. {Number}]{reference}".Trim();
                }
                string walletName = Provider == "N" ? "NEQUI" : "DAVIPLATA";
                string walletRef = string.IsNullOrEmpty(Reference) ? "" : $" - RF. {Reference}";
                return $"{walletName} - {Number}{walletRef}";
            }
        }

        public string PaymentMethodName
        {
            get
            {
                if (IsTraditionalBank)
                {
                    string typeLabel = Type == "A" ? "CTA. DE AHORROS" : "CUENTA CORRIENTE";
                    string tail = Number.Length > 5 ? $"* {Number[^5..]}" : string.Empty;
                    return $"TRANSF/CONSIG EN {BankName.Trim()} EN {typeLabel} TERMINADA EN {tail}";
                }
                string walletName = Provider == "N" ? "NEQUI" : "DAVIPLATA";
                return $"TRANSF/CONSIG EN {walletName} {Number}";
            }
        }

        public bool CanSave => _validator.CanSave(BuildContext(), this.HasChanges(), HasErrors);

        private BankAccountValidationContext BuildContext() => new()
        {
            Number = Number,
            AccountingAccountId = SelectedAccountingAccount?.Id ?? 0,
            AccountingAccountSelectExisting = AccountingAccountSelectExisting
        };

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand => _saveCommand ??= new AsyncCommand(SaveAsync);

        private ICommand? _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new AsyncCommand(CancelAsync);

        #endregion

        #region Constructor

        public BankAccountDetailViewModel(
            IRepository<BankAccountGraphQLModel> bankAccountService,
            IEventAggregator eventAggregator,
            AuxiliaryAccountingAccountCache accountingAccountCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            BankAccountValidator validator)
        {
            _bankAccountService = bankAccountService ?? throw new ArgumentNullException(nameof(bankAccountService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _accountingAccountCache = accountingAccountCache ?? throw new ArgumentNullException(nameof(accountingAccountCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _joinableTaskFactory = joinableTaskFactory;
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
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
            if (close) this.AcceptChanges();
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region SetForNew / SetForEdit

        /// <summary>
        /// Inicializa para crear una cuenta bancaria hija del <paramref name="bank"/> dado.
        /// </summary>
        public void SetForNew(BankGraphQLModel bank)
        {
            ArgumentNullException.ThrowIfNull(bank);

            BankId = bank.Id;
            BankName = bank.AccountingEntity?.SearchName ?? string.Empty;
            BankCaptureType = ParseCaptureType(bank.AccountingEntity?.CaptureType);

            Id = 0;
            Type = IsTraditionalBank ? "A" : "M";
            Provider = IsDigitalWallet ? "N" : string.Empty;
            Number = string.Empty;
            IsActive = true;
            Reference = string.Empty;
            DisplayOrder = 0;
            EnablePaymentMethod = false;
            AccountingAccountAutoCreate = true;
            AccountingAccountSelectExisting = false;
            SelectedAccountingAccount = null;

            SeedDefaultValues();
        }

        public void SetForEdit(BankAccountGraphQLModel bankAccount)
        {
            ArgumentNullException.ThrowIfNull(bankAccount);

            BankId = bankAccount.Bank?.Id ?? 0;
            BankName = bankAccount.Bank?.AccountingEntity?.SearchName ?? string.Empty;
            BankCaptureType = ParseCaptureType(bankAccount.Bank?.AccountingEntity?.CaptureType);

            Id = bankAccount.Id;
            Type = bankAccount.Type;
            Provider = bankAccount.Provider;
            Number = bankAccount.Number;
            IsActive = bankAccount.IsActive;
            Reference = bankAccount.Reference;
            DisplayOrder = bankAccount.DisplayOrder;
            EnablePaymentMethod = bankAccount.PaymentMethod != null && bankAccount.PaymentMethod.Id > 0;
            PaymentMethodAbbreviation = bankAccount.PaymentMethod?.Abbreviation ?? string.Empty;
            AccountingAccountAutoCreate = false;
            AccountingAccountSelectExisting = true;
            SelectedAccountingAccount = _accountingAccountCache.Items.FirstOrDefault(a => a.Id == bankAccount.AccountingAccount?.Id);

            SeedCurrentValues();
        }

        private static CaptureTypeEnum ParseCaptureType(string? type) =>
            Enum.TryParse(type ?? "PJ", out CaptureTypeEnum parsed) ? parsed : CaptureTypeEnum.PJ;

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(BankId), BankId);
            this.SeedValue(nameof(Type), Type);
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(DisplayOrder), DisplayOrder);
            this.SeedValue(nameof(Provider), Provider);
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(EnablePaymentMethod), EnablePaymentMethod);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Type), Type);
            this.SeedValue(nameof(Number), Number);
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(Reference), Reference);
            this.SeedValue(nameof(DisplayOrder), DisplayOrder);
            this.SeedValue(nameof(Provider), Provider);
            this.SeedValue(nameof(SelectedAccountingAccount), SelectedAccountingAccount);
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(EnablePaymentMethod), EnablePaymentMethod);
            this.AcceptChanges();
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<BankAccountGraphQLModel> result = await ExecuteSaveAsync();

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
                        ? new BankAccountCreateMessage { CreatedBankAccount = result }
                        : new BankAccountUpdateMessage { UpdatedBankAccount = result },
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

        public async Task<UpsertResponseType<BankAccountGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _bankAccountService.CreateAsync<UpsertResponseType<BankAccountGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _bankAccountService.UpdateAsync<UpsertResponseType<BankAccountGraphQLModel>>(query, variables);
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

        private static Dictionary<string, object> BuildMutationFields()
        {
            return FieldSpec<UpsertResponseType<BankAccountGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "bankAccount", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Type)
                    .Field(e => e.Number)
                    .Field(e => e.IsActive)
                    .Field(e => e.Description)
                    .Field(e => e.Reference)
                    .Field(e => e.DisplayOrder)
                    .Field(e => e.Provider)
                    .Select(e => e.PaymentMethod, pm => pm
                        .Field(p => p.Id)
                        .Field(p => p.Abbreviation)
                        .Field(p => p.Name))
                    .Select(e => e.AccountingAccount, aa => aa
                        .Field(a => a.Id)
                        .Field(a => a.Code)
                        .Field(a => a.Name))
                    .Select(e => e.Bank, b => b
                        .Field(a => a.Id)
                        .Select(a => a.AccountingEntity, ae => ae
                            .Field(x => x.Id)
                            .Field(x => x.SearchName)
                            .Field(x => x.CaptureType))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            GraphQLQueryFragment fragment = new("createBankAccount",
                [new("input", "CreateBankAccountInput!")],
                BuildMutationFields(), "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            GraphQLQueryFragment fragment = new("updateBankAccount",
                [new("data", "UpdateBankAccountInput!"), new("id", "ID!")],
                BuildMutationFields(), "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Validation (INotifyDataErrorInfo)

        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? errors))
                return Enumerable.Empty<string>();
            return errors;
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private void ValidateProperty(string propertyName)
        {
            BankAccountValidationContext context = BuildContext();
            object? value = propertyName switch
            {
                nameof(BankAccountValidationContext.Number) => context.Number,
                nameof(BankAccountValidationContext.AccountingAccountId) => context.AccountingAccountId,
                _ => null
            };
            IReadOnlyList<string> errors = _validator.Validate(propertyName, value, context);
            SetPropertyErrors(propertyName, errors);
        }

        private void ValidateProperties()
        {
            Dictionary<string, IReadOnlyList<string>> all = _validator.ValidateAll(BuildContext());
            foreach (string prop in new[]
            {
                nameof(BankAccountValidationContext.Number),
                nameof(BankAccountValidationContext.AccountingAccountId)
            })
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
