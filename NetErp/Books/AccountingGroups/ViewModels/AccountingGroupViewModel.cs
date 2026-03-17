using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
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

namespace NetErp.Books.AccountingGroups.ViewModels
{
    public class AccountingGroupViewModel : Screen,
        IHandle<AccountingGroupCreateMessage>,
        IHandle<AccountingGroupUpdateMessage>,
        IHandle<AccountingGroupDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<AccountingGroupGraphQLModel> _accountingGroupService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly TaxCache _taxCache;
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

        private ObservableCollection<AccountingGroupGraphQLModel> _accountingGroups = [];
        public ObservableCollection<AccountingGroupGraphQLModel> AccountingGroups
        {
            get => _accountingGroups;
            set
            {
                if (_accountingGroups != value)
                {
                    _accountingGroups = value;
                    NotifyOfPropertyChange(nameof(AccountingGroups));
                }
            }
        }

        private AccountingGroupGraphQLModel? _selectedItem;
        public AccountingGroupGraphQLModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanEditAccountingGroup));
                    NotifyOfPropertyChange(nameof(CanDeleteAccountingGroup));
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = LoadAccountingGroupsAsync();
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

        public bool CanEditAccountingGroup => SelectedItem != null;
        public bool CanDeleteAccountingGroup => SelectedItem != null;

        #endregion

        #region Commands

        private ICommand? _createAccountingGroupCommand;
        public ICommand CreateAccountingGroupCommand
        {
            get
            {
                _createAccountingGroupCommand ??= new AsyncCommand(CreateAccountingGroupAsync);
                return _createAccountingGroupCommand;
            }
        }

        private ICommand? _editAccountingGroupCommand;
        public ICommand EditAccountingGroupCommand
        {
            get
            {
                _editAccountingGroupCommand ??= new AsyncCommand(EditAccountingGroupAsync);
                return _editAccountingGroupCommand;
            }
        }

        private ICommand? _deleteAccountingGroupCommand;
        public ICommand DeleteAccountingGroupCommand
        {
            get
            {
                _deleteAccountingGroupCommand ??= new AsyncCommand(DeleteAccountingGroupAsync);
                return _deleteAccountingGroupCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadAccountingGroupsAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public AccountingGroupViewModel(
            IEventAggregator eventAggregator,
            IRepository<AccountingGroupGraphQLModel> accountingGroupService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            TaxCache taxCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _eventAggregator = eventAggregator;
            _accountingGroupService = accountingGroupService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _taxCache = taxCache;
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
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.AccountingGroup);
                await LoadAccountingGroupsAsync();
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

        public async Task CreateAccountingGroupAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new AccountingGroupDetailViewModel(
                    _accountingGroupService, _eventAggregator, _auxiliaryAccountingAccountCache,
                    _taxCache, _stringLengthCache, _joinableTaskFactory);
                await detail.InitializeAsync();
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.80;

                await _dialogService.ShowDialogAsync(detail, "Nuevo grupo contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al crear el registro.\r\n{GetType().Name}.{nameof(CreateAccountingGroupAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditAccountingGroupAsync()
        {
            if (SelectedItem == null) return;
            try
            {
                IsBusy = true;
                var detail = new AccountingGroupDetailViewModel(
                    _accountingGroupService, _eventAggregator, _auxiliaryAccountingAccountCache,
                    _taxCache, _stringLengthCache, _joinableTaskFactory);
                await detail.InitializeAsync();
                await detail.LoadDataForEditAsync(SelectedItem.Id);
                detail.SetForEdit();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.80;

                await _dialogService.ShowDialogAsync(detail, "Editar grupo contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al editar el registro.\r\n{GetType().Name}.{nameof(EditAccountingGroupAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteAccountingGroupAsync()
        {
            if (SelectedItem == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteQuery.Value;
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedItem.Id)
                    .Build();
                var validation = await _accountingGroupService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                DeleteResponseType deletedAccountingGroup = await ExecuteDeleteAsync(SelectedItem.Id);

                if (!deletedAccountingGroup.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedAccountingGroup.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new AccountingGroupDeleteMessage { DeletedAccountingGroup = deletedAccountingGroup },
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
                    text: $"Error al eliminar el registro.\r\n{GetType().Name}.{nameof(DeleteAccountingGroupAsync)}: {ex.Message}",
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
                var (fragment, query) = _deleteQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();
                return await _accountingGroupService.DeleteAsync<DeleteResponseType>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #endregion

        #region Load

        public async Task LoadAccountingGroupsAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadQuery.Value;

                dynamic filters = new System.Dynamic.ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.name = FilterSearch.Trim().RemoveExtraSpaces();

                var variables = new GraphQLVariables()
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<AccountingGroupGraphQLModel> result = await _accountingGroupService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                AccountingGroups = new ObservableCollection<AccountingGroupGraphQLModel>(result.Entries);
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al cargar los datos.\r\n{GetType().Name}.{nameof(LoadAccountingGroupsAsync)}: {ex.Message}",
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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingGroupGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.AccountInventory, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.Code))
                    .Select(e => e.AccountCost, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.Code))
                    .Select(e => e.AccountIncome, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.Code)))
                .Build();

            var fragment = new GraphQLQueryFragment("accountingGroupsPage",
                [new("filters", "AccountingGroupFilters")],
                fields, "PageResponse");
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

            var fragment = new GraphQLQueryFragment("deleteAccountingGroup",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteAccountingGroup",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(AccountingGroupCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingGroupsAsync();
            _notificationService.ShowSuccess(message.CreatedAccountingGroup.Message);
        }

        public async Task HandleAsync(AccountingGroupUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingGroupsAsync();
            _notificationService.ShowSuccess(message.UpdatedAccountingGroup.Message);
        }

        public async Task HandleAsync(AccountingGroupDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadAccountingGroupsAsync();
            SelectedItem = null;
            _notificationService.ShowSuccess(message.DeletedAccountingGroup.Message);
        }

        #endregion
    }
}
