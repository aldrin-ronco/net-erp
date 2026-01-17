using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
using Models.Global;
using Models.Treasury;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Treasury.Masters.DTO;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
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

        #endregion

        #region Constructor

        public FranchisePanelEditor(
            TreasuryRootMasterViewModel masterContext,
            IRepository<FranchiseGraphQLModel> franchiseService)
            : base(masterContext)
        {
            _franchiseService = franchiseService ?? throw new ArgumentNullException(nameof(franchiseService));
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
                }
            }
        }

        private string _name = string.Empty;
        [ExpandoPath("name")]
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
        [ExpandoPath("type")]
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

        private decimal _commissionMargin;
        [ExpandoPath("commissionMargin")]
        public decimal CommissionMargin
        {
            get => _commissionMargin;
            set
            {
                if (_commissionMargin != value)
                {
                    _commissionMargin = value;
                    NotifyOfPropertyChange(nameof(CommissionMargin));
                    this.TrackChange(nameof(CommissionMargin));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private decimal _reteivaMargin;
        [ExpandoPath("reteivaMargin")]
        public decimal ReteivaMargin
        {
            get => _reteivaMargin;
            set
            {
                if (_reteivaMargin != value)
                {
                    _reteivaMargin = value;
                    NotifyOfPropertyChange(nameof(ReteivaMargin));
                    this.TrackChange(nameof(ReteivaMargin));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private decimal _reteicaMargin;
        [ExpandoPath("reteicaMargin")]
        public decimal ReteicaMargin
        {
            get => _reteicaMargin;
            set
            {
                if (_reteicaMargin != value)
                {
                    _reteicaMargin = value;
                    NotifyOfPropertyChange(nameof(ReteicaMargin));
                    this.TrackChange(nameof(ReteicaMargin));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private decimal _retefteMargin;
        [ExpandoPath("retefteMargin")]
        public decimal RetefteMargin
        {
            get => _retefteMargin;
            set
            {
                if (_retefteMargin != value)
                {
                    _retefteMargin = value;
                    NotifyOfPropertyChange(nameof(RetefteMargin));
                    this.TrackChange(nameof(RetefteMargin));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private decimal _ivaMargin;
        [ExpandoPath("ivaMargin")]
        public decimal IvaMargin
        {
            get => _ivaMargin;
            set
            {
                if (_ivaMargin != value)
                {
                    _ivaMargin = value;
                    NotifyOfPropertyChange(nameof(IvaMargin));
                    this.TrackChange(nameof(IvaMargin));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _formulaCommission = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_COMISION]/100)";
        [ExpandoPath("formulaCommission")]
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
        [ExpandoPath("formulaReteiva")]
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
        [ExpandoPath("formulaReteica")]
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
        [ExpandoPath("formulaRetefte")]
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

        private AccountingAccountGraphQLModel? _selectedAccountingAccountCommission;
        public AccountingAccountGraphQLModel? SelectedAccountingAccountCommission
        {
            get => _selectedAccountingAccountCommission;
            set
            {
                if (_selectedAccountingAccountCommission != value)
                {
                    _selectedAccountingAccountCommission = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingAccountCommission));
                    NotifyOfPropertyChange(nameof(AccountingAccountIdCommission));
                    this.TrackChange(nameof(AccountingAccountIdCommission));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        [ExpandoPath("accountingAccountIdCommission")]
        public int AccountingAccountIdCommission => SelectedAccountingAccountCommission?.Id ?? 0;

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
                    MasterContext.RefreshCanSave();
                }
            }
        }

        [ExpandoPath("costCenterId")]
        public int CostCenterId => SelectedCostCenter?.Id ?? 0;

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
                if (SelectedAccountingAccountCommission == null || SelectedAccountingAccountCommission.Id == 0) return false;
                if (SelectedBankAccount == null || SelectedBankAccount.Id == 0) return false;
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

        public override void ValidateAll()
        {
            ValidateName();
        }

        #endregion

        #region SetForNew / SetForEdit

        public override void SetForNew(object context)
        {
            OriginalDto = null;
            Id = 0;
            Name = string.Empty;
            Type = "TC";
            CommissionMargin = 0;
            ReteivaMargin = 0;
            ReteicaMargin = 0;
            RetefteMargin = 0;
            IvaMargin = 0;
            FormulaCommission = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_COMISION]/100)";
            FormulaReteica = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_ICA]/1000)";
            FormulaReteiva = "[VALOR_IVA]*([MARGEN_RETE_IVA]/100)";
            FormulaRetefte = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_FUENTE]/100)";
            SelectedAccountingAccountCommission = MasterContext.FranchiseAccountingAccountsCommission?.FirstOrDefault(x => x.Id == 0);
            SelectedBankAccount = MasterContext.FranchiseBankAccounts?.FirstOrDefault(x => x.Id == 0);
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
            CommissionMargin = franchiseDTO.CommissionMargin;
            ReteivaMargin = franchiseDTO.ReteivaMargin;
            ReteicaMargin = franchiseDTO.ReteicaMargin;
            RetefteMargin = franchiseDTO.RetefteMargin;
            IvaMargin = franchiseDTO.IvaMargin;
            FormulaCommission = franchiseDTO.FormulaCommission;
            FormulaReteiva = franchiseDTO.FormulaReteiva;
            FormulaReteica = franchiseDTO.FormulaReteica;
            FormulaRetefte = franchiseDTO.FormulaRetefte;

            SelectedAccountingAccountCommission = MasterContext.FranchiseAccountingAccountsCommission?
                .FirstOrDefault(a => a.Id == franchiseDTO.AccountingAccountCommission?.Id);
            SelectedBankAccount = MasterContext.FranchiseBankAccounts?
                .FirstOrDefault(b => b.Id == franchiseDTO.BankAccount?.Id);
            SettingsByCostCenter = new List<FranchiseByCostCenterGraphQLModel>(franchiseDTO.FranchiseSettingsByCostCenter ?? []);
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
            this.SeedValue(nameof(CommissionMargin), CommissionMargin);
            this.SeedValue(nameof(ReteivaMargin), ReteivaMargin);
            this.SeedValue(nameof(ReteicaMargin), ReteicaMargin);
            this.SeedValue(nameof(RetefteMargin), RetefteMargin);
            this.SeedValue(nameof(IvaMargin), IvaMargin);
            this.SeedValue(nameof(FormulaCommission), FormulaCommission);
            this.SeedValue(nameof(FormulaReteiva), FormulaReteiva);
            this.SeedValue(nameof(FormulaReteica), FormulaReteica);
            this.SeedValue(nameof(FormulaRetefte), FormulaRetefte);
            this.SeedValue(nameof(AccountingAccountIdCommission), AccountingAccountIdCommission);
            this.SeedValue(nameof(BankAccountId), BankAccountId);
            this.AcceptChanges();
        }

        private void SeedDefaultValues()
        {
            this.SeedValue(nameof(Type), Type);
            this.SeedValue(nameof(CommissionMargin), CommissionMargin);
            this.SeedValue(nameof(ReteivaMargin), ReteivaMargin);
            this.SeedValue(nameof(ReteicaMargin), ReteicaMargin);
            this.SeedValue(nameof(RetefteMargin), RetefteMargin);
            this.SeedValue(nameof(IvaMargin), IvaMargin);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<FranchiseGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Name)
                .Field(e => e.Type)
                .Field(e => e.CommissionMargin)
                .Field(e => e.ReteivaMargin)
                .Field(e => e.ReteicaMargin)
                .Field(e => e.RetefteMargin)
                .Field(e => e.IvaMargin)
                .Field(e => e.FormulaCommission)
                .Field(e => e.FormulaReteiva)
                .Field(e => e.FormulaReteica)
                .Field(e => e.FormulaRetefte)
                .Select(e => e.AccountingAccountCommission, aa => aa
                    .Field(a => a.Id)
                    .Field(a => a.Code)
                    .Field(a => a.Name))
                .Select(e => e.BankAccount, ba => ba
                    .Field(b => b.Id)
                    .Field(b => b.Description))
                .SelectList(e => e.FranchiseSettingsByCostCenter, fs => fs
                    .Field(f => f.Id)
                    .Field(f => f.CostCenterId)
                    .Field(f => f.CommissionMargin)
                    .Field(f => f.ReteivaMargin)
                    .Field(f => f.ReteicaMargin)
                    .Field(f => f.RetefteMargin)
                    .Field(f => f.IvaMargin)
                    .Field(f => f.BankAccountId)
                                        .Field(f => f.FormulaCommission)
                    .Field(f => f.FormulaReteiva)
                    .Field(f => f.FormulaReteica)
                    .Field(f => f.FormulaRetefte)
                    .Field(f => f.FranchiseId))
                .Build();

            var parameter = new GraphQLQueryParameter("data", "CreateFranchiseInput!");
            var fragment = new GraphQLQueryFragment("createFranchise", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<FranchiseGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Name)
                .Field(e => e.Type)
                .Field(e => e.CommissionMargin)
                .Field(e => e.ReteivaMargin)
                .Field(e => e.ReteicaMargin)
                .Field(e => e.RetefteMargin)
                .Field(e => e.IvaMargin)
                .Field(e => e.FormulaCommission)
                .Field(e => e.FormulaReteiva)
                .Field(e => e.FormulaReteica)
                .Field(e => e.FormulaRetefte)
                .Select(e => e.AccountingAccountCommission, aa => aa
                    .Field(a => a.Id)
                    .Field(a => a.Code)
                    .Field(a => a.Name))
                .Select(e => e.BankAccount, ba => ba
                    .Field(b => b.Id)
                    .Field(b => b.Description))
                .SelectList(e => e.FranchiseSettingsByCostCenter, fs => fs
                    .Field(f => f.Id)
                    .Field(f => f.CostCenterId)
                    .Field(f => f.CommissionMargin)
                    .Field(f => f.ReteivaMargin)
                    .Field(f => f.ReteicaMargin)
                    .Field(f => f.RetefteMargin)
                    .Field(f => f.IvaMargin)
                    .Field(f => f.BankAccountId)
                                        .Field(f => f.FormulaCommission)
                    .Field(f => f.FormulaReteiva)
                    .Field(f => f.FormulaReteica)
                    .Field(f => f.FormulaRetefte)
                    .Field(f => f.FranchiseId))
                .Build();

            var fragment = new GraphQLQueryFragment(
                "updateFranchise",
                [
                    new GraphQLQueryParameter("id", "Int!"),
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
            dynamic variables = new ExpandoObject();

            if (IsNewRecord)
            {
                query = GetCreateQuery();
                variables.data = new ExpandoObject();
                variables.data.name = Name.Trim().RemoveExtraSpaces();
                variables.data.type = Type;
                variables.data.commissionMargin = CommissionMargin;
                variables.data.reteivaMargin = ReteivaMargin;
                variables.data.reteicaMargin = ReteicaMargin;
                variables.data.retefteMargin = RetefteMargin;
                variables.data.ivaMargin = IvaMargin;
                variables.data.formulaCommission = FormulaCommission;
                variables.data.formulaReteiva = FormulaReteiva;
                variables.data.formulaReteica = FormulaReteica;
                variables.data.formulaRetefte = FormulaRetefte;
                variables.data.accountingAccountIdCommission = AccountingAccountIdCommission;
                variables.data.bankAccountId = BankAccountId;
                variables.data.companyId = 1; // TODO: Change to correct value
                variables.data.costCenterId = CostCenterId;

                var createResult = await _franchiseService.CreateAsync(query, variables);
                return new UpsertResponseType<FranchiseGraphQLModel> { Entity = createResult };
            }
            else
            {
                query = GetUpdateQuery();
                variables.id = Id;
                variables.data = new ExpandoObject();
                variables.data.name = Name.Trim().RemoveExtraSpaces();
                variables.data.type = Type;
                variables.data.commissionMargin = CommissionMargin;
                variables.data.reteivaMargin = ReteivaMargin;
                variables.data.reteicaMargin = ReteicaMargin;
                variables.data.retefteMargin = RetefteMargin;
                variables.data.ivaMargin = IvaMargin;
                variables.data.formulaCommission = FormulaCommission;
                variables.data.formulaReteiva = FormulaReteiva;
                variables.data.formulaReteica = FormulaReteica;
                variables.data.formulaRetefte = FormulaRetefte;
                variables.data.accountingAccountIdCommission = AccountingAccountIdCommission;
                variables.data.bankAccountId = BankAccountId;
                variables.data.companyId = 1; // TODO: Change to correct value
                variables.data.costCenterId = CostCenterId;

                var updateResult = await _franchiseService.UpdateAsync(query, variables);
                return new UpsertResponseType<FranchiseGraphQLModel> { Entity = updateResult };
            }
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<FranchiseGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new FranchiseCreateMessage { CreatedFranchise = result.Entity });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new FranchiseUpdateMessage { UpdatedFranchise = result.Entity });
            }
        }

        #endregion
    }
}
