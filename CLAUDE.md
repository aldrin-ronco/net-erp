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

### Project Structure
- **NetErp** - Main WPF application with MVVM pattern using Caliburn.Micro
- **Models** - GraphQL models and DTOs for all business entities
- **Services** - Data access layer with PostgreSQL/SQL Server support via GraphQL
- **Common** - Shared interfaces, helpers, and utilities (including `IGenericDataAccess<T>`)
- **Extensions** - Extension methods for different modules
- **Dictionaries** - Static data and lookup tables

### Key Patterns

**MVVM Architecture**: All UI follows strict Model-View-ViewModel pattern with Caliburn.Micro conventions:
- ViewModels inherit from `Screen` or `Conductor<T>`
- Master/Detail pattern: `*MasterViewModel` for list views, `*DetailViewModel` for forms
- Views auto-bind to ViewModels by naming convention

**Data Access Pattern**: All services implement `IGenericDataAccess<TModel>` interface providing:
- CRUD operations via GraphQL (Create, Update, Delete, FindById)
- Pagination support (GetPage)
- List operations (GetList, CreateList)
- Generic context methods (GetDataContext, MutationContext)

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

### API Configuration

GraphQL API endpoints configured in `Common.Helpers.ConnectionConfig`:
- **Development**: `https://localhost:7048/graphql/`
- **Production**: `https://qts-erp-fox-api.herokuapp.com/graphql`
- **Multi-tenant**: Uses `DatabaseId` header for tenant isolation

### UI Framework

**DevExpress WPF Controls**: Primary UI component library (v23.2.3/24.2.3)
- Premium grids, editors, and layout controls
- Ribbon interface for main navigation
- Professional styling with Century Gothic font

**Key Services**:
- `INotificationService` - In-app notifications
- `IBackgroundQueueService` - Async task processing
- `NetworkConnectivityService` - Internet connection monitoring
- Event aggregation via Caliburn.Micro's `IEventAggregator`

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