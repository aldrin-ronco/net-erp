using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
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

namespace NetErp.Books.Tax.ViewModels
{
    public class TaxViewModel : Screen,
        IHandle<TaxCreateMessage>,
        IHandle<TaxUpdateMessage>,
        IHandle<TaxDeleteMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<TaxGraphQLModel> _taxService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IGraphQLClient _graphQLClient;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly TaxCategoryCache _taxCategoryCache;
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

        public ObservableCollection<TaxGraphQLModel> Taxes
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Taxes));
                }
            }
        } = [];

        public TaxGraphQLModel? SelectedTax
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedTax));
                    NotifyOfPropertyChange(nameof(CanEditTax));
                    NotifyOfPropertyChange(nameof(CanDeleteTax));
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
                        _ = _searchDebounce.RunAsync(LoadTaxesAsync);
                    }
                }
            }
        } = string.Empty;

        public bool ShowOnlyActive
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowOnlyActive));
                    PageIndex = 1;
                    _ = LoadTaxesAsync();
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

        #region Permissions

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.Tax.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.Tax.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.Tax.Delete);

        #endregion

        #region Button States

        public bool CanCreateTax => HasCreatePermission && !IsBusy;
        public bool CanEditTax => HasEditPermission && SelectedTax != null;
        public bool CanDeleteTax => HasDeletePermission && SelectedTax != null;

        #endregion

        #region Commands

        private ICommand? _createTaxCommand;
        public ICommand CreateTaxCommand
        {
            get
            {
                _createTaxCommand ??= new AsyncCommand(CreateTaxAsync);
                return _createTaxCommand;
            }
        }

        private ICommand? _editTaxCommand;
        public ICommand EditTaxCommand
        {
            get
            {
                _editTaxCommand ??= new AsyncCommand(EditTaxAsync);
                return _editTaxCommand;
            }
        }

        private ICommand? _deleteTaxCommand;
        public ICommand DeleteTaxCommand
        {
            get
            {
                _deleteTaxCommand ??= new AsyncCommand(DeleteTaxAsync);
                return _deleteTaxCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadTaxesAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public TaxViewModel(
            IEventAggregator eventAggregator,
            IRepository<TaxGraphQLModel> taxService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            TaxCategoryCache taxCategoryCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            IGraphQLClient graphQLClient,
            PermissionCache permissionCache)
        {
            _eventAggregator = eventAggregator;
            _taxService = taxService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _taxCategoryCache = taxCategoryCache;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
            _graphQLClient = graphQLClient;
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
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Tax);
                await LoadTaxesAsync();
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
            NotifyOfPropertyChange(nameof(CanCreateTax));
            NotifyOfPropertyChange(nameof(CanEditTax));
            NotifyOfPropertyChange(nameof(CanDeleteTax));
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

        public async Task CreateTaxAsync()
        {
            try
            {
                IsBusy = true;
                TaxDetailViewModel detail = new(_taxService, _eventAggregator, _auxiliaryAccountingAccountCache, _taxCategoryCache, _stringLengthCache, _joinableTaskFactory, _graphQLClient);
                await detail.InitializeAsync();
                detail.SetForNew();
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.50;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nuevo impuesto");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(CreateTaxAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditTaxAsync()
        {
            if (SelectedTax == null) return;
            try
            {
                IsBusy = true;
                TaxDetailViewModel detail = new(_taxService, _eventAggregator, _auxiliaryAccountingAccountCache, _taxCategoryCache, _stringLengthCache, _joinableTaskFactory, _graphQLClient);
                await detail.InitializeAsync();
                await detail.LoadDataForEditAsync(SelectedTax.Id);
                detail.SetForEdit();
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.50;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar impuesto");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EditTaxAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteTaxAsync()
        {
            if (SelectedTax == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteTaxQuery.Value;
                ExpandoObject canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedTax.Id)
                    .Build();
                CanDeleteType validation = await _taxService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                var (deleteFragment, deleteQuery) = _deleteTaxQuery.Value;
                ExpandoObject deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedTax.Id)
                    .Build();
                DeleteResponseType deletedTax = await _taxService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedTax.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedTax.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new TaxDeleteMessage { DeletedTax = deletedTax },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(DeleteTaxAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Load

        public async Task LoadTaxesAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadTaxesQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.name = FilterSearch.Trim().RemoveExtraSpaces();
                if (ShowOnlyActive) filters.isActive = true;

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<TaxGraphQLModel> result = await _taxService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Taxes = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadTaxesAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadTaxesQuery = new(() =>
        {
            var fields = FieldSpec<PageType<TaxGraphQLModel>>
                .Create()
                .Field(it => it.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Rate)
                    .Field(e => e.IsActive)
                    .Field(e => e.Formula)
                    .Select(e => e.TaxCategory, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.GeneratedTaxAccountIsRequired)
                        .Field(c => c.GeneratedTaxRefundAccountIsRequired)
                        .Field(c => c.DeductibleTaxAccountIsRequired)
                        .Field(c => c.DeductibleTaxRefundAccountIsRequired))
                    .Select(e => e.GeneratedTaxAccount, acc => acc
                        .Field(a => a.Id)
                        .Field(a => a.Name))
                    .Select(e => e.GeneratedTaxRefundAccount, acc => acc
                        .Field(a => a.Id)
                        .Field(a => a.Name))
                    .Select(e => e.DeductibleTaxAccount, acc => acc
                        .Field(a => a.Id)
                        .Field(a => a.Name))
                    .Select(e => e.DeductibleTaxRefundAccount, acc => acc
                        .Field(a => a.Id)
                        .Field(a => a.Name)))
                .Build();

            var fragment = new GraphQLQueryFragment("taxesPage",
                [new("filters", "TaxFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteTaxQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteTax",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteTaxQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteTax",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(TaxCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxesAsync();
            _notificationService.ShowSuccess(message.CreatedTax.Message);
        }

        public async Task HandleAsync(TaxUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxesAsync();
            _notificationService.ShowSuccess(message.UpdatedTax.Message);
        }

        public async Task HandleAsync(TaxDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxesAsync();
            SelectedTax = null;
            _notificationService.ShowSuccess(message.DeletedTax.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateTax));
            NotifyOfPropertyChange(nameof(CanEditTax));
            NotifyOfPropertyChange(nameof(CanDeleteTax));
            return Task.CompletedTask;
        }

        #endregion
    }
}
