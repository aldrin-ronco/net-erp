using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
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

namespace NetErp.Books.AccountingEntities.ViewModels
{
    public class AccountingEntityViewModel : Screen,
        IHandle<AccountingEntityCreateMessage>,
        IHandle<AccountingEntityDeleteMessage>,
        IHandle<AccountingEntityUpdateMessage>,
        IHandle<CustomerCreateMessage>,
        IHandle<CustomerUpdateMessage>,
        IHandle<SellerCreateMessage>,
        IHandle<SellerUpdateMessage>,
        IHandle<SupplierCreateMessage>,
        IHandle<SupplierUpdateMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly CountryCache _countryCache;
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

        private ObservableCollection<AccountingEntityGraphQLModel> _accountingEntities = [];
        public ObservableCollection<AccountingEntityGraphQLModel> AccountingEntities
        {
            get => _accountingEntities;
            set
            {
                if (_accountingEntities != value)
                {
                    _accountingEntities = value;
                    NotifyOfPropertyChange(nameof(AccountingEntities));
                    NotifyOfPropertyChange(nameof(CanDeleteAccountingEntity));
                }
            }
        }

        private AccountingEntityGraphQLModel? _selectedAccountingEntity;
        public AccountingEntityGraphQLModel? SelectedAccountingEntity
        {
            get => _selectedAccountingEntity;
            set
            {
                if (_selectedAccountingEntity != value)
                {
                    _selectedAccountingEntity = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntity));
                    NotifyOfPropertyChange(nameof(CanEditAccountingEntity));
                    NotifyOfPropertyChange(nameof(CanDeleteAccountingEntity));
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
                        _ = LoadAccountingEntitiesAsync();
                    }
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

        public bool CanEditAccountingEntity => SelectedAccountingEntity != null;
        public bool CanDeleteAccountingEntity => SelectedAccountingEntity != null;

        #endregion

        #region Commands

        private ICommand? _createAccountingEntityCommand;
        public ICommand CreateAccountingEntityCommand
        {
            get
            {
                _createAccountingEntityCommand ??= new AsyncCommand(CreateAccountingEntityAsync);
                return _createAccountingEntityCommand;
            }
        }

        private ICommand? _editAccountingEntityCommand;
        public ICommand EditAccountingEntityCommand
        {
            get
            {
                _editAccountingEntityCommand ??= new AsyncCommand(EditAccountingEntityAsync);
                return _editAccountingEntityCommand;
            }
        }

        private ICommand? _deleteAccountingEntityCommand;
        public ICommand DeleteAccountingEntityCommand
        {
            get
            {
                _deleteAccountingEntityCommand ??= new AsyncCommand(DeleteAccountingEntityAsync);
                return _deleteAccountingEntityCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadAccountingEntitiesAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public AccountingEntityViewModel(
            IEventAggregator eventAggregator,
            Helpers.Services.INotificationService notificationService,
            IRepository<AccountingEntityGraphQLModel> accountingEntityService,
            Helpers.IDialogService dialogService,
            IdentificationTypeCache identificationTypeCache,
            CountryCache countryCache,
            StringLengthCache stringLengthCache)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _accountingEntityService = accountingEntityService ?? throw new ArgumentNullException(nameof(accountingEntityService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _identificationTypeCache = identificationTypeCache ?? throw new ArgumentNullException(nameof(identificationTypeCache));
            _countryCache = countryCache ?? throw new ArgumentNullException(nameof(countryCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));

            _eventAggregator.SubscribeOnUIThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.AccountingEntity);
            await LoadAccountingEntitiesAsync();
            this.SetFocus(() => FilterSearch);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
                AccountingEntities.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateAccountingEntityAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new AccountingEntityDetailViewModel(_accountingEntityService, _eventAggregator, _identificationTypeCache, _countryCache, _stringLengthCache);
                await detail.LoadCachesAsync();
                detail.SetForNew();
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nuevo tercero");
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

        public async Task EditAccountingEntityAsync()
        {
            if (SelectedAccountingEntity == null) return;
            try
            {
                IsBusy = true;
                var detail = new AccountingEntityDetailViewModel(_accountingEntityService, _eventAggregator, _identificationTypeCache, _countryCache, _stringLengthCache);
                await detail.LoadCachesAsync();
                await detail.LoadDataForEditAsync(SelectedAccountingEntity.Id);
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar tercero");
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

        public async Task DeleteAccountingEntityAsync()
        {
            if (SelectedAccountingEntity == null) return;
            try
            {
                IsBusy = true;
                Refresh();

                var (canDeleteFragment, canDeleteQuery) = _canDeleteAccountingEntityQuery.Value;
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedAccountingEntity.Id)
                    .Build();
                CanDeleteType validation = await _accountingEntityService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                var (deleteFragment, deleteQuery) = _deleteAccountingEntityQuery.Value;
                var deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedAccountingEntity.Id)
                    .Build();
                DeleteResponseType deletedAccountingEntity = await _accountingEntityService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedAccountingEntity.Success)
                {
                    ThemedMessageBox.Show(title: "Atención !",
                        text: $"No pudo ser eliminado el registro \n\n {deletedAccountingEntity.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new AccountingEntityDeleteMessage { DeletedAccountingEntity = deletedAccountingEntity });
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

        public async Task LoadAccountingEntitiesAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = new();
                stopwatch.Start();

                var (fragment, query) = _loadAccountingEntitiesQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                var result = await _accountingEntityService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                AccountingEntities = new ObservableCollection<AccountingEntityGraphQLModel>(result.Entries);
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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadAccountingEntitiesQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingEntityGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.IdentificationNumber)
                    .Field(e => e.VerificationDigit)
                    .Field(e => e.SearchName)
                    .Field(e => e.Regime)
                    .Field(e => e.TelephonicInformation)
                    .Field(e => e.Address))
                .Build();

            var fragment = new GraphQLQueryFragment("accountingEntitiesPage",
                [new("filters", "AccountingEntityFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteAccountingEntityQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteAccountingEntity",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteAccountingEntityQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteAccountingEntity",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(AccountingEntityCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingEntitiesAsync();
            _notificationService.ShowSuccess(message.CreatedAccountingEntity.Message);
        }

        public async Task HandleAsync(AccountingEntityUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingEntitiesAsync();
            _notificationService.ShowSuccess(message.UpdatedAccountingEntity.Message);
        }

        public async Task HandleAsync(AccountingEntityDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingEntitiesAsync();
            SelectedAccountingEntity = null;
            _notificationService.ShowSuccess(message.DeletedAccountingEntity.Message);
        }

        public Task HandleAsync(CustomerCreateMessage message, CancellationToken cancellationToken) => LoadAccountingEntitiesAsync();
        public Task HandleAsync(CustomerUpdateMessage message, CancellationToken cancellationToken) => LoadAccountingEntitiesAsync();
        public Task HandleAsync(SellerCreateMessage message, CancellationToken cancellationToken) => LoadAccountingEntitiesAsync();
        public Task HandleAsync(SellerUpdateMessage message, CancellationToken cancellationToken) => LoadAccountingEntitiesAsync();
        public Task HandleAsync(SupplierCreateMessage message, CancellationToken cancellationToken) => LoadAccountingEntitiesAsync();
        public Task HandleAsync(SupplierUpdateMessage message, CancellationToken cancellationToken) => LoadAccountingEntitiesAsync();

        #endregion
    }
}
