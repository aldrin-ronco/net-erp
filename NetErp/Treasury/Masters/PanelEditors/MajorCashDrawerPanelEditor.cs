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
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Treasury.Masters.PanelEditors
{
    /// <summary>
    /// Panel Editor para la entidad Major Cash Drawer (Caja Mayor).
    /// Maneja la lógica de edición, validación y persistencia de cajas mayores.
    /// </summary>
    public class MajorCashDrawerPanelEditor : TreasuryMastersBasePanelEditor<MajorCashDrawerMasterTreeDTO, CashDrawerGraphQLModel>
    {
        #region Fields

        private readonly IRepository<CashDrawerGraphQLModel> _cashDrawerService;

        #endregion

        #region Constructor

        public MajorCashDrawerPanelEditor(
            TreasuryRootMasterViewModel masterContext,
            IRepository<CashDrawerGraphQLModel> cashDrawerService)
            : base(masterContext)
        {
            _cashDrawerService = cashDrawerService ?? throw new ArgumentNullException(nameof(cashDrawerService));
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

        private int _costCenterId;
        [ExpandoPath("costCenterId")]
        public int CostCenterId
        {
            get => _costCenterId;
            set
            {
                if (_costCenterId != value)
                {
                    _costCenterId = value;
                    NotifyOfPropertyChange(nameof(CostCenterId));
                    this.TrackChange(nameof(CostCenterId));
                }
            }
        }

        private string _costCenterName = string.Empty;
        public string CostCenterName
        {
            get => _costCenterName;
            set
            {
                if (_costCenterName != value)
                {
                    _costCenterName = value;
                    NotifyOfPropertyChange(nameof(CostCenterName));
                }
            }
        }

        private bool _cashReviewRequired;
        [ExpandoPath("cashReviewRequired")]
        public bool CashReviewRequired
        {
            get => _cashReviewRequired;
            set
            {
                if (_cashReviewRequired != value)
                {
                    _cashReviewRequired = value;
                    NotifyOfPropertyChange(nameof(CashReviewRequired));
                    this.TrackChange(nameof(CashReviewRequired));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _autoAdjustBalance;
        [ExpandoPath("autoAdjustBalance")]
        public bool AutoAdjustBalance
        {
            get => _autoAdjustBalance;
            set
            {
                if (_autoAdjustBalance != value)
                {
                    _autoAdjustBalance = value;
                    NotifyOfPropertyChange(nameof(AutoAdjustBalance));
                    this.TrackChange(nameof(AutoAdjustBalance));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _autoTransfer;
        [ExpandoPath("autoTransfer")]
        public bool AutoTransfer
        {
            get => _autoTransfer;
            set
            {
                if (_autoTransfer != value)
                {
                    _autoTransfer = value;
                    NotifyOfPropertyChange(nameof(AutoTransfer));
                    this.TrackChange(nameof(AutoTransfer));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private AccountingAccountGraphQLModel? _selectedCashAccountingAccount;
        public AccountingAccountGraphQLModel? SelectedCashAccountingAccount
        {
            get => _selectedCashAccountingAccount;
            set
            {
                if (_selectedCashAccountingAccount != value)
                {
                    _selectedCashAccountingAccount = value;
                    NotifyOfPropertyChange(nameof(SelectedCashAccountingAccount));
                    NotifyOfPropertyChange(nameof(CashAccountingAccountId));
                    this.TrackChange(nameof(CashAccountingAccountId));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        [ExpandoPath("cashAccountingAccountId")]
        public int CashAccountingAccountId => SelectedCashAccountingAccount?.Id ?? 0;

        private AccountingAccountGraphQLModel? _selectedCheckAccountingAccount;
        public AccountingAccountGraphQLModel? SelectedCheckAccountingAccount
        {
            get => _selectedCheckAccountingAccount;
            set
            {
                if (_selectedCheckAccountingAccount != value)
                {
                    _selectedCheckAccountingAccount = value;
                    NotifyOfPropertyChange(nameof(SelectedCheckAccountingAccount));
                    NotifyOfPropertyChange(nameof(CheckAccountingAccountId));
                    this.TrackChange(nameof(CheckAccountingAccountId));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        [ExpandoPath("checkAccountingAccountId")]
        public int CheckAccountingAccountId => SelectedCheckAccountingAccount?.Id ?? 0;

        private AccountingAccountGraphQLModel? _selectedCardAccountingAccount;
        public AccountingAccountGraphQLModel? SelectedCardAccountingAccount
        {
            get => _selectedCardAccountingAccount;
            set
            {
                if (_selectedCardAccountingAccount != value)
                {
                    _selectedCardAccountingAccount = value;
                    NotifyOfPropertyChange(nameof(SelectedCardAccountingAccount));
                    NotifyOfPropertyChange(nameof(CardAccountingAccountId));
                    this.TrackChange(nameof(CardAccountingAccountId));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        [ExpandoPath("cardAccountingAccountId")]
        public int CardAccountingAccountId => SelectedCardAccountingAccount?.Id ?? 0;

        private CashDrawerGraphQLModel? _selectedAutoTransferCashDrawer;
        public CashDrawerGraphQLModel? SelectedAutoTransferCashDrawer
        {
            get => _selectedAutoTransferCashDrawer;
            set
            {
                if (_selectedAutoTransferCashDrawer != value)
                {
                    _selectedAutoTransferCashDrawer = value;
                    NotifyOfPropertyChange(nameof(SelectedAutoTransferCashDrawer));
                    NotifyOfPropertyChange(nameof(AutoTransferCashDrawerId));
                    this.TrackChange(nameof(AutoTransferCashDrawerId));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        [ExpandoPath("cashDrawerIdAutoTransfer")]
        public int AutoTransferCashDrawerId => AutoTransfer ? (SelectedAutoTransferCashDrawer?.Id ?? 0) : 0;

        /// <summary>
        /// Lista de cajas disponibles para auto-transferencia.
        /// </summary>
        public ObservableCollection<CashDrawerGraphQLModel> AutoTransferCashDrawers => MasterContext.MajorAutoTransferCashDrawerCashDrawers;

        /// <summary>
        /// Contexto guardado antes de crear un nuevo registro.
        /// </summary>
        public CashDrawerCostCenterDTO? CostCenterBeforeNew { get; set; }

        #endregion

        #region CanSave

        public override bool CanSave
        {
            get
            {
                if (!IsEditing) return false;
                if (HasErrors) return false;
                if (string.IsNullOrWhiteSpace(Name)) return false;

                // Si AutoTransfer está activo, debe haber una caja seleccionada
                if (AutoTransfer && (SelectedAutoTransferCashDrawer == null || SelectedAutoTransferCashDrawer.Id == 0))
                    return false;

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
                AddError(nameof(Name), "El nombre de la caja no puede estar vacío");
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
            if (context is CashDrawerCostCenterDTO costCenterContext)
            {
                CostCenterBeforeNew = costCenterContext;
                CostCenterId = costCenterContext.Id;
                CostCenterName = costCenterContext.Name;
            }

            OriginalDto = null;
            Id = 0;
            Name = string.Empty;
            CashReviewRequired = false;
            AutoAdjustBalance = false;
            AutoTransfer = false;
            SelectedCashAccountingAccount = null;
            SelectedCheckAccountingAccount = null;
            SelectedCardAccountingAccount = null;
            SelectedAutoTransferCashDrawer = null;

            SeedDefaultValues();
            ClearAllErrors();
            ValidateAll();

            IsEditing = true;
        }

        public override void SetForEdit(object dto)
        {
            if (dto is not MajorCashDrawerMasterTreeDTO majorCashDrawerDTO) return;

            OriginalDto = majorCashDrawerDTO;
            Id = majorCashDrawerDTO.Id;
            Name = majorCashDrawerDTO.Name;
            CostCenterId = majorCashDrawerDTO.CostCenter?.Id ?? 0;
            CostCenterName = majorCashDrawerDTO.CostCenter?.Name ?? string.Empty;
            CashReviewRequired = majorCashDrawerDTO.CashReviewRequired;
            AutoAdjustBalance = majorCashDrawerDTO.AutoAdjustBalance;
            AutoTransfer = majorCashDrawerDTO.AutoTransfer;

            // Buscar las cuentas contables en la lista del contexto
            SelectedCashAccountingAccount = MasterContext.CashDrawerAccountingAccounts?
                .FirstOrDefault(a => a.Id == majorCashDrawerDTO.CashAccountingAccount?.Id);
            SelectedCheckAccountingAccount = MasterContext.CashDrawerAccountingAccounts?
                .FirstOrDefault(a => a.Id == majorCashDrawerDTO.CheckAccountingAccount?.Id);
            SelectedCardAccountingAccount = MasterContext.CashDrawerAccountingAccounts?
                .FirstOrDefault(a => a.Id == majorCashDrawerDTO.CardAccountingAccount?.Id);

            // Buscar la caja de auto-transferencia
            SelectedAutoTransferCashDrawer = AutoTransferCashDrawers?
                .FirstOrDefault(c => c.Id == majorCashDrawerDTO.AutoTransferCashDrawer?.Id);

            SeedCurrentValues();
            ClearAllErrors();
            ValidateAll();

            IsEditing = false;
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(CostCenterId), CostCenterId);
            this.SeedValue(nameof(CashReviewRequired), CashReviewRequired);
            this.SeedValue(nameof(AutoAdjustBalance), AutoAdjustBalance);
            this.SeedValue(nameof(AutoTransfer), AutoTransfer);
            this.SeedValue(nameof(CashAccountingAccountId), CashAccountingAccountId);
            this.SeedValue(nameof(CheckAccountingAccountId), CheckAccountingAccountId);
            this.SeedValue(nameof(CardAccountingAccountId), CardAccountingAccountId);
            this.SeedValue(nameof(AutoTransferCashDrawerId), AutoTransferCashDrawerId);
            this.AcceptChanges();
        }

        private void SeedDefaultValues()
        {
            this.SeedValue(nameof(CashReviewRequired), CashReviewRequired);
            this.SeedValue(nameof(AutoAdjustBalance), AutoAdjustBalance);
            this.SeedValue(nameof(AutoTransfer), AutoTransfer);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<CashDrawerGraphQLModel>
                .Create()
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
                    .Field(a => a.Name))
                .Select(e => e.CheckAccountingAccount, acc => acc
                    .Field(a => a.Id)
                    .Field(a => a.Name))
                .Select(e => e.CardAccountingAccount, acc => acc
                    .Field(a => a.Id)
                    .Field(a => a.Name))
                .Build();

            var parameter = new GraphQLQueryParameter("data", "CreateCashDrawerInput!");
            var fragment = new GraphQLQueryFragment("createCashDrawer", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<CashDrawerGraphQLModel>
                .Create()
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
                    .Field(a => a.Name))
                .Select(e => e.CheckAccountingAccount, acc => acc
                    .Field(a => a.Id)
                    .Field(a => a.Name))
                .Select(e => e.CardAccountingAccount, acc => acc
                    .Field(a => a.Id)
                    .Field(a => a.Name))
                .Build();

            var fragment = new GraphQLQueryFragment(
                "updateCashDrawer",
                [
                    new GraphQLQueryParameter("id", "Int!"),
                    new GraphQLQueryParameter("data", "UpdateCashDrawerInput!")
                ],
                fields,
                "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<CashDrawerGraphQLModel>> ExecuteSaveAsync()
        {
            string query;
            dynamic variables = new ExpandoObject();

            if (IsNewRecord)
            {
                // Para nuevos registros, establecer el CostCenterId del contexto guardado
                CostCenterId = CostCenterBeforeNew?.Id ?? 0;

                query = GetCreateQuery();
                variables.data = new ExpandoObject();
                variables.data.name = Name.Trim().RemoveExtraSpaces();
                variables.data.cashReviewRequired = CashReviewRequired;
                variables.data.autoAdjustBalance = AutoAdjustBalance;
                variables.data.autoTransfer = AutoTransfer;
                variables.data.isPettyCash = false;
                variables.data.cashDrawerIdAutoTransfer = AutoTransferCashDrawerId;
                variables.data.costCenterId = CostCenterId;
                variables.data.parentId = 0;
                variables.data.computerName = "";

                var createResult = await _cashDrawerService.CreateAsync(query, variables);
                return new UpsertResponseType<CashDrawerGraphQLModel> { Entity = createResult };
            }
            else
            {
                query = GetUpdateQuery();
                variables.id = Id;
                variables.data = new ExpandoObject();
                variables.data.name = Name.Trim().RemoveExtraSpaces();
                variables.data.cashReviewRequired = CashReviewRequired;
                variables.data.autoAdjustBalance = AutoAdjustBalance;
                variables.data.autoTransfer = AutoTransfer;
                variables.data.cashDrawerIdAutoTransfer = AutoTransferCashDrawerId;
                variables.data.costCenterId = CostCenterId;
                variables.data.accountingAccountIdCash = CashAccountingAccountId;
                variables.data.accountingAccountIdCheck = CheckAccountingAccountId;
                variables.data.accountingAccountIdCard = CardAccountingAccountId;
                variables.data.computerName = "";

                var updateResult = await _cashDrawerService.UpdateAsync(query, variables);
                return new UpsertResponseType<CashDrawerGraphQLModel> { Entity = updateResult };
            }
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<CashDrawerGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new TreasuryCashDrawerCreateMessage { CreatedCashDrawer = result.Entity });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new TreasuryCashDrawerUpdateMessage { UpdatedCashDrawer = result.Entity });
            }
        }

        #endregion
    }
}
