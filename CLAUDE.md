# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build entire solution
dotnet build NetErp.sln

# Build specific project
dotnet build NetErp/NetErp.csproj

# Clean solution
dotnet clean NetErp.sln

# Run the WPF application (requires Windows)
dotnet run --project NetErp/NetErp.csproj
```

## Environment Setup

The application requires environment variable `NET_ERP_SQL_ENGINE` set to either:
- `POSTGRESQL` (production/primary)
- `SQLSERVER` (limited support)

Example: `$env:NET_ERP_SQL_ENGINE="POSTGRESQL"`

## Architecture Overview

### Target Framework
- **.NET 8.0 Windows** (`net8.0-windows`) - Required for WPF support

### Project Structure
- **NetErp** - Main WPF application with MVVM pattern using Caliburn.Micro
- **Models** - GraphQL models and DTOs for all business entities
- **Services** - Data access layer with PostgreSQL/SQL Server support via GraphQL
- **Common** - Shared interfaces, helpers, and utilities (including `IRepository<T>` and deprecated `IGenericDataAccess<T>`)
- **Extensions** - Extension methods for different modules
- **Dictionaries** - Static data and lookup tables
- **Interfaces** - Additional shared interfaces
- **DTOLibrary** - Data Transfer Objects for specific operations

### Key Patterns

**MVVM Architecture**: All UI follows strict Model-View-ViewModel pattern with Caliburn.Micro conventions:
- ViewModels inherit from `Screen` or `Conductor<T>`
- Master/Detail pattern: `*MasterViewModel` for list views, `*DetailViewModel` for forms
- Views auto-bind to ViewModels by naming convention

**Two module structures exist**:

1. **List-based modules** (Customers, Sellers, Zones, Suppliers, AccountingEntities): Three-tier `ViewModel` (Conductor) → `MasterViewModel` (list/search) → `DetailViewModel` (form). The Conductor activates master or detail as needed.

2. **Tree/hierarchical modules** (CatalogItems, CostCenters, Treasury): `ViewModel` (Conductor) → `MasterViewModel` (tree + panel area). The Master contains multiple `PanelEditor` instances (one per entity type in the tree). Selected tree node routes to the appropriate PanelEditor via pattern matching.

**Data Access Pattern**:
- **NEW STANDARD**: Use `IRepository<TModel>` with `GraphQLRepository<TModel>` implementation for all new services
  - CRUD operations via GraphQL with CancellationToken support (CreateAsync, UpdateAsync, DeleteAsync, FindByIdAsync)
  - Pagination support (GetPageAsync)
  - List operations (GetListAsync, CreateListAsync, SendMutationListAsync)
  - Generic context methods (GetDataContextAsync, MutationContextAsync)
- **DEPRECATED**: `IGenericDataAccess<TModel>` interface is being phased out gradually - avoid using for new implementations

**Dependency Injection**: Uses Ninject with database engine selection in `App.xaml.cs`:
```csharp
// Services bound based on NET_ERP_SQL_ENGINE environment variable
Bind(typeof(IGenericDataAccess<AccountingAccountGraphQLModel>))
    .To(SQLEngine == "POSTGRESQL" ?
        typeof(BooksServicesPostgreSQL.AccountingAccountService) :
        typeof(BooksServicesSQLServer.AccountingAccountService))
    .InSingletonScope();
```

### Business Modules

**Billing** (`NetErp.Billing`):
- Customer management with credit limits and tax exemptions
- Seller/Zone management for territories
- Price lists with multiple calculation strategies (Standard/Alternative)

**Books/Accounting** (`NetErp.Books`):
- Chart of accounts and accounting entities
- Journal entries and accounting books
- Financial reports (Auxiliary Book, Trial Balance, Income Statement)

**Inventory** (`NetErp.Inventory`):
- Product catalogs with categories and subcategories
- Measurement units and item sizes
- EAN/barcode management

**Treasury** (`NetErp.Treasury`):
- Cash drawer and bank account management
- Multi-location franchise support

**Suppliers** (`NetErp.Suppliers`):
- Supplier management with Master/Detail ViewModels
- Uses modern IRepository pattern
- Full CRUD operations

**Global** (`NetErp.Global`):
- **CostCenters** - Cost center management for accounting
- **DynamicControl** - Dynamic control generation system
- **Email/Smtp** - Email configuration and services
- **Parameter** - Global application parameters
- **MainMenu** - Application menu structure
- **Shell** - Main application shell/container
- **AuthorizationSequence** - Authorization sequence management
- **Modals** - Shared modal dialogs

**Login** (`NetErp.Login`):
- Separate authentication system
- Integrates with LoginAPIUrl endpoint

### API Configuration

GraphQL API endpoints configured in `Common.Helpers.ConnectionConfig`:

**Current Endpoints (Active)**:
- **MainGraphQLAPIUrl**: `https://api.qtsolutions.com.co/graphql` - Main API endpoint
- **LoginAPIUrl**: `https://accounts.qtsolutions.com.co/graphql` - Separate authentication API

**Deprecated Endpoints**:
- **GraphQLAPIUrl**: `https://localhost:7048/graphql/` (development) / `https://qts-erp-fox-api.herokuapp.com/graphql` (production)
- **DatabaseId**: Header for tenant isolation (being phased out)

### UI Framework

**DevExpress WPF Controls**: Primary UI component library (v23.2.3/24.2.3)
- Premium grids, editors, and layout controls
- Ribbon interface for main navigation
- Professional styling with Century Gothic font

**⚠️ CRITICAL: Tab-Based Navigation System**:
- The application uses **DXTabControl** for multi-document interface (MDI)
- Modules open in **concurrent tabs**, NOT by replacing views
- When a user opens a new module, both the previous and new modules remain active in separate tabs
- Multiple ViewModels can be active simultaneously in memory
- `OnDeactivateAsync(close: false)` is NOT called when switching between tabs
- `OnDeactivateAsync(close: true)` is ONLY called when the user explicitly closes a tab
- **OnDeactivateAsync cleanup** (unsubscribe events, clear collections) MUST check `if (close)` — otherwise switching tabs destroys module state:
  ```csharp
  protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
  {
      if (close)
      {
          Context.EventAggregator.Unsubscribe(this);
          Items.Clear();
      }
      return base.OnDeactivateAsync(close, cancellationToken);
  }
  ```

**⚠️ Caliburn.Micro Screen Lifecycle — When to Use Each Method**:

The lifecycle methods execute in this order when a Screen is activated for the first time:
`Constructor` → `OnInitializedAsync` → `OnActivatedAsync` → `OnViewAttached` → `OnViewReady`

On subsequent activations (e.g., switching back to a tab):
`OnActivatedAsync` only.

| Method | When it runs | What to do here | What NOT to do here |
|---|---|---|---|
| **Constructor** | Once, at DI resolution | Assign dependencies (`_service = service`), subscribe to events (`_eventAggregator.SubscribeOnUIThread(this)`) | **NEVER** run async logic, `Task.Run`, or access UI. Causes race conditions (e.g., fields assigned after `Task.Run` are null). |
| **OnInitializedAsync** | Once, first activation only | Load initial data that doesn't change often: caches (`EnsureLoadedAsync`), combo box sources, catalogs. This is the async equivalent of a "first-time setup". | Don't put data that needs refreshing on every tab switch. |
| **OnActivatedAsync** | Every activation (including first) | Refresh data that may have changed while the tab was inactive. Guard with `if (IsInitialized)` to skip on first activation (already handled by `OnInitializedAsync`). | Don't duplicate `OnInitializedAsync` work. |
| **OnViewReady** | Once, after the View is fully rendered | Set initial focus (`this.SetFocus(...)`), run initial validations (`ValidateProperties()`), call `AcceptChanges()`. For dialog-based detail views, this is where validation + CanSave notification happens. | Don't load data here — the view is already visible, so the user sees a blank screen while data loads. |
| **OnViewAttached** | Once, when View is attached to ViewModel | Rarely needed. Only use if you need a reference to the View object itself. | Don't use for focus management (use `OnViewReady` or code-behind `Loaded` event instead). |
| **OnDeactivateAsync** | Every deactivation or close | If `close == true`: unsubscribe events, clear collections, dispose resources. If `close == false`: do nothing (tab is just hidden, not destroyed). | Don't clean up on `close == false` — it destroys state when switching tabs. |

**Best practice examples:**

```csharp
// ✅ CORRECT: Constructor only assigns dependencies
public MyViewModel(IRepository<T> service, IEventAggregator eventAggregator, SomeCache cache)
{
    _service = service;
    _cache = cache;
    _eventAggregator = eventAggregator;
    _eventAggregator.SubscribeOnUIThread(this);
}

// ✅ CORRECT: OnInitializedAsync loads initial data (runs once)
protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
{
    await _cache.EnsureLoadedAsync();
    Combos = [.. _cache.Items];
    await LoadDataAsync();
    await base.OnInitializedAsync(cancellationToken);
}

// ✅ CORRECT: OnActivatedAsync refreshes on re-entry (skips first time)
protected override async Task OnActivatedAsync(CancellationToken cancellationToken)
{
    if (IsInitialized) await LoadDataAsync(); // refresh only on re-entry
    await base.OnActivatedAsync(cancellationToken);
}

// ✅ CORRECT: OnViewReady for focus and validation
protected override void OnViewReady(object view)
{
    base.OnViewReady(view);
    ValidateProperties();
    this.AcceptChanges();
    NotifyOfPropertyChange(nameof(CanSave));
}
```

```csharp
// ❌ WRONG: Async logic in constructor causes race conditions
public MyViewModel(IRepository<T> service, SomeCache cache)
{
    _service = service;
    _ = Task.Run(async () => await LoadDataAsync()); // ← NEVER DO THIS
    _cache = cache; // ← may be null when Task.Run accesses it
}
```

**For dialog-based detail views** (opened via `IDialogService.ShowDialogAsync`):
- Focus management goes in the **View's code-behind** via `Loaded` event, not in the ViewModel
- `OnViewReady` handles `ValidateProperties()` + `AcceptChanges()` + `NotifyOfPropertyChange(nameof(CanSave))`
- `SetForNew()` / `SetForEdit()` are called **before** `ShowDialogAsync`, not in lifecycle methods

**Key Services** (registered in `NinjectBootstrapper.cs`):
- `INotificationService` - In-app notifications
- `IBackgroundQueueService` - Async task processing (use with explicit instructions)
- `IParallelBatchProcessor` - Parallel batch processing (use with explicit instructions)
- `INetworkConnectivityService` - Internet connection monitoring
- `IGraphQLClient` - GraphQL client for API communication
- `ILoginService` - Authentication service
- `ISQLiteEmailStorageService` - Local SQLite storage for emails
- `IDialogService` - Dialog management
- `ICreditLimitValidator` - Customer credit limit validation
- Event aggregation via Caliburn.Micro's `IEventAggregator`

**Special Services**:
- `IPriceListCalculator` / `StandardPriceListCalculator` - Single calculator that handles both pricing strategies (margen-sobre-venta vs markup) via the `UseAlternativeFormula` flag on `PriceListGraphQLModel`, and honors `PriceListIncludeTax` to apply IVA when the stored price is tax-inclusive.

### Core Helpers and Utilities

**SanitizerRegistry** (`Common/Helpers/SanitizerRegistry.cs`):
- Automatic sanitization system for data cleaning
- Registered globally in `NinjectBootstrapper.cs`
- Integrated automatically into `ChangeCollector` and `ViewModelExtensions`
- Registration example:
  ```csharp
  SanitizerRegistry.RegisterType<string>(s =>
      string.IsNullOrWhiteSpace(s) ? null : s.Trim().RemoveExtraSpaces()
  );
  ```
- Supports:
  - Type-based sanitization (applies to all properties of a type)
  - Property-specific sanitization (override for specific properties)

**Entity Cache System** (`NetErp/Helpers/Cache/`):
- Two-tier interface: `IEntityCache` (non-generic, for centralized cleanup) and `IEntityCache<T>` (generic, for typed CRUD)
- All caches registered as **both concrete type AND `IEntityCache`** in Ninject:
  ```csharp
  _ = kernel.Bind<CostCenterCache>().ToSelf().InSingletonScope();
  _ = kernel.Bind<IEntityCache>().ToMethod(ctx => ctx.Kernel.Get<CostCenterCache>());
  ```
- `ShellViewModel` receives `IEnumerable<IEntityCache>` and calls `ClearAllCaches()` on logout/company switch
- Caches use **lazy loading** via `EnsureLoadedAsync()` — only load from API on first use
- Thread-safe with `lock` for all CRUD operations
- Event-driven updates: caches implement `IHandle<TCreateMessage>`, `IHandle<TUpdateMessage>`, `IHandle<TDeleteMessage>` to stay in sync
- Available caches: `CostCenterCache`, `IdentificationTypeCache`, `CountryCache`, `ZoneCache`, `WithholdingTypeCache`, `BankAccountCache`, `StringLengthCache`, and more

**ChangeTracker** (`Common/Helpers/ChangeTracker.cs`):
- Smart change tracking for ViewModels
- Automatically removes changes when values return to seed
- `AcceptChanges()` only clears the `_changed` HashSet, NOT `_seedValues` dictionary — seeds survive AcceptChanges
- `ClearSeeds()` clears the `_seedValues` dictionary — used before re-seeding for new records
- Supports collection observation via `INotifyCollectionChanged`
- Methods:
  - `RegisterChange(propertyName, currentValue)` - Track a change
  - `Seed(propertyName, value)` - Set initial/seed value
  - `AcceptChanges()` - Clear all changes (keeps seeds)
  - `ClearSeeds()` - Clear all seed values
  - `ObserveCollection(propertyName, collection)` - Track collection changes automatically
  - `HasChanges` - Boolean property indicating if any changes exist

**ViewModelExtensions** (`Common/Helpers/ViewModelExtensions.cs`):
- Extension methods for ViewModel change tracking
- Uses `ConditionalWeakTable<object, ChangeTracker>` to prevent memory leaks
- Methods:
  - `this.TrackChange(nameof(PropertyName))` - Track property change
  - `this.SeedValue(nameof(PropertyName), value)` - Set seed value
  - `this.HasChanges()` - Check if ViewModel has changes
  - `this.AcceptChanges()` - Clear all tracked changes
  - `this.ClearSeeds()` - Clear all seed values
  - `this.ObserveCollection(nameof(PropertyName), collection)` - Observe collection changes

**ChangeCollector** (`Common/Helpers/ChangeCollector.cs`):
- Extracts only modified properties from ViewModels for API calls
- Automatically applies sanitizers from `SanitizerRegistry`
- For CREATE operations (prefix contains "create"), iterates `tracker.SeedValues` and includes non-modified seeds in the payload — this is why seeding defaults is critical for new records
- Advanced features:
  - `collectionItemTransformers` - Transform items in collections before sending
  - `excludeProperties` - Exclude specific properties from payload
  - `ExpandoPathAttribute` - Customize property paths in generated payload
  - `NormalizeForPayload()` - Normalize objects with `SerializeAsId` attribute
- Usage:
  ```csharp
  dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createInput");
  ```

**StringLengthCache** (`NetErp/Helpers/Cache/StringLengthCache.cs`):
- Centralized cache that queries the API for max string lengths per entity field
- Queries the `stringLengths` GraphQL endpoint once per entity during the session
- Registered as singleton in `NinjectBootstrapper.cs` and implements `IEntityCache` (cleared on logout/company switch)
- **This is a standard part of every module's refactoring** — when evaluating what a module needs to be up to standard, StringLengthCache integration is required
- **Integration steps for a module**:
  1. **`StringLengthEntities.cs`** (`NetErp/Helpers/Cache/StringLengthEntities.cs`): Add a `Type[]` entry grouping the GraphQL model types the module needs (e.g., `public static readonly Type[] Customer = [typeof(CustomerGraphQLModel), typeof(AccountingEntityGraphQLModel)]`)
  2. **Root ViewModel** (e.g., `CustomerViewModel`): Inject `StringLengthCache` in constructor, call `await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Customer)` in `ActivateMasterViewAsync`
  3. **Detail ViewModel** (e.g., `CustomerDetailViewModel`): Receive `StringLengthCache` via constructor, expose MaxLength properties:
     ```csharp
     public int NameMaxLength => _stringLengthCache.GetMaxLength<ZoneGraphQLModel>(nameof(ZoneGraphQLModel.Name));
     ```
  4. **XAML View**: Bind `MaxLength="{Binding NameMaxLength}"` on `TextEdit` controls
  5. **For fields with RegEx mask** (e.g., IdentificationNumber, phones): MaxLength is ignored by DevExpress when mask is set — use `IdentificationNumberMask` property pattern instead:
     ```csharp
     public string IdentificationNumberMask
     {
         get
         {
             int max = IdentificationNumberMaxLength;
             bool allowsLetters = SelectedIdentificationType?.AllowsLetters ?? false;
             return allowsLetters ? $"[a-zA-Z0-9]{{0,{max}}}" : $"[0-9]{{0,{max}}}";
         }
     }
     ```
- Entity name resolution is automatic by convention: `AccountingEntityGraphQLModel` → strip "GraphQLModel" → `ToSnakeCase()` → `"accounting_entity"`. No hardcoded strings needed.
- `GetMaxLength` returns `0` when not found (DevExpress `TextEdit.MaxLength = 0` means no limit)

**QueryBuilder** (`NetErp/Helpers/GraphQLQueryBuilder/`):
- Type-safe GraphQL query construction
- Components:
  - `FieldSpec<T>` - Fluent builder for selecting fields
    - `.Field(f => f.PropertyName)` - Select simple field
    - `.Select(f => f.ComplexProperty, nested: sq => ...)` - Select nested object
    - `.SelectList(f => f.ListProperty, sq => ...)` - Select list/array
  - `GraphQLQueryParameter` - Define query parameters
  - `GraphQLQueryFragment` - Define reusable fragments
  - `GraphQLQueryBuilder` - Compose final query
- Supports both QUERY and MUTATION operations
- Auto-formats field names to camelCase by default
- Example:
  ```csharp
  var fields = FieldSpec<EntityType>
      .Create()
      .Field(f => f.Id)
      .Field(f => f.Name)
      .Select(f => f.Country, nested: sq => sq.Field(c => c.Name))
      .Build();

  var parameter = new GraphQLQueryParameter("input", "CreateEntityInput!");
  var fragment = new GraphQLQueryFragment("createEntity", [parameter], fields, "CreateResponse");
  var query = new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
  ```

### Standard ViewModel Patterns

#### SetForNew / SetForEdit Pattern

All Detail ViewModels and PanelEditors must use this dual-initialization pattern:

- **`SetForNew(context?)`**: Initialize for CREATE. Sets defaults (Id=0, empty strings, default selections from caches). Calls `SeedDefaultValues()` at the end.
- **`SetForEdit(dto/model)`**: Initialize for UPDATE. Populates all properties from the entity data. Calls `SeedCurrentValues()` at the end.

```csharp
public void SetForNew()
{
    Id = 0;
    Name = string.Empty;
    IsActive = true;
    SelectedCountry = Countries?.FirstOrDefault(c => c.Id == defaultCountryId);
    // ... set all defaults
    SeedDefaultValues();
}

public void SetForEdit(CustomerGraphQLModel customer)
{
    Id = customer.Id;
    Name = customer.Name;
    IsActive = customer.IsActive;
    SelectedCountry = Countries?.FirstOrDefault(c => c.Id == customer.Country?.Id);
    // ... populate all properties from entity
    SeedCurrentValues();
}
```

#### SeedDefaultValues / SeedCurrentValues Pattern

These methods establish the ChangeTracker baseline. Critical for ChangeCollector to work correctly:

- **`SeedDefaultValues()`**: For NEW records. Clears previous seeds, seeds only properties with meaningful defaults, then accepts changes.
- **`SeedCurrentValues()`**: For EDIT records. Seeds ALL editable properties with their current values, then accepts changes.

```csharp
private void SeedDefaultValues()
{
    this.ClearSeeds();  // Clear any previous seeds
    this.SeedValue(nameof(SelectedRegime), SelectedRegime);
    this.SeedValue(nameof(SelectedCaptureType), SelectedCaptureType);
    this.SeedValue(nameof(IsActive), IsActive);
    // ... seed properties that have defaults
    this.AcceptChanges();
}

private void SeedCurrentValues()
{
    this.SeedValue(nameof(SelectedRegime), SelectedRegime);
    this.SeedValue(nameof(BusinessName), BusinessName);
    this.SeedValue(nameof(FirstName), FirstName);
    this.SeedValue(nameof(IsActive), IsActive);
    // ... seed ALL editable properties
    this.AcceptChanges();
}
```

**Why this matters**: For CREATE, `ChangeCollector` uses seeded values as the payload base (it includes non-modified seeds). Without proper seeding, required fields like `regime` or `captureType` would be `null` in the API call.

#### CanSave Pattern

Standard validation for the Save button:

```csharp
public bool CanSave
{
    get
    {
        if (string.IsNullOrEmpty(RequiredField)) return false;
        if (SomeCollection.Where(f => f.IsSelected).Count() == 0) return false;
        if (!this.HasChanges()) return false;
        return _errors.Count <= 0;
    }
}
```

Every property setter that affects CanSave must call `NotifyOfPropertyChange(nameof(CanSave))`.

#### Save Flow with ChangeCollector

```csharp
public async Task<UpsertResponseType<TModel>> ExecuteSaveAsync()
{
    if (IsNewRecord)
    {
        string query = GetCreateQuery();
        dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
        return await _service.CreateAsync<UpsertResponseType<TModel>>(query, variables);
    }
    else
    {
        string query = GetUpdateQuery();
        dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
        variables.updateResponseId = Id;
        return await _service.UpdateAsync<UpsertResponseType<TModel>>(query, variables);
    }
}
```

#### Event Aggregation Pattern (Create/Update/Delete Messages)

After a successful save, publish a message so the MasterViewModel reloads:

```csharp
// In DetailViewModel after save
await Context.EventAggregator.PublishOnCurrentThreadAsync(
    IsNewRecord
        ? new CustomerCreateMessage() { CreatedCustomer = result }
        : new CustomerUpdateMessage() { UpdatedCustomer = result }
);

// In MasterViewModel - handle by reloading the list
public async Task HandleAsync(CustomerCreateMessage message, CancellationToken cancellationToken)
{
    await LoadCustomersAsync();
    _notificationService.ShowSuccess(message.CreatedCustomer.Message);
}
```

#### INotifyDataErrorInfo Validation Pattern

Detail ViewModels implement `INotifyDataErrorInfo` with a `Dictionary<string, List<string>> _errors`:

```csharp
private void ValidateProperty(string propertyName, string value)
{
    ClearErrors(propertyName);
    switch (propertyName)
    {
        case nameof(FirstName):
            if (string.IsNullOrEmpty(value.Trim()) && CaptureInfoAsPN)
                AddError(propertyName, "El primer nombre no puede estar vacío");
            break;
    }
}
```

#### Tab Validation Indicators

DXTabItem headers can show an orange dot when their tab contains validation errors, plus a ToolTip summarizing the errors:

```csharp
// ViewModel - group errors by tab
private static readonly string[] _basicDataFields = [nameof(FirstName), nameof(FirstLastName), ...];
public bool HasBasicDataErrors => _basicDataFields.Any(f => _errors.ContainsKey(f));
public string BasicDataTabTooltip => GetTabTooltip(_basicDataFields);

// Notify in RaiseErrorsChanged
if (_basicDataFields.Contains(propertyName))
{
    NotifyOfPropertyChange(nameof(BasicDataTabTooltip));
    NotifyOfPropertyChange(nameof(HasBasicDataErrors));
}
```

```xml
<!-- XAML -->
<dx:DXTabItem ToolTip="{Binding Data.BasicDataTabTooltip, Source={StaticResource DataContextProxy}}">
    <dx:DXTabItem.Header>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="Datos Básicos" VerticalAlignment="Center"/>
            <Ellipse Width="8" Height="8" Fill="#FFA500" Margin="6,0,0,0" VerticalAlignment="Center"
                     Visibility="{Binding Data.HasBasicDataErrors, Source={StaticResource DataContextProxy}, Converter={StaticResource BooleanToVisibilityConverter}}"/>
        </StackPanel>
    </dx:DXTabItem.Header>
```

#### Tree Module Structure (CostCenters, Treasury, CatalogItems)

Tree modules use PanelEditors instead of DetailViewModels:

```csharp
// MasterViewModel routes selected tree node to panel editor
public void HandleSelectedItemChanged()
{
    CurrentPanelEditor = _selectedItem switch
    {
        ItemTypeDTO => ItemTypeEditor,
        ItemCategoryDTO => ItemCategoryEditor,
        ItemDTO => ItemEditor,
        _ => null
    };

    if (!IsNewRecord && CurrentPanelEditor != null)
        CurrentPanelEditor.SetForEdit(_selectedItem);
}
```

PanelEditors inherit from a generic base class:
```csharp
public abstract class CostCentersBasePanelEditor<TDto, TGraphQLModel> : PropertyChangedBase, ICostCentersPanelEditor
```

### Development Notes

All GraphQL operations include error handling with custom exception extraction from API responses. The `IGenericDataAccess<T>` interface provides consistent error handling across all data operations.

Master ViewModels typically handle:
- Paginated data loading
- Search/filter operations
- CRUD operations with navigation to Detail views
- Selection state management

Detail ViewModels handle:
- Entity creation/editing forms
- Validation logic
- Save/Cancel operations
- Parent-child relationship management

### Standard Module Refactoring Checklist

When refactoring a module to be up to standard, ensure it has:

- [ ] `IRepository<T>` instead of `IGenericDataAccess<T>`
- [ ] `ChangeTracker` with `TrackChange()` in every property setter
- [ ] `SetForNew()` / `SetForEdit()` with `SeedDefaultValues()` / `SeedCurrentValues()`
- [ ] `ChangeCollector.CollectChanges()` for save payload generation
- [ ] `StringLengthCache` integration (MaxLength on all string TextEdits)
- [ ] `INotifyDataErrorInfo` validation
- [ ] `CanSave` with `HasChanges()` + error check
- [ ] Event aggregation messages (Create/Update/Delete) with MasterViewModel handlers
- [ ] `OnDeactivateAsync` guarded by `if (close)` for cleanup
- [ ] `ExpandoPath` attributes on properties that map to different GraphQL field names
- [ ] Queries built with `FieldSpec<T>` + `GraphQLQueryBuilder`

## Project Guidelines

- **Localization Guidelines**:
  - Informative messages to the user should be in Spanish
  - Class names, method names, variables, and other code elements should be in English

## Service Usage Guidelines

**IBackgroundQueueService** (`NetErp/Helpers/Services/BackgroundQueueService.cs`):
- Async task processing with queuing
- **Not a standard service** - use only with explicit instructions
- Registered in `NinjectBootstrapper` as singleton
- If one ViewModel uses it, doesn't mean all ViewModels should use it
- Usage requires understanding of queuing patterns and background processing

**IParallelBatchProcessor** (`NetErp/Helpers/Services/ParallelBatchProcessor.cs`):
- Parallel batch processing for large data sets
- **Not a standard service** - use only with explicit instructions
- Registered in `NinjectBootstrapper` as singleton
- Useful for processing multiple items concurrently
- If one ViewModel uses it, doesn't mean all ViewModels should use it
- Usage requires understanding of parallel processing patterns and thread safety

**General Guidelines**:
- Most ViewModels should use standard `IRepository<T>` pattern
- Only use specialized services when explicitly instructed or when the use case clearly requires them
- Performance optimizations should be justified by actual performance issues
