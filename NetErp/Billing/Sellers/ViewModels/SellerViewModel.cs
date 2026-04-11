using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Global;
using NetErp.Billing.Sellers.Validators;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.Sellers.ViewModels
{
    public class SellerViewModel : Screen,
        IHandle<SellerCreateMessage>,
        IHandle<SellerUpdateMessage>,
        IHandle<SellerDeleteMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
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
        private readonly IGraphQLClient _graphQLClient;
        private readonly SellerValidator _validator;
        private readonly PermissionCache _permissionCache;

        #endregion

        #region Grid Properties

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

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanCreateSeller));
                }
            }
        }

        private readonly DebouncedAction _searchDebounce;

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
                        _ = _searchDebounce.RunAsync(LoadSellersAsync);
                    }
                }
            }
        } = string.Empty;

        public bool ShowActiveSellersOnly
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowActiveSellersOnly));
                    _ = LoadSellersAsync();
                }
            }
        } = true;

        public int? SelectedCostCenterId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenterId));
                    _ = LoadSellersAsync();
                }
            }
        }

        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        } = [];

        public ObservableCollection<SellerDTO> Sellers
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Sellers));
                }
            }
        } = [];

        public SellerDTO? SelectedSeller
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedSeller));
                    NotifyOfPropertyChange(nameof(CanDeleteSeller));
                    NotifyOfPropertyChange(nameof(CanEditSeller));
                }
            }
        }

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

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.Seller.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.Seller.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.Seller.Delete);

        #endregion

        #region Button States

        public bool CanCreateSeller => HasCreatePermission && !IsBusy;
        public bool CanEditSeller => HasEditPermission && SelectedSeller is not null;
        public bool CanDeleteSeller => HasDeletePermission && SelectedSeller is not null;

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
            JoinableTaskFactory joinableTaskFactory,
            IGraphQLClient graphQLClient,
            SellerValidator validator,
            PermissionCache permissionCache,
            DebouncedAction searchDebounce)
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
            _graphQLClient = graphQLClient;
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _permissionCache = permissionCache ?? throw new ArgumentNullException(nameof(permissionCache));
            _searchDebounce = searchDebounce ?? throw new ArgumentNullException(nameof(searchDebounce));

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

                NotifyOfPropertyChange(nameof(HasCreatePermission));
                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasDeletePermission));
                NotifyOfPropertyChange(nameof(CanCreateSeller));
                NotifyOfPropertyChange(nameof(CanEditSeller));
                NotifyOfPropertyChange(nameof(CanDeleteSeller));

                await _costCenterCache.EnsureLoadedAsync();
                CostCenters = [.. _costCenterCache.Items];
                await LoadSellersAsync();
                _isInitialized = true;
                ShowEmptyState = Sellers == null || Sellers.Count == 0;
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
                var detail = new SellerDetailViewModel(_sellerService, _eventAggregator, _identificationTypeCache, _countryCache, _zoneCache, _costCenterCache, _stringLengthCache, AutoMapper, _joinableTaskFactory, _graphQLClient, _validator);
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
                    $"{GetType().Name}.{nameof(CreateSellerAsync)}: {ex.GetErrorMessage()}",
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
                var detail = new SellerDetailViewModel(_sellerService, _eventAggregator, _identificationTypeCache, _countryCache, _zoneCache, _costCenterCache, _stringLengthCache, AutoMapper, _joinableTaskFactory, _graphQLClient, _validator);
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
                    $"{GetType().Name}.{nameof(EditSellerAsync)}: {ex.GetErrorMessage()}",
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
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(DeleteSellerAsync)}: {ex.GetErrorMessage()}",
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

                PageType<SellerGraphQLModel> result = await _sellerService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Sellers = new ObservableCollection<SellerDTO>(AutoMapper.Map<ObservableCollection<SellerDTO>>(result.Entries));
                stopwatch.Stop();

                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadSellersAsync)}: {ex.GetErrorMessage()}",
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
                        .Field(c => c!.Id)
                        .Field(c => c!.VerificationDigit)
                        .Field(c => c!.IdentificationNumber)
                        .Field(c => c!.Address)
                        .Field(c => c!.SearchName)
                        .Field(c => c!.TelephonicInformation)))
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
            ShowEmptyState = false;
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
            ShowEmptyState = Sellers == null || Sellers.Count == 0;
            SelectedSeller = null;
            _notificationService.ShowSuccess(message.DeletedSeller.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateSeller));
            NotifyOfPropertyChange(nameof(CanEditSeller));
            NotifyOfPropertyChange(nameof(CanDeleteSeller));
            return Task.CompletedTask;
        }

        #endregion
    }
}
