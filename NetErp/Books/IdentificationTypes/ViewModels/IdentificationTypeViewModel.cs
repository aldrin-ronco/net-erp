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

namespace NetErp.Books.IdentificationTypes.ViewModels
{
    public class IdentificationTypeViewModel : Screen,
        IHandle<IdentificationTypeCreateMessage>,
        IHandle<IdentificationTypeUpdateMessage>,
        IHandle<IdentificationTypeDeleteMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<IdentificationTypeGraphQLModel> _identificationTypeService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly StringLengthCache _stringLengthCache;

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

        private ObservableCollection<IdentificationTypeGraphQLModel> _identificationTypes = [];
        public ObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes
        {
            get => _identificationTypes;
            set
            {
                if (_identificationTypes != value)
                {
                    _identificationTypes = value;
                    NotifyOfPropertyChange(nameof(IdentificationTypes));
                }
            }
        }

        private IdentificationTypeGraphQLModel? _selectedIdentificationType;
        public IdentificationTypeGraphQLModel? SelectedIdentificationType
        {
            get => _selectedIdentificationType;
            set
            {
                if (_selectedIdentificationType != value)
                {
                    _selectedIdentificationType = value;
                    NotifyOfPropertyChange(nameof(SelectedIdentificationType));
                    NotifyOfPropertyChange(nameof(CanEditIdentificationType));
                    NotifyOfPropertyChange(nameof(CanDeleteIdentificationType));
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = LoadIdentificationTypesAsync();
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

        public bool CanEditIdentificationType => SelectedIdentificationType != null;
        public bool CanDeleteIdentificationType => SelectedIdentificationType != null;

        #endregion

        #region Commands

        private ICommand? _createIdentificationTypeCommand;
        public ICommand CreateIdentificationTypeCommand
        {
            get
            {
                _createIdentificationTypeCommand ??= new AsyncCommand(CreateIdentificationTypeAsync);
                return _createIdentificationTypeCommand;
            }
        }

        private ICommand? _editIdentificationTypeCommand;
        public ICommand EditIdentificationTypeCommand
        {
            get
            {
                _editIdentificationTypeCommand ??= new AsyncCommand(EditIdentificationTypeAsync);
                return _editIdentificationTypeCommand;
            }
        }

        private ICommand? _deleteIdentificationTypeCommand;
        public ICommand DeleteIdentificationTypeCommand
        {
            get
            {
                _deleteIdentificationTypeCommand ??= new AsyncCommand(DeleteIdentificationTypeAsync);
                return _deleteIdentificationTypeCommand;
            }
        }

        private ICommand? _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                _paginationCommand ??= new AsyncCommand(LoadIdentificationTypesAsync);
                return _paginationCommand;
            }
        }

        #endregion

        #region Constructor

        public IdentificationTypeViewModel(
            IEventAggregator eventAggregator,
            IRepository<IdentificationTypeGraphQLModel> identificationTypeService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            StringLengthCache stringLengthCache)
        {
            _eventAggregator = eventAggregator;
            _identificationTypeService = identificationTypeService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _stringLengthCache = stringLengthCache;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.IdentificationType);
            await LoadIdentificationTypesAsync();
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

        public async Task CreateIdentificationTypeAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new IdentificationTypeDetailViewModel(_identificationTypeService, _eventAggregator, _stringLengthCache);
                detail.SetForNew();
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nuevo tipo de documento");
            }
            catch (Exception ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al crear el registro.\r\n{GetType().Name}.{nameof(CreateIdentificationTypeAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditIdentificationTypeAsync()
        {
            if (SelectedIdentificationType == null) return;
            try
            {
                IsBusy = true;
                var detail = new IdentificationTypeDetailViewModel(_identificationTypeService, _eventAggregator, _stringLengthCache);
                detail.SetForEdit(SelectedIdentificationType);
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar tipo de documento");
            }
            catch (Exception ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al editar el registro.\r\n{GetType().Name}.{nameof(EditIdentificationTypeAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteIdentificationTypeAsync()
        {
            if (SelectedIdentificationType == null) return;
            try
            {
                IsBusy = true;

                var (canDeleteFragment, canDeleteQuery) = _canDeleteIdentificationTypeQuery.Value;
                var canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedIdentificationType.Id)
                    .Build();
                var validation = await _identificationTypeService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
                DeleteResponseType deletedIdentificationType = await ExecuteDeleteAsync(SelectedIdentificationType.Id);

                if (!deletedIdentificationType.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"No pudo ser eliminado el registro\r\n\r\n{deletedIdentificationType.Message}\r\n\r\nVerifique la información e intente más tarde.",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new IdentificationTypeDeleteMessage { DeletedIdentificationType = deletedIdentificationType },
                    CancellationToken.None);
            }
            catch (AsyncException ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al eliminar el registro.\r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al eliminar el registro.\r\n{GetType().Name}.{nameof(DeleteIdentificationTypeAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
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
                var (fragment, query) = _deleteIdentificationTypeQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "id", id)
                    .Build();
                return await _identificationTypeService.DeleteAsync<DeleteResponseType>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #endregion

        #region Load

        public async Task LoadIdentificationTypesAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadIdentificationTypesQuery.Value;

                dynamic filters = new System.Dynamic.ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<IdentificationTypeGraphQLModel> result = await _identificationTypeService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                IdentificationTypes = new ObservableCollection<IdentificationTypeGraphQLModel>(result.Entries);
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al cargar los datos.\r\n{GetType().Name}.{nameof(LoadIdentificationTypesAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadIdentificationTypesQuery = new(() =>
        {
            var fields = FieldSpec<PageType<IdentificationTypeGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Code)
                    .Field(e => e.Name)
                    .Field(e => e.HasVerificationDigit)
                    .Field(e => e.AllowsLetters)
                    .Field(e => e.MinimumDocumentLength))
                .Build();

            var fragment = new GraphQLQueryFragment("identificationTypesPage",
                [new("filters", "IdentificationTypeFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteIdentificationTypeQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteIdentificationType",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteIdentificationTypeQuery = new(() =>
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("canDeleteIdentificationType",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Event Handlers

        public async Task HandleAsync(IdentificationTypeCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadIdentificationTypesAsync();
            _notificationService.ShowSuccess(message.CreatedIdentificationType.Message);
        }

        public async Task HandleAsync(IdentificationTypeUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadIdentificationTypesAsync();
            _notificationService.ShowSuccess(message.UpdatedIdentificationType.Message);
        }

        public async Task HandleAsync(IdentificationTypeDeleteMessage message, CancellationToken cancellationToken)
        {
            await LoadIdentificationTypesAsync();
            SelectedIdentificationType = null;
            _notificationService.ShowSuccess(message.DeletedIdentificationType.Message);
        }

        #endregion
    }
}
