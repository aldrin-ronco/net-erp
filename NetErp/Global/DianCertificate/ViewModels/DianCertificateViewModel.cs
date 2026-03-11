using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
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

namespace NetErp.Global.DianCertificate.ViewModels
{
    public class DianCertificateViewModel : Screen,
        IHandle<DianCertificateCreateMessage>,
        IHandle<DianCertificateDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<DianCertificateGraphQLModel> _dianCertificateService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;

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

        private ObservableCollection<DianCertificateGraphQLModel> _certificates = [];
        public ObservableCollection<DianCertificateGraphQLModel> Certificates
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

        private DianCertificateGraphQLModel? _selectedCertificate;
        public DianCertificateGraphQLModel? SelectedCertificate
        {
            get => _selectedCertificate;
            set
            {
                if (_selectedCertificate != value)
                {
                    _selectedCertificate = value;
                    NotifyOfPropertyChange(nameof(SelectedCertificate));
                    NotifyOfPropertyChange(nameof(CanDeleteCertificate));
                }
            }
        }

        private int _defaultCertificateId;

        private bool _isValidFilter = true;
        public bool IsValidFilter
        {
            get => _isValidFilter;
            set
            {
                if (_isValidFilter != value)
                {
                    _isValidFilter = value;
                    NotifyOfPropertyChange(nameof(IsValidFilter));
                    if (_isInitialized)
                    {
                        PageIndex = 1;
                        _ = LoadCertificatesAsync();
                    }
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3)
                    {
                        PageIndex = 1;
                        _ = LoadCertificatesAsync();
                    }
                }
            }
        }

        #endregion

        #region Pagination

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

        public bool CanDeleteCertificate => SelectedCertificate != null;

        #endregion

        #region Commands

        private ICommand? _createCommand;
        public ICommand CreateCommand
        {
            get
            {
                _createCommand ??= new AsyncCommand(CreateCertificateAsync);
                return _createCommand;
            }
        }

        private ICommand? _deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                _deleteCommand ??= new AsyncCommand(DeleteCertificateAsync);
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

        #region State

        private bool _isInitialized;

        #endregion

        #region Constructor

        public DianCertificateViewModel(
            IEventAggregator eventAggregator,
            IRepository<DianCertificateGraphQLModel> dianCertificateService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService)
        {
            DisplayName = "Certificados DIAN";
            _eventAggregator = eventAggregator;
            _dianCertificateService = dianCertificateService;
            _notificationService = notificationService;
            _dialogService = dialogService;
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
                await LoadDefaultCertificateIdAsync();
                await LoadCertificatesAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.OnViewReady: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
                this.SetFocus(() => FilterSearch);
            }
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

        public async Task CreateCertificateAsync()
        {
            var detail = new DianCertificateDetailViewModel(_dianCertificateService, _eventAggregator);
            await _dialogService.ShowDialogAsync(detail, "Nuevo certificado DIAN");
        }

        public async Task DeleteCertificateAsync()
        {
            if (SelectedCertificate == null) return;
            try
            {
                IsBusy = true;

                string query = _canDeleteQuery.Value;
                object variables = new { canDeleteResponseId = SelectedCertificate.Id };
                var validation = await _dianCertificateService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención !",
                        "¿Confirma que desea eliminar el certificado seleccionado?",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención !",
                        "El certificado no puede ser eliminado\r\n\r\n" + validation.Message,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                string deleteQuery = _deleteQuery.Value;
                object deleteVars = new { deleteResponseId = SelectedCertificate.Id };
                DeleteResponseType deletedRecord = await _dianCertificateService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!deletedRecord.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedRecord.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new DianCertificateDeleteMessage { DeletedCertificate = deletedRecord });
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

        public async Task LoadCertificatesAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = new();
                stopwatch.Start();

                string query = _loadQuery.Value;

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.page = PageIndex;
                variables.pageResponsePagination.pageSize = PageSize;

                if (IsValidFilter) variables.pageResponseFilters.isValid = IsValidFilter;
                variables.pageResponseFilters.matching = string.IsNullOrEmpty(FilterSearch)
                    ? ""
                    : FilterSearch.Trim().RemoveExtraSpaces();

                PageType<DianCertificateGraphQLModel> result = await _dianCertificateService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                foreach (var cert in result.Entries)
                {
                    cert.IsDefault = cert.Id == _defaultCertificateId;
                }
                Certificates = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
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

        private async Task LoadDefaultCertificateIdAsync()
        {
            try
            {
                string query = _globalConfigQuery.Value;
                dynamic variables = new ExpandoObject();
                var context = await _dianCertificateService.GetDataContextAsync<GlobalConfigDianCertificateContext>(query, variables);
                _defaultCertificateId = context?.GlobalConfig?.DefaultDianCertificate?.Id ?? 0;
            }
            catch
            {
                _defaultCertificateId = 0;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<string> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<DianCertificateGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.SerialNumber)
                    .Field(e => e.Issuer)
                    .Field(e => e.Subject)
                    .Field(e => e.ValidFrom)
                    .Field(e => e.ValidTo))
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "DianCertificateFilters");
            var fragment = new GraphQLQueryFragment("dianCertificatesPage", [paginationParam, filtersParam], fields, "PageResponse");

            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });

        private static readonly Lazy<string> _globalConfigQuery = new(() =>
        {
            var fields = FieldSpec<GlobalConfigDefaultCertificate>
                .Create()
                .Select(f => f.DefaultDianCertificate, nested: sq => sq
                    .Field(e => e.Id))
                .Build();

            var fragment = new GraphQLQueryFragment("globalConfig", [], fields);
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
            var fragment = new GraphQLQueryFragment("deleteDianCertificate", [parameter], fields, alias: "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        private static readonly Lazy<string> _canDeleteQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteDianCertificate", [parameter], fields, alias: "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(DianCertificateCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadCertificatesAsync();
            _notificationService.ShowSuccess(message.CreatedCertificate.Message);
        }

        public async Task HandleAsync(DianCertificateDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadCertificatesAsync();
            SelectedCertificate = null;
            _notificationService.ShowSuccess(message.DeletedCertificate.Message);
        }

        #endregion
    }
}
