using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using Models.Books;
using Models.Global;
using NetErp.Global.CostCenters.DTO;
using NetErp.Global.CostCenters.ViewModels;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.CostCenters.PanelEditors
{
    /// <summary>
    /// Panel Editor para la entidad Company.
    /// Maneja la lógica de edición y persistencia de compañías.
    /// Company solo soporta Update (no Create).
    /// </summary>
    public class CompanyPanelEditor : CostCentersBasePanelEditor<CompanyDTO, CompanyGraphQLModel>
    {
        #region Fields

        private readonly IRepository<CompanyGraphQLModel> _companyService;
        private readonly Helpers.IDialogService _dialogService;

        #endregion

        #region Constructor

        public CompanyPanelEditor(
            CostCenterMasterViewModel masterContext,
            IRepository<CompanyGraphQLModel> companyService,
            Helpers.IDialogService dialogService)
            : base(masterContext)
        {
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            Messenger.Default.Register<ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel>>(
                this,
                SearchWithTwoColumnsGridMessageToken.CompanyAccountingEntity,
                false,
                OnFindCompanyAccountingEntityMessage);
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

        private int _accountingEntityCompanyId;
        [ExpandoPath("accountingEntityCompanyId")]
        public int AccountingEntityCompanyId
        {
            get => _accountingEntityCompanyId;
            set
            {
                if (_accountingEntityCompanyId != value)
                {
                    _accountingEntityCompanyId = value;
                    NotifyOfPropertyChange(nameof(AccountingEntityCompanyId));
                    this.TrackChange(nameof(AccountingEntityCompanyId));
                    ValidateAccountingEntityCompany();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _accountingEntityCompanySearchName = string.Empty;
        public string AccountingEntityCompanySearchName
        {
            get => _accountingEntityCompanySearchName;
            set
            {
                if (_accountingEntityCompanySearchName != value)
                {
                    _accountingEntityCompanySearchName = value;
                    NotifyOfPropertyChange(nameof(AccountingEntityCompanySearchName));
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

            string fieldHeader1 = "Identificación";
            string fieldHeader2 = "Razón social";
            string fieldData1 = "IdentificationNumber";
            string fieldData2 = "SearchName";

            var viewModel = new SearchWithTwoColumnsGridViewModel<AccountingEntityGraphQLModel>(
                query, fieldHeader1, fieldHeader2, fieldData1, fieldData2, null,
                SearchWithTwoColumnsGridMessageToken.CompanyAccountingEntity, _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Buscar entidad contable");
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
                    .Field(e => e.SearchName))
                .Build();

            var filterParameter = new GraphQLQueryParameter("filters", "AccountingEntityFilters");
            var paginationParameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("accountingEntitiesPage", [filterParameter, paginationParameter], fields, "PageResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        public bool CanSearchAccountingEntity => IsEditing;

        private void OnFindCompanyAccountingEntityMessage(ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel> message)
        {
            if (message.ReturnedData is null) return;
            AccountingEntityCompanyId = message.ReturnedData.Id;
            AccountingEntityCompanySearchName = message.ReturnedData.SearchName;
        }

        #endregion

        #region CanSave

        public override bool CanSave
        {
            get
            {
                if (!IsEditing) return false;
                if (HasErrors) return false;
                if (!this.HasChanges()) return false;
                return true;
            }
        }

        #endregion

        #region Validation

        private void ValidateAccountingEntityCompany()
        {
            ClearErrors(nameof(AccountingEntityCompanySearchName));
            if (AccountingEntityCompanyId <= 0)
            {
                AddError(nameof(AccountingEntityCompanySearchName), "Debe seleccionar una entidad contable");
            }
        }

        public override void ValidateAll()
        {
            ValidateAccountingEntityCompany();
        }

        #endregion

        #region SetForNew / SetForEdit

        public override void SetForNew(object context)
        {
            // Company no soporta Create, solo Update
            throw new NotSupportedException("Company no soporta creación, solo actualización.");
        }

        public override void SetForEdit(object dto)
        {
            if (dto is not CompanyDTO companyDTO) return;

            OriginalDto = companyDTO;
            Id = companyDTO.Id;
            AccountingEntityCompanyId = companyDTO.CompanyEntity?.Id ?? 0;
            AccountingEntityCompanySearchName = companyDTO.CompanyEntity?.SearchName ?? string.Empty;

            this.SeedValue(nameof(AccountingEntityCompanyId), AccountingEntityCompanyId);
            this.AcceptChanges();
            ClearAllErrors();

            IsEditing = false;
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            // Company no soporta Create
            throw new NotSupportedException("Company no soporta creación.");
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<CompanyGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, overrideName: "company", alias: "entity", nested: entity => entity
                    .Field(e => e.Id)
                    .Select(e => e.CompanyEntity, companyEntity => companyEntity
                        .Field(ce => ce.Id)
                        .Field(ce => ce.SearchName)))
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameters = new GraphQLQueryParameter("accountingEntityId", "ID!");
            var fragment = new GraphQLQueryFragment("changeCurrentCompanyEntity", [parameters], fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<CompanyGraphQLModel>> ExecuteSaveAsync()
        {
            // Company solo soporta Update
            string query = GetUpdateQuery();
            dynamic variables = new ExpandoObject();
            variables.updateResponseAccountingEntityId = AccountingEntityCompanyId;

            return await _companyService.UpdateAsync<UpsertResponseType<CompanyGraphQLModel>>(query, variables);
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<CompanyGraphQLModel> result)
        {
            await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                new CompanyUpdateMessage { UpdatedCompany = result });
        }

        #endregion
    }
}
