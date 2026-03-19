using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        private bool _showEmptyState;
        public bool ShowEmptyState
        {
            get => _showEmptyState;
            set
            {
                if (_showEmptyState != value)
                {
                    _showEmptyState = value;
                    NotifyOfPropertyChange(nameof(ShowEmptyState));
                    NotifyOfPropertyChange(nameof(HasRecords));
                }
            }
        }

        public bool HasRecords => !ShowEmptyState;

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

        private readonly DebouncedAction _searchDebounce = new();

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
                        _ = _searchDebounce.RunAsync(LoadCertificatesAsync);
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
            Helpers.IDialogService dialogService,
            JoinableTaskFactory joinableTaskFactory)
        {
            DisplayName = "Certificados DIAN";
            _eventAggregator = eventAggregator;
            _dianCertificateService = dianCertificateService;
            _notificationService = notificationService;
            _dialogService = dialogService;
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
                IsBusy = true;
                await LoadDefaultCertificateIdAsync();
                await LoadCertificatesAsync();
                _isInitialized = true;
                ShowEmptyState = Certificates == null || Certificates.Count == 0;
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(OnViewReady)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
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

        public async Task CreateCertificateAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new DianCertificateDetailViewModel(_dianCertificateService, _eventAggregator, _joinableTaskFactory);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.65;

                await _dialogService.ShowDialogAsync(detail, "Nuevo certificado DIAN");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(CreateCertificateAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteCertificateAsync()
        {
            if (SelectedCertificate == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteQuery.Value;
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedCertificate.Id)
                    .Build();
                var validation = await _dianCertificateService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención!",
                        "¿Confirma que desea eliminar el certificado seleccionado?",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención!",
                        $"El certificado no puede ser eliminado\r\n\r\n{validation.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                DeleteResponseType deletedRecord = await ExecuteDeleteAsync(SelectedCertificate.Id);

                if (!deletedRecord.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedRecord.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new DianCertificateDeleteMessage { DeletedCertificate = deletedRecord },
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
                    $"{GetType().Name}.{nameof(DeleteCertificateAsync)}: {ex.Message}",
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
                var (fragment, query) = _deleteQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();
                return await _dianCertificateService.DeleteAsync<DeleteResponseType>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #endregion

        #region Load

        public async Task LoadCertificatesAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadQuery.Value;

                dynamic filters = new System.Dynamic.ExpandoObject();
                if (IsValidFilter) filters.isValid = IsValidFilter;
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { page = PageIndex, pageSize = PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<DianCertificateGraphQLModel> result = await _dianCertificateService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                foreach (var cert in result.Entries)
                {
                    cert.IsDefault = cert.Id == _defaultCertificateId;
                }
                Certificates = new ObservableCollection<DianCertificateGraphQLModel>(result.Entries);
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadCertificatesAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                var (_, query) = _globalConfigQuery.Value;
                var context = await _dianCertificateService.GetDataContextAsync<GlobalConfigDianCertificateContext>(query, new { });
                _defaultCertificateId = context?.GlobalConfig?.DefaultDianCertificate?.Id ?? 0;
            }
            catch
            {
                _defaultCertificateId = 0;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<DianCertificateGraphQLModel>>
                .Create()
                .Field(o => o.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.SerialNumber)
                    .Field(e => e.Issuer)
                    .Field(e => e.Subject)
                    .Field(e => e.ValidFrom)
                    .Field(e => e.ValidTo))
                .Build();

            var fragment = new GraphQLQueryFragment("dianCertificatesPage",
                [new("pagination", "Pagination"), new("filters", "DianCertificateFilters")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _globalConfigQuery = new(() =>
        {
            var fields = FieldSpec<GlobalConfigDefaultCertificate>
                .Create()
                .Select(f => f.DefaultDianCertificate, nested: sq => sq
                    .Field(e => e.Id))
                .Build();

            var fragment = new GraphQLQueryFragment("globalConfig", [], fields);
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

            var fragment = new GraphQLQueryFragment("deleteDianCertificate",
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

            var fragment = new GraphQLQueryFragment("canDeleteDianCertificate",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(DianCertificateCreateMessage message, CancellationToken cancellationToken)
        {
            ShowEmptyState = false;
            await LoadCertificatesAsync();
            _notificationService.ShowSuccess(message.CreatedCertificate.Message);
        }

        public async Task HandleAsync(DianCertificateDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadCertificatesAsync();
            ShowEmptyState = Certificates == null || Certificates.Count == 0;
            SelectedCertificate = null;
            _notificationService.ShowSuccess(message.DeletedCertificate.Message);
        }

        #endregion
    }
}
