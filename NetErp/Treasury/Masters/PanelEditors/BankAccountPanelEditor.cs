using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
using Models.Treasury;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Treasury.Masters.DTO;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        private string _type = string.Empty;
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
                    this.TrackChange(nameof(Description));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _number = string.Empty;
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
                    this.TrackChange(nameof(Description));
                    ValidateNumber();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _isActive;
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
                    this.TrackChange(nameof(Description));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private int _displayOrder;
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
                    this.TrackChange(nameof(Description));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private int _bankId;
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

        private bool _enablePaymentMethod;
        public bool EnablePaymentMethod
        {
            get => _enablePaymentMethod;
            set
            {
                if (_enablePaymentMethod != value)
                {
                    _enablePaymentMethod = value;
                    NotifyOfPropertyChange(nameof(EnablePaymentMethod));
                    this.TrackChange(nameof(EnablePaymentMethod));
                    MasterContext.RefreshCanSave();
                }
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
                        SelectedAccountingAccount = null;
                    }
                }
            }
        }

        private AccountingAccountGraphQLModel? _selectedAccountingAccount;

        [ExpandoPath("accountingAccountId", SerializeAsId = true)]
        public AccountingAccountGraphQLModel? SelectedAccountingAccount
        {
            get => _selectedAccountingAccount;
            set
            {
                if (_selectedAccountingAccount != value)
                {
                    _selectedAccountingAccount = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingAccount));
                    this.TrackChange(nameof(SelectedAccountingAccount));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        /// <summary>
        /// Banco padre guardado antes de crear una nueva cuenta.
        /// </summary>
        public TreasuryBankMasterTreeDTO? BankBeforeNew { get; set; }

        /// <summary>
        /// Delegación de cuentas contables para cuentas bancarias.
        /// </summary>
        public ObservableCollection<AccountingAccountGraphQLModel> BankAccountAccountingAccounts => MasterContext.BankAccountAccountingAccounts;

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
                if (AccountingAccountSelectExisting && SelectedAccountingAccount == null)
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
            EnablePaymentMethod = false;
            AccountingAccountAutoCreate = true;
            AccountingAccountSelectExisting = false;
            SelectedAccountingAccount = null;

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
            EnablePaymentMethod = bankAccountDTO.PaymentMethod != null && bankAccountDTO.PaymentMethod.Id > 0;
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
            this.SeedValue(nameof(SelectedAccountingAccount), SelectedAccountingAccount);
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(EnablePaymentMethod), EnablePaymentMethod);
            this.AcceptChanges();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(BankId), BankId);
            this.SeedValue(nameof(Type), Type);
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(DisplayOrder), DisplayOrder);
            this.SeedValue(nameof(Provider), Provider);
            this.SeedValue(nameof(SelectedAccountingAccount), SelectedAccountingAccount);
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(EnablePaymentMethod), EnablePaymentMethod);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<BankAccountGraphQLModel>>
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

            var parameter = new GraphQLQueryParameter("input", "CreateBankAccountInput!");
            var fragment = new GraphQLQueryFragment("createBankAccount", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<BankAccountGraphQLModel>>
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

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateBankAccountInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateBankAccount", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<BankAccountGraphQLModel>> ExecuteSaveAsync()
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
                ? await _bankAccountService.CreateAsync<UpsertResponseType<BankAccountGraphQLModel>>(query, variables)
                : await _bankAccountService.UpdateAsync<UpsertResponseType<BankAccountGraphQLModel>>(query, variables);
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<BankAccountGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new BankAccountCreateMessage { CreatedBankAccount = result });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new BankAccountUpdateMessage { UpdatedBankAccount = result });
            }
        }

        #endregion
    }
}
