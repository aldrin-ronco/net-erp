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
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Treasury.Masters.ViewModels
{
    /// <summary>
    /// ViewModel modal para crear/editar una <see cref="FranchiseGraphQLModel"/>.
    /// Cubre la configuración general de la franquicia (rates, fórmulas, cuenta comisiones,
    /// cuenta bancaria) + un simulador inline. El flujo de configuración por centro de costo
    /// es manejado por un modal separado.
    /// </summary>
    public class FranchiseDetailViewModel : Screen, INotifyDataErrorInfo
    {
        private const string DefaultFormulaCommission = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_COMISION]/100)";
        private const string DefaultFormulaReteiva = "[VALOR_IVA]*([MARGEN_RETE_IVA]/100)";
        private const string DefaultFormulaReteica = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_ICA]/1000)";
        private const string DefaultFormulaRetefte = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_FUENTE]/100)";

        #region Dependencies

        private readonly IRepository<FranchiseGraphQLModel> _franchiseService;
        private readonly IEventAggregator _eventAggregator;
        private readonly BankAccountCache _bankAccountCache;
        private readonly AuxiliaryAccountingAccountCache _accountingAccountCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly FranchiseValidator _validator;

        #endregion

        #region MaxLength Properties

        public int NameMaxLength => _stringLengthCache.GetMaxLength<FranchiseGraphQLModel>(nameof(FranchiseGraphQLModel.Name));

        #endregion

        #region Dialog Size

        public double DialogWidth
        {
            get;
            set
            {
                if (field != value) { field = value; NotifyOfPropertyChange(nameof(DialogWidth)); }
            }
        } = 900;

        public double DialogHeight
        {
            get;
            set
            {
                if (field != value) { field = value; NotifyOfPropertyChange(nameof(DialogHeight)); }
            }
        } = 650;

        #endregion

        #region LookUp Sources

        public ReadOnlyObservableCollection<BankAccountGraphQLModel> BankAccounts => _bankAccountCache.Items;
        public ReadOnlyObservableCollection<AccountingAccountGraphQLModel> CommissionAccountingAccounts => _accountingAccountCache.Items;

        public IReadOnlyList<FranchiseTypeOption> FranchiseTypes { get; } =
        [
            new("TC", "Tarjeta Crédito"),
            new("TD", "Tarjeta Débito")
        ];

        public string FranchiseDecimalsMask => "n2";

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
                    ValidateProperty(nameof(FranchiseValidationContext.Name));
                    this.TrackChange(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Type
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Type));
                    this.TrackChange(nameof(Type), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = "TC";

        public decimal CommissionRate
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(CommissionRate)); this.TrackChange(nameof(CommissionRate), value); NotifyOfPropertyChange(nameof(CanSave)); } }
        }

        public decimal ReteivaRate
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(ReteivaRate)); this.TrackChange(nameof(ReteivaRate), value); NotifyOfPropertyChange(nameof(CanSave)); } }
        }

        public decimal ReteicaRate
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(ReteicaRate)); this.TrackChange(nameof(ReteicaRate), value); NotifyOfPropertyChange(nameof(CanSave)); } }
        }

        public decimal RetefteRate
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(RetefteRate)); this.TrackChange(nameof(RetefteRate), value); NotifyOfPropertyChange(nameof(CanSave)); } }
        }

        public decimal TaxRate
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(TaxRate)); this.TrackChange(nameof(TaxRate), value); NotifyOfPropertyChange(nameof(CanSave)); } }
        }

        public string FormulaCommission
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(FormulaCommission)); this.TrackChange(nameof(FormulaCommission), value); NotifyOfPropertyChange(nameof(CanSave)); } }
        } = DefaultFormulaCommission;

        public string FormulaReteiva
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(FormulaReteiva)); this.TrackChange(nameof(FormulaReteiva), value); NotifyOfPropertyChange(nameof(CanSave)); } }
        } = DefaultFormulaReteiva;

        public string FormulaReteica
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(FormulaReteica)); this.TrackChange(nameof(FormulaReteica), value); NotifyOfPropertyChange(nameof(CanSave)); } }
        } = DefaultFormulaReteica;

        public string FormulaRetefte
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(FormulaRetefte)); this.TrackChange(nameof(FormulaRetefte), value); NotifyOfPropertyChange(nameof(CanSave)); } }
        } = DefaultFormulaRetefte;

        public AccountingAccountGraphQLModel? SelectedCommissionAccountingAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCommissionAccountingAccount));
                    NotifyOfPropertyChange(nameof(CommissionAccountingAccountId));
                    ValidateProperty(nameof(FranchiseValidationContext.CommissionAccountingAccountId));
                    this.TrackChange(nameof(CommissionAccountingAccountId), CommissionAccountingAccountId);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("commissionAccountingAccountId")]
        public int CommissionAccountingAccountId => SelectedCommissionAccountingAccount?.Id ?? 0;

        public BankAccountGraphQLModel? SelectedBankAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedBankAccount));
                    NotifyOfPropertyChange(nameof(BankAccountId));
                    ValidateProperty(nameof(FranchiseValidationContext.BankAccountId));
                    this.TrackChange(nameof(BankAccountId), BankAccountId);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("bankAccountId")]
        public int BankAccountId => SelectedBankAccount?.Id ?? 0;

        public bool CanSave => _validator.CanSave(BuildContext(), this.HasChanges(), HasErrors);

        private FranchiseValidationContext BuildContext() => new()
        {
            Name = Name,
            CommissionAccountingAccountId = CommissionAccountingAccountId,
            BankAccountId = BankAccountId
        };

        #endregion

        #region Simulator Properties

        public decimal CardValue
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(CardValue)); } }
        }

        public decimal SimulatedIvaValue
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(SimulatedIvaValue)); } }
        }

        public decimal SimulatedCommission
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(SimulatedCommission)); } }
        }

        public decimal SimulatedReteiva
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(SimulatedReteiva)); } }
        }

        public decimal SimulatedReteica
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(SimulatedReteica)); } }
        }

        public decimal SimulatedRetefte
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(SimulatedRetefte)); } }
        }

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand => _saveCommand ??= new AsyncCommand(SaveAsync);

        private ICommand? _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new AsyncCommand(CancelAsync);

        private ICommand? _resetFormulaCommissionCommand;
        public ICommand ResetFormulaCommissionCommand => _resetFormulaCommissionCommand ??=
            new RelayCommand(_ => true, _ => FormulaCommission = DefaultFormulaCommission);

        private ICommand? _resetFormulaReteivaCommand;
        public ICommand ResetFormulaReteivaCommand => _resetFormulaReteivaCommand ??=
            new RelayCommand(_ => true, _ => FormulaReteiva = DefaultFormulaReteiva);

        private ICommand? _resetFormulaReteicaCommand;
        public ICommand ResetFormulaReteicaCommand => _resetFormulaReteicaCommand ??=
            new RelayCommand(_ => true, _ => FormulaReteica = DefaultFormulaReteica);

        private ICommand? _resetFormulaRetefteCommand;
        public ICommand ResetFormulaRetefteCommand => _resetFormulaRetefteCommand ??=
            new RelayCommand(_ => true, _ => FormulaRetefte = DefaultFormulaRetefte);

        private ICommand? _simulatorCommand;
        public ICommand SimulatorCommand => _simulatorCommand ??=
            new RelayCommand(CanSimulate, Simulate);

        #endregion

        #region Constructor

        public FranchiseDetailViewModel(
            IRepository<FranchiseGraphQLModel> franchiseService,
            IEventAggregator eventAggregator,
            BankAccountCache bankAccountCache,
            AuxiliaryAccountingAccountCache accountingAccountCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            FranchiseValidator validator)
        {
            _franchiseService = franchiseService ?? throw new ArgumentNullException(nameof(franchiseService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _bankAccountCache = bankAccountCache ?? throw new ArgumentNullException(nameof(bankAccountCache));
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

        public void SetForNew()
        {
            Id = 0;
            Name = string.Empty;
            Type = "TC";
            CommissionRate = 0;
            ReteivaRate = 0;
            ReteicaRate = 0;
            RetefteRate = 0;
            TaxRate = 0;
            FormulaCommission = DefaultFormulaCommission;
            FormulaReteiva = DefaultFormulaReteiva;
            FormulaReteica = DefaultFormulaReteica;
            FormulaRetefte = DefaultFormulaRetefte;
            SelectedCommissionAccountingAccount = null;
            SelectedBankAccount = null;
            ResetSimulator();
            SeedDefaultValues();
        }

        public void SetForEdit(FranchiseGraphQLModel franchise)
        {
            Id = franchise.Id;
            Name = franchise.Name;
            Type = string.IsNullOrEmpty(franchise.Type) ? "TC" : franchise.Type;
            CommissionRate = franchise.CommissionRate;
            ReteivaRate = franchise.ReteivaRate;
            ReteicaRate = franchise.ReteicaRate;
            RetefteRate = franchise.RetefteRate;
            TaxRate = franchise.TaxRate;
            FormulaCommission = string.IsNullOrEmpty(franchise.FormulaCommission) ? DefaultFormulaCommission : franchise.FormulaCommission;
            FormulaReteiva = string.IsNullOrEmpty(franchise.FormulaReteiva) ? DefaultFormulaReteiva : franchise.FormulaReteiva;
            FormulaReteica = string.IsNullOrEmpty(franchise.FormulaReteica) ? DefaultFormulaReteica : franchise.FormulaReteica;
            FormulaRetefte = string.IsNullOrEmpty(franchise.FormulaRetefte) ? DefaultFormulaRetefte : franchise.FormulaRetefte;
            SelectedCommissionAccountingAccount = _accountingAccountCache.Items.FirstOrDefault(a => a.Id == franchise.CommissionAccountingAccount?.Id);
            SelectedBankAccount = _bankAccountCache.Items.FirstOrDefault(b => b.Id == franchise.BankAccount?.Id);
            ResetSimulator();
            SeedCurrentValues();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(Type), Type);
            this.SeedValue(nameof(CommissionRate), CommissionRate);
            this.SeedValue(nameof(ReteivaRate), ReteivaRate);
            this.SeedValue(nameof(ReteicaRate), ReteicaRate);
            this.SeedValue(nameof(RetefteRate), RetefteRate);
            this.SeedValue(nameof(TaxRate), TaxRate);
            this.SeedValue(nameof(FormulaCommission), FormulaCommission);
            this.SeedValue(nameof(FormulaReteiva), FormulaReteiva);
            this.SeedValue(nameof(FormulaReteica), FormulaReteica);
            this.SeedValue(nameof(FormulaRetefte), FormulaRetefte);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(Type), Type);
            this.SeedValue(nameof(CommissionRate), CommissionRate);
            this.SeedValue(nameof(ReteivaRate), ReteivaRate);
            this.SeedValue(nameof(ReteicaRate), ReteicaRate);
            this.SeedValue(nameof(RetefteRate), RetefteRate);
            this.SeedValue(nameof(TaxRate), TaxRate);
            this.SeedValue(nameof(FormulaCommission), FormulaCommission);
            this.SeedValue(nameof(FormulaReteiva), FormulaReteiva);
            this.SeedValue(nameof(FormulaReteica), FormulaReteica);
            this.SeedValue(nameof(FormulaRetefte), FormulaRetefte);
            this.SeedValue(nameof(CommissionAccountingAccountId), CommissionAccountingAccountId);
            this.SeedValue(nameof(BankAccountId), BankAccountId);
            this.AcceptChanges();
        }

        private void ResetSimulator()
        {
            CardValue = 0;
            SimulatedIvaValue = 0;
            SimulatedCommission = 0;
            SimulatedReteiva = 0;
            SimulatedReteica = 0;
            SimulatedRetefte = 0;
        }

        #endregion

        #region Simulator

        private bool CanSimulate(object? _) =>
            CardValue != 0
            && !string.IsNullOrEmpty(FormulaCommission)
            && !string.IsNullOrEmpty(FormulaReteiva)
            && !string.IsNullOrEmpty(FormulaReteica)
            && !string.IsNullOrEmpty(FormulaRetefte);

        private void Simulate(object? _)
        {
            try
            {
                decimal ivaValue = TaxRate == 0 ? 0 : CardValue - (CardValue / (1 + (TaxRate / 100)));
                SimulatedIvaValue = ivaValue;

                Dictionary<string, decimal> variables = new()
                {
                    { "VALOR_TARJETA", CardValue },
                    { "MARGEN_COMISION", CommissionRate },
                    { "MARGEN_RETE_IVA", ReteivaRate },
                    { "MARGEN_RETE_ICA", ReteicaRate },
                    { "MARGEN_RETE_FUENTE", RetefteRate },
                    { "VALOR_IVA", ivaValue }
                };

                SimulatedCommission = EvaluateFormula(FormulaCommission, variables);
                SimulatedReteiva = EvaluateFormula(FormulaReteiva, variables);
                SimulatedReteica = EvaluateFormula(FormulaReteica, variables);
                SimulatedRetefte = EvaluateFormula(FormulaRetefte, variables);
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al simular la franquicia. \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
        }

        private static decimal EvaluateFormula(string formula, Dictionary<string, decimal> variables)
        {
            string replaced = formula;
            foreach (KeyValuePair<string, decimal> kv in variables)
                replaced = replaced.Replace($"[{kv.Key}]", kv.Value.ToString(CultureInfo.InvariantCulture));
            return Convert.ToDecimal(new DataTable().Compute(replaced, null), CultureInfo.InvariantCulture);
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<FranchiseGraphQLModel> result = await ExecuteSaveAsync();

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
                        ? new FranchiseCreateMessage { CreatedFranchise = result }
                        : new FranchiseUpdateMessage { UpdatedFranchise = result },
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

        public async Task<UpsertResponseType<FranchiseGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _franchiseService.CreateAsync<UpsertResponseType<FranchiseGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _franchiseService.UpdateAsync<UpsertResponseType<FranchiseGraphQLModel>>(query, variables);
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

        private static FieldSpec<UpsertResponseType<FranchiseGraphQLModel>> FranchiseMutationFields() =>
            FieldSpec<UpsertResponseType<FranchiseGraphQLModel>>
                .Create();

        private static Dictionary<string, object> BuildMutationFields()
        {
            return FieldSpec<UpsertResponseType<FranchiseGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "franchise", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Type)
                    .Field(e => e.CommissionRate)
                    .Field(e => e.ReteivaRate)
                    .Field(e => e.ReteicaRate)
                    .Field(e => e.RetefteRate)
                    .Field(e => e.TaxRate)
                    .Field(e => e.FormulaCommission)
                    .Field(e => e.FormulaReteiva)
                    .Field(e => e.FormulaReteica)
                    .Field(e => e.FormulaRetefte)
                    .Select(e => e.CommissionAccountingAccount, aa => aa
                        .Field(a => a.Id)
                        .Field(a => a.Code)
                        .Field(a => a.Name))
                    .Select(e => e.BankAccount, ba => ba
                        .Field(b => b.Id)
                        .Field(b => b.Description)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            GraphQLQueryFragment fragment = new("createFranchise",
                [new("input", "CreateFranchiseInput!")],
                BuildMutationFields(), "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            GraphQLQueryFragment fragment = new("updateFranchise",
                [new("id", "ID!"), new("data", "UpdateFranchiseInput!")],
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
            FranchiseValidationContext context = BuildContext();
            object? value = propertyName switch
            {
                nameof(FranchiseValidationContext.Name) => context.Name,
                nameof(FranchiseValidationContext.CommissionAccountingAccountId) => context.CommissionAccountingAccountId,
                nameof(FranchiseValidationContext.BankAccountId) => context.BankAccountId,
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
                nameof(FranchiseValidationContext.Name),
                nameof(FranchiseValidationContext.CommissionAccountingAccountId),
                nameof(FranchiseValidationContext.BankAccountId)
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

    public class FranchiseTypeOption
    {
        public string Code { get; }
        public string Display { get; }
        public FranchiseTypeOption(string code, string display) { Code = code; Display = display; }
    }
}
