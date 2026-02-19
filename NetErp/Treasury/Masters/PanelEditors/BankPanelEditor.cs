using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using Models.Books;
using Models.Global;
using Models.Treasury;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Treasury.Masters.DTO;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Treasury.Masters.PanelEditors
{
    /// <summary>
    /// Panel Editor para la entidad Bank (Banco).
    /// Maneja la lógica de edición, validación y persistencia de bancos.
    /// Un banco está asociado a una AccountingEntity.
    /// </summary>
    public class BankPanelEditor : TreasuryMastersBasePanelEditor<TreasuryBankMasterTreeDTO, BankGraphQLModel>
    {
        #region Fields

        private readonly IRepository<BankGraphQLModel> _bankService;
        private readonly Helpers.IDialogService _dialogService;

        #endregion

        #region Constructor

        public BankPanelEditor(
            TreasuryRootMasterViewModel masterContext,
            IRepository<BankGraphQLModel> bankService,
            Helpers.IDialogService dialogService)
            : base(masterContext)
        {
            _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            Messenger.Default.Register<ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel>>(
                this,
                SearchWithTwoColumnsGridMessageToken.BankAccountingEntity,
                false,
                OnFindBankAccountingEntityMessage);
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

        private string _code = string.Empty;
        public string Code
        {
            get => _code;
            set
            {
                if (_code != value)
                {
                    _code = value;
                    NotifyOfPropertyChange(nameof(Code));
                    this.TrackChange(nameof(Code));
                    ValidateCode();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private int _accountingEntityId;
        public int AccountingEntityId
        {
            get => _accountingEntityId;
            set
            {
                if (_accountingEntityId != value)
                {
                    _accountingEntityId = value;
                    NotifyOfPropertyChange(nameof(AccountingEntityId));
                    this.TrackChange(nameof(AccountingEntityId));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _accountingEntityName = string.Empty;
        public string AccountingEntityName
        {
            get => _accountingEntityName;
            set
            {
                if (_accountingEntityName != value)
                {
                    _accountingEntityName = value;
                    NotifyOfPropertyChange(nameof(AccountingEntityName));
                    ValidateAccountingEntityName();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _paymentMethodPrefix = "Z";
        public string PaymentMethodPrefix
        {
            get => _paymentMethodPrefix;
            set
            {
                if (_paymentMethodPrefix != value)
                {
                    _paymentMethodPrefix = value;
                    NotifyOfPropertyChange(nameof(PaymentMethodPrefix));
                    this.TrackChange(nameof(PaymentMethodPrefix));
                    ValidatePaymentMethodPrefix();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        #endregion

        #region Commands

        private ICommand? _searchAccountingEntityCommand;
        public ICommand SearchAccountingEntityCommand
        {
            get
            {
                _searchAccountingEntityCommand ??= new AsyncCommand(SearchAccountingEntityAsync, CanSearchAccountingEntity);
                return _searchAccountingEntityCommand;
            }
        }

        public async Task SearchAccountingEntityAsync()
        {
            string query = GetSearchAccountingEntityQuery();

            string fieldHeader1 = "NIT";
            string fieldHeader2 = "Nombre o razón social";
            string fieldData1 = "IdentificationNumberWithVerificationDigit";
            string fieldData2 = "SearchName";

            var viewModel = new SearchWithTwoColumnsGridViewModel<AccountingEntityGraphQLModel>(
                query, fieldHeader1, fieldHeader2, fieldData1, fieldData2, null,
                SearchWithTwoColumnsGridMessageToken.BankAccountingEntity, _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Búsqueda de terceros");
        }

        private string GetSearchAccountingEntityQuery()
        {
            var fields = FieldSpec<PageType<AccountingEntityGraphQLModel>>
                .Create()
                .Field(f => f.PageNumber)
                .Field(f => f.TotalEntries)
                .Field(f => f.PageSize)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.IdentificationNumber)
                    .Field(e => e.VerificationDigit)
                    .Field(e => e.SearchName))
                .Build();

            var filterParameter = new GraphQLQueryParameter("filters", "AccountingEntityFilters");
            var paginationParameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("accountingEntitiesPage", [filterParameter, paginationParameter], fields, "PageResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        public bool CanSearchAccountingEntity() => IsEditing;

        private void OnFindBankAccountingEntityMessage(ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel> message)
        {
            if (message.ReturnedData is null) return;
            AccountingEntityId = message.ReturnedData.Id;
            AccountingEntityName = message.ReturnedData.SearchName;
        }

        #endregion

        #region CanSave

        public override bool CanSave
        {
            get
            {
                if (!IsEditing) return false;
                if (HasErrors) return false;
                if (AccountingEntityId == 0 || string.IsNullOrWhiteSpace(AccountingEntityName)) return false;
                if (!this.HasChanges()) return false;
                return true;
            }
        }

        #endregion

        #region Validation Methods

        private void ValidateCode()
        {
            ClearErrors(nameof(Code));
            if (string.IsNullOrWhiteSpace(Code))
            {
                AddError(nameof(Code), "El código es obligatorio");
            }
            else if (Code.Length != 3)
            {
                AddError(nameof(Code), "El código debe tener exactamente 3 dígitos");
            }
        }

        private void ValidateAccountingEntityName()
        {
            ClearErrors(nameof(AccountingEntityName));
            if (string.IsNullOrWhiteSpace(AccountingEntityName))
            {
                AddError(nameof(AccountingEntityName), "Debe seleccionar una entidad contable");
            }
        }

        private void ValidatePaymentMethodPrefix()
        {
            ClearErrors(nameof(PaymentMethodPrefix));
            if (string.IsNullOrWhiteSpace(PaymentMethodPrefix))
            {
                AddError(nameof(PaymentMethodPrefix), "El prefijo de método de pago es obligatorio");
            }
            else if (PaymentMethodPrefix.Length != 1)
            {
                AddError(nameof(PaymentMethodPrefix), "El prefijo debe ser exactamente una letra");
            }
        }

        public override void ValidateAll()
        {
            ValidateCode();
            ValidateAccountingEntityName();
            ValidatePaymentMethodPrefix();
        }

        #endregion

        #region SetForNew / SetForEdit

        public override void SetForNew(object context)
        {
            OriginalDto = null;
            Id = 0;
            Code = string.Empty;
            AccountingEntityId = 0;
            AccountingEntityName = string.Empty;
            PaymentMethodPrefix = "Z";

            SeedDefaultValues();
            ClearAllErrors();
            ValidateAll();

            IsEditing = true;
        }

        public override void SetForEdit(object dto)
        {
            if (dto is not TreasuryBankMasterTreeDTO bankDTO) return;

            OriginalDto = bankDTO;
            Id = bankDTO.Id;
            Code = bankDTO.Code;
            AccountingEntityId = bankDTO.AccountingEntity?.Id ?? 0;
            AccountingEntityName = bankDTO.AccountingEntity?.SearchName ?? string.Empty;
            PaymentMethodPrefix = bankDTO.PaymentMethodPrefix;

            SeedCurrentValues();
            ClearAllErrors();
            ValidateAll();

            IsEditing = false;
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Code), Code);
            this.SeedValue(nameof(AccountingEntityId), AccountingEntityId);
            this.SeedValue(nameof(PaymentMethodPrefix), PaymentMethodPrefix);
            this.AcceptChanges();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(PaymentMethodPrefix), PaymentMethodPrefix);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<BankGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "bank", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Code)
                    .Field(e => e.PaymentMethodPrefix)
                    .Select(e => e.AccountingEntity, ae => ae
                        .Field(a => a.Id)
                        .Field(a => a.SearchName)
                        .Field(a => a.CaptureType)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateBankInput!");
            var fragment = new GraphQLQueryFragment("createBank", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<BankGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "bank", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Code)
                    .Field(e => e.PaymentMethodPrefix)
                    .Select(e => e.AccountingEntity, ae => ae
                        .Field(a => a.Id)
                        .Field(a => a.SearchName)
                        .Field(a => a.CaptureType)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateBankInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateBank", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<BankGraphQLModel>> ExecuteSaveAsync()
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
                ? await _bankService.CreateAsync<UpsertResponseType<BankGraphQLModel>>(query, variables)
                : await _bankService.UpdateAsync<UpsertResponseType<BankGraphQLModel>>(query, variables);
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<BankGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new BankCreateMessage { CreatedBank = result });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new BankUpdateMessage { UpdatedBank = result });
            }
        }

        #endregion
    }
}
