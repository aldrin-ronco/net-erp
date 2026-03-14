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
    public class ItemCategoryPanelEditor : CatalogItemsBasePanelEditor<ItemCategoryDTO, ItemCategoryGraphQLModel>
    {
        #region Fields

        private readonly IRepository<ItemCategoryGraphQLModel> _itemCategoryService;
        private readonly Helpers.Cache.StringLengthCache _stringLengthCache;

        #endregion

        #region Constructor

        public ItemCategoryPanelEditor(
            CatalogRootMasterViewModel masterContext,
            IRepository<ItemCategoryGraphQLModel> itemCategoryService,
            Helpers.Cache.StringLengthCache stringLengthCache)
            : base(masterContext)
        {
            _itemCategoryService = itemCategoryService ?? throw new ArgumentNullException(nameof(itemCategoryService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
        }

        #endregion

        #region Properties

        // MaxLength properties from StringLengthCache
        public int NameMaxLength => _stringLengthCache.GetMaxLength<ItemCategoryGraphQLModel>(nameof(ItemCategoryGraphQLModel.Name));

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

        private int _itemTypeId;
        public int ItemTypeId
        {
            get => _itemTypeId;
            set
            {
                if (_itemTypeId != value)
                {
                    _itemTypeId = value;
                    NotifyOfPropertyChange(nameof(ItemTypeId));
                    this.TrackChange(nameof(ItemTypeId));
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
                AddError(nameof(Name), "El nombre de la categoría no puede estar vacío");
        }

        public override void ValidateAll()
        {
            ValidateName();
        }

        #endregion

        #region SetForNew / SetForEdit

        public override void SetForNew(object context)
        {
            if (context is not int itemTypeId) return;

            OriginalDto = null;
            Id = 0;
            ItemTypeId = itemTypeId;
            Name = string.Empty;

            SeedDefaultValues();
            ClearAllErrors();
            ValidateAll();
            IsEditing = true;
        }

        public override void SetForEdit(object dto)
        {
            if (dto is not ItemCategoryDTO itemCategoryDTO) return;

            OriginalDto = itemCategoryDTO;
            Id = itemCategoryDTO.Id;
            ItemTypeId = itemCategoryDTO.ItemType?.Id ?? 0;
            Name = itemCategoryDTO.Name;

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
            this.SeedValue(nameof(ItemTypeId), ItemTypeId);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<ItemCategoryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "itemCategory", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.ItemType, it => it
                        .Field(t => t.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateItemCategoryInput!");
            var fragment = new GraphQLQueryFragment("createItemCategory", [parameter], fields, "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<ItemCategoryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "itemCategory", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.ItemType, it => it
                        .Field(t => t.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateItemCategoryInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateItemCategory", parameters, fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<ItemCategoryGraphQLModel>> ExecuteSaveAsync()
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
                ? await _itemCategoryService.CreateAsync<UpsertResponseType<ItemCategoryGraphQLModel>>(query, variables)
                : await _itemCategoryService.UpdateAsync<UpsertResponseType<ItemCategoryGraphQLModel>>(query, variables);
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<ItemCategoryGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new ItemCategoryCreateMessage { CreatedItemCategory = result });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new ItemCategoryUpdateMessage { UpdatedItemCategory = result });
            }
        }

        #endregion
    }
}
