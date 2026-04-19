using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Treasury;
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
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Treasury.Masters.ViewModels
{
    /// <summary>
    /// ViewModel modal para crear/editar una caja mayor (<see cref="CashDrawerGraphQLModel"/> con
    /// <c>IsPettyCash=false, Parent=null</c>). El cost center padre se pasa vía <see cref="SetForNew"/>.
    /// </summary>
    public class MajorCashDrawerDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<CashDrawerGraphQLModel> _cashDrawerService;
        private readonly IEventAggregator _eventAggregator;
        private readonly AuxiliaryAccountingAccountCache _accountingAccountCache;
        private readonly MajorCashDrawerCache _majorCashDrawerCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly MajorCashDrawerValidator _validator;

        #endregion

        #region MaxLength Properties

        public int NameMaxLength => _stringLengthCache.GetMaxLength<CashDrawerGraphQLModel>(nameof(CashDrawerGraphQLModel.Name));

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
        public IEnumerable<CashDrawerGraphQLModel> AutoTransferCashDrawers => _majorCashDrawerCache.Items.Where(m => m.Id != Id);

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
                    NotifyOfPropertyChange(nameof(AutoTransferCashDrawers));
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
                    ValidateProperty(nameof(MajorCashDrawerValidationContext.Name));
                    this.TrackChange(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("costCenterId")]
        public int CostCenterId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CostCenterId));
                    this.TrackChange(nameof(CostCenterId), value);
                }
            }
        }

        public string CostCenterName
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(CostCenterName)); } }
        } = string.Empty;

        public bool CashReviewRequired
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CashReviewRequired));
                    this.TrackChange(nameof(CashReviewRequired), value);
                    if (!value)
                    {
                        AutoAdjustBalance = false;
                        AutoTransfer = false;
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool AutoAdjustBalance
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AutoAdjustBalance));
                    this.TrackChange(nameof(AutoAdjustBalance), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool AutoTransfer
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AutoTransfer));
                    this.TrackChange(nameof(AutoTransfer), value);
                    ValidateProperty(nameof(MajorCashDrawerValidationContext.AutoTransferCashDrawerId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("isPettyCash")]
        public bool IsPettyCash
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsPettyCash));
                    this.TrackChange(nameof(IsPettyCash), value);
                }
            }
        }

        [ExpandoPath("cashAccountingAccountId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedCashAccountingAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCashAccountingAccount));
                    this.TrackChange(nameof(SelectedCashAccountingAccount), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("checkAccountingAccountId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedCheckAccountingAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCheckAccountingAccount));
                    this.TrackChange(nameof(SelectedCheckAccountingAccount), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("cardAccountingAccountId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedCardAccountingAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCardAccountingAccount));
                    this.TrackChange(nameof(SelectedCardAccountingAccount), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public CashDrawerGraphQLModel? SelectedAutoTransferCashDrawer
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedAutoTransferCashDrawer));
                    NotifyOfPropertyChange(nameof(AutoTransferCashDrawerId));
                    this.TrackChange(nameof(AutoTransferCashDrawerId), AutoTransferCashDrawerId);
                    ValidateProperty(nameof(MajorCashDrawerValidationContext.AutoTransferCashDrawerId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("autoTransferCashDrawerId")]
        public int AutoTransferCashDrawerId => AutoTransfer ? (SelectedAutoTransferCashDrawer?.Id ?? 0) : 0;

        public bool CanSave => _validator.CanSave(BuildContext(), this.HasChanges(), HasErrors);

        private MajorCashDrawerValidationContext BuildContext() => new()
        {
            Name = Name,
            AutoTransfer = AutoTransfer,
            AutoTransferCashDrawerId = AutoTransferCashDrawerId
        };

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand => _saveCommand ??= new AsyncCommand(SaveAsync);

        private ICommand? _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new AsyncCommand(CancelAsync);

        #endregion

        #region Constructor

        public MajorCashDrawerDetailViewModel(
            IRepository<CashDrawerGraphQLModel> cashDrawerService,
            IEventAggregator eventAggregator,
            AuxiliaryAccountingAccountCache accountingAccountCache,
            MajorCashDrawerCache majorCashDrawerCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            MajorCashDrawerValidator validator)
        {
            _cashDrawerService = cashDrawerService ?? throw new ArgumentNullException(nameof(cashDrawerService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _accountingAccountCache = accountingAccountCache ?? throw new ArgumentNullException(nameof(accountingAccountCache));
            _majorCashDrawerCache = majorCashDrawerCache ?? throw new ArgumentNullException(nameof(majorCashDrawerCache));
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

        public void SetForNew(CostCenterGraphQLModel costCenter)
        {
            ArgumentNullException.ThrowIfNull(costCenter);

            CostCenterId = costCenter.Id;
            CostCenterName = costCenter.Name;

            Id = 0;
            Name = string.Empty;
            CashReviewRequired = false;
            AutoAdjustBalance = false;
            AutoTransfer = false;
            IsPettyCash = false;
            SelectedCashAccountingAccount = null;
            SelectedCheckAccountingAccount = null;
            SelectedCardAccountingAccount = null;
            SelectedAutoTransferCashDrawer = null;

            SeedDefaultValues();
        }

        public void SetForEdit(CashDrawerGraphQLModel cashDrawer)
        {
            ArgumentNullException.ThrowIfNull(cashDrawer);

            CostCenterId = cashDrawer.CostCenter?.Id ?? 0;
            CostCenterName = cashDrawer.CostCenter?.Name ?? string.Empty;

            Id = cashDrawer.Id;
            Name = cashDrawer.Name;
            CashReviewRequired = cashDrawer.CashReviewRequired;
            AutoAdjustBalance = cashDrawer.AutoAdjustBalance;
            AutoTransfer = cashDrawer.AutoTransfer;
            IsPettyCash = cashDrawer.IsPettyCash;

            SelectedCashAccountingAccount = ResolveAccountingAccount(cashDrawer.CashAccountingAccount);
            SelectedCheckAccountingAccount = ResolveAccountingAccount(cashDrawer.CheckAccountingAccount);
            SelectedCardAccountingAccount = ResolveAccountingAccount(cashDrawer.CardAccountingAccount);
            SelectedAutoTransferCashDrawer = _majorCashDrawerCache.Items.FirstOrDefault(c => c.Id == cashDrawer.AutoTransferCashDrawer?.Id);

            SeedCurrentValues();
        }

        /// <summary>
        /// Resuelve una cuenta contable contra el caché de cuentas auxiliares.
        /// DevExpress LookUpEdit requiere que la selección sea una referencia existente
        /// dentro de ItemsSource — devolver un objeto externo hace que el combo aparezca vacío.
        /// <para>
        /// Estrategia:
        /// 1) Si la cuenta ya está en el caché (por Id), devolver la referencia cacheada.
        /// 2) Si no está pero cumple el criterio de auxiliar (código ≥ 8 dígitos, p.ej.
        ///    cuenta autogenerada por el backend al crear la caja y aún no publicada al caché),
        ///    agregarla y devolver la referencia ya cacheada.
        /// 3) Si no cumple el criterio, devolver el objeto recibido sin contaminar el caché.
        /// </para>
        /// La mutación ahora pide Id/Code/Name, por lo que <paramref name="source"/>
        /// siempre viene con datos completos (el problema original de datos incompletos
        /// contaminando el caché ya no aplica).
        /// </summary>
        private AccountingAccountGraphQLModel? ResolveAccountingAccount(AccountingAccountGraphQLModel? source)
        {
            if (source == null || source.Id == 0) return null;
            var found = _accountingAccountCache.Items.FirstOrDefault(a => a.Id == source.Id);
            if (found != null) return found;
            if (!string.IsNullOrEmpty(source.Code) && source.Code.Length >= 8)
            {
                _accountingAccountCache.Add(source);
                return _accountingAccountCache.Items.FirstOrDefault(a => a.Id == source.Id) ?? source;
            }
            return source;
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(CostCenterId), CostCenterId);
            this.SeedValue(nameof(CashReviewRequired), CashReviewRequired);
            this.SeedValue(nameof(AutoAdjustBalance), AutoAdjustBalance);
            this.SeedValue(nameof(AutoTransfer), AutoTransfer);
            this.SeedValue(nameof(IsPettyCash), IsPettyCash);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(CostCenterId), CostCenterId);
            this.SeedValue(nameof(CashReviewRequired), CashReviewRequired);
            this.SeedValue(nameof(AutoAdjustBalance), AutoAdjustBalance);
            this.SeedValue(nameof(AutoTransfer), AutoTransfer);
            this.SeedValue(nameof(IsPettyCash), IsPettyCash);
            this.SeedValue(nameof(SelectedCashAccountingAccount), SelectedCashAccountingAccount);
            this.SeedValue(nameof(SelectedCheckAccountingAccount), SelectedCheckAccountingAccount);
            this.SeedValue(nameof(SelectedCardAccountingAccount), SelectedCardAccountingAccount);
            this.SeedValue(nameof(AutoTransferCashDrawerId), AutoTransferCashDrawerId);
            this.AcceptChanges();
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<CashDrawerGraphQLModel> result = await ExecuteSaveAsync();

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
                        ? new TreasuryCashDrawerCreateMessage { CreatedCashDrawer = result }
                        : new TreasuryCashDrawerUpdateMessage { UpdatedCashDrawer = result },
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

        public async Task<UpsertResponseType<CashDrawerGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _cashDrawerService.CreateAsync<UpsertResponseType<CashDrawerGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _cashDrawerService.UpdateAsync<UpsertResponseType<CashDrawerGraphQLModel>>(query, variables);
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
            return FieldSpec<UpsertResponseType<CashDrawerGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "cashDrawer", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.CashReviewRequired)
                    .Field(e => e.AutoAdjustBalance)
                    .Field(e => e.AutoTransfer)
                    .Field(e => e.IsPettyCash)
                    .Select(e => e.AutoTransferCashDrawer, at => at
                        .Field(a => a.Id)
                        .Field(a => a.Name))
                    .Select(e => e.CostCenter, cc => cc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Select(c => c.CompanyLocation, loc => loc
                            .Field(l => l.Id)))
                    .Select(e => e.CashAccountingAccount, acc => acc
                        .Field(a => a.Id)
                        .Field(a => a.Code)
                        .Field(a => a.Name))
                    .Select(e => e.CheckAccountingAccount, acc => acc
                        .Field(a => a.Id)
                        .Field(a => a.Code)
                        .Field(a => a.Name))
                    .Select(e => e.CardAccountingAccount, acc => acc
                        .Field(a => a.Id)
                        .Field(a => a.Code)
                        .Field(a => a.Name)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            GraphQLQueryFragment fragment = new("createCashDrawer",
                [new("input", "CreateCashDrawerInput!")],
                BuildMutationFields(), "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            GraphQLQueryFragment fragment = new("updateCashDrawer",
                [new("data", "UpdateCashDrawerInput!"), new("id", "ID!")],
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
            MajorCashDrawerValidationContext context = BuildContext();
            object? value = propertyName switch
            {
                nameof(MajorCashDrawerValidationContext.Name) => context.Name,
                nameof(MajorCashDrawerValidationContext.AutoTransferCashDrawerId) => context.AutoTransferCashDrawerId,
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
                nameof(MajorCashDrawerValidationContext.Name),
                nameof(MajorCashDrawerValidationContext.AutoTransferCashDrawerId)
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
