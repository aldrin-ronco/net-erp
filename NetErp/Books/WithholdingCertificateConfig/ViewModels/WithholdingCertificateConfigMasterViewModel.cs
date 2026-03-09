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
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.WithholdingCertificateConfig.ViewModels
{
    public class WithholdingCertificateConfigMasterViewModel : Screen,
        IHandle<WithholdingCertificateConfigDeleteMessage>,
        IHandle<WithholdingCertificateConfigUpdateMessage>,
        IHandle<WithholdingCertificateConfigCreateMessage>
    {
        #region Dependencies

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<WithholdingCertificateConfigGraphQLModel> _withholdingCertificateConfigService;

        public WithholdingCertificateConfigViewModel Context { get; set; }

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

        private ObservableCollection<WithholdingCertificateConfigGraphQLModel> _certificates = [];
        public ObservableCollection<WithholdingCertificateConfigGraphQLModel> Certificates
        {
            get => _certificates;
            set
            {
                if (_certificates != value)
                {
                    _certificates = value;
                    NotifyOfPropertyChange(nameof(Certificates));
                }
            }
        }

        private WithholdingCertificateConfigGraphQLModel? _selectedCertificate;
        public WithholdingCertificateConfigGraphQLModel? SelectedCertificate
        {
            get => _selectedCertificate;
            set
            {
                if (_selectedCertificate != value)
                {
                    _selectedCertificate = value;
                    NotifyOfPropertyChange(nameof(SelectedCertificate));
                    NotifyOfPropertyChange(nameof(CanEditCertificate));
                    NotifyOfPropertyChange(nameof(CanDeleteCertificate));
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = LoadCertificatesAsync();
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

        public bool CanEditCertificate => SelectedCertificate != null;
        public bool CanDeleteCertificate => SelectedCertificate != null;

        #endregion

        #region Commands

        private ICommand? _createCommand;
        public ICommand CreateCommand
        {
            get
            {
                _createCommand ??= new AsyncCommand(CreateAsync);
                return _createCommand;
            }
        }

        private ICommand? _editCommand;
        public ICommand EditCommand
        {
            get
            {
                _editCommand ??= new AsyncCommand(EditAsync);
                return _editCommand;
            }
        }

        private ICommand? _deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                _deleteCommand ??= new AsyncCommand(DeleteAsync);
                return _deleteCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadCertificatesAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public WithholdingCertificateConfigMasterViewModel(
            WithholdingCertificateConfigViewModel context,
            Helpers.Services.INotificationService notificationService,
            IRepository<WithholdingCertificateConfigGraphQLModel> withholdingCertificateConfigService)
        {
            Context = context;
            _notificationService = notificationService;
            _withholdingCertificateConfigService = withholdingCertificateConfigService;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await LoadCertificatesAsync();
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
            }
            await base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateAsync()
        {
            await Context.ActivateDetailViewForNew();
        }

        public async Task EditAsync()
        {
            if (SelectedCertificate == null) return;
            await Context.ActivateDetailViewForEdit(SelectedCertificate);
        }

        public async Task DeleteAsync()
        {
            if (SelectedCertificate == null) return;
            try
            {
                IsBusy = true;
                Refresh();

                string query = _canDeleteQuery.Value;
                object variables = new { canDeleteResponseId = SelectedCertificate.Id };
                var validation = await _withholdingCertificateConfigService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedCertificate = await Task.Run(() => ExecuteDeleteAsync(SelectedCertificate.Id));

                if (!deletedCertificate.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedCertificate.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(
                    new WithholdingCertificateConfigDeleteMessage { DeletedWithholdingCertificateConfig = deletedCertificate });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!)!;
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

        public async Task<DeleteResponseType> ExecuteDeleteAsync(int id)
        {
            string query = _deleteQuery.Value;
            object variables = new { deleteResponseId = id };
            return await _withholdingCertificateConfigService.DeleteAsync<DeleteResponseType>(query, variables);
        }

        #endregion

        #region Load

        public async Task LoadCertificatesAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = new();
                stopwatch.Start();

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.name = string.IsNullOrEmpty(FilterSearch)
                    ? ""
                    : FilterSearch.Trim().RemoveExtraSpaces();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.Page = PageIndex;
                variables.pageResponsePagination.PageSize = PageSize;

                string query = _loadQuery.Value;
                PageType<WithholdingCertificateConfigGraphQLModel> result = await _withholdingCertificateConfigService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Certificates = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!)!;
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

        private static readonly Lazy<string> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<WithholdingCertificateConfigGraphQLModel>>
                .Create()
                .Field(o => o.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Description))
                .Build();

            var filtersParam = new GraphQLQueryParameter("filters", "WithholdingCertificateFilters");
            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("withholdingCertificatesPage", [filtersParam, paginationParam], fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });

        private static readonly Lazy<string> _canDeleteQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteWithholdingCertificate", [parameter], fields, alias: "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });

        private static readonly Lazy<string> _deleteQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteWithholdingCertificate", [parameter], fields, alias: "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(WithholdingCertificateConfigCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadCertificatesAsync();
            _notificationService.ShowSuccess(message.CreatedWithholdingCertificateConfig.Message);
        }

        public async Task HandleAsync(WithholdingCertificateConfigUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadCertificatesAsync();
            _notificationService.ShowSuccess(message.UpdatedWithholdingCertificateConfig.Message);
        }

        public async Task HandleAsync(WithholdingCertificateConfigDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadCertificatesAsync();
            SelectedCertificate = null;
            _notificationService.ShowSuccess(message.DeletedWithholdingCertificateConfig.Message);
        }

        #endregion
    }
}
