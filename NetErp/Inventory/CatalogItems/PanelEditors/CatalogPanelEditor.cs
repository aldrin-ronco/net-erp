using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Inventory;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.CatalogItems.PanelEditors
{
    public class CatalogPanelEditor : CatalogItemsBasePanelEditor<CatalogDTO, CatalogGraphQLModel>
    {
        #region Fields

        private readonly IRepository<CatalogGraphQLModel> _catalogService;
        private readonly Helpers.Cache.StringLengthCache _stringLengthCache;

        #endregion

        #region Constructor

        public CatalogPanelEditor(
            CatalogRootMasterViewModel masterContext,
            IRepository<CatalogGraphQLModel> catalogService,
            Helpers.Cache.StringLengthCache stringLengthCache)
            : base(masterContext)
        {
            _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
        }

        #endregion

        #region Properties

        // MaxLength properties from StringLengthCache
        public int NameMaxLength => _stringLengthCache.GetMaxLength<CatalogGraphQLModel>(nameof(Name));

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

        private void ValidateName()
        {
            ClearErrors(nameof(Name));
            if (string.IsNullOrWhiteSpace(Name))
                AddError(nameof(Name), "El nombre del catálogo no puede estar vacío");
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

            SeedDefaultValues();
            ClearAllErrors();
            ValidateAll();
            IsEditing = true;
        }

        public override void SetForEdit(object dto)
        {
            if (dto is not CatalogDTO catalogDTO) return;

            OriginalDto = catalogDTO;
            Id = catalogDTO.Id;
            Name = catalogDTO.Name;

            SeedCurrentValues();
            ClearAllErrors();
            ValidateAll();
            IsEditing = false;
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.AcceptChanges();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<CatalogGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "catalog", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .SelectList(e => e.ItemTypes, it => it
                        .Field(t => t.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateCatalogInput!");
            var fragment = new GraphQLQueryFragment("createCatalog", [parameter], fields, "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<CatalogGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "catalog", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .SelectList(e => e.ItemTypes, it => it
                        .Field(t => t.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateCatalogInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateCatalog", parameters, fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<CatalogGraphQLModel>> ExecuteSaveAsync()
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
                ? await _catalogService.CreateAsync<UpsertResponseType<CatalogGraphQLModel>>(query, variables)
                : await _catalogService.UpdateAsync<UpsertResponseType<CatalogGraphQLModel>>(query, variables);
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<CatalogGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new CatalogCreateMessage { CreatedCatalog = result });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new CatalogUpdateMessage { UpdatedCatalog = result });
            }
        }

        #endregion
    }
}
