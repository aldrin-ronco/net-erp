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
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.Customers.ViewModels
{
    public class CustomerViewModel : Screen,
        IHandle<CustomerDeleteMessage>,
        IHandle<CustomerCreateMessage>,
        IHandle<CustomerUpdateMessage>,
        IHandle<AccountingEntityUpdateMessage>,
        IHandle<SellerUpdateMessage>,
        IHandle<SupplierUpdateMessage>
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
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region Grid Properties

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        private ObservableCollection<CustomerGraphQLModel> _customers = [];
        public ObservableCollection<CustomerGraphQLModel> Customers
        {
            get => _customers;
            set
            {
                if (_customers != value)
                {
                    _customers = value;
                    NotifyOfPropertyChange(nameof(Customers));
                    NotifyOfPropertyChange(nameof(CanDeleteCustomer));
                }
            }
        }

        private bool _showEmptyState;
        public bool ShowEmptyState
        {
            get => _showEmptyState;
            set
            {
                if (_showEmptyState != value)
                {
                    _showEmptyState = value;
                    NotifyOfPropertyChange(nameof(ShowEmptyState));
                    NotifyOfPropertyChange(nameof(HasRecords));
                }
            }
        }

        public bool HasRecords => !ShowEmptyState;

        private CustomerGraphQLModel? _selectedCustomer;
        public CustomerGraphQLModel? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (_selectedCustomer != value)
                {
                    _selectedCustomer = value;
                    NotifyOfPropertyChange(nameof(SelectedCustomer));
                    NotifyOfPropertyChange(nameof(CanDeleteCustomer));
                    NotifyOfPropertyChange(nameof(CanEditCustomer));
                }
            }
        }

        private readonly DebouncedAction _searchDebounce = new();

        private string _filterSearch = string.Empty;
        public string FilterSearch
        {
            get => _filterSearch;
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3)
                    {
                        PageIndex = 1;
                        _ = _searchDebounce.RunAsync(LoadCustomersAsync);
                    }
                }
            }
        }

        private bool _showActiveCustomersOnly = true;
        public bool ShowActiveCustomersOnly
        {
            get => _showActiveCustomersOnly;
            set
            {
                if (_showActiveCustomersOnly != value)
                {
                    _showActiveCustomersOnly = value;
                    NotifyOfPropertyChange(nameof(ShowActiveCustomersOnly));
                    _ = LoadCustomersAsync();
                }
            }
        }

        private int _pageIndex = 1;
        public int PageIndex
        {
            get => _pageIndex;
            set
            {
                if (_pageIndex != value)
                {
                    _pageIndex = value;
                    NotifyOfPropertyChange(nameof(PageIndex));
                }
            }
        }

        private int _pageSize = 50;
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    NotifyOfPropertyChange(nameof(PageSize));
                }
            }
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
            }
        }

        private string _responseTime = string.Empty;
        public string ResponseTime
        {
            get => _responseTime;
            set
            {
                if (_responseTime != value)
                {
                    _responseTime = value;
                    NotifyOfPropertyChange(nameof(ResponseTime));
                }
            }
        }

        #endregion

        #region Button States

        public bool CanEditCustomer => SelectedCustomer != null;
        public bool CanDeleteCustomer => SelectedCustomer != null;

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
                                 JoinableTaskFactory joinableTaskFactory)
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
            _joinableTaskFactory = joinableTaskFactory;

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
                await LoadCustomersAsync();
                ShowEmptyState = Customers == null || Customers.Count == 0;
                this.SetFocus(() => FilterSearch);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnViewReady)}: {ex.Message}",
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
                var detail = new CustomerDetailViewModel(_customerService, _eventAggregator, _identificationTypeCache, _countryCache, _withholdingTypeCache, _zoneCache, _stringLengthCache, AutoMapper, _joinableTaskFactory);
                await detail.LoadCachesAsync();
                detail.SetForNew();
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nuevo cliente");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(CreateCustomerAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                var detail = new CustomerDetailViewModel(_customerService, _eventAggregator, _identificationTypeCache, _countryCache, _withholdingTypeCache, _zoneCache, _stringLengthCache, AutoMapper, _joinableTaskFactory);
                await detail.LoadCachesAsync();
                await detail.LoadDataForEditAsync(SelectedCustomer.Id);
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar cliente");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(EditCustomerAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"Error al eliminar el registro.\r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(DeleteCustomerAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadCustomersAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

        #endregion
    }
}
