using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using IDialogService = NetErp.Helpers.IDialogService;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Global.AccessProfile.DTO;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.AccessProfileGraphQLModel;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.AccessProfile.ViewModels
{
    public class AccessProfileViewModel : Screen,
        IHandle<AccessProfileCreateMessage>,
        IHandle<AccessProfileUpdateMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<AccessProfileGraphQLModel> _accessProfileService;
        private readonly IRepository<MenuModuleGraphQLModel> _menuModuleService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly PermissionCache _permissionCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region Constructor

        public AccessProfileViewModel(
            IEventAggregator eventAggregator,
            IRepository<AccessProfileGraphQLModel> accessProfileService,
            IRepository<MenuModuleGraphQLModel> menuModuleService,
            Helpers.Services.INotificationService notificationService,
            IDialogService dialogService,
            StringLengthCache stringLengthCache,
            PermissionCache permissionCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _eventAggregator = eventAggregator;
            _accessProfileService = accessProfileService;
            _menuModuleService = menuModuleService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _stringLengthCache = stringLengthCache;
            _permissionCache = permissionCache;
            _joinableTaskFactory = joinableTaskFactory;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Properties

        public ObservableCollection<AccessProfileGraphQLModel> Profiles
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Profiles));
                }
            }
        } = [];

        public AccessProfileGraphQLModel? SelectedProfile
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedProfile));
                    NotifyOfPropertyChange(nameof(HasProfileSelected));
                    NotifyOfPropertyChange(nameof(CanCloneProfile));
                    NotifyOfPropertyChange(nameof(CanEditProfileHeader));
                    NotifyOfPropertyChange(nameof(CanEditProfile));
                    NotifyOfPropertyChange(nameof(CanDeleteProfile));
                    IsEditing = false;
                    if (value != null)
                        _ = LoadProfileMenuItemsAsync(value.Id);
                    else
                        ClearTreeView();
                }
            }
        }

        public bool IsEditing
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsEditing));
                    NotifyOfPropertyChange(nameof(CanEditProfileHeader));
                    NotifyOfPropertyChange(nameof(CanEditProfile));
                    NotifyOfPropertyChange(nameof(CanDeleteProfile));
                    NotifyOfPropertyChange(nameof(CanCreateProfile));
                    NotifyOfPropertyChange(nameof(CanCloneProfile));
                    NotifyOfPropertyChange(nameof(CanSaveMenuChanges));
                }
            }
        }

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

        public bool HasProfileSelected => SelectedProfile is not null;
        public bool HasModuleFilterSelected => SelectedModuleFilter is not null;

        public bool ShowActiveProfilesOnly
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowActiveProfilesOnly));
                    _ = LoadProfilesAsync();
                }
            }
        } = true;
        #region Permissions

        public bool HasCreatePermission => _permissionCache.IsAllowed(PermissionCodes.AccessProfile.Create);
        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.AccessProfile.Edit);
        public bool HasDeletePermission => _permissionCache.IsAllowed(PermissionCodes.AccessProfile.Delete);
        public bool HasClonePermission => _permissionCache.IsAllowed(PermissionCodes.AccessProfile.Clone);

        #endregion

        public bool CanCreateProfile => HasCreatePermission && !IsEditing;
        public bool CanCloneProfile => HasClonePermission && SelectedProfile is not null && !IsEditing;
        public bool CanEditProfileHeader => HasEditPermission && SelectedProfile is not null && !SelectedProfile.IsSystemAdmin && !IsEditing;
        public bool CanEditProfile => HasEditPermission && SelectedProfile is not null && !SelectedProfile.IsSystemAdmin && !IsEditing;
        public bool CanDeleteProfile => HasDeletePermission && SelectedProfile is not null && !SelectedProfile.IsSystemAdmin && !IsEditing;
        public bool CanSaveMenuChanges => IsEditing && HasMenuChanges;

        private bool _isInitialized;

        public bool HasRecords => _isInitialized && !ShowEmptyState;

        public bool ShowEmptyState
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ShowEmptyState));
                    NotifyOfPropertyChange(nameof(HasRecords));
                }
            }
        }

        public ObservableCollection<MenuTreeNodeDTO> MenuTreeNodes { get; set; } = [];

        public ObservableCollection<MenuTreeNodeDTO> ModuleFilters
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ModuleFilters));
                }
            }
        } = [];

        public ObservableCollection<MenuTreeNodeDTO> GroupFilters
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(GroupFilters));
                }
            }
        } = [];

        public MenuTreeNodeDTO? SelectedModuleFilter
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedModuleFilter));
                    NotifyOfPropertyChange(nameof(HasModuleFilterSelected));
                    RebuildGroupFilters();
                    SelectedGroupFilter = null;
                    ApplyTreeFilter();
                }
            }
        }

        public MenuTreeNodeDTO? SelectedGroupFilter
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedGroupFilter));
                    ApplyTreeFilter();
                }
            }
        }

        public ObservableCollection<MenuTreeNodeDTO> DisplayTreeNodes
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DisplayTreeNodes));
                }
            }
        } = [];

        #endregion

        #region Menu Tree State

        private List<MenuModuleGraphQLModel> _fullMenuHierarchy = [];
        private HashSet<int> _originalMenuItemIds = [];
        private Dictionary<int, int> _originalMenuItemRelationIds = [];

        public bool HasMenuChanges
        {
            get
            {
                HashSet<int> currentIds = GetCheckedMenuItemIds();
                return !_originalMenuItemIds.SetEquals(currentIds);
            }
        }

        #endregion

        #region Commands

        private ICommand? _editProfileHeaderCommand;
        public ICommand EditProfileHeaderCommand
        {
            get
            {
                _editProfileHeaderCommand ??= new AsyncCommand(EditProfileHeaderAsync);
                return _editProfileHeaderCommand;
            }
        }

        private ICommand? _createProfileCommand;
        public ICommand CreateProfileCommand
        {
            get
            {
                _createProfileCommand ??= new AsyncCommand(CreateProfileAsync);
                return _createProfileCommand;
            }
        }

        private ICommand? _cloneProfileCommand;
        public ICommand CloneProfileCommand
        {
            get
            {
                _cloneProfileCommand ??= new AsyncCommand(CloneProfileAsync);
                return _cloneProfileCommand;
            }
        }

        private ICommand? _editProfileCommand;
        public ICommand EditProfileCommand
        {
            get
            {
                _editProfileCommand ??= new AsyncCommand(EditProfileAsync);
                return _editProfileCommand;
            }
        }

        private ICommand? _saveMenuChangesCommand;
        public ICommand SaveMenuChangesCommand
        {
            get
            {
                _saveMenuChangesCommand ??= new AsyncCommand(SaveMenuChangesAsync);
                return _saveMenuChangesCommand;
            }
        }

        private ICommand? _undoCommand;
        public ICommand UndoCommand
        {
            get
            {
                _undoCommand ??= new DelegateCommand(() => UndoMenuChanges());
                return _undoCommand;
            }
        }

        private ICommand? _deleteProfileCommand;
        public ICommand DeleteProfileCommand
        {
            get
            {
                _deleteProfileCommand ??= new AsyncCommand(DeleteProfileAsync);
                return _deleteProfileCommand;
            }
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.AccessProfile);

                NotifyOfPropertyChange(nameof(HasCreatePermission));
                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasDeletePermission));
                NotifyOfPropertyChange(nameof(HasClonePermission));
                NotifyOfPropertyChange(nameof(CanCreateProfile));
                NotifyOfPropertyChange(nameof(CanCloneProfile));
                NotifyOfPropertyChange(nameof(CanEditProfileHeader));
                NotifyOfPropertyChange(nameof(CanEditProfile));
                NotifyOfPropertyChange(nameof(CanDeleteProfile));

                await LoadMenuHierarchyAsync();
                BuildMenuTree();
                await LoadProfilesAsync();
                _isInitialized = true;
                ShowEmptyState = Profiles == null || Profiles.Count == 0;
                NotifyOfPropertyChange(nameof(HasRecords));
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
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
                Profiles?.Clear();
                MenuTreeNodes?.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Profile Operations

        private async Task LoadProfilesAsync()
        {
            try
            {
                IsBusy = true;

                var (fragment, query) = _loadProfilesQuery.Value;

                dynamic filters = new ExpandoObject();
                if (ShowActiveProfilesOnly) filters.isActive = true;

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<AccessProfileGraphQLModel> result = await _accessProfileService.GetPageAsync(query, variables);
                Profiles = [.. result.Entries];
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadProfilesAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CreateProfileAsync()
        {
            try
            {
                IsBusy = true;
                AccessProfileDetailViewModel detail = new(_accessProfileService, _eventAggregator, _stringLengthCache, _joinableTaskFactory);
                detail.SetForNew();
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.40;

                await _dialogService.ShowDialogAsync(detail, "Nuevo perfil de acceso");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(CreateProfileAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditProfileHeaderAsync()
        {
            try
            {
                IsBusy = true;
                AccessProfileDetailViewModel detail = new(_accessProfileService, _eventAggregator, _stringLengthCache, _joinableTaskFactory);
                detail.SetForEdit(SelectedProfile!);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.40;

                await _dialogService.ShowDialogAsync(detail, "Editar perfil de acceso");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EditProfileHeaderAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CloneProfileAsync()
        {
            try
            {
                IsBusy = true;
                AccessProfileDetailViewModel detail = new(_accessProfileService, _eventAggregator, _stringLengthCache, _joinableTaskFactory);
                detail.SetForClone(SelectedProfile!.Id, SelectedProfile.Name);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                    detail.DialogWidth = parentView.ActualWidth * 0.40;

                await _dialogService.ShowDialogAsync(detail, "Copiar perfil de acceso");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(CloneProfileAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditProfileAsync()
        {
            try
            {
                IsBusy = true;
                ApplyTreeFilter();
                IsEditing = true;
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(EditProfileAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SaveMenuChangesAsync()
        {
            try
            {
                IsBusy = true;
                await SyncMenuItemsAsync(SelectedProfile!.Id);
                _notificationService.ShowSuccess("Opciones del menú actualizadas correctamente");
                _originalMenuItemIds = GetCheckedMenuItemIds();
                NotifyOfPropertyChange(nameof(CanSaveMenuChanges));
                IsEditing = false;
                ApplyTreeFilter();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(SaveMenuChangesAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UndoMenuChanges()
        {
            UncheckAllNodes();
            foreach (MenuTreeNodeDTO module in MenuTreeNodes)
            {
                foreach (MenuTreeNodeDTO group in module.Children)
                {
                    foreach (MenuTreeNodeDTO item in group.Children)
                    {
                        if (_originalMenuItemIds.Contains(item.Id))
                            item.IsChecked = true;
                    }
                }
            }
            IsEditing = false;
            ApplyTreeFilter();
        }

        public async Task DeleteProfileAsync()
        {
            try
            {
                IsBusy = true;

                (GraphQLQueryFragment canDeleteFragment, string canDeleteQuery) = _canDeleteQuery.Value;
                dynamic canDeleteVariables = new GraphQLVariables()
                    .For(canDeleteFragment, "id", SelectedProfile!.Id)
                    .Build();
                CanDeleteType validation = await _accessProfileService.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea eliminar el perfil seleccionado?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención !",
                        $"El perfil no puede ser eliminado\r\n\r\n{validation.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                (GraphQLQueryFragment deleteFragment, string deleteQuery) = _deleteQuery.Value;
                dynamic deleteVariables = new GraphQLVariables()
                    .For(deleteFragment, "id", SelectedProfile.Id)
                    .Build();
                DeleteResponseType deletedProfile = await _accessProfileService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVariables);

                if (!deletedProfile.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedProfile.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                _notificationService.ShowSuccess(deletedProfile.Message);
                SelectedProfile = null;
                await LoadProfilesAsync();
                ShowEmptyState = Profiles == null || Profiles.Count == 0;
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(DeleteProfileAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Event Handlers

        public async Task HandleAsync(AccessProfileCreateMessage message, CancellationToken cancellationToken)
        {
            ShowEmptyState = false;
            await LoadProfilesAsync();
            AccessProfileGraphQLModel? created = Profiles.FirstOrDefault(p => p.Id == message.CreatedAccessProfile.Entity.Id);
            if (created != null) SelectedProfile = created;
            _notificationService.ShowSuccess(message.CreatedAccessProfile.Message);
        }

        public async Task HandleAsync(AccessProfileUpdateMessage message, CancellationToken cancellationToken)
        {
            int selectedId = SelectedProfile?.Id ?? 0;
            await LoadProfilesAsync();
            AccessProfileGraphQLModel? updated = Profiles.FirstOrDefault(p => p.Id == selectedId);
            if (updated != null) SelectedProfile = updated;
            _notificationService.ShowSuccess(message.UpdatedAccessProfile.Message);
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasCreatePermission));
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasDeletePermission));
            NotifyOfPropertyChange(nameof(HasClonePermission));
            NotifyOfPropertyChange(nameof(CanCreateProfile));
            NotifyOfPropertyChange(nameof(CanCloneProfile));
            NotifyOfPropertyChange(nameof(CanEditProfileHeader));
            NotifyOfPropertyChange(nameof(CanEditProfile));
            NotifyOfPropertyChange(nameof(CanDeleteProfile));
            return Task.CompletedTask;
        }

        #endregion

        #region Menu Tree

        private async Task LoadMenuHierarchyAsync()
        {
            var (fragment, query) = _menuHierarchyQuery.Value;

            ExpandoObject variables = new GraphQLVariables()
                .For(fragment, "pagination", new { pageSize = -1 })
                .Build();

            PageType<MenuModuleGraphQLModel> result = await _menuModuleService.GetPageAsync(query, variables);
            _fullMenuHierarchy = [.. result.Entries];
        }

        private void BuildMenuTree()
        {
            MenuTreeNodes.Clear();
            foreach (MenuModuleGraphQLModel module in _fullMenuHierarchy.OrderBy(m => m.DisplayOrder))
            {
                MenuTreeNodeDTO moduleNode = new()
                {
                    Id = module.Id,
                    Name = module.Name,
                    NodeType = MenuTreeNodeType.Module
                };

                foreach (MenuItemGroupGraphQLModel group in module.MenuItemGroups.OrderBy(g => g.DisplayOrder))
                {
                    MenuTreeNodeDTO groupNode = new()
                    {
                        Id = group.Id,
                        Name = group.Name,
                        NodeType = MenuTreeNodeType.Group,
                        Parent = moduleNode
                    };

                    foreach (MenuItemGraphQLModel item in group.MenuItems.OrderBy(i => i.DisplayOrder))
                    {
                        if (!item.IsActive) continue;

                        MenuTreeNodeDTO itemNode = new()
                        {
                            Id = item.Id,
                            Name = item.Name,
                            NodeType = MenuTreeNodeType.Item,
                            Parent = groupNode
                        };
                        itemNode.PropertyChanged += (_, e) =>
                        {
                            if (e.PropertyName == nameof(MenuTreeNodeDTO.IsChecked))
                                NotifyOfPropertyChange(nameof(CanSaveMenuChanges));
                        };
                        groupNode.Children.Add(itemNode);
                    }

                    if (groupNode.Children.Count > 0)
                        moduleNode.Children.Add(groupNode);
                }

                if (moduleNode.Children.Count > 0)
                    MenuTreeNodes.Add(moduleNode);
            }

            ModuleFilters = [.. MenuTreeNodes];
        }

        private void RebuildGroupFilters()
        {
            if (SelectedModuleFilter is null)
            {
                GroupFilters = [];
                return;
            }
            GroupFilters = [.. SelectedModuleFilter.Children];
        }

        private void ApplyTreeFilter()
        {
            if (SelectedModuleFilter is null)
            {
                DisplayTreeNodes = MenuTreeNodes;
                return;
            }

            if (SelectedGroupFilter is not null)
            {
                // Mostrar solo el grupo seleccionado dentro de un wrapper de módulo
                // que preserve las relaciones Parent de los items originales
                MenuTreeNodeDTO moduleWrapper = new()
                {
                    Id = SelectedModuleFilter.Id,
                    Name = SelectedModuleFilter.Name,
                    NodeType = MenuTreeNodeType.Module
                };
                MenuTreeNodeDTO groupWrapper = new()
                {
                    Id = SelectedGroupFilter.Id,
                    Name = SelectedGroupFilter.Name,
                    NodeType = MenuTreeNodeType.Group,
                    Parent = moduleWrapper
                };
                // Referenciar los items originales (no copias) para preservar IsChecked y Parent
                foreach (MenuTreeNodeDTO item in SelectedGroupFilter.Children)
                    groupWrapper.Children.Add(item);
                moduleWrapper.Children.Add(groupWrapper);
                DisplayTreeNodes = [moduleWrapper];
            }
            else
            {
                DisplayTreeNodes = [SelectedModuleFilter];
            }
        }

        private async Task LoadProfileMenuItemsAsync(int profileId)
        {
            try
            {
                IsBusy = true;

                (GraphQLQueryFragment fragment, string query) = _loadByIdQuery.Value;

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "id", profileId)
                    .Build();

                AccessProfileGraphQLModel entity = await _accessProfileService.FindByIdAsync(query, variables);

                UncheckAllNodes();

                HashSet<int> assignedIds = [.. entity.AccessProfileMenuItems.Select(x => x.MenuItem?.Id ?? 0)];
                _originalMenuItemRelationIds = entity.AccessProfileMenuItems
                    .Where(x => x.MenuItem != null)
                    .ToDictionary(x => x.MenuItem!.Id, x => x.Id);

                foreach (MenuTreeNodeDTO module in MenuTreeNodes)
                {
                    foreach (MenuTreeNodeDTO group in module.Children)
                    {
                        foreach (MenuTreeNodeDTO item in group.Children)
                        {
                            if (assignedIds.Contains(item.Id))
                                item.IsChecked = true;
                        }
                    }
                }

                _originalMenuItemIds = GetCheckedMenuItemIds();
                ApplyTreeFilter();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadProfileMenuItemsAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ClearTreeView()
        {
            DisplayTreeNodes = [];
        }

        private void UncheckAllNodes()
        {
            foreach (MenuTreeNodeDTO module in MenuTreeNodes)
            {
                foreach (MenuTreeNodeDTO group in module.Children)
                {
                    foreach (MenuTreeNodeDTO item in group.Children)
                        item.IsChecked = false;
                }
            }
        }

        private HashSet<int> GetCheckedMenuItemIds()
        {
            HashSet<int> ids = [];
            foreach (MenuTreeNodeDTO module in MenuTreeNodes)
            {
                foreach (MenuTreeNodeDTO group in module.Children)
                {
                    foreach (MenuTreeNodeDTO item in group.Children)
                    {
                        if (item.IsChecked)
                            ids.Add(item.Id);
                    }
                }
            }
            return ids;
        }

        private async Task SyncMenuItemsAsync(int profileId)
        {
            HashSet<int> currentIds = GetCheckedMenuItemIds();
            IEnumerable<int> toAdd = currentIds.Except(_originalMenuItemIds);
            IEnumerable<int> toRemove = _originalMenuItemIds.Except(currentIds);

            (GraphQLQueryFragment createFragment, string createQuery) = _createMenuItemQuery.Value;
            (GraphQLQueryFragment deleteFragment, string deleteMenuItemQuery) = _deleteMenuItemQuery.Value;

            foreach (int menuItemId in toAdd)
            {
                dynamic variables = new GraphQLVariables()
                    .For(createFragment, "input", new { accessProfileId = profileId, menuItemId })
                    .Build();
                await _accessProfileService.CreateAsync<UpsertResponseType<AccessProfileMenuItemGraphQLModel>>(createQuery, variables);
            }

            foreach (int menuItemId in toRemove)
            {
                if (_originalMenuItemRelationIds.TryGetValue(menuItemId, out int relationId))
                {
                    dynamic variables = new GraphQLVariables()
                        .For(deleteFragment, "id", relationId)
                        .Build();
                    await _accessProfileService.DeleteAsync<DeleteResponseType>(deleteMenuItemQuery, variables);
                }
            }
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadProfilesQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<AccessProfileGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Description)
                    .Field(e => e.IsActive)
                    .Field(e => e.IsSystemAdmin))
                .Build();

            GraphQLQueryParameter parameter = new("filters", "AccessProfileFilters");
            GraphQLQueryFragment fragment = new("accessProfilesPage", [parameter], fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadByIdQuery = new(() =>
        {
            var fields = FieldSpec<AccessProfileGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Name)
                .Field(e => e.Description)
                .Field(e => e.IsActive)
                .Field(e => e.IsSystemAdmin)
                .SelectList(e => e.AccessProfileMenuItems, sq => sq
                    .Field(m => m.Id)
                    .Select(m => m.MenuItem, mi => mi
                        .Field(i => i!.Id)))
                .Build();

            var fragment = new GraphQLQueryFragment("accessProfile",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            GraphQLQueryParameter parameter = new("id", "ID!");
            GraphQLQueryFragment fragment = new("deleteAccessProfile", [parameter], fields, alias: "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            GraphQLQueryParameter parameter = new("id", "ID!");
            GraphQLQueryFragment fragment = new("canDeleteAccessProfile", [parameter], fields, alias: "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createMenuItemQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccessProfileMenuItemGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accessProfileMenuItem", nested: sq => sq
                    .Field(e => e.Id))
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("createAccessProfileMenuItem",
                [new("input", "CreateAccessProfileMenuItemInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteMenuItemQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            GraphQLQueryParameter parameter = new("id", "ID!");
            GraphQLQueryFragment fragment = new("deleteAccessProfileMenuItem", [parameter], fields, alias: "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _menuHierarchyQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<MenuModuleGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.DisplayOrder)
                    .SelectList(e => e.MenuItemGroups, groups => groups
                        .Field(g => g.Id)
                        .Field(g => g.Name)
                        .Field(g => g.DisplayOrder)
                        .SelectList(g => g.MenuItems, items => items
                            .Field(i => i.Id)
                            .Field(i => i.Name)
                            .Field(i => i.DisplayOrder)
                            .Field(i => i.IsActive))))
                .Build();

            GraphQLQueryParameter pagination = new("pagination", "Pagination");
            GraphQLQueryFragment fragment = new("menuModulesPage", [pagination], fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
    }
}
