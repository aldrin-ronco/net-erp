using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
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

namespace NetErp.Books.TaxCategory.ViewModels
{
    public class TaxCategoryViewModel : Screen,
        IHandle<TaxCategoryCreateMessage>,
        IHandle<TaxCategoryUpdateMessage>,
        IHandle<TaxCategoryDeleteMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<TaxCategoryGraphQLModel> _taxCategoryService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly PermissionCache _permissionCache;
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

        public ObservableCollection<TaxCategoryGraphQLModel> TaxCategories
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(TaxCategories));
                }
            }
        } = [];

        public TaxCategoryGraphQLModel? SelectedTaxCategory
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedTaxCategory));
                    NotifyOfPropertyChange(nameof(CanEditTaxCategory));
                    NotifyOfPropertyChange(nameof(CanDeleteTaxCategory));
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
                        _ = _searchDebounce.RunAsync(LoadTaxCategoriesAsync);
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

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.TaxCategory.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.TaxCategory.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.TaxCategory.Delete);

        #endregion

        #region Button States

        public bool CanCreateTaxCategory => HasCreatePermission && !IsBusy;
        public bool CanEditTaxCategory => HasEditPermission && SelectedTaxCategory != null;
        public bool CanDeleteTaxCategory => HasDeletePermission && SelectedTaxCategory != null;

        #endregion

        #region Commands

        private ICommand? _createTaxCategoryCommand;
        public ICommand CreateTaxCategoryCommand
        {
            get
            {
                _createTaxCategoryCommand ??= new AsyncCommand(CreateTaxCategoryAsync);
                return _createTaxCategoryCommand;
            }
        }

        private ICommand? _editTaxCategoryCommand;
        public ICommand EditTaxCategoryCommand
        {
            get
            {
                _editTaxCategoryCommand ??= new AsyncCommand(EditTaxCategoryAsync);
                return _editTaxCategoryCommand;
            }
        }

        private ICommand? _deleteTaxCategoryCommand;
        public ICommand DeleteTaxCategoryCommand
        {
            get
            {
                _deleteTaxCategoryCommand ??= new AsyncCommand(DeleteTaxCategoryAsync);
                return _deleteTaxCategoryCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadTaxCategoriesAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public TaxCategoryViewModel(
            IEventAggregator eventAggregator,
            IRepository<TaxCategoryGraphQLModel> taxCategoryService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            PermissionCache permissionCache)
        {
            _eventAggregator = eventAggregator;
            _taxCategoryService = taxCategoryService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
            _permissionCache = permissionCache;
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
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.TaxCategory);
                await LoadTaxCategoriesAsync();
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
            NotifyOfPropertyChange(nameof(CanCreateTaxCategory));
            NotifyOfPropertyChange(nameof(CanEditTaxCategory));
            NotifyOfPropertyChange(nameof(CanDeleteTaxCategory));
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

        public async Task CreateTaxCategoryAsync()
        {
            try
            {
                IsBusy = true;
                TaxCategoryDetailViewModel detail = new(_taxCategoryService, _eventAggregator, _stringLengthCache, _joinableTaskFactory);
                detail.SetForNew();
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.50;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nueva categoría de impuesto");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(CreateTaxCategoryAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditTaxCategoryAsync()
        {
            if (SelectedTaxCategory == null) return;
            try
            {
                IsBusy = true;
                TaxCategoryDetailViewModel detail = new(_taxCategoryService, _eventAggregator, _stringLengthCache, _joinableTaskFactory);
                detail.SetForEdit(SelectedTaxCategory);
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.50;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar categoría de impuesto");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EditTaxCategoryAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteTaxCategoryAsync()
        {
            if (SelectedTaxCategory == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteTaxCategoryQuery.Value;
                ExpandoObject canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedTaxCategory.Id)
                    .Build();
                CanDeleteType validation = await _taxCategoryService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                var (deleteFragment, deleteQuery) = _deleteTaxCategoryQuery.Value;
                ExpandoObject deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedTaxCategory.Id)
                    .Build();
                DeleteResponseType deletedTaxCategory = await _taxCategoryService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedTaxCategory.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedTaxCategory.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new TaxCategoryDeleteMessage { DeletedTaxCategory = deletedTaxCategory },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(DeleteTaxCategoryAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Load

        public async Task LoadTaxCategoriesAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadTaxCategoriesQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<TaxCategoryGraphQLModel> result = await _taxCategoryService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                TaxCategories = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadTaxCategoriesAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadTaxCategoriesQuery = new(() =>
        {
            var fields = FieldSpec<PageType<TaxCategoryGraphQLModel>>
                .Create()
                .Field(it => it.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Code)
                    .Field(e => e.Name)
                    .Field(e => e.Prefix)
                    .Field(e => e.UsesPercentage)
                    .Field(e => e.GeneratedTaxAccountIsRequired)
                    .Field(e => e.GeneratedTaxRefundAccountIsRequired)
                    .Field(e => e.DeductibleTaxAccountIsRequired)
                    .Field(e => e.DeductibleTaxRefundAccountIsRequired))
                .Build();

            var fragment = new GraphQLQueryFragment("taxCategoriesPage",
                [new("filters", "TaxCategoryFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteTaxCategoryQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteTaxCategory",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteTaxCategoryQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteTaxCategory",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(TaxCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxCategoriesAsync();
            _notificationService.ShowSuccess(message.CreatedTaxCategory.Message);
        }

        public async Task HandleAsync(TaxCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxCategoriesAsync();
            _notificationService.ShowSuccess(message.UpdatedTaxCategory.Message);
        }

        public async Task HandleAsync(TaxCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxCategoriesAsync();
            SelectedTaxCategory = null;
            _notificationService.ShowSuccess(message.DeletedTaxCategory.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateTaxCategory));
            NotifyOfPropertyChange(nameof(CanEditTaxCategory));
            NotifyOfPropertyChange(nameof(CanDeleteTaxCategory));
            return Task.CompletedTask;
        }

        #endregion
    }
}
