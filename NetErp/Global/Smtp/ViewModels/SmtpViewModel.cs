using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.Smtp.ViewModels
{
    public class SmtpViewModel : Screen,
        IHandle<SmtpCreateMessage>,
        IHandle<SmtpUpdateMessage>,
        IHandle<SmtpDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<SmtpGraphQLModel> _smtpService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        private bool _isInitialized;

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

        private ObservableCollection<SmtpGraphQLModel> _smtps = [];
        public ObservableCollection<SmtpGraphQLModel> Smtps
        {
            get => _smtps;
            set
            {
                if (_smtps != value)
                {
                    _smtps = value;
                    NotifyOfPropertyChange(nameof(Smtps));
                    NotifyOfPropertyChange(nameof(HasRecords));
                    NotifyOfPropertyChange(nameof(ShowEmptyState));
                }
            }
        }

        public bool HasRecords => _isInitialized && Smtps != null && Smtps.Count > 0;
        public bool ShowEmptyState => _isInitialized && (Smtps == null || Smtps.Count == 0);

        private SmtpGraphQLModel? _selectedSmtp;
        public SmtpGraphQLModel? SelectedSmtp
        {
            get => _selectedSmtp;
            set
            {
                if (_selectedSmtp != value)
                {
                    _selectedSmtp = value;
                    NotifyOfPropertyChange(nameof(SelectedSmtp));
                    NotifyOfPropertyChange(nameof(CanEditSmtp));
                    NotifyOfPropertyChange(nameof(CanDeleteSmtp));
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = LoadSmtpsAsync();
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

        public bool CanEditSmtp => SelectedSmtp != null;
        public bool CanDeleteSmtp => SelectedSmtp != null;

        #endregion

        #region Commands

        private ICommand? _createSmtpCommand;
        public ICommand CreateSmtpCommand
        {
            get
            {
                _createSmtpCommand ??= new AsyncCommand(CreateSmtpAsync);
                return _createSmtpCommand;
            }
        }

        private ICommand? _editSmtpCommand;
        public ICommand EditSmtpCommand
        {
            get
            {
                _editSmtpCommand ??= new AsyncCommand(EditSmtpAsync);
                return _editSmtpCommand;
            }
        }

        private ICommand? _deleteSmtpCommand;
        public ICommand DeleteSmtpCommand
        {
            get
            {
                _deleteSmtpCommand ??= new AsyncCommand(DeleteSmtpAsync);
                return _deleteSmtpCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadSmtpsAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public SmtpViewModel(
            IEventAggregator eventAggregator,
            IRepository<SmtpGraphQLModel> smtpService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _eventAggregator = eventAggregator;
            _smtpService = smtpService;
            _notificationService = notificationService;
            _dialogService = dialogService;
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
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Smtp);
                await LoadSmtpsAsync();
                _isInitialized = true;
                NotifyOfPropertyChange(nameof(HasRecords));
                NotifyOfPropertyChange(nameof(ShowEmptyState));
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

        public async Task CreateSmtpAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new SmtpDetailViewModel(_smtpService, _eventAggregator, _stringLengthCache, _joinableTaskFactory);
                detail.SetForNew();
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nuevo SMTP");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al crear el registro.\r\n{GetType().Name}.{nameof(CreateSmtpAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditSmtpAsync()
        {
            if (SelectedSmtp == null) return;
            try
            {
                IsBusy = true;
                var detail = new SmtpDetailViewModel(_smtpService, _eventAggregator, _stringLengthCache, _joinableTaskFactory);
                detail.SetForEdit(SelectedSmtp);
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar SMTP");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al editar el registro.\r\n{GetType().Name}.{nameof(EditSmtpAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteSmtpAsync()
        {
            if (SelectedSmtp == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteSmtpQuery.Value;
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedSmtp.Id)
                    .Build();
                var validation = await _smtpService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                DeleteResponseType deletedSmtp = await ExecuteDeleteAsync(SelectedSmtp.Id);

                if (!deletedSmtp.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedSmtp.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new SmtpDeleteMessage { DeletedSmtp = deletedSmtp },
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
                    text: $"Error al eliminar el registro.\r\n{GetType().Name}.{nameof(DeleteSmtpAsync)}: {ex.Message}",
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
                var (fragment, query) = _deleteSmtpQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();
                return await _smtpService.DeleteAsync<DeleteResponseType>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #endregion

        #region Load

        public async Task LoadSmtpsAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadSmtpsQuery.Value;

                dynamic filters = new System.Dynamic.ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.name = FilterSearch.Trim().RemoveExtraSpaces();

                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<SmtpGraphQLModel> result = await _smtpService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Smtps = new ObservableCollection<SmtpGraphQLModel>(result.Entries);
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al cargar los datos.\r\n{GetType().Name}.{nameof(LoadSmtpsAsync)}: {ex.Message}",
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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadSmtpsQuery = new(() =>
        {
            var fields = FieldSpec<PageType<SmtpGraphQLModel>>
                .Create()
                .Field(it => it.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Host)
                    .Field(e => e.Name)
                    .Field(e => e.Port))
                .Build();

            var fragment = new GraphQLQueryFragment("smtpsPage",
                [new("filters", "SmtpFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteSmtpQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteSmtp",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteSmtpQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteSmtp",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(SmtpCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadSmtpsAsync();
            _notificationService.ShowSuccess(message.CreatedSmtp.Message);
        }

        public async Task HandleAsync(SmtpUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadSmtpsAsync();
            _notificationService.ShowSuccess(message.UpdatedSmtp.Message);
        }

        public async Task HandleAsync(SmtpDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadSmtpsAsync();
            SelectedSmtp = null;
            _notificationService.ShowSuccess(message.DeletedSmtp.Message);
        }

        #endregion
    }
}
