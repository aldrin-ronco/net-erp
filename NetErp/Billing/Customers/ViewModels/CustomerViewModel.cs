using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Books;
using Models.Suppliers;
using NetErp.Billing.Customers.Validators;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Models.Global;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.Customers.ViewModels
{
    public class CustomerViewModel : Screen,
        IHandle<CustomerDeleteMessage>,
        IHandle<CustomerCreateMessage>,
        IHandle<CustomerUpdateMessage>,
        IHandle<AccountingEntityUpdateMessage>,
        IHandle<SellerUpdateMessage>,
        IHandle<SupplierUpdateMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        public IMapper AutoMapper { get; private set; }
        private readonly IEventAggregator _eventAggregator;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<CustomerGraphQLModel> _customerService;
        private readonly Helpers.IDialogService _dialogService;

        // Caches
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly CountryCache _countryCache;
        private readonly WithholdingTypeCache _withholdingTypeCache;
        private readonly ZoneCache _zoneCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly PermissionCache _permissionCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IGraphQLClient _graphQLClient;
        private readonly CustomerValidator _validator;

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
                    NotifyOfPropertyChange(nameof(CanCreateCustomer));
                }
            }
        }

        public ObservableCollection<CustomerGraphQLModel> Customers
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Customers));
                    NotifyOfPropertyChange(nameof(CanDeleteCustomer));
                }
            }
        } = [];

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

        private bool _isInitialized;

        public bool HasRecords => _isInitialized && !ShowEmptyState;

        public CustomerGraphQLModel? SelectedCustomer
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCustomer));
                    NotifyOfPropertyChange(nameof(CanDeleteCustomer));
                    NotifyOfPropertyChange(nameof(CanEditCustomer));
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
                        _ = _searchDebounce.RunAsync(LoadCustomersAsync);
                    }
                }
            }
        } = string.Empty;

        public bool ShowActiveCustomersOnly
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowActiveCustomersOnly));
                    _ = LoadCustomersAsync();
                }
            }
        } = true;

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

        #region Permissions

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.Customer.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.Customer.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.Customer.Delete);

        #endregion

        #region Button States

        public bool CanCreateCustomer => HasCreatePermission && !IsBusy;
        public bool CanEditCustomer => HasEditPermission && SelectedCustomer != null;
        public bool CanDeleteCustomer => HasDeletePermission && SelectedCustomer != null;

        #endregion

        #region Commands

        private ICommand? _createCustomerCommand;
        public ICommand CreateCustomerCommand
        {
            get
            {
                _createCustomerCommand ??= new AsyncCommand(CreateCustomerAsync);
                return _createCustomerCommand;
            }
        }

        private ICommand? _editCustomerCommand;
        public ICommand EditCustomerCommand
        {
            get
            {
                _editCustomerCommand ??= new AsyncCommand(EditCustomerAsync);
                return _editCustomerCommand;
            }
        }

        private ICommand? _deleteCustomerCommand;
        public ICommand DeleteCustomerCommand
        {
            get
            {
                _deleteCustomerCommand ??= new AsyncCommand(DeleteCustomerAsync);
                return _deleteCustomerCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadCustomersAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public CustomerViewModel(IMapper mapper,
                                 IEventAggregator eventAggregator,
                                 Helpers.Services.INotificationService notificationService,
                                 IRepository<CustomerGraphQLModel> customerService,
                                 Helpers.IDialogService dialogService,
                                 IdentificationTypeCache identificationTypeCache,
                                 CountryCache countryCache,
                                 WithholdingTypeCache withholdingTypeCache,
                                 ZoneCache zoneCache,
                                 StringLengthCache stringLengthCache,
                                 PermissionCache permissionCache,
                                 JoinableTaskFactory joinableTaskFactory,
                                 IGraphQLClient graphQLClient,
                                 CustomerValidator validator)
        {
            AutoMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _identificationTypeCache = identificationTypeCache ?? throw new ArgumentNullException(nameof(identificationTypeCache));
            _countryCache = countryCache ?? throw new ArgumentNullException(nameof(countryCache));
            _withholdingTypeCache = withholdingTypeCache ?? throw new ArgumentNullException(nameof(withholdingTypeCache));
            _zoneCache = zoneCache ?? throw new ArgumentNullException(nameof(zoneCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _permissionCache = permissionCache;
            _joinableTaskFactory = joinableTaskFactory;
            _graphQLClient = graphQLClient;
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));

            _eventAggregator.SubscribeOnUIThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Customer);
                NotifyOfPropertyChange(nameof(HasCreatePermission));
                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasDeletePermission));
                NotifyOfPropertyChange(nameof(CanCreateCustomer));
                NotifyOfPropertyChange(nameof(CanEditCustomer));
                NotifyOfPropertyChange(nameof(CanDeleteCustomer));
                await LoadCustomersAsync();
                _isInitialized = true;
                ShowEmptyState = Customers == null || Customers.Count == 0;
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
                Customers.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateCustomerAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new CustomerDetailViewModel(_customerService, _eventAggregator, _identificationTypeCache, _countryCache, _withholdingTypeCache, _zoneCache, _stringLengthCache, AutoMapper, _joinableTaskFactory, _graphQLClient, _validator);
                await detail.LoadCachesAsync();
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.55;
                    detail.DialogHeight = parentView.ActualHeight * 0.95;
                }

                await _dialogService.ShowDialogAsync(detail, "Nuevo cliente");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(CreateCustomerAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditCustomerAsync()
        {
            if (SelectedCustomer == null) return;
            try
            {
                IsBusy = true;
                var detail = new CustomerDetailViewModel(_customerService, _eventAggregator, _identificationTypeCache, _countryCache, _withholdingTypeCache, _zoneCache, _stringLengthCache, AutoMapper, _joinableTaskFactory, _graphQLClient, _validator);
                await detail.LoadCachesAsync();
                await detail.LoadDataForEditAsync(SelectedCustomer.Id);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.55;
                    detail.DialogHeight = parentView.ActualHeight * 0.95;
                }

                await _dialogService.ShowDialogAsync(detail, "Editar cliente");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(EditCustomerAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteCustomerAsync()
        {
            if (SelectedCustomer == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteCustomerQuery.Value;
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedCustomer.Id)
                    .Build();
                CanDeleteType validation = await _customerService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                DeleteResponseType deletedCustomer = await ExecuteDeleteAsync(SelectedCustomer.Id);

                if (!deletedCustomer.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedCustomer.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new CustomerDeleteMessage { DeletedCustomer = deletedCustomer },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(DeleteCustomerAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<DeleteResponseType> ExecuteDeleteAsync(int id)
        {
            try
            {
                var (fragment, query) = _deleteCustomerQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();
                return await _customerService.DeleteAsync<DeleteResponseType>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #endregion

        #region Load

        public async Task LoadCustomersAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadCustomersQuery.Value;

                dynamic filters = new ExpandoObject();
                if (ShowActiveCustomersOnly) filters.isActive = true;
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                var result = await _customerService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Customers = new ObservableCollection<CustomerGraphQLModel>(result.Entries);
                stopwatch.Stop();

                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(LoadCustomersAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadCustomersQuery = new(() =>
        {
            var fields = FieldSpec<PageType<CustomerGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.IsActive)
                    .Select(selector: e => e.AccountingEntity, nested: entity => entity
                        .Field(en => en.IdentificationNumber)
                        .Field(en => en.VerificationDigit)
                        .Field(en => en.SearchName)
                        .Field(en => en.Regime)
                        .Field(en => en.TelephonicInformation)
                        .Field(en => en.Address)))
                .Build();

            var fragment = new GraphQLQueryFragment("customersPage",
                [new("filters", "CustomerFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteCustomerQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteCustomer",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteCustomerQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteCustomer",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(CustomerDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadCustomersAsync();
            ShowEmptyState = Customers == null || Customers.Count == 0;
            SelectedCustomer = null;
            _notificationService.ShowSuccess(message.DeletedCustomer.Message);
        }

        public async Task HandleAsync(CustomerCreateMessage message, CancellationToken cancellationToken)
        {
            ShowEmptyState = false;
            await LoadCustomersAsync();
            _notificationService.ShowSuccess(message.CreatedCustomer.Message);
        }

        public async Task HandleAsync(CustomerUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadCustomersAsync();
            _notificationService.ShowSuccess(message.UpdatedCustomer.Message);
        }

        public Task HandleAsync(AccountingEntityUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadCustomersAsync();
        }

        public Task HandleAsync(SellerUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadCustomersAsync();
        }

        public Task HandleAsync(SupplierUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadCustomersAsync();
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateCustomer));
            NotifyOfPropertyChange(nameof(CanEditCustomer));
            NotifyOfPropertyChange(nameof(CanDeleteCustomer));
            return Task.CompletedTask;
        }

        #endregion
    }
}
