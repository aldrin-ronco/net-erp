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
using System.Linq;
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
                }
            }
        }

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
            AutoMapper.IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<SmtpGraphQLModel> smtpService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService)
        {
            _eventAggregator = eventAggregator;
            _smtpService = smtpService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await LoadSmtpsAsync();
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

        public async Task CreateSmtpAsync()
        {
            var detail = new SmtpDetailViewModel(_smtpService, _eventAggregator);
            await _dialogService.ShowDialogAsync(detail, "Nuevo SMTP");
        }

        public async Task EditSmtpAsync()
        {
            if (SelectedSmtp == null) return;
            var detail = new SmtpDetailViewModel(_smtpService, _eventAggregator);
            detail.SmtpId = SelectedSmtp.Id;
            detail.Name = SelectedSmtp.Name;
            detail.Host = SelectedSmtp.Host;
            detail.Port = SelectedSmtp.Port;
            detail.AcceptChanges();
            await _dialogService.ShowDialogAsync(detail, "Editar SMTP");
        }

        public async Task DeleteSmtpAsync()
        {
            if (SelectedSmtp == null) return;
            try
            {
                IsBusy = true;
                Refresh();

                string query = GetCanDeleteSmtpQuery();
                object variables = new { canDeleteResponseId = SelectedSmtp.Id };
                var validation = await _smtpService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedSmtp = await Task.Run(() => ExecuteDeleteSmtpAsync(SelectedSmtp.Id));

                if (!deletedSmtp.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro \n\n {deletedSmtp.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await _eventAggregator.PublishOnUIThreadAsync(new SmtpDeleteMessage { DeletedSmtp = deletedSmtp });
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

        public async Task<DeleteResponseType> ExecuteDeleteSmtpAsync(int id)
        {
            string query = GetDeleteSmtpQuery();
            object variables = new { deleteResponseId = id };
            return await _smtpService.DeleteAsync<DeleteResponseType>(query, variables);
        }

        #endregion

        #region Load

        public async Task LoadSmtpsAsync()
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

                string query = GetLoadSmtpsQuery();
                PageType<SmtpGraphQLModel> result = await _smtpService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                Smtps = [.. result.Entries];
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

        #endregion

        #region GraphQL Queries

        public string GetLoadSmtpsQuery()
        {
            var smtpsFields = FieldSpec<PageType<SmtpGraphQLModel>>
                .Create()
                .Field(it => it.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Host)
                    .Field(e => e.Name)
                    .Field(e => e.Port))
                .Build();

            var smtpsParameters = new GraphQLQueryParameter("filters", "SmtpFilters");
            var smtpsPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var smtpsFragment = new GraphQLQueryFragment("smtpsPage", [smtpsParameters, smtpsPagParameters], smtpsFields, "PageResponse");

            return new GraphQLQueryBuilder([smtpsFragment]).GetQuery();
        }

        public string GetDeleteSmtpQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteSmtp", [parameter], fields, alias: "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetCanDeleteSmtpQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteSmtp", [parameter], fields, alias: "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

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
