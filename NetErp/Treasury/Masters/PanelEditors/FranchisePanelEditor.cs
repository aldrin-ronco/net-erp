using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using Models.Treasury;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Treasury.Masters.DTO;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Treasury.Masters.PanelEditors
{
    /// <summary>
    /// Panel Editor para la entidad Franchise (Franquicia).
    /// Maneja la lógica de edición, validación y persistencia de franquicias
    /// para procesamiento de tarjetas de crédito/débito.
    /// </summary>
    public class FranchisePanelEditor : TreasuryMastersBasePanelEditor<TreasuryFranchiseMasterTreeDTO, FranchiseGraphQLModel>
    {
        #region Fields

        private readonly IRepository<FranchiseGraphQLModel> _franchiseService;
        private readonly Helpers.Services.INotificationService _notificationService;

        #endregion

        #region Constructor

        public FranchisePanelEditor(
            TreasuryRootMasterViewModel masterContext,
            IRepository<FranchiseGraphQLModel> franchiseService,
            Helpers.Services.INotificationService notificationService)
            : base(masterContext)
        {
            _franchiseService = franchiseService ?? throw new ArgumentNullException(nameof(franchiseService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        #endregion

        #region Properties

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

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
                    this.TrackChange(nameof(Name));
                    ValidateName();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _type = "TC";
        public string Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    NotifyOfPropertyChange(nameof(Type));
                    this.TrackChange(nameof(Type));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private decimal _commissionRate;
        public decimal CommissionRate
        {
            get => _commissionRate;
            set
            {
                if (_commissionRate != value)
                {
                    _commissionRate = value;
                    NotifyOfPropertyChange(nameof(CommissionRate));
                    this.TrackChange(nameof(CommissionRate));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private decimal _reteivaRate;
        public decimal ReteivaRate
        {
            get => _reteivaRate;
            set
            {
                if (_reteivaRate != value)
                {
                    _reteivaRate = value;
                    NotifyOfPropertyChange(nameof(ReteivaRate));
                    this.TrackChange(nameof(ReteivaRate));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private decimal _reteicaRate;
        public decimal ReteicaRate
        {
            get => _reteicaRate;
            set
            {
                if (_reteicaRate != value)
                {
                    _reteicaRate = value;
                    NotifyOfPropertyChange(nameof(ReteicaRate));
                    this.TrackChange(nameof(ReteicaRate));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private decimal _retefteRate;
        public decimal RetefteRate
        {
            get => _retefteRate;
            set
            {
                if (_retefteRate != value)
                {
                    _retefteRate = value;
                    NotifyOfPropertyChange(nameof(RetefteRate));
                    this.TrackChange(nameof(RetefteRate));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private decimal _taxRate;
        public decimal TaxRate
        {
            get => _taxRate;
            set
            {
                if (_taxRate != value)
                {
                    _taxRate = value;
                    NotifyOfPropertyChange(nameof(TaxRate));
                    this.TrackChange(nameof(TaxRate));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _formulaCommission = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_COMISION]/100)";
        public string FormulaCommission
        {
            get => _formulaCommission;
            set
            {
                if (_formulaCommission != value)
                {
                    _formulaCommission = value;
                    NotifyOfPropertyChange(nameof(FormulaCommission));
                    this.TrackChange(nameof(FormulaCommission));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _formulaReteiva = "[VALOR_IVA]*([MARGEN_RETE_IVA]/100)";
        public string FormulaReteiva
        {
            get => _formulaReteiva;
            set
            {
                if (_formulaReteiva != value)
                {
                    _formulaReteiva = value;
                    NotifyOfPropertyChange(nameof(FormulaReteiva));
                    this.TrackChange(nameof(FormulaReteiva));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _formulaReteica = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_ICA]/1000)";
        public string FormulaReteica
        {
            get => _formulaReteica;
            set
            {
                if (_formulaReteica != value)
                {
                    _formulaReteica = value;
                    NotifyOfPropertyChange(nameof(FormulaReteica));
                    this.TrackChange(nameof(FormulaReteica));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _formulaRetefte = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_FUENTE]/100)";
        public string FormulaRetefte
        {
            get => _formulaRetefte;
            set
            {
                if (_formulaRetefte != value)
                {
                    _formulaRetefte = value;
                    NotifyOfPropertyChange(nameof(FormulaRetefte));
                    this.TrackChange(nameof(FormulaRetefte));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private AccountingAccountGraphQLModel? _selectedCommissionAccountingAccount;
        public AccountingAccountGraphQLModel? SelectedCommissionAccountingAccount
        {
            get => _selectedCommissionAccountingAccount;
            set
            {
                if (_selectedCommissionAccountingAccount != value)
                {
                    _selectedCommissionAccountingAccount = value;
                    NotifyOfPropertyChange(nameof(SelectedCommissionAccountingAccount));
                    NotifyOfPropertyChange(nameof(CommissionAccountingAccountId));
                    this.TrackChange(nameof(CommissionAccountingAccountId));
                    ValidateCommissionAccountingAccount();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        [ExpandoPath("commissionAccountingAccountId")]
        public int CommissionAccountingAccountId => SelectedCommissionAccountingAccount?.Id ?? 0;

        private BankAccountGraphQLModel? _selectedBankAccount;
        public BankAccountGraphQLModel? SelectedBankAccount
        {
            get => _selectedBankAccount;
            set
            {
                if (_selectedBankAccount != value)
                {
                    _selectedBankAccount = value;
                    NotifyOfPropertyChange(nameof(SelectedBankAccount));
                    NotifyOfPropertyChange(nameof(BankAccountId));
                    this.TrackChange(nameof(BankAccountId));
                    ValidateBankAccount();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        [ExpandoPath("bankAccountId")]
        public int BankAccountId => SelectedBankAccount?.Id ?? 0;

        private TreasuryFranchiseCostCenterDTO? _selectedCostCenter;
        public TreasuryFranchiseCostCenterDTO? SelectedCostCenter
        {
            get => _selectedCostCenter;
            set
            {
                if (_selectedCostCenter != value)
                {
                    _selectedCostCenter = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenter));
                    NotifyOfPropertyChange(nameof(CostCenterId));
                    this.TrackChange(nameof(CostCenterId));
                    LoadCostCenterSettings();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        [ExpandoPath("costCenterId")]
        public int CostCenterId => SelectedCostCenter?.Id ?? 0;

        public bool IsCostCenterSelected => CostCenterId > 0;

        private List<FranchiseByCostCenterGraphQLModel> _settingsByCostCenter = [];
        public List<FranchiseByCostCenterGraphQLModel> SettingsByCostCenter
        {
            get => _settingsByCostCenter;
            set
            {
                if (_settingsByCostCenter != value)
                {
                    _settingsByCostCenter = value;
                    NotifyOfPropertyChange(nameof(SettingsByCostCenter));
                }
            }
        }

        #region Master Delegations

        public ObservableCollection<AccountingAccountGraphQLModel> FranchiseAccountingAccountsCommission => MasterContext.FranchiseAccountingAccountsCommission;
        public ObservableCollection<BankAccountGraphQLModel> FranchiseBankAccounts => MasterContext.FranchiseBankAccounts;
        public ObservableCollection<TreasuryFranchiseCostCenterDTO> FranchiseCostCenters => MasterContext.FranchiseCostCenters;

        public string FranchiseDecimalsCount => "n2";

        private ICommand? _franchiseResetFormulaCommissionCommand;
        public ICommand FranchiseResetFormulaCommissionCommand
        {
            get
            {
                _franchiseResetFormulaCommissionCommand ??= new RelayCommand(_ => true, _ => FormulaCommission = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_COMISION]/100)");
                return _franchiseResetFormulaCommissionCommand;
            }
        }

        private ICommand? _franchiseResetFormulaReteivaCommand;
        public ICommand FranchiseResetFormulaReteivaCommand
        {
            get
            {
                _franchiseResetFormulaReteivaCommand ??= new RelayCommand(_ => true, _ => FormulaReteiva = "[VALOR_IVA]*([MARGEN_RETE_IVA]/100)");
                return _franchiseResetFormulaReteivaCommand;
            }
        }

        private ICommand? _franchiseResetFormulaReteicaCommand;
        public ICommand FranchiseResetFormulaReteicaCommand
        {
            get
            {
                _franchiseResetFormulaReteicaCommand ??= new RelayCommand(_ => true, _ => FormulaReteica = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_ICA]/1000)");
                return _franchiseResetFormulaReteicaCommand;
            }
        }

        private ICommand? _franchiseResetFormulaRetefteCommand;
        public ICommand FranchiseResetFormulaRetefteCommand
        {
            get
            {
                _franchiseResetFormulaRetefteCommand ??= new RelayCommand(_ => true, _ => FormulaRetefte = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_FUENTE]/100)");
                return _franchiseResetFormulaRetefteCommand;
            }
        }

        private ICommand? _franchiseSimulatorCommand;
        public ICommand FranchiseSimulatorCommand
        {
            get
            {
                _franchiseSimulatorCommand ??= new RelayCommand(CanFranchiseSimulator, FranchiseSimulator);
                return _franchiseSimulatorCommand;
            }
        }

        private void FranchiseSimulator(object p)
        {
            try
            {
                string replacedFormulaCommission = FormulaCommission;
                string replacedFormulaReteiva = FormulaReteiva;
                string replacedFormulaReteica = FormulaReteica;
                string replacedFormulaRetefte = FormulaRetefte;

                decimal simulatedIvaValue = CardValue - (CardValue / (1 + (TaxRate / 100)));
                SimulatedIvaValue = simulatedIvaValue;

                Dictionary<string, decimal> formulaVariables = new()
                {
                    { "VALOR_TARJETA", CardValue },
                    { "MARGEN_COMISION", CommissionRate },
                    { "MARGEN_RETE_IVA", ReteivaRate },
                    { "MARGEN_RETE_ICA", ReteicaRate },
                    { "MARGEN_RETE_FUENTE", RetefteRate },
                    { "VALOR_IVA", simulatedIvaValue }
                };

                foreach (var item in formulaVariables)
                {
                    replacedFormulaCommission = replacedFormulaCommission.Replace($"[{item.Key}]", item.Value.ToString(CultureInfo.InvariantCulture));
                    replacedFormulaReteiva = replacedFormulaReteiva.Replace($"[{item.Key}]", item.Value.ToString(CultureInfo.InvariantCulture));
                    replacedFormulaReteica = replacedFormulaReteica.Replace($"[{item.Key}]", item.Value.ToString(CultureInfo.InvariantCulture));
                    replacedFormulaRetefte = replacedFormulaRetefte.Replace($"[{item.Key}]", item.Value.ToString(CultureInfo.InvariantCulture));
                }

                SimulatedCommission = Convert.ToDecimal(new DataTable().Compute(replacedFormulaCommission, null), CultureInfo.InvariantCulture);
                SimulatedReteiva = Convert.ToDecimal(new DataTable().Compute(replacedFormulaReteiva, null), CultureInfo.InvariantCulture);
                SimulatedReteica = Convert.ToDecimal(new DataTable().Compute(replacedFormulaReteica, null), CultureInfo.InvariantCulture);
                SimulatedRetefte = Convert.ToDecimal(new DataTable().Compute(replacedFormulaRetefte, null), CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"Error al simular la franquicia. \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        private bool CanFranchiseSimulator(object p)
        {
            return CardValue != 0
                && !string.IsNullOrEmpty(FormulaCommission)
                && !string.IsNullOrEmpty(FormulaReteiva)
                && !string.IsNullOrEmpty(FormulaReteica)
                && !string.IsNullOrEmpty(FormulaRetefte);
        }

        #endregion

        // Simulation properties for UI
        private decimal _cardValue;
        public decimal CardValue
        {
            get => _cardValue;
            set
            {
                if (_cardValue != value)
                {
                    _cardValue = value;
                    NotifyOfPropertyChange(nameof(CardValue));
                }
            }
        }

        private decimal _simulatedCommission;
        public decimal SimulatedCommission
        {
            get => _simulatedCommission;
            set
            {
                if (_simulatedCommission != value)
                {
                    _simulatedCommission = value;
                    NotifyOfPropertyChange(nameof(SimulatedCommission));
                }
            }
        }

        private decimal _simulatedReteiva;
        public decimal SimulatedReteiva
        {
            get => _simulatedReteiva;
            set
            {
                if (_simulatedReteiva != value)
                {
                    _simulatedReteiva = value;
                    NotifyOfPropertyChange(nameof(SimulatedReteiva));
                }
            }
        }

        private decimal _simulatedReteica;
        public decimal SimulatedReteica
        {
            get => _simulatedReteica;
            set
            {
                if (_simulatedReteica != value)
                {
                    _simulatedReteica = value;
                    NotifyOfPropertyChange(nameof(SimulatedReteica));
                }
            }
        }

        private decimal _simulatedRetefte;
        public decimal SimulatedRetefte
        {
            get => _simulatedRetefte;
            set
            {
                if (_simulatedRetefte != value)
                {
                    _simulatedRetefte = value;
                    NotifyOfPropertyChange(nameof(SimulatedRetefte));
                }
            }
        }

        private decimal _simulatedIvaValue;
        public decimal SimulatedIvaValue
        {
            get => _simulatedIvaValue;
            set
            {
                if (_simulatedIvaValue != value)
                {
                    _simulatedIvaValue = value;
                    NotifyOfPropertyChange(nameof(SimulatedIvaValue));
                }
            }
        }

        #endregion

        #region CanSave

        public override bool CanSave
        {
            get
            {
                if (!IsEditing) return false;
                if (HasErrors) return false;
                if (string.IsNullOrWhiteSpace(Name)) return false;
                if (SelectedCommissionAccountingAccount == null) return false;
                if (SelectedBankAccount == null) return false;
                if (!this.HasChanges()) return false;
                return true;
            }
        }

        #endregion

        #region Validation Methods

        private void ValidateName()
        {
            ClearErrors(nameof(Name));
            if (string.IsNullOrWhiteSpace(Name))
            {
                AddError(nameof(Name), "El nombre de la franquicia no puede estar vacío");
            }
        }

        private void ValidateBankAccount()
        {
            ClearErrors(nameof(SelectedBankAccount));
            if (SelectedBankAccount == null)
            {
                AddError(nameof(SelectedBankAccount), "Debe seleccionar una cuenta bancaria");
            }
        }

        private void ValidateCommissionAccountingAccount()
        {
            ClearErrors(nameof(SelectedCommissionAccountingAccount));
            if (SelectedCommissionAccountingAccount == null)
            {
                AddError(nameof(SelectedCommissionAccountingAccount), "Debe seleccionar una cuenta contable de comisión");
            }
        }

        public override void ValidateAll()
        {
            ValidateName();
            ValidateBankAccount();
            ValidateCommissionAccountingAccount();
        }

        #endregion

        #region CostCenter Settings

        private void LoadCostCenterSettings()
        {
            if (CostCenterId == 0)
            {
                // Restaurar valores de la franquicia (aplicación general)
                if (OriginalDto != null)
                {
                    CommissionRate = OriginalDto.CommissionRate;
                    ReteivaRate = OriginalDto.ReteivaRate;
                    ReteicaRate = OriginalDto.ReteicaRate;
                    RetefteRate = OriginalDto.RetefteRate;
                    TaxRate = OriginalDto.TaxRate;
                    FormulaCommission = OriginalDto.FormulaCommission;
                    FormulaReteiva = OriginalDto.FormulaReteiva;
                    FormulaReteica = OriginalDto.FormulaReteica;
                    FormulaRetefte = OriginalDto.FormulaRetefte;
                    SelectedBankAccount = MasterContext.FranchiseBankAccounts?.FirstOrDefault(b => b.Id == OriginalDto.BankAccount?.Id);
                    SelectedCommissionAccountingAccount = MasterContext.FranchiseAccountingAccountsCommission?.FirstOrDefault(a => a.Id == OriginalDto.CommissionAccountingAccount?.Id);
                }
                NotifyOfPropertyChange(nameof(IsCostCenterSelected));
                this.AcceptChanges();
                return;
            }

            var setting = SettingsByCostCenter.FirstOrDefault(x => x.CostCenter.Id == CostCenterId);

            if (setting != null)
            {
                CommissionRate = setting.CommissionRate;
                ReteivaRate = setting.ReteivaRate;
                ReteicaRate = setting.ReteicaRate;
                RetefteRate = setting.RetefteRate;
                TaxRate = setting.TaxRate;
                FormulaCommission = setting.FormulaCommission;
                FormulaReteiva = setting.FormulaReteiva;
                FormulaReteica = setting.FormulaReteica;
                FormulaRetefte = setting.FormulaRetefte;
                SelectedBankAccount = MasterContext.FranchiseBankAccounts?.FirstOrDefault(b => b.Id == setting.BankAccount.Id);
                SelectedCommissionAccountingAccount = MasterContext.FranchiseAccountingAccountsCommission?.FirstOrDefault(a => a.Id == setting.CommissionAccountingAccount.Id);
            }
            else
            {
                CommissionRate = 0;
                ReteivaRate = 0;
                ReteicaRate = 0;
                RetefteRate = 0;
                TaxRate = 0;
                FormulaCommission = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_COMISION]/100)";
                FormulaReteiva = "[VALOR_IVA]*([MARGEN_RETE_IVA]/100)";
                FormulaReteica = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_ICA]/1000)";
                FormulaRetefte = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_FUENTE]/100)";
                SelectedBankAccount = null;
                SelectedCommissionAccountingAccount = null;
            }

            NotifyOfPropertyChange(nameof(IsCostCenterSelected));
            this.AcceptChanges();
        }

        #endregion

        #region SetForNew / SetForEdit

        public override void SetForNew(object context)
        {
            OriginalDto = null;
            Id = 0;
            Name = string.Empty;
            Type = "TC";
            CommissionRate = 0;
            ReteivaRate = 0;
            ReteicaRate = 0;
            RetefteRate = 0;
            TaxRate = 0;
            FormulaCommission = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_COMISION]/100)";
            FormulaReteica = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_ICA]/1000)";
            FormulaReteiva = "[VALOR_IVA]*([MARGEN_RETE_IVA]/100)";
            FormulaRetefte = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_FUENTE]/100)";
            SelectedCommissionAccountingAccount = null;
            SelectedBankAccount = null;
            SettingsByCostCenter = [];
            SelectedCostCenter = MasterContext.FranchiseCostCenters?.FirstOrDefault(x => x.Id == 0);
            CardValue = 0;
            SimulatedCommission = 0;
            SimulatedReteiva = 0;
            SimulatedReteica = 0;
            SimulatedRetefte = 0;
            SimulatedIvaValue = 0;

            SeedDefaultValues();
            ClearAllErrors();
            ValidateAll();

            IsEditing = true;
        }

        public override void SetForEdit(object dto)
        {
            if (dto is not TreasuryFranchiseMasterTreeDTO franchiseDTO) return;

            OriginalDto = franchiseDTO;
            Id = franchiseDTO.Id;
            Name = franchiseDTO.Name;
            Type = franchiseDTO.Type;
            CommissionRate = franchiseDTO.CommissionRate;
            ReteivaRate = franchiseDTO.ReteivaRate;
            ReteicaRate = franchiseDTO.ReteicaRate;
            RetefteRate = franchiseDTO.RetefteRate;
            TaxRate = franchiseDTO.TaxRate;
            FormulaCommission = franchiseDTO.FormulaCommission;
            FormulaReteiva = franchiseDTO.FormulaReteiva;
            FormulaReteica = franchiseDTO.FormulaReteica;
            FormulaRetefte = franchiseDTO.FormulaRetefte;

            SelectedCommissionAccountingAccount = MasterContext.FranchiseAccountingAccountsCommission?
                .FirstOrDefault(a => a.Id == franchiseDTO.CommissionAccountingAccount?.Id);
            SelectedBankAccount = MasterContext.FranchiseBankAccounts?
                .FirstOrDefault(b => b.Id == franchiseDTO.BankAccount?.Id);
            SettingsByCostCenter = new List<FranchiseByCostCenterGraphQLModel>(franchiseDTO.FranchisesByCostCenter ?? []);
            SelectedCostCenter = MasterContext.FranchiseCostCenters?.FirstOrDefault(x => x.Id == 0);

            CardValue = 0;
            SimulatedCommission = 0;
            SimulatedReteiva = 0;
            SimulatedReteica = 0;
            SimulatedRetefte = 0;
            SimulatedIvaValue = 0;

            SeedCurrentValues();
            ClearAllErrors();
            ValidateAll();

            IsEditing = false;
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

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<FranchiseGraphQLModel>>
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
                        .Field(b => b.Description))
                    .SelectList(e => e.FranchisesByCostCenter, fs => fs
                        .Field(f => f.Id)
                        .Select(f => f.CostCenter, cc => cc.Field(c => c.Id))
                        .Field(f => f.CommissionRate)
                        .Field(f => f.ReteivaRate)
                        .Field(f => f.ReteicaRate)
                        .Field(f => f.RetefteRate)
                        .Field(f => f.TaxRate)
                        .Select(f => f.BankAccount, ba => ba.Field(b => b.Id))
                        .Field(f => f.FormulaCommission)
                        .Field(f => f.FormulaReteiva)
                        .Field(f => f.FormulaReteica)
                        .Field(f => f.FormulaRetefte)
                        .Select(f => f.Franchise, fr => fr.Field(x => x.Id))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateFranchiseInput!");
            var fragment = new GraphQLQueryFragment("createFranchise", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<FranchiseGraphQLModel>>
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
                        .Field(b => b.Description))
                    .SelectList(e => e.FranchisesByCostCenter, fs => fs
                        .Field(f => f.Id)
                        .Select(f => f.CostCenter, cc => cc.Field(c => c.Id))
                        .Field(f => f.CommissionRate)
                        .Field(f => f.ReteivaRate)
                        .Field(f => f.ReteicaRate)
                        .Field(f => f.RetefteRate)
                        .Field(f => f.TaxRate)
                        .Select(f => f.BankAccount, ba => ba.Field(b => b.Id))
                        .Field(f => f.FormulaCommission)
                        .Field(f => f.FormulaReteiva)
                        .Field(f => f.FormulaReteica)
                        .Field(f => f.FormulaRetefte)
                        .Select(f => f.Franchise, fr => fr.Field(x => x.Id))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var fragment = new GraphQLQueryFragment(
                "updateFranchise",
                [
                    new GraphQLQueryParameter("id", "ID!"),
                    new GraphQLQueryParameter("data", "UpdateFranchiseInput!")
                ],
                fields,
                "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<FranchiseGraphQLModel>> ExecuteSaveAsync()
        {
            string query;
            dynamic variables;

            if (IsNewRecord)
            {
                query = GetCreateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
            }
            else
            {
                query = GetUpdateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;
            }

            return IsNewRecord
                ? await _franchiseService.CreateAsync<UpsertResponseType<FranchiseGraphQLModel>>(query, variables)
                : await _franchiseService.UpdateAsync<UpsertResponseType<FranchiseGraphQLModel>>(query, variables);
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<FranchiseGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new FranchiseCreateMessage { CreatedFranchise = result });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new FranchiseUpdateMessage { UpdatedFranchise = result });
            }
        }

        #endregion

        #region ByCostCenter Operations

        private Dictionary<string, object> GetByCostCenterFields()
        {
            return FieldSpec<UpsertResponseType<FranchiseByCostCenterGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "franchiseByCostCenter", nested: entity => entity
                    .Field(e => e.Id)
                    .Select(e => e.CostCenter, cc => cc.Field(c => c.Id))
                    .Field(e => e.CommissionRate)
                    .Field(e => e.ReteivaRate)
                    .Field(e => e.ReteicaRate)
                    .Field(e => e.RetefteRate)
                    .Field(e => e.TaxRate)
                    .Select(e => e.BankAccount, ba => ba.Field(b => b.Id))
                    .Select(e => e.CommissionAccountingAccount, aa => aa
                        .Field(a => a.Id)
                        .Field(a => a.Code)
                        .Field(a => a.Name))
                    .Field(e => e.FormulaCommission)
                    .Field(e => e.FormulaReteiva)
                    .Field(e => e.FormulaReteica)
                    .Field(e => e.FormulaRetefte)
                    .Select(e => e.Franchise, fr => fr.Field(x => x.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();
        }

        private string GetCreateByCostCenterQuery()
        {
            var fields = GetByCostCenterFields();
            var parameter = new GraphQLQueryParameter("input", "CreateFranchiseByCostCenterInput!");
            var fragment = new GraphQLQueryFragment("createFranchiseByCostCenter", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        private string GetUpdateByCostCenterQuery()
        {
            var fields = GetByCostCenterFields();
            var parameters = new List<GraphQLQueryParameter>
            {
                new("id", "ID!"),
                new("data", "UpdateFranchiseByCostCenterInput!")
            };
            var fragment = new GraphQLQueryFragment("updateFranchiseByCostCenter", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public override async Task<bool> SaveAsync()
        {
            // Casos 1 y 2: crear franquicia o actualizar franquicia general
            if (IsNewRecord || CostCenterId == 0)
            {
                return await base.SaveAsync();
            }

            // Casos 3 y 4: operaciones por centro de costo
            try
            {
                IsBusy = true;
                MasterContext.Refresh();

                var existingSetting = SettingsByCostCenter.FirstOrDefault(x => x.CostCenter.Id == CostCenterId);
                string query;
                dynamic variables;

                string[] excludeProperties = [nameof(Name), nameof(Type)];

                if (existingSetting != null)
                {
                    // Caso 3: ya existe configuración para este centro de costo → update
                    query = GetUpdateByCostCenterQuery();
                    variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData", excludeProperties: excludeProperties);
                    variables.updateResponseId = existingSetting.Id;
                }
                else
                {
                    // Caso 4: primera configuración para este centro de costo → create
                    query = GetCreateByCostCenterQuery();
                    variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput", excludeProperties: excludeProperties);
                    variables.createResponseInput.franchiseId = Id;
                    variables.createResponseInput.costCenterId = CostCenterId;
                }

                var result = existingSetting != null
                    ? await _franchiseService.UpdateAsync<UpsertResponseType<FranchiseByCostCenterGraphQLModel>>(query, variables)
                    : await _franchiseService.CreateAsync<UpsertResponseType<FranchiseByCostCenterGraphQLModel>>(query, variables);

                if (!result.Success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        ThemedMessageBox.Show(
                            title: $"{result.Message}!",
                            text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo",
                            messageBoxButtons: MessageBoxButton.OK,
                            image: MessageBoxImage.Error));
                    return false;
                }

                // Actualizar SettingsByCostCenter
                var entity = result.Entity;
                if (existingSetting != null)
                {
                    existingSetting.CommissionRate = entity.CommissionRate;
                    existingSetting.ReteivaRate = entity.ReteivaRate;
                    existingSetting.ReteicaRate = entity.ReteicaRate;
                    existingSetting.RetefteRate = entity.RetefteRate;
                    existingSetting.TaxRate = entity.TaxRate;
                    existingSetting.BankAccount = entity.BankAccount;
                    existingSetting.CommissionAccountingAccount = entity.CommissionAccountingAccount;
                    existingSetting.FormulaCommission = entity.FormulaCommission;
                    existingSetting.FormulaReteiva = entity.FormulaReteiva;
                    existingSetting.FormulaReteica = entity.FormulaReteica;
                    existingSetting.FormulaRetefte = entity.FormulaRetefte;
                }
                else
                {
                    SettingsByCostCenter.Add(entity);
                }

                _notificationService.ShowSuccess(result.Message);
                IsEditing = false;
                SelectedCostCenter = MasterContext.FranchiseCostCenters?.FirstOrDefault(x => x.Id == 0);
                this.AcceptChanges();
                return true;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                HandleGraphQLError(exGraphQL, nameof(SaveAsync));
                return false;
            }
            catch (Exception ex)
            {
                HandleGenericError(ex, nameof(SaveAsync));
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion
    }
}
