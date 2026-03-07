using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using IDialogService = NetErp.Helpers.IDialogService;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
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

        #endregion

        #region Constructor

        public AccountingPresentationViewModel(
            IEventAggregator eventAggregator,
            IRepository<AccountingPresentationGraphQLModel> accountingPresentationService,
            Helpers.Services.INotificationService notificationService,
            IDialogService dialogService,
            AccountingBookCache accountingBookCache)
        {
            _eventAggregator = eventAggregator;
            _accountingPresentationService = accountingPresentationService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _accountingBookCache = accountingBookCache;
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
                var detail = new AccountingPresentationDetailViewModel(_accountingPresentationService, _eventAggregator, _accountingBookCache);
                await detail.InitializeAsync();
                await _dialogService.ShowDialogAsync(detail, "Nueva presentación contable");
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{currentMethod!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task EditPresentationAsync()
        {
            if (SelectedPresentation == null) return;
            try
            {
                var detail = new AccountingPresentationDetailViewModel(_accountingPresentationService, _eventAggregator, _accountingBookCache);
                await detail.InitializeAsync();
                await detail.LoadDataForEditAsync(SelectedPresentation.Id);
                await _dialogService.ShowDialogAsync(detail, "Editar presentación contable");
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{currentMethod!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task DeletePresentationAsync()
        {
            if (SelectedPresentation == null) return;
            try
            {
                IsBusy = true;
                Refresh();

                string query = GetCanDeleteAccountingPresentationQuery();
                object variables = new { canDeleteResponseId = SelectedPresentation.Id };
                var validation = await _accountingPresentationService.CanDeleteAsync(query, variables);

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
                string deleteQuery = GetDeleteAccountingPresentationQuery();
                object deleteVars = new { deleteResponseId = SelectedPresentation.Id };
                DeleteResponseType deletedPresentation = await _accountingPresentationService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedPresentation.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedPresentation.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new AccountingPresentationDeleteMessage { DeletedAccountingPresentation = deletedPresentation });
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

        public async Task LoadAccountingPresentationsAsync()
        {
            try
            {
                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.name = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                string query = GetLoadAccountingPresentationsQuery();
                PageType<AccountingPresentationGraphQLModel> result = await _accountingPresentationService.GetPageAsync(query, variables);

                AccountingPresentations = [.. result.Entries];
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

        #region GraphQL Queries

        public string GetLoadAccountingPresentationsQuery()
        {
            var fields = FieldSpec<PageType<AccountingPresentationGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.AllowsClosure)
                    .Select(e => e.ClosureAccountingBook, acc => acc
                        .Field(c => c.Id)
                        .Field(c => c.Name)))
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "AccountingPresentationFilters");
            var fragment = new GraphQLQueryFragment("accountingPresentationsPage", [paginationParam, filtersParam], fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public string GetDeleteAccountingPresentationQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteAccountingPresentation", [parameter], fields, alias: "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetCanDeleteAccountingPresentationQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteAccountingPresentation", [parameter], fields, alias: "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

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
