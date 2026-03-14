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
    public class ItemSubCategoryPanelEditor : CatalogItemsBasePanelEditor<ItemSubCategoryDTO, ItemSubCategoryGraphQLModel>
    {
        #region Fields

        private readonly IRepository<ItemSubCategoryGraphQLModel> _itemSubCategoryService;
        private readonly Helpers.Cache.StringLengthCache _stringLengthCache;

        #endregion

        #region Constructor

        public ItemSubCategoryPanelEditor(
            CatalogRootMasterViewModel masterContext,
            IRepository<ItemSubCategoryGraphQLModel> itemSubCategoryService,
            Helpers.Cache.StringLengthCache stringLengthCache)
            : base(masterContext)
        {
            _itemSubCategoryService = itemSubCategoryService ?? throw new ArgumentNullException(nameof(itemSubCategoryService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
        }

        #endregion

        #region Properties

        // MaxLength properties from StringLengthCache
        public int NameMaxLength => _stringLengthCache.GetMaxLength<ItemSubCategoryGraphQLModel>(nameof(ItemSubCategoryGraphQLModel.Name));

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

        private int _itemCategoryId;
        public int ItemCategoryId
        {
            get => _itemCategoryId;
            set
            {
                if (_itemCategoryId != value)
                {
                    _itemCategoryId = value;
                    NotifyOfPropertyChange(nameof(ItemCategoryId));
                    this.TrackChange(nameof(ItemCategoryId));
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
                AddError(nameof(Name), "El nombre de la subcategoría no puede estar vacío");
        }

        public override void ValidateAll()
        {
            ValidateName();
        }

        #endregion

        #region SetForNew / SetForEdit

        public override void SetForNew(object context)
        {
            if (context is not int itemCategoryId) return;

            OriginalDto = null;
            Id = 0;
            ItemCategoryId = itemCategoryId;
            Name = string.Empty;

            SeedDefaultValues();
            ClearAllErrors();
            ValidateAll();
            IsEditing = true;
        }

        public override void SetForEdit(object dto)
        {
            if (dto is not ItemSubCategoryDTO itemSubCategoryDTO) return;

            OriginalDto = itemSubCategoryDTO;
            Id = itemSubCategoryDTO.Id;
            ItemCategoryId = itemSubCategoryDTO.ItemCategory?.Id ?? 0;
            Name = itemSubCategoryDTO.Name;

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
            this.SeedValue(nameof(ItemCategoryId), ItemCategoryId);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<ItemSubCategoryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "itemSubCategory", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.ItemCategory, ic => ic
                        .Field(c => c.Id)
                        .Select(c => c.ItemType, it => it
                            .Field(t => t.Id))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateItemSubCategoryInput!");
            var fragment = new GraphQLQueryFragment("createItemSubCategory", [parameter], fields, "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<ItemSubCategoryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "itemSubCategory", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.ItemCategory, ic => ic
                        .Field(c => c.Id)
                        .Select(c => c.ItemType, it => it
                            .Field(t => t.Id))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateItemSubCategoryInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateItemSubCategory", parameters, fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<ItemSubCategoryGraphQLModel>> ExecuteSaveAsync()
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
                ? await _itemSubCategoryService.CreateAsync<UpsertResponseType<ItemSubCategoryGraphQLModel>>(query, variables)
                : await _itemSubCategoryService.UpdateAsync<UpsertResponseType<ItemSubCategoryGraphQLModel>>(query, variables);
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<ItemSubCategoryGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new ItemSubCategoryCreateMessage { CreatedItemSubCategory = result });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new ItemSubCategoryUpdateMessage { UpdatedItemSubCategory = result });
            }
        }

        #endregion
    }
}
