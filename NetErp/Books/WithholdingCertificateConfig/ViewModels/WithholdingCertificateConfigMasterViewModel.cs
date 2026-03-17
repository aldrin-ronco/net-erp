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
using Microsoft.VisualStudio.Threading;
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
        private readonly Helpers.IDialogService _dialogService;
        private readonly IRepository<WithholdingCertificateConfigGraphQLModel> _withholdingCertificateConfigService;
        private readonly IRepository<AccountingAccountGroupGraphQLModel> _accountingAccountGroupService;
        private readonly AccountingAccountGroupCache _accountingAccountGroupCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

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
            Helpers.IDialogService dialogService,
            IRepository<WithholdingCertificateConfigGraphQLModel> withholdingCertificateConfigService,
            IRepository<AccountingAccountGroupGraphQLModel> accountingAccountGroupService,
            AccountingAccountGroupCache accountingAccountGroupCache,
            CostCenterCache costCenterCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            Context = context;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _withholdingCertificateConfigService = withholdingCertificateConfigService;
            _accountingAccountGroupService = accountingAccountGroupService;
            _accountingAccountGroupCache = accountingAccountGroupCache;
            _costCenterCache = costCenterCache;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await LoadCertificatesAsync();
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region CRUD Operations

        public async Task CreateAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new WithholdingCertificateConfigDetailViewModel(
                    Context, _withholdingCertificateConfigService,
                    _accountingAccountGroupService, _accountingAccountGroupCache,
                    _costCenterCache, _stringLengthCache, _joinableTaskFactory);
                await detail.InitializeAsync();
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.75;
                    detail.DialogHeight = parentView.ActualHeight * 0.90;
                }

                await _dialogService.ShowDialogAsync(detail, "Nuevo certificado de retención");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al crear el registro.\r\n{GetType().Name}.{nameof(CreateAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditAsync()
        {
            if (SelectedCertificate == null) return;
            try
            {
                IsBusy = true;
                var detail = new WithholdingCertificateConfigDetailViewModel(
                    Context, _withholdingCertificateConfigService,
                    _accountingAccountGroupService, _accountingAccountGroupCache,
                    _costCenterCache, _stringLengthCache, _joinableTaskFactory);
                await detail.LoadDataForEditAsync(SelectedCertificate.Id);
                await detail.InitializeAsync();
                detail.SetForEdit();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.75;
                    detail.DialogHeight = parentView.ActualHeight * 0.90;
                }

                await _dialogService.ShowDialogAsync(detail, "Editar certificado de retención");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al editar el registro.\r\n{GetType().Name}.{nameof(EditAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteAsync()
        {
            if (SelectedCertificate == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteQuery.Value;
                var canDeleteVariables = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedCertificate.Id)
                    .Build();

                var validation = await _withholdingCertificateConfigService.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

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
                DeleteResponseType deletedCertificate = await ExecuteDeleteAsync(SelectedCertificate.Id);

                if (!deletedCertificate.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedCertificate.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    new WithholdingCertificateConfigDeleteMessage { DeletedWithholdingCertificateConfig = deletedCertificate },
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
                    text: $"Error al eliminar el registro.\r\n{GetType().Name}.{nameof(DeleteAsync)}: {ex.Message}",
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
                return await _withholdingCertificateConfigService.DeleteAsync<DeleteResponseType>(query, variables);
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
                var variables = new GraphQLVariables()
                    .For(fragment, "filters", new
                    {
                        name = string.IsNullOrEmpty(FilterSearch)
                            ? ""
                            : FilterSearch.Trim().RemoveExtraSpaces()
                    })
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .Build();

                PageType<WithholdingCertificateConfigGraphQLModel> result = await _withholdingCertificateConfigService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Certificates = new ObservableCollection<WithholdingCertificateConfigGraphQLModel>(result.Entries);
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al cargar los datos.\r\n{GetType().Name}.{nameof(LoadCertificatesAsync)}: {ex.Message}",
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
            var fields = FieldSpec<PageType<WithholdingCertificateConfigGraphQLModel>>
                .Create()
                .Field(o => o.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Description))
                .Build();

            var fragment = new GraphQLQueryFragment("withholdingCertificatesPage",
                [new("filters", "WithholdingCertificateFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteWithholdingCertificate",
                [new("id", "ID!")],
                fields, "CanDeleteResponse");
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

            var fragment = new GraphQLQueryFragment("deleteWithholdingCertificate",
                [new("id", "ID!")],
                fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
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
