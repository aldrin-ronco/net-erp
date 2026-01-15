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
- This affects decisions about:
  - CancellationToken usage (not needed for quick operations since tabs stay open)
  - Memory management (multiple ViewModels active at once)
  - Event subscriptions (ViewModels stay subscribed while tab is open)

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
- `PriceListCalculatorFactory` - Factory for price calculation strategies:
  - `StandardPriceListCalculator` - Standard pricing calculations
  - `AlternativePriceListCalculator` - Alternative pricing strategy

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

**GlobalDataCache** (`Common/Helpers/GlobalDataCache.cs`):
- Centralized cache for common master data (IdentificationTypes, Countries, etc.)
- Thread-safe with locks
- Initialized in `ShellViewModel` on application startup
- Prevents redundant API calls for frequently accessed data
- Methods:
  - `Initialize()` - Load all cached data
  - `Clear()` - Clear all cached data
  - `Refresh()` - Reload specific data sets

**ChangeTracker** (`Common/Helpers/ChangeTracker.cs`):
- Smart change tracking for ViewModels
- Automatically removes changes when values return to seed
- Supports collection observation via `INotifyCollectionChanged`
- Methods:
  - `RegisterChange(propertyName, currentValue)` - Track a change
  - `Seed(propertyName, value)` - Set initial/seed value
  - `AcceptChanges()` - Clear all changes
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
  - `this.ObserveCollection(nameof(PropertyName), collection)` - Observe collection changes

**ChangeCollector** (`Common/Helpers/ChangeCollector.cs`):
- Extracts only modified properties from ViewModels for API calls
- Automatically applies sanitizers from `SanitizerRegistry`
- Advanced features:
  - `collectionItemTransformers` - Transform items in collections before sending
  - `ExpandoPathAttribute` - Customize property paths in generated payload
  - `NormalizeForPayload()` - Normalize objects with `SerializeAsId` attribute
- Usage:
  ```csharp
  dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createInput");
  ```

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