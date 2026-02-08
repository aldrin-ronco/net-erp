using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
using Models.Treasury;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Treasury.Masters.DTO;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Treasury.Masters.PanelEditors
{
    /// <summary>
    /// Panel Editor para la entidad Auxiliary Cash Drawer (Caja Auxiliar).
    /// Maneja la lógica de edición, validación y persistencia de cajas auxiliares.
    /// Las cajas auxiliares están asociadas a una caja mayor (parentId).
    /// </summary>
    public class AuxiliaryCashDrawerPanelEditor : TreasuryMastersBasePanelEditor<TreasuryAuxiliaryCashDrawerMasterTreeDTO, CashDrawerGraphQLModel>
    {
        #region Fields

        private readonly IRepository<CashDrawerGraphQLModel> _cashDrawerService;

        #endregion

        #region Constructor

        public AuxiliaryCashDrawerPanelEditor(
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

        private int _costCenterId;
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

        private bool _cashReviewRequired;
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

        private bool _isPettyCash;
        public bool IsPettyCash
        {
            get => _isPettyCash;
            set
            {
                if (_isPettyCash != value)
                {
                    _isPettyCash = value;
                    NotifyOfPropertyChange(nameof(IsPettyCash));
                    this.TrackChange(nameof(IsPettyCash));
                }
            }
        }

        private int _parentId;
        public int ParentId
        {
            get => _parentId;
            set
            {
                if (_parentId != value)
                {
                    _parentId = value;
                    NotifyOfPropertyChange(nameof(ParentId));
                    this.TrackChange(nameof(ParentId));
                }
            }
        }

        private string _computerName = string.Empty;
        public string ComputerName
        {
            get => _computerName;
            set
            {
                if (_computerName != value)
                {
                    _computerName = value;
                    NotifyOfPropertyChange(nameof(ComputerName));
                    this.TrackChange(nameof(ComputerName));
                    ValidateComputerName();
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

        public int AutoTransferCashDrawerId => AutoTransfer ? (SelectedAutoTransferCashDrawer?.Id ?? 0) : 0;

        /// <summary>
        /// Lista de cajas disponibles para auto-transferencia.
        /// </summary>
        public ObservableCollection<CashDrawerGraphQLModel> AutoTransferCashDrawers => MasterContext.AuxiliaryAutoTransferCashDrawerCashDrawers;

        /// <summary>
        /// Delegación de cuentas contables para cash drawers.
        /// </summary>
        public ObservableCollection<AccountingAccountGraphQLModel> CashDrawerAccountingAccounts => MasterContext.CashDrawerAccountingAccounts;

        /// <summary>
        /// Delegación del comando de búsqueda de nombre de equipo.
        /// </summary>
        public ICommand SearchComputerNameCommand => MasterContext.SearchComputerNameCommand;

        #endregion

        #region CanSave

        public override bool CanSave
        {
            get
            {
                if (!IsEditing) return false;
                if (HasErrors) return false;
                if (string.IsNullOrWhiteSpace(Name)) return false;
                if (string.IsNullOrWhiteSpace(ComputerName)) return false;

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

        private void ValidateComputerName()
        {
            ClearErrors(nameof(ComputerName));
            if (string.IsNullOrWhiteSpace(ComputerName))
            {
                AddError(nameof(ComputerName), "El nombre del equipo no puede estar vacío");
            }
        }

        public override void ValidateAll()
        {
            ValidateName();
            ValidateComputerName();
        }

        #endregion

        #region SetForNew / SetForEdit

        public override void SetForNew(object context)
        {
            if (context is MajorCashDrawerMasterTreeDTO majorCashDrawer)
            {
                ParentId = majorCashDrawer.Id;
                CostCenterId = majorCashDrawer.CostCenter?.Id ?? 0;
            }

            OriginalDto = null;
            Id = 0;
            Name = "CAJA AUXILIAR";
            CashReviewRequired = false;
            AutoAdjustBalance = false;
            AutoTransfer = false;
            IsPettyCash = false;
            ComputerName = SessionInfo.GetComputerName();
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
            if (dto is not TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawerDTO) return;

            OriginalDto = auxiliaryCashDrawerDTO;
            Id = auxiliaryCashDrawerDTO.Id;
            Name = auxiliaryCashDrawerDTO.Name;
            CashReviewRequired = auxiliaryCashDrawerDTO.CashReviewRequired;
            AutoAdjustBalance = auxiliaryCashDrawerDTO.AutoAdjustBalance;
            AutoTransfer = auxiliaryCashDrawerDTO.AutoTransfer;
            ComputerName = auxiliaryCashDrawerDTO.ComputerName;

            // Buscar las cuentas contables en la lista del contexto
            SelectedCashAccountingAccount = MasterContext.CashDrawerAccountingAccounts?
                .FirstOrDefault(a => a.Id == auxiliaryCashDrawerDTO.CashAccountingAccount?.Id);
            SelectedCheckAccountingAccount = MasterContext.CashDrawerAccountingAccounts?
                .FirstOrDefault(a => a.Id == auxiliaryCashDrawerDTO.CheckAccountingAccount?.Id);
            SelectedCardAccountingAccount = MasterContext.CashDrawerAccountingAccounts?
                .FirstOrDefault(a => a.Id == auxiliaryCashDrawerDTO.CardAccountingAccount?.Id);

            // Buscar la caja de auto-transferencia
            SelectedAutoTransferCashDrawer = AutoTransferCashDrawers?
                .FirstOrDefault(c => c.Id == auxiliaryCashDrawerDTO.AutoTransferCashDrawer?.Id);

            SeedCurrentValues();
            ClearAllErrors();
            ValidateAll();

            IsEditing = false;
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(CashReviewRequired), CashReviewRequired);
            this.SeedValue(nameof(AutoAdjustBalance), AutoAdjustBalance);
            this.SeedValue(nameof(AutoTransfer), AutoTransfer);
            this.SeedValue(nameof(ComputerName), ComputerName);
            this.SeedValue(nameof(CashAccountingAccountId), CashAccountingAccountId);
            this.SeedValue(nameof(CheckAccountingAccountId), CheckAccountingAccountId);
            this.SeedValue(nameof(CardAccountingAccountId), CardAccountingAccountId);
            this.SeedValue(nameof(AutoTransferCashDrawerId), AutoTransferCashDrawerId);
            this.AcceptChanges();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(CostCenterId), CostCenterId);
            this.SeedValue(nameof(ParentId), ParentId);
            this.SeedValue(nameof(CashReviewRequired), CashReviewRequired);
            this.SeedValue(nameof(AutoAdjustBalance), AutoAdjustBalance);
            this.SeedValue(nameof(AutoTransfer), AutoTransfer);
            this.SeedValue(nameof(ComputerName), ComputerName);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<CashDrawerGraphQLModel>>
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

            var parameter = new GraphQLQueryParameter("input", "CreateCashDrawerInput!");
            var fragment = new GraphQLQueryFragment("createCashDrawer", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<CashDrawerGraphQLModel>>
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

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateCashDrawerInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateCashDrawer", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<CashDrawerGraphQLModel>> ExecuteSaveAsync()
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
                ? await _cashDrawerService.CreateAsync<UpsertResponseType<CashDrawerGraphQLModel>>(query, variables)
                : await _cashDrawerService.UpdateAsync<UpsertResponseType<CashDrawerGraphQLModel>>(query, variables);
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<CashDrawerGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new TreasuryCashDrawerCreateMessage { CreatedCashDrawer = result });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new TreasuryCashDrawerUpdateMessage { UpdatedCashDrawer = result });
            }
        }

        #endregion
    }
}
