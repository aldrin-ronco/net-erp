using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using NetErp.Helpers;
using IDialogService = NetErp.Helpers.IDialogService;
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

namespace NetErp.Books.AccountingPresentations.ViewModels
{
    public class AccountingPresentationViewModel : Screen,
        IHandle<AccountingPresentationCreateMessage>,
        IHandle<AccountingPresentationUpdateMessage>,
        IHandle<AccountingPresentationDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<AccountingPresentationGraphQLModel> _accountingPresentationService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly AccountingBookCache _accountingBookCache;
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

        private ObservableCollection<AccountingPresentationGraphQLModel> _accountingPresentations = [];
        public ObservableCollection<AccountingPresentationGraphQLModel> AccountingPresentations
        {
            get => _accountingPresentations;
            set
            {
                if (_accountingPresentations != value)
                {
                    _accountingPresentations = value;
                    NotifyOfPropertyChange(nameof(AccountingPresentations));
                }
            }
        }

        private AccountingPresentationGraphQLModel? _selectedPresentation;
        public AccountingPresentationGraphQLModel? SelectedPresentation
        {
            get => _selectedPresentation;
            set
            {
                if (_selectedPresentation != value)
                {
                    _selectedPresentation = value;
                    NotifyOfPropertyChange(nameof(SelectedPresentation));
                    NotifyOfPropertyChange(nameof(CanEditPresentation));
                    NotifyOfPropertyChange(nameof(CanDeletePresentation));
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = LoadAccountingPresentationsAsync();
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

        public bool CanEditPresentation => SelectedPresentation != null;
        public bool CanDeletePresentation => SelectedPresentation != null;

        #endregion

        #region Commands

        private ICommand? _createPresentationCommand;
        public ICommand CreatePresentationCommand
        {
            get
            {
                _createPresentationCommand ??= new AsyncCommand(CreatePresentationAsync);
                return _createPresentationCommand;
            }
        }

        private ICommand? _editPresentationCommand;
        public ICommand EditPresentationCommand
        {
            get
            {
                _editPresentationCommand ??= new AsyncCommand(EditPresentationAsync);
                return _editPresentationCommand;
            }
        }

        private ICommand? _deletePresentationCommand;
        public ICommand DeletePresentationCommand
        {
            get
            {
                _deletePresentationCommand ??= new AsyncCommand(DeletePresentationAsync);
                return _deletePresentationCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadAccountingPresentationsAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public AccountingPresentationViewModel(
            IEventAggregator eventAggregator,
            IRepository<AccountingPresentationGraphQLModel> accountingPresentationService,
            Helpers.Services.INotificationService notificationService,
            IDialogService dialogService,
            AccountingBookCache accountingBookCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _eventAggregator = eventAggregator;
            _accountingPresentationService = accountingPresentationService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _accountingBookCache = accountingBookCache;
            _joinableTaskFactory = joinableTaskFactory;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await LoadAccountingPresentationsAsync();
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
            }
            await base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreatePresentationAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new AccountingPresentationDetailViewModel(_accountingPresentationService, _eventAggregator, _accountingBookCache, _joinableTaskFactory);
                await detail.InitializeAsync();
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nueva presentación contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{nameof(CreatePresentationAsync)} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditPresentationAsync()
        {
            if (SelectedPresentation == null) return;
            try
            {
                IsBusy = true;
                var detail = new AccountingPresentationDetailViewModel(_accountingPresentationService, _eventAggregator, _accountingBookCache, _joinableTaskFactory);
                await detail.InitializeAsync();
                await detail.LoadDataForEditAsync(SelectedPresentation.Id);
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar presentación contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{nameof(EditPresentationAsync)} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeletePresentationAsync()
        {
            if (SelectedPresentation == null) return;
            try
            {
                IsBusy = true;
                Refresh();

                var (canDeleteFragment, canDeleteQuery) = _canDeleteAccountingPresentationQuery.Value;
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedPresentation.Id)
                    .Build();
                var validation = await _accountingPresentationService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea eliminar el registro seleccionado?",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención !",
                        $"El registro no puede ser eliminado\r\n\r\n{validation.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                var (deleteFragment, deleteQuery) = _deleteAccountingPresentationQuery.Value;
                var deleteVars = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedPresentation.Id)
                    .Build();
                DeleteResponseType deletedPresentation = await _accountingPresentationService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedPresentation.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedPresentation.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new AccountingPresentationDeleteMessage { DeletedAccountingPresentation = deletedPresentation });
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{nameof(DeletePresentationAsync)} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Load

        public async Task LoadAccountingPresentationsAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = new();
                stopwatch.Start();

                var (fragment, query) = _loadAccountingPresentationsQuery.Value;

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.name = FilterSearch.Trim().RemoveExtraSpaces();

                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<AccountingPresentationGraphQLModel> result = await _accountingPresentationService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                AccountingPresentations = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{nameof(LoadAccountingPresentationsAsync)} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadAccountingPresentationsQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingPresentationGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.AllowsClosure)
                    .Select(e => e.ClosureAccountingBook, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)))
                .Build();

            var fragment = new GraphQLQueryFragment("accountingPresentationsPage",
                [new("filters", "AccountingPresentationFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteAccountingPresentationQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteAccountingPresentation",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteAccountingPresentationQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteAccountingPresentation",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(AccountingPresentationCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingPresentationsAsync();
            _notificationService.ShowSuccess(message.CreatedAccountingPresentation.Message);
        }

        public async Task HandleAsync(AccountingPresentationUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingPresentationsAsync();
            _notificationService.ShowSuccess(message.UpdatedAccountingPresentation.Message);
        }

        public async Task HandleAsync(AccountingPresentationDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingPresentationsAsync();
            SelectedPresentation = null;
            _notificationService.ShowSuccess(message.DeletedAccountingPresentation.Message);
        }

        #endregion
    }
}
