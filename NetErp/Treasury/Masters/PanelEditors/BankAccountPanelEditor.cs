using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
using Models.Treasury;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Treasury.Masters.DTO;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Treasury.Masters.PanelEditors
{
    /// <summary>
    /// Panel Editor para la entidad Bank Account (Cuenta Bancaria).
    /// Maneja la lógica de edición, validación y persistencia de cuentas bancarias.
    /// </summary>
    public class BankAccountPanelEditor : TreasuryMastersBasePanelEditor<TreasuryBankAccountMasterTreeDTO, BankAccountGraphQLModel>
    {
        #region Fields

        private readonly IRepository<BankAccountGraphQLModel> _bankAccountService;

        #endregion

        #region Constructor

        public BankAccountPanelEditor(
            TreasuryRootMasterViewModel masterContext,
            IRepository<BankAccountGraphQLModel> bankAccountService)
            : base(masterContext)
        {
            _bankAccountService = bankAccountService ?? throw new ArgumentNullException(nameof(bankAccountService));
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

        private string _type = string.Empty;
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
                    NotifyOfPropertyChange(nameof(Description));
                    NotifyOfPropertyChange(nameof(PaymentMethodName));
                    this.TrackChange(nameof(Type));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _number = string.Empty;
        [ExpandoPath("number")]
        public string Number
        {
            get => _number;
            set
            {
                if (_number != value)
                {
                    _number = value;
                    NotifyOfPropertyChange(nameof(Number));
                    NotifyOfPropertyChange(nameof(Description));
                    NotifyOfPropertyChange(nameof(PaymentMethodName));
                    this.TrackChange(nameof(Number));
                    ValidateNumber();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _isActive;
        [ExpandoPath("isActive")]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.TrackChange(nameof(IsActive));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _reference = string.Empty;
        [ExpandoPath("reference")]
        public string Reference
        {
            get => _reference;
            set
            {
                if (_reference != value)
                {
                    _reference = value;
                    NotifyOfPropertyChange(nameof(Reference));
                    NotifyOfPropertyChange(nameof(Description));
                    this.TrackChange(nameof(Reference));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private int _displayOrder;
        [ExpandoPath("displayOrder")]
        public int DisplayOrder
        {
            get => _displayOrder;
            set
            {
                if (_displayOrder != value)
                {
                    _displayOrder = value;
                    NotifyOfPropertyChange(nameof(DisplayOrder));
                    this.TrackChange(nameof(DisplayOrder));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _provider = string.Empty;
        [ExpandoPath("provider")]
        public string Provider
        {
            get => _provider;
            set
            {
                if (_provider != value)
                {
                    _provider = value;
                    NotifyOfPropertyChange(nameof(Provider));
                    NotifyOfPropertyChange(nameof(Description));
                    NotifyOfPropertyChange(nameof(PaymentMethodName));
                    this.TrackChange(nameof(Provider));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private int _bankId;
        [ExpandoPath("bankId")]
        public int BankId
        {
            get => _bankId;
            set
            {
                if (_bankId != value)
                {
                    _bankId = value;
                    NotifyOfPropertyChange(nameof(BankId));
                    this.TrackChange(nameof(BankId));
                }
            }
        }

        private string _bankName = string.Empty;
        public string BankName
        {
            get => _bankName;
            set
            {
                if (_bankName != value)
                {
                    _bankName = value;
                    NotifyOfPropertyChange(nameof(BankName));
                    NotifyOfPropertyChange(nameof(Description));
                    NotifyOfPropertyChange(nameof(PaymentMethodName));
                }
            }
        }

        private CaptureTypeEnum _bankCaptureType;
        public CaptureTypeEnum BankCaptureType
        {
            get => _bankCaptureType;
            set
            {
                if (_bankCaptureType != value)
                {
                    _bankCaptureType = value;
                    NotifyOfPropertyChange(nameof(BankCaptureType));
                    NotifyOfPropertyChange(nameof(BankCaptureInfoAsPN));
                    NotifyOfPropertyChange(nameof(BankCaptureInfoAsPJ));
                }
            }
        }

        public bool BankCaptureInfoAsPN => BankCaptureType.Equals(CaptureTypeEnum.PN);
        public bool BankCaptureInfoAsPJ => BankCaptureType.Equals(CaptureTypeEnum.PJ);
        public bool BankCaptureInfoAsRS => !BankCaptureInfoAsPN; // Traditional bank accounts (Ahorros/Corriente)

        /// <summary>
        /// Descripción compuesta de la cuenta bancaria.
        /// </summary>
        public string Description
        {
            get
            {
                if (BankCaptureInfoAsPJ)
                {
                    return $"{BankName} [{(Type == "A" ? "CTA. DE AHORROS" : "CTA. CORRIENTE")} No. {Number}] {(string.IsNullOrEmpty(Reference) ? "" : $"- RF. {Reference}")}".Trim();
                }
                return $"{(Provider == "N" ? "NEQUI" : "DAVIPLATA")} - {Number} {(string.IsNullOrEmpty(Reference) ? "" : $"- RF. {Reference}")}";
            }
        }

        /// <summary>
        /// Nombre del método de pago generado automáticamente.
        /// </summary>
        public string PaymentMethodName
        {
            get
            {
                if (BankCaptureInfoAsPJ)
                {
                    return $"TRANSF/CONSIG EN {BankName.Trim()} EN {(Type == "A" ? "CTA. DE AHORROS" : "CUENTA CORRIENTE")} TERMINADA EN {(Number.Length > 5 ? $"* {Number[^5..]}" : "")}";
                }
                return $"TRANSF/CONSGI EN {(Provider == "N" ? "NEQUI" : "DAVIPLATA")} {Number}";
            }
        }

        private string _paymentMethodAbbreviation = string.Empty;
        public string PaymentMethodAbbreviation
        {
            get => _paymentMethodAbbreviation;
            set
            {
                if (_paymentMethodAbbreviation != value)
                {
                    _paymentMethodAbbreviation = value;
                    NotifyOfPropertyChange(nameof(PaymentMethodAbbreviation));
                }
            }
        }

        private bool _accountingAccountAutoCreate = true;
        public bool AccountingAccountAutoCreate
        {
            get => _accountingAccountAutoCreate;
            set
            {
                if (_accountingAccountAutoCreate != value)
                {
                    _accountingAccountAutoCreate = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountAutoCreate));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _accountingAccountSelectExisting = false;
        public bool AccountingAccountSelectExisting
        {
            get => _accountingAccountSelectExisting;
            set
            {
                if (_accountingAccountSelectExisting != value)
                {
                    _accountingAccountSelectExisting = value;
                    NotifyOfPropertyChange(nameof(AccountingAccountSelectExisting));
                    MasterContext.RefreshCanSave();
                    if (!_accountingAccountSelectExisting)
                    {
                        SelectedAccountingAccount = MasterContext.BankAccountAccountingAccounts?.FirstOrDefault(x => x.Id == 0);
                    }
                }
            }
        }

        private AccountingAccountGraphQLModel? _selectedAccountingAccount;
        public AccountingAccountGraphQLModel? SelectedAccountingAccount
        {
            get => _selectedAccountingAccount;
            set
            {
                if (_selectedAccountingAccount != value)
                {
                    _selectedAccountingAccount = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingAccount));
                    NotifyOfPropertyChange(nameof(AccountingAccountId));
                    this.TrackChange(nameof(AccountingAccountId));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        [ExpandoPath("accountingAccountId")]
        public int AccountingAccountId => AccountingAccountSelectExisting ? (SelectedAccountingAccount?.Id ?? 0) : 0;

        /// <summary>
        /// Banco padre guardado antes de crear una nueva cuenta.
        /// </summary>
        public TreasuryBankMasterTreeDTO? BankBeforeNew { get; set; }

        /// <summary>
        /// Lista de cost centers permitidos para la cuenta bancaria.
        /// </summary>
        public ObservableCollection<TreasuryBankAccountCostCenterDTO> CostCenters => MasterContext.BankAccountCostCenters;

        #endregion

        #region CanSave

        public override bool CanSave
        {
            get
            {
                if (!IsEditing) return false;
                if (HasErrors) return false;
                if (string.IsNullOrWhiteSpace(Number)) return false;
                if (AccountingAccountSelectExisting && (SelectedAccountingAccount == null || SelectedAccountingAccount.Id == 0))
                    return false;
                if (!this.HasChanges()) return false;
                return true;
            }
        }

        #endregion

        #region Validation Methods

        private void ValidateNumber()
        {
            ClearErrors(nameof(Number));
            if (string.IsNullOrWhiteSpace(Number))
            {
                AddError(nameof(Number), "El número de cuenta no puede estar vacío");
            }
        }

        public override void ValidateAll()
        {
            ValidateNumber();
        }

        #endregion

        #region SetForNew / SetForEdit

        public override void SetForNew(object context)
        {
            if (context is TreasuryBankMasterTreeDTO bankContext)
            {
                BankBeforeNew = bankContext;
                BankId = bankContext.Id;
                BankName = bankContext.AccountingEntity?.SearchName ?? string.Empty;
                BankCaptureType = (CaptureTypeEnum)Enum.Parse(typeof(CaptureTypeEnum), bankContext.AccountingEntity?.CaptureType ?? "PJ");
            }

            // Reset cost centers
            foreach (var costCenter in CostCenters)
            {
                costCenter.IsChecked = false;
            }

            OriginalDto = null;
            Id = 0;
            Type = BankCaptureInfoAsPJ ? "A" : "M";
            Provider = BankCaptureInfoAsPN ? "N" : "";
            Number = string.Empty;
            IsActive = true;
            Reference = string.Empty;
            DisplayOrder = 0;
            AccountingAccountAutoCreate = true;
            AccountingAccountSelectExisting = false;
            SelectedAccountingAccount = MasterContext.BankAccountAccountingAccounts?.FirstOrDefault(x => x.Id == 0);

            SeedDefaultValues();
            ClearAllErrors();
            ValidateAll();

            IsEditing = true;
        }

        public override void SetForEdit(object dto)
        {
            if (dto is not TreasuryBankAccountMasterTreeDTO bankAccountDTO) return;

            // Reset cost centers first
            foreach (var costCenter in CostCenters)
            {
                costCenter.IsChecked = false;
            }

            OriginalDto = bankAccountDTO;
            Id = bankAccountDTO.Id;
            Type = bankAccountDTO.Type;
            BankCaptureType = (CaptureTypeEnum)Enum.Parse(typeof(CaptureTypeEnum), bankAccountDTO.Bank?.AccountingEntity?.CaptureType ?? "PJ");
            Provider = bankAccountDTO.Provider;
            Number = bankAccountDTO.Number;
            IsActive = bankAccountDTO.IsActive;
            Reference = bankAccountDTO.Reference;
            DisplayOrder = bankAccountDTO.DisplayOrder;
            BankId = bankAccountDTO.Bank?.Id ?? 0;
            BankName = bankAccountDTO.Bank?.AccountingEntity?.SearchName ?? string.Empty;
            AccountingAccountAutoCreate = false;
            AccountingAccountSelectExisting = true;
            SelectedAccountingAccount = MasterContext.BankAccountAccountingAccounts?
                .FirstOrDefault(a => a.Id == bankAccountDTO.AccountingAccount?.Id);
            PaymentMethodAbbreviation = bankAccountDTO.PaymentMethod?.Abbreviation ?? string.Empty;

            // Set allowed cost centers
            foreach (var costCenter in CostCenters)
            {
                if (bankAccountDTO.AllowedCostCenters != null &&
                    bankAccountDTO.AllowedCostCenters.Any(ac => ac.Id == costCenter.Id))
                {
                    costCenter.IsChecked = true;
                }
            }

            SeedCurrentValues();
            ClearAllErrors();
            ValidateAll();

            IsEditing = false;
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Type), Type);
            this.SeedValue(nameof(Number), Number);
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(Reference), Reference);
            this.SeedValue(nameof(DisplayOrder), DisplayOrder);
            this.SeedValue(nameof(Provider), Provider);
            this.SeedValue(nameof(AccountingAccountId), AccountingAccountId);
            this.AcceptChanges();
        }

        private void SeedDefaultValues()
        {
            this.SeedValue(nameof(IsActive), IsActive);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<BankAccountGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Type)
                .Field(e => e.Number)
                .Field(e => e.IsActive)
                .Field(e => e.Description)
                .Field(e => e.Reference)
                .Field(e => e.DisplayOrder)
                .Field(e => e.Provider)
                .SelectList(e => e.AllowedCostCenters, ac => ac
                    .Field(a => a.Id)
                    .Field(a => a.Name))
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
                        .Field(x => x.CaptureType)))
                .Build();

            var parameter = new GraphQLQueryParameter("data", "CreateBankAccountInput!");
            var fragment = new GraphQLQueryFragment("createBankAccount", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<BankAccountGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Type)
                .Field(e => e.Number)
                .Field(e => e.IsActive)
                .Field(e => e.Description)
                .Field(e => e.Reference)
                .Field(e => e.DisplayOrder)
                .Field(e => e.Provider)
                .SelectList(e => e.AllowedCostCenters, ac => ac
                    .Field(a => a.Id)
                    .Field(a => a.Name))
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
                        .Field(x => x.CaptureType)))
                .Build();

            var fragment = new GraphQLQueryFragment(
                "updateBankAccount",
                [
                    new GraphQLQueryParameter("id", "Int!"),
                    new GraphQLQueryParameter("data", "UpdateBankAccountInput!")
                ],
                fields,
                "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<BankAccountGraphQLModel>> ExecuteSaveAsync()
        {
            string query;
            dynamic variables = new ExpandoObject();

            if (IsNewRecord)
            {
                query = GetCreateQuery();
                variables.data = new ExpandoObject();
                variables.data.type = Type;
                variables.data.number = Number.Trim().RemoveExtraSpaces();
                variables.data.isActive = IsActive;
                variables.data.description = Description;
                variables.data.reference = Reference?.Trim() ?? "";
                variables.data.displayOrder = DisplayOrder;
                variables.data.accountingAccountId = AccountingAccountId;
                variables.data.provider = BankCaptureInfoAsPN ? Provider : "";
                variables.data.bankId = BankId;
                variables.data.paymentMethodName = PaymentMethodName;
                variables.data.allowedCostCenters = CostCenters.Where(x => x.IsChecked).Select(x => x.Id).ToList();

                var createResult = await _bankAccountService.CreateAsync(query, variables);
                return new UpsertResponseType<BankAccountGraphQLModel> { Entity = createResult };
            }
            else
            {
                query = GetUpdateQuery();
                variables.id = Id;
                variables.data = new ExpandoObject();
                variables.data.type = Type;
                variables.data.number = Number.Trim().RemoveExtraSpaces();
                variables.data.isActive = IsActive;
                variables.data.description = Description;
                variables.data.reference = Reference?.Trim() ?? "";
                variables.data.displayOrder = DisplayOrder;
                variables.data.accountingAccountId = AccountingAccountId;
                variables.data.provider = BankCaptureInfoAsPN ? Provider : "";
                variables.data.bankId = BankId;
                variables.data.paymentMethodName = PaymentMethodName;
                variables.data.allowedCostCenters = CostCenters.Where(x => x.IsChecked).Select(x => x.Id).ToList();

                var updateResult = await _bankAccountService.UpdateAsync(query, variables);
                return new UpsertResponseType<BankAccountGraphQLModel> { Entity = updateResult };
            }
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<BankAccountGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new BankAccountCreateMessage { CreatedBankAccount = result.Entity });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new BankAccountUpdateMessage { UpdatedBankAccount = result.Entity });
            }
        }

        #endregion
    }
}
