using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using NetErp.Helpers;
using Models.Books;
using Models.Global;
using NetErp.Billing.Zones.DTO;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.Sellers.ViewModels
{
    public class SellerViewModel : Screen,
        IHandle<SellerCreateMessage>,
        IHandle<SellerUpdateMessage>,
        IHandle<SellerDeleteMessage>
    {
        #region Dependencies

        public IMapper AutoMapper { get; private set; }
        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<SellerGraphQLModel> _sellerService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;

        // Caches
        private readonly CostCenterCache _costCenterCache;
        private readonly IdentificationTypeCache _identificationTypeCache;
        private readonly CountryCache _countryCache;
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
                        _ = LoadSellersAsync();
                    }
                }
            }
        }

        private bool _showActiveSellersOnly = true;
        public bool ShowActiveSellersOnly
        {
            get => _showActiveSellersOnly;
            set
            {
                if (_showActiveSellersOnly != value)
                {
                    _showActiveSellersOnly = value;
                    NotifyOfPropertyChange(nameof(ShowActiveSellersOnly));
                    _ = LoadSellersAsync();
                }
            }
        }

        private int? _selectedCostCenterId;
        public int? SelectedCostCenterId
        {
            get => _selectedCostCenterId;
            set
            {
                if (_selectedCostCenterId != value)
                {
                    _selectedCostCenterId = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenterId));
                    _ = LoadSellersAsync();
                }
            }
        }

        private ObservableCollection<CostCenterGraphQLModel> _costCenters = [];
        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get => _costCenters;
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        private ObservableCollection<SellerDTO> _sellers = [];
        public ObservableCollection<SellerDTO> Sellers
        {
            get => _sellers;
            set
            {
                if (_sellers != value)
                {
                    _sellers = value;
                    NotifyOfPropertyChange(nameof(Sellers));
                }
            }
        }

        private SellerDTO? _selectedSeller;
        public SellerDTO? SelectedSeller
        {
            get => _selectedSeller;
            set
            {
                if (_selectedSeller != value)
                {
                    _selectedSeller = value;
                    NotifyOfPropertyChange(nameof(SelectedSeller));
                    NotifyOfPropertyChange(nameof(CanDeleteSeller));
                    NotifyOfPropertyChange(nameof(CanEditSeller));
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

        public bool CanEditSeller => SelectedSeller is not null;
        public bool CanDeleteSeller => SelectedSeller is not null;

        #endregion

        #region Commands

        private ICommand? _createSellerCommand;
        public ICommand CreateSellerCommand
        {
            get
            {
                _createSellerCommand ??= new AsyncCommand(CreateSellerAsync);
                return _createSellerCommand;
            }
        }

        private ICommand? _editSellerCommand;
        public ICommand EditSellerCommand
        {
            get
            {
                _editSellerCommand ??= new AsyncCommand(EditSellerAsync);
                return _editSellerCommand;
            }
        }

        private ICommand? _deleteSellerCommand;
        public ICommand DeleteSellerCommand
        {
            get
            {
                _deleteSellerCommand ??= new AsyncCommand(DeleteSellerAsync);
                return _deleteSellerCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadSellersAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public SellerViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<SellerGraphQLModel> sellerService,
            Helpers.Services.INotificationService notificationService,
            CostCenterCache costCenterCache,
            IdentificationTypeCache identificationTypeCache,
            CountryCache countryCache,
            ZoneCache zoneCache,
            StringLengthCache stringLengthCache,
            Helpers.IDialogService dialogService,
            JoinableTaskFactory joinableTaskFactory)
        {
            AutoMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _sellerService = sellerService ?? throw new ArgumentNullException(nameof(sellerService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _costCenterCache = costCenterCache ?? throw new ArgumentNullException(nameof(costCenterCache));
            _identificationTypeCache = identificationTypeCache ?? throw new ArgumentNullException(nameof(identificationTypeCache));
            _countryCache = countryCache ?? throw new ArgumentNullException(nameof(countryCache));
            _zoneCache = zoneCache ?? throw new ArgumentNullException(nameof(zoneCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
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
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Seller);
            }
            catch (StringLengthNotAvailableException ex)
            {
                ThemedMessageBox.Show("Atención!", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
                await TryCloseAsync();
                return;
            }

            await _costCenterCache.EnsureLoadedAsync();
            CostCenters = [.. _costCenterCache.Items];

            await LoadSellersAsync();
            this.SetFocus(() => FilterSearch);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
                Sellers.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateSellerAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new SellerDetailViewModel(_sellerService, _eventAggregator, _identificationTypeCache, _countryCache, _zoneCache, _costCenterCache, _stringLengthCache, AutoMapper, _joinableTaskFactory);
                await detail.InitializeAsync();
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.55;
                    detail.DialogHeight = parentView.ActualHeight * 0.85;
                }

                await _dialogService.ShowDialogAsync(detail, "Nuevo vendedor");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(CreateSellerAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditSellerAsync()
        {
            if (SelectedSeller == null) return;
            try
            {
                IsBusy = true;
                var detail = new SellerDetailViewModel(_sellerService, _eventAggregator, _identificationTypeCache, _countryCache, _zoneCache, _costCenterCache, _stringLengthCache, AutoMapper, _joinableTaskFactory);
                await detail.InitializeAsync();
                await detail.LoadDataForEditAsync(SelectedSeller.Id);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.55;
                    detail.DialogHeight = parentView.ActualHeight * 0.85;
                }

                await _dialogService.ShowDialogAsync(detail, "Editar vendedor");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(EditSellerAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteSellerAsync()
        {
            if (SelectedSeller == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteSellerQuery.Value;
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedSeller.Id)
                    .Build();
                CanDeleteType validation = await _sellerService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                DeleteResponseType deletedSeller = await ExecuteDeleteAsync(SelectedSeller.Id);

                if (!deletedSeller.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedSeller.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new SellerDeleteMessage { DeletedSeller = deletedSeller },
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
                    $"{GetType().Name}.{nameof(DeleteSellerAsync)}: {ex.Message}",
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
                var (fragment, query) = _deleteSellerQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();
                return await _sellerService.DeleteAsync<DeleteResponseType>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #endregion

        #region Load

        public async Task LoadSellersAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadSellersQuery.Value;

                dynamic filters = new System.Dynamic.ExpandoObject();
                if (ShowActiveSellersOnly) filters.isActive = true;
                if (!string.IsNullOrEmpty(FilterSearch)) filters.Matching = FilterSearch.Trim().RemoveExtraSpaces();
                if (SelectedCostCenterId.HasValue) filters.costCenterId = SelectedCostCenterId.Value;

                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                var result = await _sellerService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Sellers = new ObservableCollection<SellerDTO>(AutoMapper.Map<ObservableCollection<SellerDTO>>(result.Entries));
                stopwatch.Stop();

                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadSellersAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadSellersQuery = new(() =>
        {
            var fields = FieldSpec<PageType<SellerGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.IsActive)
                    .Select(e => e.AccountingEntity, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.VerificationDigit)
                        .Field(c => c.IdentificationNumber)
                        .Field(c => c.Address)
                        .Field(c => c.SearchName)
                        .Field(c => c.TelephonicInformation)))
                .Build();

            var fragment = new GraphQLQueryFragment("sellersPage",
                [new("filters", "SellerFilters"), new("pagination", "Pagination")],
                fields, "pageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteSellerQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteSeller",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteSellerQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteSeller",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(SellerCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadSellersAsync();
            _notificationService.ShowSuccess(message.CreatedSeller.Message);
        }

        public async Task HandleAsync(SellerUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadSellersAsync();
            _notificationService.ShowSuccess(message.UpdatedSeller.Message);
        }

        public async Task HandleAsync(SellerDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadSellersAsync();
            SelectedSeller = null;
            _notificationService.ShowSuccess(message.DeletedSeller.Message);
        }

        #endregion
    }
}
