using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
using Models.Inventory;
using NetErp.Books.AccountingAccounts.DTO;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.ViewModels;
using NetErp.Inventory.MeasurementUnits.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.CatalogItems.PanelEditors
{
    public class ItemTypePanelEditor : CatalogItemsBasePanelEditor<ItemTypeDTO, ItemTypeGraphQLModel>
    {
        #region Fields

        private readonly IRepository<ItemTypeGraphQLModel> _itemTypeService;

        #endregion

        #region Constructor

        public ItemTypePanelEditor(
            CatalogRootMasterViewModel masterContext,
            IRepository<ItemTypeGraphQLModel> itemTypeService)
            : base(masterContext)
        {
            _itemTypeService = itemTypeService ?? throw new ArgumentNullException(nameof(itemTypeService));
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

        private string _prefixChar = string.Empty;
        public string PrefixChar
        {
            get => _prefixChar;
            set
            {
                if (_prefixChar != value)
                {
                    _prefixChar = value;
                    NotifyOfPropertyChange(nameof(PrefixChar));
                    this.TrackChange(nameof(PrefixChar));
                    ValidatePrefixChar();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _stockControl;
        public bool StockControl
        {
            get => _stockControl;
            set
            {
                if (_stockControl != value)
                {
                    _stockControl = value;
                    NotifyOfPropertyChange(nameof(StockControl));
                    this.TrackChange(nameof(StockControl));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private bool _stockControlEnable = true;
        public bool StockControlEnable
        {
            get => _stockControlEnable;
            set
            {
                if (_stockControlEnable != value)
                {
                    _stockControlEnable = value;
                    NotifyOfPropertyChange(nameof(StockControlEnable));
                }
            }
        }

        private int _catalogId;
        public int CatalogId
        {
            get => _catalogId;
            set
            {
                if (_catalogId != value)
                {
                    _catalogId = value;
                    NotifyOfPropertyChange(nameof(CatalogId));
                    this.TrackChange(nameof(CatalogId));
                }
            }
        }

        private int _defaultMeasurementUnitId;
        public int DefaultMeasurementUnitId
        {
            get => _defaultMeasurementUnitId;
            set
            {
                if (_defaultMeasurementUnitId != value)
                {
                    _defaultMeasurementUnitId = value;
                    NotifyOfPropertyChange(nameof(DefaultMeasurementUnitId));
                    this.TrackChange(nameof(DefaultMeasurementUnitId));
                    ValidateDefaultMeasurementUnit();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private int _defaultAccountingGroupId;
        public int DefaultAccountingGroupId
        {
            get => _defaultAccountingGroupId;
            set
            {
                if (_defaultAccountingGroupId != value)
                {
                    _defaultAccountingGroupId = value;
                    NotifyOfPropertyChange(nameof(DefaultAccountingGroupId));
                    this.TrackChange(nameof(DefaultAccountingGroupId));
                    ValidateDefaultAccountingGroup();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private MeasurementUnitDTO _selectedMeasurementUnit;
        public MeasurementUnitDTO SelectedMeasurementUnit
        {
            get => _selectedMeasurementUnit;
            set
            {
                if (_selectedMeasurementUnit != value)
                {
                    _selectedMeasurementUnit = value;
                    NotifyOfPropertyChange(nameof(SelectedMeasurementUnit));
                    if (value != null) DefaultMeasurementUnitId = value.Id;
                }
            }
        }

        private AccountingGroupDTO _selectedAccountingGroup;
        public AccountingGroupDTO SelectedAccountingGroup
        {
            get => _selectedAccountingGroup;
            set
            {
                if (_selectedAccountingGroup != value)
                {
                    _selectedAccountingGroup = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingGroup));
                    if (value != null) DefaultAccountingGroupId = value.Id;
                }
            }
        }

        public ObservableCollection<MeasurementUnitDTO> MeasurementUnits => MasterContext.MeasurementUnits;
        public ObservableCollection<AccountingGroupDTO> AccountingGroups => MasterContext.AccountingGroups;

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
                AddError(nameof(Name), "El nombre del tipo de item no puede estar vacío");
        }

        private void ValidatePrefixChar()
        {
            ClearErrors(nameof(PrefixChar));
            if (string.IsNullOrWhiteSpace(PrefixChar))
                AddError(nameof(PrefixChar), "El nombre corto del tipo de item no puede estar vacío");
            else if (PrefixChar.Length != 1)
                AddError(nameof(PrefixChar), "El nombre corto debe ser exactamente un caracter");
        }

        private void ValidateDefaultMeasurementUnit()
        {
            ClearErrors(nameof(SelectedMeasurementUnit));
            if (DefaultMeasurementUnitId <= 0)
                AddError(nameof(SelectedMeasurementUnit), "Debe seleccionar una unidad de medida");
        }

        private void ValidateDefaultAccountingGroup()
        {
            ClearErrors(nameof(SelectedAccountingGroup));
            if (DefaultAccountingGroupId <= 0)
                AddError(nameof(SelectedAccountingGroup), "Debe seleccionar un grupo contable");
        }

        public override void ValidateAll()
        {
            ValidateName();
            ValidatePrefixChar();
            ValidateDefaultMeasurementUnit();
            ValidateDefaultAccountingGroup();
        }

        #endregion

        #region SetForNew / SetForEdit

        public override void SetForNew(object context)
        {
            if (context is not int catalogId) return;

            OriginalDto = null;
            Id = 0;
            CatalogId = catalogId;
            Name = string.Empty;
            PrefixChar = string.Empty;
            StockControl = false;
            StockControlEnable = true;
            DefaultMeasurementUnitId = 0;
            DefaultAccountingGroupId = 0;
            SelectedMeasurementUnit = MeasurementUnits?.FirstOrDefault(x => x.Id == 0);
            SelectedAccountingGroup = AccountingGroups?.FirstOrDefault(x => x.Id == 0);

            SeedDefaultValues();
            ClearAllErrors();
            ValidateAll();
            IsEditing = true;
        }

        public override void SetForEdit(object dto)
        {
            if (dto is not ItemTypeDTO itemTypeDTO) return;

            OriginalDto = itemTypeDTO;
            Id = itemTypeDTO.Id;
            CatalogId = itemTypeDTO.Catalog?.Id ?? 0;
            Name = itemTypeDTO.Name;
            PrefixChar = itemTypeDTO.PrefixChar;
            StockControl = itemTypeDTO.StockControl;
            StockControlEnable = false;
            DefaultMeasurementUnitId = itemTypeDTO.DefaultMeasurementUnit?.Id ?? 0;
            DefaultAccountingGroupId = itemTypeDTO.DefaultAccountingGroup?.Id ?? 0;
            SelectedMeasurementUnit = MeasurementUnits?.FirstOrDefault(x => x.Id == DefaultMeasurementUnitId);
            SelectedAccountingGroup = AccountingGroups?.FirstOrDefault(x => x.Id == DefaultAccountingGroupId);

            SeedCurrentValues();
            ClearAllErrors();
            ValidateAll();
            IsEditing = false;
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(PrefixChar), PrefixChar);
            this.SeedValue(nameof(StockControl), StockControl);
            this.SeedValue(nameof(DefaultMeasurementUnitId), DefaultMeasurementUnitId);
            this.SeedValue(nameof(DefaultAccountingGroupId), DefaultAccountingGroupId);
            this.AcceptChanges();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(CatalogId), CatalogId);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<ItemTypeGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "itemType", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.PrefixChar)
                    .Field(e => e.StockControl)
                    .Select(e => e.DefaultMeasurementUnit, mu => mu
                        .Field(m => m.Id))
                    .Select(e => e.DefaultAccountingGroup, ag => ag
                        .Field(a => a.Id))
                    .Select(e => e.Catalog, c => c
                        .Field(cat => cat.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateItemTypeInput!");
            var fragment = new GraphQLQueryFragment("createItemType", [parameter], fields, "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<ItemTypeGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "itemType", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.PrefixChar)
                    .Field(e => e.StockControl)
                    .Select(e => e.DefaultMeasurementUnit, mu => mu
                        .Field(m => m.Id))
                    .Select(e => e.DefaultAccountingGroup, ag => ag
                        .Field(a => a.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateItemTypeInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateItemType", parameters, fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<ItemTypeGraphQLModel>> ExecuteSaveAsync()
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
                ? await _itemTypeService.CreateAsync<UpsertResponseType<ItemTypeGraphQLModel>>(query, variables)
                : await _itemTypeService.UpdateAsync<UpsertResponseType<ItemTypeGraphQLModel>>(query, variables);
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<ItemTypeGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new ItemTypeCreateMessage { CreatedItemType = result });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new ItemTypeUpdateMessage { UpdatedItemType = result });
            }
        }

        #endregion
    }
}
