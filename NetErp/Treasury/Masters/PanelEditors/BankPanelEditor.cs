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
                }
            }
        }

        private int _accountingEntityId;
        [ExpandoPath("accountingEntityId")]
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
        [ExpandoPath("paymentMethodPrefix")]
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

        private void ValidateAccountingEntityName()
        {
            ClearErrors(nameof(AccountingEntityName));
            if (string.IsNullOrWhiteSpace(AccountingEntityName))
            {
                AddError(nameof(AccountingEntityName), "Debe seleccionar una entidad contable");
            }
        }

        public override void ValidateAll()
        {
            ValidateAccountingEntityName();
        }

        #endregion

        #region SetForNew / SetForEdit

        public override void SetForNew(object context)
        {
            OriginalDto = null;
            Id = 0;
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
            this.SeedValue(nameof(AccountingEntityId), AccountingEntityId);
            this.SeedValue(nameof(PaymentMethodPrefix), PaymentMethodPrefix);
            this.AcceptChanges();
        }

        private void SeedDefaultValues()
        {
            this.SeedValue(nameof(PaymentMethodPrefix), PaymentMethodPrefix);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<BankGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.PaymentMethodPrefix)
                .Select(e => e.AccountingEntity, ae => ae
                    .Field(a => a.Id)
                    .Field(a => a.SearchName)
                    .Field(a => a.CaptureType))
                .Build();

            var parameter = new GraphQLQueryParameter("data", "CreateBankInput!");
            var fragment = new GraphQLQueryFragment("createBank", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<BankGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.PaymentMethodPrefix)
                .Select(e => e.AccountingEntity, ae => ae
                    .Field(a => a.Id)
                    .Field(a => a.SearchName)
                    .Field(a => a.CaptureType))
                .Build();

            var fragment = new GraphQLQueryFragment(
                "updateBank",
                [
                    new GraphQLQueryParameter("id", "Int!"),
                    new GraphQLQueryParameter("data", "UpdateBankInput!")
                ],
                fields,
                "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<BankGraphQLModel>> ExecuteSaveAsync()
        {
            string query;
            dynamic variables = new ExpandoObject();

            if (IsNewRecord)
            {
                query = GetCreateQuery();
                variables.data = new ExpandoObject();
                variables.data.accountingEntityId = AccountingEntityId;
                variables.data.paymentMethodPrefix = PaymentMethodPrefix;

                var createResult = await _bankService.CreateAsync(query, variables);
                return new UpsertResponseType<BankGraphQLModel> { Entity = createResult };
            }
            else
            {
                query = GetUpdateQuery();
                variables.id = Id;
                variables.data = new ExpandoObject();
                variables.data.accountingEntityId = AccountingEntityId;
                variables.data.paymentMethodPrefix = PaymentMethodPrefix;

                var updateResult = await _bankService.UpdateAsync(query, variables);
                return new UpsertResponseType<BankGraphQLModel> { Entity = updateResult };
            }
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<BankGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new BankCreateMessage { CreatedBank = result.Entity });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new BankUpdateMessage { UpdatedBank = result.Entity });
            }
        }

        #endregion
    }
}
