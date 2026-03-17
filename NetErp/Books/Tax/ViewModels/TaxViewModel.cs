using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Models.Books;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.Tax.ViewModels
{
    public class TaxViewModel : Screen,
        IHandle<TaxCreateMessage>,
        IHandle<TaxUpdateMessage>,
        IHandle<TaxDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<TaxGraphQLModel> _taxService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly TaxCategoryCache _taxCategoryCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly Microsoft.VisualStudio.Threading.JoinableTaskFactory _joinableTaskFactory;

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

        private ObservableCollection<TaxGraphQLModel> _taxes = [];
        public ObservableCollection<TaxGraphQLModel> Taxes
        {
            get => _taxes;
            set
            {
                if (_taxes != value)
                {
                    _taxes = value;
                    NotifyOfPropertyChange(nameof(Taxes));
                }
            }
        }

        private TaxGraphQLModel? _selectedTax;
        public TaxGraphQLModel? SelectedTax
        {
            get => _selectedTax;
            set
            {
                if (_selectedTax != value)
                {
                    _selectedTax = value;
                    NotifyOfPropertyChange(nameof(SelectedTax));
                    NotifyOfPropertyChange(nameof(CanEditTax));
                    NotifyOfPropertyChange(nameof(CanDeleteTax));
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 2) _ = LoadTaxesAsync();
                }
            }
        }

        private bool _showOnlyActive = true;
        public bool ShowOnlyActive
        {
            get => _showOnlyActive;
            set
            {
                if (_showOnlyActive != value)
                {
                    _showOnlyActive = value;
                    NotifyOfPropertyChange(nameof(ShowOnlyActive));
                    PageIndex = 1;
                    _ = LoadTaxesAsync();
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

        public bool CanEditTax => SelectedTax != null;
        public bool CanDeleteTax => SelectedTax != null;

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
            Microsoft.VisualStudio.Threading.JoinableTaskFactory joinableTaskFactory)
        {
            _eventAggregator = eventAggregator;
            _taxService = taxService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _taxCategoryCache = taxCategoryCache;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Tax);
                await LoadTaxesAsync();
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
                var detail = new TaxDetailViewModel(_taxService, _eventAggregator, _auxiliaryAccountingAccountCache, _taxCategoryCache, _stringLengthCache, _joinableTaskFactory);
                await detail.InitializeAsync();
                detail.SetForNew();
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nuevo impuesto");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al crear el registro.\r\n{GetType().Name}.{nameof(CreateTaxAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
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
                var detail = new TaxDetailViewModel(_taxService, _eventAggregator, _auxiliaryAccountingAccountCache, _taxCategoryCache, _stringLengthCache, _joinableTaskFactory);
                await detail.InitializeAsync();
                await detail.LoadDataForEditAsync(SelectedTax.Id);
                detail.SetForEdit();
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar impuesto");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al editar el registro.\r\n{GetType().Name}.{nameof(EditTaxAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
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
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedTax.Id)
                    .Build();
                var validation = await _taxService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                DeleteResponseType deletedTax = await ExecuteDeleteAsync(SelectedTax.Id);

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
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al eliminar el registro.\r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al eliminar el registro.\r\n{GetType().Name}.{nameof(DeleteTaxAsync)}: {ex.Message}",
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
                var (fragment, query) = _deleteTaxQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();
                return await _taxService.DeleteAsync<DeleteResponseType>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
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

                dynamic filters = new System.Dynamic.ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.name = FilterSearch.Trim().RemoveExtraSpaces();
                if (ShowOnlyActive) filters.isActive = true;

                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<TaxGraphQLModel> result = await _taxService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Taxes = new ObservableCollection<TaxGraphQLModel>(result.Entries);
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al cargar los datos.\r\n{GetType().Name}.{nameof(LoadTaxesAsync)}: {ex.Message}",
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

        #endregion
    }
}
