using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Inventory.CatalogItems.Validators;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    /// <summary>
    /// Detail dialog ViewModel para ItemType. Soporta Create y Update.
    /// Campos: Name, PrefixChar, StockControl, DefaultMeasurementUnit, DefaultAccountingGroup.
    /// Padre: CatalogId.
    /// </summary>
    public class ItemTypeDetailViewModel : CatalogItemsDetailViewModelBase,
        IHandle<ItemTypeCreateMessage>,
        IHandle<ItemTypeUpdateMessage>,
        IHandle<ItemTypeDeleteMessage>
    {
        // A-Z letters as a shared read-only array so the pool recomputation does not
        // allocate 26 strings on every dialog open or refresh.
        private static readonly string[] _allLetters =
            [.. Enumerable.Range('A', 26).Select(c => ((char)c).ToString())];

        #region Dependencies

        private readonly IRepository<ItemTypeGraphQLModel> _itemTypeService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly MeasurementUnitCache _measurementUnitCache;
        private readonly AccountingGroupCache _accountingGroupCache;
        private readonly CatalogCache _catalogCache;
        private readonly ItemTypeValidator _validator;

        #endregion

        #region Constructor

        public ItemTypeDetailViewModel(
            IRepository<ItemTypeGraphQLModel> itemTypeService,
            IEventAggregator eventAggregator,
            StringLengthCache stringLengthCache,
            MeasurementUnitCache measurementUnitCache,
            AccountingGroupCache accountingGroupCache,
            CatalogCache catalogCache,
            JoinableTaskFactory joinableTaskFactory,
            ItemTypeValidator validator)
            : base(joinableTaskFactory, eventAggregator)
        {
            _itemTypeService = itemTypeService ?? throw new ArgumentNullException(nameof(itemTypeService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _measurementUnitCache = measurementUnitCache ?? throw new ArgumentNullException(nameof(measurementUnitCache));
            _accountingGroupCache = accountingGroupCache ?? throw new ArgumentNullException(nameof(accountingGroupCache));
            _catalogCache = catalogCache ?? throw new ArgumentNullException(nameof(catalogCache));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));

            DialogWidth = 520;
            DialogHeight = 380;
        }

        #endregion

        #region MaxLength

        public int NameMaxLength => _stringLengthCache.GetMaxLength<ItemTypeGraphQLModel>(nameof(ItemTypeGraphQLModel.Name));
        public int PrefixCharMaxLength => _stringLengthCache.GetMaxLength<ItemTypeGraphQLModel>(nameof(ItemTypeGraphQLModel.PrefixChar));

        #endregion

        #region Combo Sources

        public ObservableCollection<MeasurementUnitGraphQLModel> MeasurementUnits { get; private set; } = [];
        public ObservableCollection<AccountingGroupGraphQLModel> AccountingGroups { get; private set; } = [];

        /// <summary>
        /// Pool of prefix letters (A-Z) not yet used by any ItemType across the company.
        /// Uniqueness is tenant-global, so the pool is derived from <see cref="CatalogCache"/>
        /// by walking every catalog's <c>ItemTypes</c>. In edit mode, the current letter is
        /// kept in the pool so it stays selectable.
        /// </summary>
        public ObservableCollection<string> AvailablePrefixChars { get; private set; } = [];

        #endregion

        #region Form Properties

        [ExpandoPath("name")]
        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Name));
                    ValidateProperty(nameof(Name), value);
                    this.TrackChange(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("prefixChar")]
        public string PrefixChar
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PrefixChar));
                    ValidateProperty(nameof(PrefixChar), value);
                    this.TrackChange(nameof(PrefixChar), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("stockControl")]
        public bool StockControl
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(StockControl));
                    this.TrackChange(nameof(StockControl), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool StockControlEnable
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(StockControlEnable));
                }
            }
        } = true;

        [ExpandoPath("catalogId")]
        public int CatalogId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CatalogId));
                    this.TrackChange(nameof(CatalogId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("defaultMeasurementUnitId")]
        public int DefaultMeasurementUnitId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DefaultMeasurementUnitId));
                    ValidateProperty(nameof(DefaultMeasurementUnitId), value);
                    this.TrackChange(nameof(DefaultMeasurementUnitId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("defaultAccountingGroupId")]
        public int DefaultAccountingGroupId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DefaultAccountingGroupId));
                    ValidateProperty(nameof(DefaultAccountingGroupId), value);
                    this.TrackChange(nameof(DefaultAccountingGroupId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public MeasurementUnitGraphQLModel? SelectedMeasurementUnit
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedMeasurementUnit));
                    DefaultMeasurementUnitId = value?.Id ?? 0;
                }
            }
        }

        public AccountingGroupGraphQLModel? SelectedAccountingGroup
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingGroup));
                    DefaultAccountingGroupId = value?.Id ?? 0;
                }
            }
        }

        #endregion

        #region CanSave

        public override bool CanSave => _validator.CanSave(new ItemTypeCanSaveContext
        {
            IsBusy = IsBusy,
            Name = Name,
            PrefixChar = PrefixChar,
            DefaultMeasurementUnitId = DefaultMeasurementUnitId,
            DefaultAccountingGroupId = DefaultAccountingGroupId,
            HasChanges = this.HasChanges(),
            HasErrors = _errors.Count > 0
        });

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand => _saveCommand ??= new AsyncCommand(SaveAsync);

        private ICommand? _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new AsyncCommand(CancelAsync);

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew(int parentCatalogId)
        {
            LoadComboSources(currentPrefix: null);

            Id = 0;
            Name = string.Empty;
            PrefixChar = string.Empty;
            StockControl = false;
            StockControlEnable = true;
            CatalogId = parentCatalogId;
            DefaultMeasurementUnitId = 0;
            DefaultAccountingGroupId = 0;
            SelectedMeasurementUnit = null;
            SelectedAccountingGroup = null;

            SeedDefaultValues();
        }

        public void SetForEdit(ItemTypeGraphQLModel entity)
        {
            LoadComboSources(currentPrefix: entity.PrefixChar);

            Id = entity.Id;
            Name = entity.Name;
            PrefixChar = entity.PrefixChar;
            StockControl = entity.StockControl;
            StockControlEnable = false;
            CatalogId = entity.Catalog?.Id ?? 0;
            DefaultMeasurementUnitId = entity.DefaultMeasurementUnit?.Id ?? 0;
            DefaultAccountingGroupId = entity.DefaultAccountingGroup?.Id ?? 0;
            SelectedMeasurementUnit = MeasurementUnits.FirstOrDefault(x => x.Id == DefaultMeasurementUnitId);
            SelectedAccountingGroup = AccountingGroups.FirstOrDefault(x => x.Id == DefaultAccountingGroupId);

            SeedCurrentValues();
        }

        private void LoadComboSources(string? currentPrefix)
        {
            MeasurementUnits = [.. _measurementUnitCache.Items];
            AccountingGroups = [.. _accountingGroupCache.Items];

            RefreshPrefixPool(currentPrefix);

            NotifyOfPropertyChange(nameof(MeasurementUnits));
            NotifyOfPropertyChange(nameof(AccountingGroups));
        }

        /// <summary>
        /// Rebuilds <see cref="AvailablePrefixChars"/> from <see cref="CatalogCache"/>. The
        /// uniqueness of PrefixChar is tenant-global, so the pool is A-Z minus every letter
        /// already used across every catalog. The <paramref name="currentPrefix"/> is kept in
        /// the pool so an existing selection is not dropped (e.g. in edit mode, or when the
        /// live event handlers refresh the pool while the dialog is open).
        /// Comparison is case-insensitive and the "used" set is normalized to upper so legacy
        /// records with lowercase prefixes do not produce phantom collisions.
        /// </summary>
        private void RefreshPrefixPool(string? currentPrefix)
        {
            HashSet<string> used = new(StringComparer.OrdinalIgnoreCase);
            foreach (CatalogGraphQLModel catalog in _catalogCache.Items)
            {
                if (catalog.ItemTypes is null) continue;
                foreach (ItemTypeGraphQLModel type in catalog.ItemTypes)
                {
                    if (!string.IsNullOrEmpty(type.PrefixChar))
                        used.Add(type.PrefixChar.ToUpperInvariant());
                }
            }

            if (!string.IsNullOrEmpty(currentPrefix))
                used.Remove(currentPrefix.ToUpperInvariant());

            AvailablePrefixChars = [.. _allLetters.Where(letter => !used.Contains(letter))];
            NotifyOfPropertyChange(nameof(AvailablePrefixChars));
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(CatalogId), CatalogId);
            this.SeedValue(nameof(StockControl), StockControl);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(PrefixChar), PrefixChar);
            this.SeedValue(nameof(StockControl), StockControl);
            this.SeedValue(nameof(CatalogId), CatalogId);
            this.SeedValue(nameof(DefaultMeasurementUnitId), DefaultMeasurementUnitId);
            this.SeedValue(nameof(DefaultAccountingGroupId), DefaultAccountingGroupId);
            this.AcceptChanges();
        }

        #endregion

        #region Lifecycle

        protected override Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            // Subscribe once the dialog actually activates so an aborted open (e.g. when the
            // master returns null from setup) does not leave a dangling subscription behind.
            _eventAggregator.SubscribeOnUIThread(this);
            return base.OnInitializedAsync(cancellationToken);
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
                _eventAggregator.Unsubscribe(this);
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region IHandle — live prefix pool sync

        // If another tab creates / updates / deletes an ItemType while this dialog is open,
        // refresh the pool from the (already-updated) CatalogCache. Ordering is guaranteed:
        // CatalogCache is a singleton subscribed at app startup, so its handler runs before
        // this VM's handler in Caliburn's subscriber iteration.

        public Task HandleAsync(ItemTypeCreateMessage message, CancellationToken cancellationToken)
        {
            RefreshPrefixPool(currentPrefix: PrefixChar);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemTypeUpdateMessage message, CancellationToken cancellationToken)
        {
            RefreshPrefixPool(currentPrefix: PrefixChar);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemTypeDeleteMessage message, CancellationToken cancellationToken)
        {
            RefreshPrefixPool(currentPrefix: PrefixChar);
            return Task.CompletedTask;
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<ItemTypeGraphQLModel> result = await ExecuteSaveAsync();

                if (!result.Success)
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new ItemTypeCreateMessage { CreatedItemType = result }
                        : new ItemTypeUpdateMessage { UpdatedItemType = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"Error al realizar operación.\r\n{ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<ItemTypeGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    (GraphQLQueryFragment _, string query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _itemTypeService.CreateAsync<UpsertResponseType<ItemTypeGraphQLModel>>(query, variables);
                }
                else
                {
                    (GraphQLQueryFragment _, string query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _itemTypeService.UpdateAsync<UpsertResponseType<ItemTypeGraphQLModel>>(query, variables);
                }
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region Validation

        private void ValidateProperty(string propertyName, object? value)
        {
            ItemTypeValidationContext context = new()
            {
                Name = Name,
                PrefixChar = PrefixChar,
                DefaultMeasurementUnitId = DefaultMeasurementUnitId,
                DefaultAccountingGroupId = DefaultAccountingGroupId
            };
            IReadOnlyList<string> errors = _validator.Validate(propertyName, value, context);
            SetPropertyErrors(propertyName, errors);
        }

        private void ValidateProperties()
        {
            ItemTypeValidationContext context = new()
            {
                Name = Name,
                PrefixChar = PrefixChar,
                DefaultMeasurementUnitId = DefaultMeasurementUnitId,
                DefaultAccountingGroupId = DefaultAccountingGroupId
            };
            Dictionary<string, IReadOnlyList<string>> allErrors = _validator.ValidateAll(context);
            SetPropertyErrors(nameof(Name), allErrors.TryGetValue(nameof(Name), out IReadOnlyList<string>? e1) ? e1 : []);
            SetPropertyErrors(nameof(PrefixChar), allErrors.TryGetValue(nameof(PrefixChar), out IReadOnlyList<string>? e2) ? e2 : []);
            SetPropertyErrors(nameof(DefaultMeasurementUnitId), allErrors.TryGetValue(nameof(DefaultMeasurementUnitId), out IReadOnlyList<string>? e3) ? e3 : []);
            SetPropertyErrors(nameof(DefaultAccountingGroupId), allErrors.TryGetValue(nameof(DefaultAccountingGroupId), out IReadOnlyList<string>? e4) ? e4 : []);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<UpsertResponseType<ItemTypeGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "itemType", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.PrefixChar)
                    .Field(f => f.StockControl)
                    .Select(f => f.Catalog, c => c.Field(x => x.Id))
                    .Select(f => f.DefaultMeasurementUnit, mu => mu.Field(x => x.Id))
                    .Select(f => f.DefaultAccountingGroup, ag => ag.Field(x => x.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            GraphQLQueryFragment fragment = new("createItemType",
                [new("input", "CreateItemTypeInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<UpsertResponseType<ItemTypeGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "itemType", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.PrefixChar)
                    .Field(f => f.StockControl)
                    .Select(f => f.Catalog, c => c.Field(x => x.Id))
                    .Select(f => f.DefaultMeasurementUnit, mu => mu.Field(x => x.Id))
                    .Select(f => f.DefaultAccountingGroup, ag => ag.Field(x => x.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            GraphQLQueryFragment fragment = new("updateItemType",
                [new("data", "UpdateItemTypeInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
