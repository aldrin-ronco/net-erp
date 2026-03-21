using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
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

namespace NetErp.Global.MenuItem.ViewModels
{
    public class MenuItemViewModel : Screen,
        IHandle<MenuItemCreateMessage>,
        IHandle<MenuItemUpdateMessage>,
        IHandle<MenuItemDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<MenuItemGraphQLModel> _menuItemService;
        private readonly IRepository<MenuItemGroupGraphQLModel> _menuItemGroupService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly MenuModuleCache _menuModuleCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

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
                }
            }
        }

        public ObservableCollection<MenuItemGraphQLModel> MenuItems
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(MenuItems));
                }
            }
        } = [];

        public MenuItemGraphQLModel? SelectedMenuItem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedMenuItem));
                    NotifyOfPropertyChange(nameof(CanEditMenuItem));
                    NotifyOfPropertyChange(nameof(CanDeleteMenuItem));
                }
            }
        }

        #endregion

        #region Filter Properties

        private List<MenuItemGraphQLModel> _allItems = [];

        public ObservableCollection<MenuModuleGraphQLModel> Modules
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Modules));
                }
            }
        } = [];

        public ObservableCollection<MenuItemGroupGraphQLModel> Groups
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Groups));
                }
            }
        } = [];

        public int? SelectedModuleId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedModuleId));
                    UpdateGroupsForSelectedModule();
                    SelectedGroupId = null;
                    ApplyLocalFilters();
                }
            }
        }

        public int? SelectedGroupId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedGroupId));
                    ApplyLocalFilters();
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
                        _ = _searchDebounce.RunAsync(() => { ApplyLocalFilters(); return Task.CompletedTask; });
                    }
                }
            }
        } = string.Empty;

        public bool ShowActiveOnly
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowActiveOnly));
                    ApplyLocalFilters();
                }
            }
        } = true;

        #endregion

        #region Pagination

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

        #region Button States

        public bool CanEditMenuItem => SelectedMenuItem != null;
        public bool CanDeleteMenuItem => SelectedMenuItem != null;
        public bool CanReorder => SelectedModuleId.HasValue && SelectedGroupId.HasValue && MenuItems.Count > 1;

        #endregion

        #region Commands

        private ICommand? _createCommand;
        public ICommand CreateCommand
        {
            get
            {
                _createCommand ??= new AsyncCommand(CreateMenuItemAsync);
                return _createCommand;
            }
        }

        private ICommand? _editCommand;
        public ICommand EditCommand
        {
            get
            {
                _editCommand ??= new AsyncCommand(EditMenuItemAsync);
                return _editCommand;
            }
        }

        private ICommand? _deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                _deleteCommand ??= new AsyncCommand(DeleteMenuItemAsync);
                return _deleteCommand;
            }
        }

        private ICommand? _reorderCommand;
        public ICommand ReorderCommand
        {
            get
            {
                _reorderCommand ??= new AsyncCommand(ReorderAsync);
                return _reorderCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadAllItemsAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public MenuItemViewModel(
            IEventAggregator eventAggregator,
            IRepository<MenuItemGraphQLModel> menuItemService,
            IRepository<MenuItemGroupGraphQLModel> menuItemGroupService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            MenuModuleCache menuModuleCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _menuItemService = menuItemService ?? throw new ArgumentNullException(nameof(menuItemService));
            _menuItemGroupService = menuItemGroupService ?? throw new ArgumentNullException(nameof(menuItemGroupService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _menuModuleCache = menuModuleCache ?? throw new ArgumentNullException(nameof(menuModuleCache));
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
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.MenuItem);
                await _menuModuleCache.EnsureLoadedAsync();
                Modules = new ObservableCollection<MenuModuleGraphQLModel>(_menuModuleCache.Items);
                await LoadAllItemsAsync();
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
                MenuItems.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Load

        public async Task LoadAllItemsAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                (GraphQLQueryFragment fragment, string query) = _loadQuery.Value;

                dynamic variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = 1, PageSize = -1 })
                    .Build();

                PageType<MenuItemGraphQLModel> result = await _menuItemService.GetPageAsync(query, variables);

                _allItems = [.. result.Entries
                    .OrderBy(i => i.MenuItemGroup?.MenuModule?.DisplayOrder ?? 0)
                    .ThenBy(i => i.MenuItemGroup?.DisplayOrder ?? 0)
                    .ThenBy(i => i.DisplayOrder)];

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                ApplyLocalFilters();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadAllItemsAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateGroupsForSelectedModule()
        {
            if (SelectedModuleId == null)
            {
                Groups = [];
                return;
            }

            List<MenuItemGroupGraphQLModel> groupsForModule = [.. _allItems
                .Where(i => i.MenuItemGroup?.MenuModule?.Id == SelectedModuleId)
                .Select(i => i.MenuItemGroup!)
                .DistinctBy(g => g.Id)
                .OrderBy(g => g.DisplayOrder)];

            Groups = new ObservableCollection<MenuItemGroupGraphQLModel>(groupsForModule);
        }

        private void ApplyLocalFilters()
        {
            IEnumerable<MenuItemGraphQLModel> filtered = _allItems;

            if (SelectedModuleId.HasValue)
                filtered = filtered.Where(i => i.MenuItemGroup?.MenuModule?.Id == SelectedModuleId.Value);

            if (SelectedGroupId.HasValue)
                filtered = filtered.Where(i => i.MenuItemGroup?.Id == SelectedGroupId.Value);

            if (ShowActiveOnly)
                filtered = filtered.Where(i => i.IsActive);

            if (!string.IsNullOrEmpty(FilterSearch))
            {
                string search = FilterSearch.Trim().ToUpperInvariant();
                filtered = filtered.Where(i =>
                    (i.Name?.ToUpperInvariant().Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    (i.ItemKey?.ToUpperInvariant().Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false));
            }

            List<MenuItemGraphQLModel> result = filtered.ToList();
            TotalCount = result.Count;
            MenuItems = new ObservableCollection<MenuItemGraphQLModel>(result);
            NotifyOfPropertyChange(nameof(CanReorder));
        }

        #endregion

        #region CRUD Operations

        public async Task CreateMenuItemAsync()
        {
            try
            {
                IsBusy = true;
                MenuItemDetailViewModel detail = new(
                    _menuItemService, _menuItemGroupService, _eventAggregator,
                    _menuModuleCache, _stringLengthCache, _joinableTaskFactory);
                await detail.InitializeAsync();
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                    detail.DialogHeight = parentView.ActualHeight * 0.65;
                }

                await _dialogService.ShowDialogAsync(detail, "Nuevo ítem de menú");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(CreateMenuItemAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditMenuItemAsync()
        {
            if (SelectedMenuItem == null) return;
            try
            {
                IsBusy = true;
                MenuItemDetailViewModel detail = new(
                    _menuItemService, _menuItemGroupService, _eventAggregator,
                    _menuModuleCache, _stringLengthCache, _joinableTaskFactory);
                await detail.InitializeAsync();
                detail.SetForEdit(SelectedMenuItem);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                    detail.DialogHeight = parentView.ActualHeight * 0.65;
                }

                await _dialogService.ShowDialogAsync(detail, "Editar ítem de menú");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(EditMenuItemAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteMenuItemAsync()
        {
            if (SelectedMenuItem == null) return;
            try
            {
                IsBusy = true;

                (GraphQLQueryFragment canDeleteFragment, string canDeleteQuery) = _canDeleteQuery.Value;
                dynamic canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedMenuItem.Id)
                    .Build();
                CanDeleteType validation = await _menuItemService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención!",
                        "¿Confirma que desea eliminar el registro seleccionado?",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención!",
                        "El registro no puede ser eliminado" + (char)13 + (char)13 + validation.Message,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                DeleteResponseType deletedItem = await ExecuteDeleteAsync(SelectedMenuItem.Id);

                if (!deletedItem.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedItem.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new MenuItemDeleteMessage { DeletedMenuItem = deletedItem },
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
                    $"{GetType().Name}.{nameof(DeleteMenuItemAsync)}: {ex.Message}",
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
                (GraphQLQueryFragment fragment, string query) = _deleteQuery.Value;
                dynamic variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();
                return await _menuItemService.DeleteAsync<DeleteResponseType>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #endregion

        #region Ordering

        public async Task ReorderAsync()
        {
            if (!SelectedModuleId.HasValue || !SelectedGroupId.HasValue) return;
            try
            {
                IsBusy = true;
                List<MenuItemGraphQLModel> groupItems = _allItems
                    .Where(i => i.MenuItemGroup?.Id == SelectedGroupId.Value)
                    .OrderBy(i => i.DisplayOrder)
                    .ToList();

                MenuItemOrderViewModel orderVm = new(
                    _menuItemService, _joinableTaskFactory, groupItems);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    orderVm.DialogWidth = parentView.ActualWidth * 0.50;
                    orderVm.DialogHeight = parentView.ActualHeight * 0.80;
                }

                await _dialogService.ShowDialogAsync(orderVm, "Ordenar ítems de menú");

                if (orderVm.OrderChanged)
                {
                    await LoadAllItemsAsync();
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(ReorderAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<MenuItemGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.ItemKey)
                    .Field(e => e.Name)
                    .Field(e => e.Icon)
                    .Field(e => e.DisplayOrder)
                    .Field(e => e.IsLockable)
                    .Field(e => e.IsActive)
                    .Select(e => e.MenuItemGroup, g => g
                        .Field(g => g!.Id)
                        .Field(g => g!.Name)
                        .Field(g => g!.DisplayOrder)
                        .Select(g => g!.MenuModule, m => m
                            .Field(m => m!.Id)
                            .Field(m => m!.Name)
                            .Field(m => m!.DisplayOrder))))
                .Build();

            var fragment = new GraphQLQueryFragment("menuItemsPage",
                [new("filters", "MenuItemFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteMenuItem",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteMenuItem",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(MenuItemCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadAllItemsAsync();
            _notificationService.ShowSuccess(message.CreatedMenuItem.Message);
        }

        public async Task HandleAsync(MenuItemUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadAllItemsAsync();
            _notificationService.ShowSuccess(message.UpdatedMenuItem.Message);
        }

        public async Task HandleAsync(MenuItemDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadAllItemsAsync();
            SelectedMenuItem = null;
            _notificationService.ShowSuccess(message.DeletedMenuItem.Message);
        }

        #endregion
    }
}
