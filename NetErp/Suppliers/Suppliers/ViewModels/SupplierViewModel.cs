using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Suppliers;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Suppliers.Suppliers.Validators;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Suppliers.Suppliers.ViewModels
{
    public class SupplierViewModel : Screen,
        IHandle<SupplierCreateMessage>,
        IHandle<SupplierUpdateMessage>,
        IHandle<SupplierDeleteMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        public IMapper AutoMapper { get; private set; }
        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<SupplierGraphQLModel> _supplierService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;

        private readonly IGraphQLClient _graphQLClient;
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly CountryCache _countryCache;
        private readonly WithholdingTypeCache _withholdingTypeCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly SupplierValidator _validator;
        private readonly PermissionCache _permissionCache;

        #endregion

        #region Grid Properties

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanCreateSupplier));
                    NotifyOfPropertyChange(nameof(CanEditSupplier));
                    NotifyOfPropertyChange(nameof(CanDeleteSupplier));
                }
            }
        }

        private readonly DebouncedAction _searchDebounce = new();

        public string FilterSearch
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3)
                    {
                        PageIndex = 1;
                        _ = _searchDebounce.RunAsync(() => LoadSuppliersAsync());
                    }
                }
            }
        } = string.Empty;

        public ObservableCollection<SupplierDTO> Suppliers
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Suppliers));
                }
            }
        } = [];

        public SupplierDTO? SelectedSupplier
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedSupplier));
                    NotifyOfPropertyChange(nameof(CanDeleteSupplier));
                    NotifyOfPropertyChange(nameof(CanEditSupplier));
                }
            }
        }

        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                }
            }
        } = [];

        public int PageIndex
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PageIndex));
                }
            }
        } = 1;

        public int PageSize
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PageSize));
                }
            }
        } = 50;

        public int TotalCount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
            }
        }

        public string ResponseTime
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ResponseTime));
                }
            }
        } = string.Empty;

        #endregion

        #region Empty State

        private bool _isInitialized;

        public bool HasRecords => _isInitialized && !ShowEmptyState;

        public bool ShowEmptyState
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowEmptyState));
                    NotifyOfPropertyChange(nameof(HasRecords));
                }
            }
        }

        #endregion

        #region Permissions

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.Supplier.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.Supplier.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.Supplier.Delete);

        #endregion

        #region Button States

        public bool CanCreateSupplier => HasCreatePermission && !IsBusy;
        public bool CanEditSupplier => HasEditPermission && SelectedSupplier is not null && !IsBusy;
        public bool CanDeleteSupplier => HasDeletePermission && SelectedSupplier is not null && !IsBusy;

        #endregion

        #region Commands

        private ICommand? _createSupplierCommand;
        public ICommand CreateSupplierCommand
        {
            get
            {
                _createSupplierCommand ??= new AsyncCommand(CreateSupplierAsync);
                return _createSupplierCommand;
            }
        }

        private ICommand? _editSupplierCommand;
        public ICommand EditSupplierCommand
        {
            get
            {
                _editSupplierCommand ??= new AsyncCommand(EditSupplierAsync);
                return _editSupplierCommand;
            }
        }

        private ICommand? _deleteSupplierCommand;
        public ICommand DeleteSupplierCommand
        {
            get
            {
                _deleteSupplierCommand ??= new AsyncCommand(DeleteSupplierAsync);
                return _deleteSupplierCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(() => LoadSuppliersAsync());
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public SupplierViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<SupplierGraphQLModel> supplierService,
            Helpers.Services.INotificationService notificationService,
            IdentificationTypeCache identificationTypeCache,
            CountryCache countryCache,
            WithholdingTypeCache withholdingTypeCache,
            StringLengthCache stringLengthCache,
            Helpers.IDialogService dialogService,
            IGraphQLClient graphQLClient,
            JoinableTaskFactory joinableTaskFactory,
            SupplierValidator validator,
            PermissionCache permissionCache)
        {
            AutoMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _identificationTypeCache = identificationTypeCache ?? throw new ArgumentNullException(nameof(identificationTypeCache));
            _countryCache = countryCache ?? throw new ArgumentNullException(nameof(countryCache));
            _withholdingTypeCache = withholdingTypeCache ?? throw new ArgumentNullException(nameof(withholdingTypeCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _graphQLClient = graphQLClient ?? throw new ArgumentNullException(nameof(graphQLClient));
            _joinableTaskFactory = joinableTaskFactory;
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _permissionCache = permissionCache ?? throw new ArgumentNullException(nameof(permissionCache));

            _eventAggregator.SubscribeOnUIThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Supplier);

                await _permissionCache.EnsureLoadedAsync();
                NotifyOfPropertyChange(nameof(HasCreatePermission));
                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasDeletePermission));
                NotifyOfPropertyChange(nameof(CanCreateSupplier));
                NotifyOfPropertyChange(nameof(CanEditSupplier));
                NotifyOfPropertyChange(nameof(CanDeleteSupplier));

                await LoadSuppliersAsync(withDependencies: true);
                _isInitialized = true;
                ShowEmptyState = Suppliers == null || Suppliers.Count == 0;
                NotifyOfPropertyChange(nameof(HasRecords));
                this.SetFocus(() => FilterSearch);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnViewReady)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
                await TryCloseAsync();
            }
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
                Suppliers.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateSupplierAsync()
        {
            try
            {
                IsBusy = true;
                SupplierDetailViewModel detail = new(_supplierService, _eventAggregator, AccountingAccounts, _identificationTypeCache, _countryCache, _withholdingTypeCache, _stringLengthCache, AutoMapper, _graphQLClient, _joinableTaskFactory, _validator);
                await detail.InitializeAsync();
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.6;
                    detail.DialogHeight = parentView.ActualHeight * 0.95;
                }

                await _dialogService.ShowDialogAsync(detail, "Nuevo proveedor");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(CreateSupplierAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditSupplierAsync()
        {
            if (SelectedSupplier == null) return;
            try
            {
                IsBusy = true;
                SupplierDetailViewModel detail = new(_supplierService, _eventAggregator, AccountingAccounts, _identificationTypeCache, _countryCache, _withholdingTypeCache, _stringLengthCache, AutoMapper, _graphQLClient, _joinableTaskFactory, _validator);
                await detail.InitializeAsync();
                await detail.LoadDataForEditAsync(SelectedSupplier.Id);
                IsBusy = false;

                if (this.GetView() is FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.6;
                    detail.DialogHeight = parentView.ActualHeight * 0.95;
                }

                await _dialogService.ShowDialogAsync(detail, "Editar proveedor");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(EditSupplierAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteSupplierAsync()
        {
            if (SelectedSupplier == null) return;
            try
            {
                IsBusy = true;

                (GraphQLQueryFragment canDeleteFragment, string canDeleteQuery) = _canDeleteSupplierQuery.Value;
                object canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedSupplier.Id)
                    .Build();
                CanDeleteType validation = await _supplierService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención !",
                        "¿Confirma que desea eliminar el registro seleccionado?",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención !",
                        "El registro no puede ser eliminado" + (char)13 + (char)13 + validation.Message,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                (GraphQLQueryFragment deleteFragment, string deleteQuery) = _deleteSupplierQuery.Value;
                object deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedSupplier.Id)
                    .Build();
                DeleteResponseType deletedSupplier = await _supplierService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedSupplier.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedSupplier.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new SupplierDeleteMessage { DeletedSupplier = deletedSupplier },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(DeleteSupplierAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Load

        public async Task LoadSuppliersAsync(bool withDependencies = false)
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                dynamic filters = new System.Dynamic.ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.Matching = FilterSearch.Trim().RemoveExtraSpaces();

                if (withDependencies)
                {
                    (GraphQLQueryFragment suppliersFragment, GraphQLQueryFragment accountingAccountsFragment, string query) = _loadSuppliersWithDepsQuery.Value;

                    dynamic accountingFilters = new System.Dynamic.ExpandoObject();
                    accountingFilters.only_auxiliary_accounts = true;

                    object variables = new GraphQLVariables()
                        .For(suppliersFragment, "pagination", new { Page = PageIndex, PageSize })
                        .For(suppliersFragment, "filters", filters)
                        .For(accountingAccountsFragment, "filters", accountingFilters)
                        .Build();

                    SupplierDataContext result = await _supplierService.GetDataContextAsync<SupplierDataContext>(query, variables);
                    TotalCount = result.Suppliers.TotalEntries;
                    Suppliers = AutoMapper.Map<ObservableCollection<SupplierDTO>>(result.Suppliers.Entries);
                    AccountingAccounts = result.AccountingAccounts.Entries;
                }
                else
                {
                    (GraphQLQueryFragment fragment, string query) = _loadSuppliersQuery.Value;

                    object variables = new GraphQLVariables()
                        .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                        .For(fragment, "filters", filters)
                        .Build();

                    PageType<SupplierGraphQLModel> result = await _supplierService.GetPageAsync(query, variables);
                    TotalCount = result.TotalEntries;
                    Suppliers = AutoMapper.Map<ObservableCollection<SupplierDTO>>(result.Entries);
                }

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadSuppliersAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static FieldSpec<PageType<SupplierGraphQLModel>> BuildSuppliersFields()
        {
            return FieldSpec<PageType<SupplierGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.IsTaxFree)
                    .Field(e => e.IcaWithholdingRate)
                    .Select(e => e.IcaAccountingAccount, acc => acc
                        .Field(c => c!.Id)
                        .Field(c => c!.Code)
                        .Field(c => c!.Name))
                    .Select(e => e.AccountingEntity, acc => acc
                        .Field(c => c!.Id)
                        .Field(c => c!.IdentificationNumber)
                        .Field(c => c!.VerificationDigit)
                        .Field(c => c!.CaptureType)
                        .Field(c => c!.SearchName)
                        .Field(c => c!.FirstLastName)
                        .Field(c => c!.MiddleLastName)
                        .Field(c => c!.BusinessName)
                        .Field(c => c!.PrimaryPhone)
                        .Field(c => c!.SecondaryPhone)
                        .Field(c => c!.PrimaryCellPhone)
                        .Field(c => c!.SecondaryCellPhone)
                        .Field(c => c!.Address)
                        .Field(c => c!.TelephonicInformation)
                        .SelectList(e => e!.Emails, email => email
                            .Field(c => c.Id)
                            .Field(c => c.Email)
                            .Field(c => c.Description)
                            .Field(c => c.IsElectronicInvoiceRecipient))))
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries);
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadSuppliersQuery = new(() =>
        {
            Dictionary<string, object> fields = BuildSuppliersFields().Build();

            GraphQLQueryFragment fragment = new("suppliersPage",
                [new("filters", "SupplierFilters"), new("pagination", "Pagination")],
                fields, "pageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment SuppliersFragment, GraphQLQueryFragment AccountingAccountsFragment, string Query)> _loadSuppliersWithDepsQuery = new(() =>
        {
            Dictionary<string, object> suppliersFields = BuildSuppliersFields().Build();

            Dictionary<string, object> accountingAccountFields = FieldSpec<PageType<AccountingAccountGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Code))
                .Build();

            GraphQLQueryFragment suppliersFragment = new("suppliersPage",
                [new("pagination", "Pagination"), new("filters", "SupplierFilters")],
                suppliersFields, "suppliers");

            GraphQLQueryFragment accountingAccountsFragment = new("accountingAccountsPage",
                [new("filters", "AccountingAccountFilters")],
                accountingAccountFields, "accountingAccounts");

            return (suppliersFragment, accountingAccountsFragment,
                    new GraphQLQueryBuilder([suppliersFragment, accountingAccountsFragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteSupplierQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            GraphQLQueryFragment fragment = new("canDeleteSupplier",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteSupplierQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            GraphQLQueryFragment fragment = new("deleteSupplier",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(SupplierCreateMessage message, CancellationToken cancellationToken)
        {
            ShowEmptyState = false;
            await LoadSuppliersAsync();
            _notificationService.ShowSuccess(message.CreatedSupplier.Message);
        }

        public async Task HandleAsync(SupplierUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadSuppliersAsync();
            _notificationService.ShowSuccess(message.UpdatedSupplier.Message);
        }

        public async Task HandleAsync(SupplierDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadSuppliersAsync();
            ShowEmptyState = Suppliers == null || Suppliers.Count == 0;
            SelectedSupplier = null;
            _notificationService.ShowSuccess(message.DeletedSupplier.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateSupplier));
            NotifyOfPropertyChange(nameof(CanEditSupplier));
            NotifyOfPropertyChange(nameof(CanDeleteSupplier));
            return Task.CompletedTask;
        }

        #endregion
    }
}
