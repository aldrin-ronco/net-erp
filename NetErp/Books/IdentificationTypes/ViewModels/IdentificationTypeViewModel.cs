using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
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

namespace NetErp.Books.IdentificationTypes.ViewModels
{
    public class IdentificationTypeViewModel : Screen,
        IHandle<IdentificationTypeCreateMessage>,
        IHandle<IdentificationTypeUpdateMessage>,
        IHandle<IdentificationTypeDeleteMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<IdentificationTypeGraphQLModel> _identificationTypeService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly PermissionCache _permissionCache;
        private readonly DebouncedAction _searchDebounce = new();

        #endregion

        #region Grid Properties

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public ObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IdentificationTypes));
                }
            }
        } = [];

        public IdentificationTypeGraphQLModel? SelectedIdentificationType
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedIdentificationType));
                    NotifyOfPropertyChange(nameof(CanEditIdentificationType));
                    NotifyOfPropertyChange(nameof(CanDeleteIdentificationType));
                }
            }
        }

        public string FilterSearch
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3)
                    {
                        PageIndex = 1;
                        _ = _searchDebounce.RunAsync(LoadIdentificationTypesAsync);
                    }
                }
            }
        } = string.Empty;

        #endregion

        #region Pagination

        public int PageIndex
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PageIndex));
                }
            }
        } = 1;

        public int PageSize
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PageSize));
                }
            }
        } = 50;

        public int TotalCount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
            }
        }

        public string ResponseTime
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ResponseTime));
                }
            }
        } = string.Empty;

        #endregion

        #region Permissions

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.IdentificationType.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.IdentificationType.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.IdentificationType.Delete);

        #endregion

        #region Button States

        public bool CanCreateIdentificationType => HasCreatePermission && !IsBusy;
        public bool CanEditIdentificationType => HasEditPermission && SelectedIdentificationType != null;
        public bool CanDeleteIdentificationType => HasDeletePermission && SelectedIdentificationType != null;

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
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            PermissionCache permissionCache)
        {
            _eventAggregator = eventAggregator;
            _identificationTypeService = identificationTypeService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
            _permissionCache = permissionCache;
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
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.IdentificationType);
                await LoadIdentificationTypesAsync();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnViewReady)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
                await TryCloseAsync();
            }
            finally
            {
                IsBusy = false;
            }
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateIdentificationType));
            NotifyOfPropertyChange(nameof(CanEditIdentificationType));
            NotifyOfPropertyChange(nameof(CanDeleteIdentificationType));
            this.SetFocus(() => FilterSearch);
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
                IdentificationTypeDetailViewModel detail = new(_identificationTypeService, _eventAggregator, _stringLengthCache, _joinableTaskFactory);
                detail.SetForNew();
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.50;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Nuevo tipo de documento");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(CreateIdentificationTypeAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
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
                IdentificationTypeDetailViewModel detail = new(_identificationTypeService, _eventAggregator, _stringLengthCache, _joinableTaskFactory);
                detail.SetForEdit(SelectedIdentificationType);
                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.50;
                IsBusy = false;
                await _dialogService.ShowDialogAsync(detail, "Editar tipo de documento");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EditIdentificationTypeAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
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
                ExpandoObject canDeleteVars = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedIdentificationType.Id)
                    .Build();
                CanDeleteType validation = await _identificationTypeService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

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
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(DeleteIdentificationTypeAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<DeleteResponseType> ExecuteDeleteAsync(int id)
        {
            var (fragment, query) = _deleteIdentificationTypeQuery.Value;
            ExpandoObject variables = new GraphQLVariables()
                .For(fragment, "id", id)
                .Build();
            return await _identificationTypeService.DeleteAsync<DeleteResponseType>(query, variables);
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

                dynamic filters = new ExpandoObject();
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<IdentificationTypeGraphQLModel> result = await _identificationTypeService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                IdentificationTypes = [.. result.Entries];
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadIdentificationTypesAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
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

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(CanCreateIdentificationType));
            NotifyOfPropertyChange(nameof(CanEditIdentificationType));
            NotifyOfPropertyChange(nameof(CanDeleteIdentificationType));
            return Task.CompletedTask;
        }

        #endregion
    }
}
