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
    /// ViewModel modal para crear/editar una caja auxiliar (<see cref="CashDrawerGraphQLModel"/> con
    /// <c>Parent != null</c>). La caja mayor padre se pasa vía <see cref="SetForNew"/>.
    /// </summary>
    public class AuxiliaryCashDrawerDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<CashDrawerGraphQLModel> _cashDrawerService;
        private readonly IEventAggregator _eventAggregator;
        private readonly AuxiliaryAccountingAccountCache _accountingAccountCache;
        private readonly AuxiliaryCashDrawerCache _auxiliaryCashDrawerCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly AuxiliaryCashDrawerValidator _validator;

        #endregion

        #region MaxLength Properties

        public int NameMaxLength => _stringLengthCache.GetMaxLength<CashDrawerGraphQLModel>(nameof(CashDrawerGraphQLModel.Name));
        public int ComputerNameMaxLength => _stringLengthCache.GetMaxLength<CashDrawerGraphQLModel>(nameof(CashDrawerGraphQLModel.ComputerName));

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
        } = 600;

        #endregion

        #region LookUp Sources

        public ReadOnlyObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts => _accountingAccountCache.Items;

        /// <summary>
        /// Destinos posibles de auto-transferencia: otras cajas auxiliares del sistema (excluyendo self).
        /// </summary>
        public IEnumerable<CashDrawerGraphQLModel> AutoTransferCashDrawers =>
            _auxiliaryCashDrawerCache.Items.Where(c => c.Id != Id);

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
                    ValidateProperty(nameof(AuxiliaryCashDrawerValidationContext.Name));
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

        [ExpandoPath("parentId")]
        public int ParentId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ParentId));
                    this.TrackChange(nameof(ParentId), value);
                }
            }
        }

        public string ParentName
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(ParentName)); } }
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
                    ValidateProperty(nameof(AuxiliaryCashDrawerValidationContext.AutoTransferCashDrawerId));
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

        public string ComputerName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ComputerName));
                    ValidateProperty(nameof(AuxiliaryCashDrawerValidationContext.ComputerName));
                    this.TrackChange(nameof(ComputerName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

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
                    ValidateProperty(nameof(AuxiliaryCashDrawerValidationContext.AutoTransferCashDrawerId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("autoTransferCashDrawerId")]
        public int AutoTransferCashDrawerId => AutoTransfer ? (SelectedAutoTransferCashDrawer?.Id ?? 0) : 0;

        public bool CanSave => _validator.CanSave(BuildContext(), this.HasChanges(), HasErrors);

        private AuxiliaryCashDrawerValidationContext BuildContext() => new()
        {
            Name = Name,
            ComputerName = ComputerName,
            AutoTransfer = AutoTransfer,
            AutoTransferCashDrawerId = AutoTransferCashDrawerId
        };

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand => _saveCommand ??= new AsyncCommand(SaveAsync);

        private ICommand? _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new AsyncCommand(CancelAsync);

        private ICommand? _useThisComputerCommand;
        public ICommand UseThisComputerCommand => _useThisComputerCommand ??=
            new RelayCommand(_ => true, _ => ComputerName = SessionInfo.GetComputerName());

        #endregion

        #region Constructor

        public AuxiliaryCashDrawerDetailViewModel(
            IRepository<CashDrawerGraphQLModel> cashDrawerService,
            IEventAggregator eventAggregator,
            AuxiliaryAccountingAccountCache accountingAccountCache,
            AuxiliaryCashDrawerCache auxiliaryCashDrawerCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            AuxiliaryCashDrawerValidator validator)
        {
            _cashDrawerService = cashDrawerService ?? throw new ArgumentNullException(nameof(cashDrawerService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _accountingAccountCache = accountingAccountCache ?? throw new ArgumentNullException(nameof(accountingAccountCache));
            _auxiliaryCashDrawerCache = auxiliaryCashDrawerCache ?? throw new ArgumentNullException(nameof(auxiliaryCashDrawerCache));
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
        /// Inicializa para crear una caja auxiliar hija de la <paramref name="majorCashDrawer"/> dada.
        /// </summary>
        public void SetForNew(CashDrawerGraphQLModel majorCashDrawer)
        {
            ArgumentNullException.ThrowIfNull(majorCashDrawer);

            ParentId = majorCashDrawer.Id;
            ParentName = majorCashDrawer.Name;
            CostCenterId = majorCashDrawer.CostCenter?.Id ?? 0;

            Id = 0;
            CashReviewRequired = false;
            AutoAdjustBalance = false;
            AutoTransfer = false;
            IsPettyCash = false;
            SelectedCashAccountingAccount = null;
            SelectedCheckAccountingAccount = null;
            SelectedCardAccountingAccount = null;
            SelectedAutoTransferCashDrawer = null;

            SeedDefaultValues();

            // Name y ComputerName después del seeding para que el tracker
            // registre los defaults como cambios.
            Name = "CAJA AUXILIAR";
            ComputerName = SessionInfo.GetComputerName();
        }

        public void SetForEdit(CashDrawerGraphQLModel cashDrawer)
        {
            ArgumentNullException.ThrowIfNull(cashDrawer);

            ParentId = cashDrawer.Parent?.Id ?? 0;
            ParentName = cashDrawer.Parent?.Name ?? string.Empty;
            CostCenterId = cashDrawer.CostCenter?.Id ?? 0;

            Id = cashDrawer.Id;
            Name = cashDrawer.Name;
            CashReviewRequired = cashDrawer.CashReviewRequired;
            AutoAdjustBalance = cashDrawer.AutoAdjustBalance;
            AutoTransfer = cashDrawer.AutoTransfer;
            IsPettyCash = cashDrawer.IsPettyCash;
            ComputerName = cashDrawer.ComputerName;

            SelectedCashAccountingAccount = _accountingAccountCache.Items.FirstOrDefault(a => a.Id == cashDrawer.CashAccountingAccount?.Id);
            SelectedCheckAccountingAccount = _accountingAccountCache.Items.FirstOrDefault(a => a.Id == cashDrawer.CheckAccountingAccount?.Id);
            SelectedCardAccountingAccount = _accountingAccountCache.Items.FirstOrDefault(a => a.Id == cashDrawer.CardAccountingAccount?.Id);
            SelectedAutoTransferCashDrawer = _auxiliaryCashDrawerCache.Items.FirstOrDefault(c => c.Id == cashDrawer.AutoTransferCashDrawer?.Id);

            SeedCurrentValues();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(CostCenterId), CostCenterId);
            this.SeedValue(nameof(ParentId), ParentId);
            this.SeedValue(nameof(CashReviewRequired), CashReviewRequired);
            this.SeedValue(nameof(AutoAdjustBalance), AutoAdjustBalance);
            this.SeedValue(nameof(AutoTransfer), AutoTransfer);
            this.SeedValue(nameof(IsPettyCash), IsPettyCash);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(ParentId), ParentId);
            this.SeedValue(nameof(CostCenterId), CostCenterId);
            this.SeedValue(nameof(CashReviewRequired), CashReviewRequired);
            this.SeedValue(nameof(AutoAdjustBalance), AutoAdjustBalance);
            this.SeedValue(nameof(AutoTransfer), AutoTransfer);
            this.SeedValue(nameof(IsPettyCash), IsPettyCash);
            this.SeedValue(nameof(ComputerName), ComputerName);
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
                    .Field(e => e.ComputerName)
                    .Select(e => e.AutoTransferCashDrawer, at => at
                        .Field(a => a.Id)
                        .Field(a => a.Name))
                    .Select(e => e.Parent, p => p
                        .Field(a => a.Id)
                        .Field(a => a.Name)
                        .Select(a => a.CostCenter, cc => cc
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            .Select(c => c.CompanyLocation, loc => loc
                                .Field(l => l.Id))))
                    .Select(e => e.CashAccountingAccount, acc => acc
                        .Field(a => a.Id)
                        .Field(a => a.Name))
                    .Select(e => e.CheckAccountingAccount, acc => acc
                        .Field(a => a.Id)
                        .Field(a => a.Name))
                    .Select(e => e.CardAccountingAccount, acc => acc
                        .Field(a => a.Id)
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
            AuxiliaryCashDrawerValidationContext context = BuildContext();
            object? value = propertyName switch
            {
                nameof(AuxiliaryCashDrawerValidationContext.Name) => context.Name,
                nameof(AuxiliaryCashDrawerValidationContext.ComputerName) => context.ComputerName,
                nameof(AuxiliaryCashDrawerValidationContext.AutoTransferCashDrawerId) => context.AutoTransferCashDrawerId,
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
                nameof(AuxiliaryCashDrawerValidationContext.Name),
                nameof(AuxiliaryCashDrawerValidationContext.ComputerName),
                nameof(AuxiliaryCashDrawerValidationContext.AutoTransferCashDrawerId)
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
