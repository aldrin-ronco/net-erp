using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.ItemBrand.ViewModels
{
    internal class ItemBrandViewModel : Screen, 
        IHandle<ItemBrandCreateMessage>, 
        IHandle<ItemBrandUpdateMessage>, 
        IHandle<ItemBrandDeleteMessage>, 
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<ItemBrandGraphQLModel> _itemBrandService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly PermissionCache _permissionCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly DebouncedAction _searchDebounce = new();

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

        public ObservableCollection<ItemBrandGraphQLModel> ItemBrands
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ItemBrands));
                }
            }
        } = [];

        public ItemBrandGraphQLModel? SelectedItemBrand
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItemBrand));
                    NotifyOfPropertyChange(nameof(CanEditItem));
                    NotifyOfPropertyChange(nameof(CanDeleteItem));
                }
            }
        }

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
                        _ = _searchDebounce.RunAsync(LoadItemBrandsAsync);
                    }
                }
            }
        } = string.Empty;

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

        #region Permissions

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.ItemBrand.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.ItemBrand.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.ItemBrand.Delete);

        #endregion

        #region Button States

        public bool CanCreateItem => HasCreatePermission && !IsBusy;
        public bool CanEditItem => HasEditPermission && SelectedItemBrand != null;
        public bool CanDeleteItem => HasDeletePermission && SelectedItemBrand != null;

        #endregion

        #region Commands

        private ICommand? _createItemCommand;
        public ICommand CreateItemCommand
        {
            get
            {
                _createItemCommand ??= new AsyncCommand(CreateItemAsync);
                return _createItemCommand;
            }
        }

        private ICommand? _editItemCommand;
        public ICommand EditItemCommand
        {
            get
            {
                _editItemCommand ??= new AsyncCommand(EditItemAsync);
                return _editItemCommand;
            }
        }

        private ICommand? _deleteItemCommand;
        public ICommand DeleteItemCommand
        {
            get
            {
                _deleteItemCommand ??= new AsyncCommand(DeleteItemAsync);
                return _deleteItemCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadItemBrandsAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public ItemBrandViewModel(
            IEventAggregator eventAggregator,
            IRepository<ItemBrandGraphQLModel> itemBrandService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            JoinableTaskFactory joinableTaskFactory,
            PermissionCache permissionCache,
            StringLengthCache stringLengthCache)
        {
            _eventAggregator = eventAggregator;
            _itemBrandService = itemBrandService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _joinableTaskFactory = joinableTaskFactory;
            _permissionCache = permissionCache;
            _stringLengthCache = stringLengthCache;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                IsBusy = true;
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.ItemBrand);
                await LoadItemBrandsAsync();
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
            finally
            {
                IsBusy = false;
            }
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateItem));
            NotifyOfPropertyChange(nameof(CanEditItem));
            NotifyOfPropertyChange(nameof(CanDeleteItem));
            this.SetFocus(() => FilterSearch);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateItemAsync()
        {
            try
            {
                IsBusy = true;
                ItemBrandDetailViewModel detail = new(_itemBrandService, _eventAggregator, _joinableTaskFactory, _stringLengthCache);
                detail.SetForNew();
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nuevo marca");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(CreateItemAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditItemAsync()
        {
            if (SelectedItemBrand == null) return;
            try
            {
                IsBusy = true;
                ItemBrandDetailViewModel detail = new(_itemBrandService, _eventAggregator, _joinableTaskFactory, _stringLengthCache);
                detail.SetForEdit(SelectedItemBrand);
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar Item");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EditItemAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteItemAsync()
        {
            if (SelectedItemBrand == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteItemBrandQuery.Value;
                ExpandoObject canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedItemBrand.Id)
                    .Build();
                CanDeleteType validation = await _itemBrandService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                        $"El registro no puede ser eliminado\r\n\r\n{validation.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                var (deleteFragment, deleteQuery) = _deleteItemBrandQuery.Value;
                ExpandoObject deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedItemBrand.Id)
                    .Build();
                DeleteResponseType deletedItem = await _itemBrandService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedItem.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedItem.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new ItemBrandDeleteMessage { DeletedItemBrand = deletedItem });
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(DeleteItemAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Load

        public async Task LoadItemBrandsAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadItemBrandsQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<ItemBrandGraphQLModel> result = await _itemBrandService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                ItemBrands = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadItemBrandsAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadItemBrandsQuery = new(() =>
        {
            var fields = FieldSpec<PageType<ItemBrandGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name))
                .Build();

            var fragment = new GraphQLQueryFragment("ItemBrandsPage",
                [new("filters", "ItemBrandFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteItemBrandQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteItemBrand",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteItemBrandQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteItemBrand",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(ItemBrandCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadItemBrandsAsync();
            _notificationService.ShowSuccess(message.CreatedItemBrand.Message);
        }

        public async Task HandleAsync(ItemBrandUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadItemBrandsAsync();
            _notificationService.ShowSuccess(message.UpdatedItemBrand.Message);
        }

        public async Task HandleAsync(ItemBrandDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadItemBrandsAsync();
            SelectedItemBrand = null;
            _notificationService.ShowSuccess(message.DeletedItemBrand.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateItem));
            NotifyOfPropertyChange(nameof(CanEditItem));
            NotifyOfPropertyChange(nameof(CanDeleteItem));
            return Task.CompletedTask;
        }

     

        #endregion
    
    }
}
