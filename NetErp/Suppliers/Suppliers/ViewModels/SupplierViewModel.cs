using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
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
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Suppliers.Suppliers.ViewModels
{
    public class SupplierViewModel : Screen,
        IHandle<SupplierCreateMessage>,
        IHandle<SupplierUpdateMessage>,
        IHandle<SupplierDeleteMessage>
    {
        #region Dependencies

        public IMapper AutoMapper { get; private set; }
        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<SupplierGraphQLModel> _supplierService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;

        // Caches
        private readonly IGraphQLClient _graphQLClient;
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly CountryCache _countryCache;
        private readonly WithholdingTypeCache _withholdingTypeCache;
        private readonly StringLengthCache _stringLengthCache;

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
                        _ = LoadSuppliersAsync();
                    }
                }
            }
        }

        private ObservableCollection<SupplierDTO> _suppliers = [];
        public ObservableCollection<SupplierDTO> Suppliers
        {
            get => _suppliers;
            set
            {
                if (_suppliers != value)
                {
                    _suppliers = value;
                    NotifyOfPropertyChange(nameof(Suppliers));
                }
            }
        }

        private SupplierDTO? _selectedSupplier;
        public SupplierDTO? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                if (_selectedSupplier != value)
                {
                    _selectedSupplier = value;
                    NotifyOfPropertyChange(nameof(SelectedSupplier));
                    NotifyOfPropertyChange(nameof(CanDeleteSupplier));
                    NotifyOfPropertyChange(nameof(CanEditSupplier));
                }
            }
        }

        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccounts = [];
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts
        {
            get => _accountingAccounts;
            set
            {
                if (_accountingAccounts != value)
                {
                    _accountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
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

        public bool CanEditSupplier => SelectedSupplier is not null;
        public bool CanDeleteSupplier => SelectedSupplier is not null;

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
            IGraphQLClient graphQLClient)
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

            _eventAggregator.SubscribeOnUIThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Supplier);
            await LoadSuppliersAsync(withDependencies: true);
            this.SetFocus(() => FilterSearch);
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
                var detail = new SupplierDetailViewModel(_supplierService, _eventAggregator, AccountingAccounts, _identificationTypeCache, _countryCache, _withholdingTypeCache, _stringLengthCache, AutoMapper, _graphQLClient);
                await detail.InitializeAsync();
                detail.SetForNew();
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nuevo proveedor");
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
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
                var detail = new SupplierDetailViewModel(_supplierService, _eventAggregator, AccountingAccounts, _identificationTypeCache, _countryCache, _withholdingTypeCache, _stringLengthCache, AutoMapper, _graphQLClient);
                await detail.InitializeAsync();
                await detail.LoadDataForEditAsync(SelectedSupplier.Id);
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar proveedor");
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
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
                Refresh();

                string canDeleteQuery = GetCanDeleteSupplierQuery();
                object canDeleteVars = new { canDeleteResponseId = SelectedSupplier.Id };
                var validation = await _supplierService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                string deleteQuery = GetDeleteSupplierQuery();
                object deleteVars = new { deleteResponseId = SelectedSupplier.Id };
                DeleteResponseType deletedSupplier = await _supplierService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedSupplier.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedSupplier.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new SupplierDeleteMessage { DeletedSupplier = deletedSupplier });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!);
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
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

                Stopwatch stopwatch = new();
                stopwatch.Start();

                string query = GetLoadSuppliersDataQuery(withDependencies);

                dynamic variables = new ExpandoObject();

                variables.pageResponseFilters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch))
                {
                    variables.pageResponseFilters.Matching = FilterSearch;
                }

                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.Page = PageIndex;
                variables.pageResponsePagination.PageSize = PageSize;

                if (withDependencies)
                {
                    variables.accountingAccountsFilters = new ExpandoObject();
                    variables.accountingAccountsFilters.only_auxiliary_accounts = true;

                    SupplierDataContext result = await _supplierService.GetDataContextAsync<SupplierDataContext>(query, variables);
                    TotalCount = result.Suppliers.TotalEntries;
                    Suppliers = AutoMapper.Map<ObservableCollection<SupplierDTO>>(result.Suppliers.Entries);
                    AccountingAccounts = result.AccountingAccounts.Entries;
                }
                else
                {
                    PageType<SupplierGraphQLModel> result = await _supplierService.GetPageAsync(query, variables);
                    TotalCount = result.TotalEntries;
                    Suppliers = AutoMapper.Map<ObservableCollection<SupplierDTO>>(result.Entries);
                }

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod()!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        public string GetLoadSuppliersDataQuery(bool withDependencies = false)
        {
            var suppliersFields = FieldSpec<PageType<SupplierGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.IsTaxFree)
                    .Field(e => e.IcaWithholdingRate)
                    .Select(e => e.IcaAccountingAccount, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Code)
                        .Field(c => c.Name))
                    .Select(e => e.AccountingEntity, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.IdentificationNumber)
                        .Field(c => c.VerificationDigit)
                        .Field(c => c.CaptureType)
                        .Field(c => c.SearchName)
                        .Field(c => c.FirstLastName)
                        .Field(c => c.MiddleLastName)
                        .Field(c => c.BusinessName)
                        .Field(c => c.PrimaryPhone)
                        .Field(c => c.SecondaryPhone)
                        .Field(c => c.PrimaryCellPhone)
                        .Field(c => c.SecondaryCellPhone)
                        .Field(c => c.Address)
                        .Field(c => c.TelephonicInformation)
                        .SelectList(e => e.Emails, email => email
                            .Field(c => c.Id)
                            .Field(c => c.Email)
                            .Field(c => c.Description)
                            .Field(c => c.IsElectronicInvoiceRecipient))))
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();

            var accountingAccountFields = FieldSpec<PageType<AccountingAccountGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Code))
                .Build();

            var accountingAccountParameters = new GraphQLQueryParameter("filters", "AccountingAccountFilters");
            var accountingAccountFragment = new GraphQLQueryFragment("accountingAccountsPage", [accountingAccountParameters], accountingAccountFields, "AccountingAccounts");

            var suppliersPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var suppliersParameters = new GraphQLQueryParameter("filters", "SupplierFilters");
            var suppliersFragment = new GraphQLQueryFragment("suppliersPage", [suppliersPagParameters, suppliersParameters], suppliersFields, withDependencies ? "suppliers" : "pageResponse");

            var builder = withDependencies
                ? new GraphQLQueryBuilder([suppliersFragment, accountingAccountFragment])
                : new GraphQLQueryBuilder([suppliersFragment]);

            return builder.GetQuery();
        }

        public string GetCanDeleteSupplierQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteSupplier", [parameter], fields, alias: "CanDeleteResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        public string GetDeleteSupplierQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteSupplier", [parameter], fields, alias: "DeleteResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        #endregion

        #region Event Handlers

        public async Task HandleAsync(SupplierCreateMessage message, CancellationToken cancellationToken)
        {
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
            SelectedSupplier = null;
            _notificationService.ShowSuccess(message.DeletedSupplier.Message);
        }

        #endregion
    }
}
