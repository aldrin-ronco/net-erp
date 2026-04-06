using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using Models.Login;
using NetErp.Global.UserPermission.DTO;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;
using IDialogService = NetErp.Helpers.IDialogService;

namespace NetErp.Global.UserPermission.ViewModels
{
    public class UserPermissionViewModel : Screen,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        private readonly IRepository<MenuModuleGraphQLModel> _menuModuleService;
        private readonly IRepository<PermissionDefinitionGraphQLModel> _permissionDefinitionService;
        private readonly IRepository<CompanyPermissionDefaultGraphQLModel> _companyPermDefaultService;
        private readonly IRepository<UserPermissionGraphQLModel> _userPermissionService;
        private readonly CollaboratorCache _collaboratorCache;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly PermissionCache _permissionCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region Constructor

        public UserPermissionViewModel(
            IRepository<MenuModuleGraphQLModel> menuModuleService,
            IRepository<PermissionDefinitionGraphQLModel> permissionDefinitionService,
            IRepository<CompanyPermissionDefaultGraphQLModel> companyPermDefaultService,
            IRepository<UserPermissionGraphQLModel> userPermissionService,
            CollaboratorCache collaboratorCache,
            Helpers.Services.INotificationService notificationService,
            IDialogService dialogService,
            IEventAggregator eventAggregator,
            PermissionCache permissionCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _menuModuleService = menuModuleService;
            _permissionDefinitionService = permissionDefinitionService;
            _companyPermDefaultService = companyPermDefaultService;
            _userPermissionService = userPermissionService;
            _collaboratorCache = collaboratorCache;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _permissionCache = permissionCache;
            _joinableTaskFactory = joinableTaskFactory;
            _eventAggregator.SubscribeOnPublishedThread(this);
        }

        #endregion

        #region Properties

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

        public ObservableCollection<CollaboratorGraphQLModel> Collaborators
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Collaborators));
                }
            }
        } = [];

        public CollaboratorGraphQLModel? SelectedCollaborator
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCollaborator));
                    NotifyOfPropertyChange(nameof(HasCollaboratorSelected));
                    if (value != null)
                        _ = LoadUserPermissionsAsync(value.Account.Id);
                    else
                        ClearTree();
                }
            }
        }

        public bool HasCollaboratorSelected => SelectedCollaborator is not null;

        public ObservableCollection<UserPermissionTreeNodeDTO> TreeNodes { get; set; } = [];

        public ObservableCollection<UserPermissionTreeNodeDTO> DisplayTreeNodes
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

        public ObservableCollection<UserPermissionTreeNodeDTO> ModuleFilters
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

        public ObservableCollection<UserPermissionTreeNodeDTO> GroupFilters
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

        public UserPermissionTreeNodeDTO? SelectedModuleFilter
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

        public UserPermissionTreeNodeDTO? SelectedGroupFilter
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

        public bool HasModuleFilterSelected => SelectedModuleFilter is not null;

        #region Permissions

        public bool HasEditPermission => _permissionCache.IsAllowed(PermissionCodes.UserPermission.Edit);
        public bool HasBatchAssignPermission => _permissionCache.IsAllowed(PermissionCodes.UserPermission.BatchAssign);

        #endregion

        public bool HasChanges
        {
            get
            {
                foreach (UserPermissionTreeNodeDTO node in _allPermissionNodes)
                {
                    if (node.HasChanged) return true;
                }
                return false;
            }
        }

        public bool CanSave => HasEditPermission && HasChanges;
        public bool CanBatchAssign => HasBatchAssignPermission;

        #endregion

        #region Private State

        private List<MenuModuleGraphQLModel> _fullMenuHierarchy = [];
        private List<PermissionDefinitionGraphQLModel> _allPermissionDefinitions = [];
        private List<CompanyPermissionDefaultGraphQLModel> _allCompanyDefaults = [];
        private List<UserPermissionGraphQLModel> _currentUserPermissions = [];
        private List<UserPermissionTreeNodeDTO> _allPermissionNodes = [];

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        private ICommand? _undoCommand;
        public ICommand UndoCommand
        {
            get
            {
                _undoCommand ??= new DelegateCommand(() => UndoChanges());
                return _undoCommand;
            }
        }

        private ICommand? _batchAssignCommand;
        public ICommand BatchAssignCommand
        {
            get
            {
                _batchAssignCommand ??= new AsyncCommand(BatchAssignAsync);
                return _batchAssignCommand;
            }
        }

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                IsBusy = true;
                await _collaboratorCache.EnsureLoadedAsync();

                NotifyOfPropertyChange(nameof(HasEditPermission));
                NotifyOfPropertyChange(nameof(HasBatchAssignPermission));
                NotifyOfPropertyChange(nameof(CanSave));
                NotifyOfPropertyChange(nameof(CanBatchAssign));
                await LoadMenuHierarchyAsync();
                await LoadPermissionDefinitionsAsync();
                await LoadCompanyPermissionDefaultsAsync();
                Collaborators = [.. _collaboratorCache.Items.OrderBy(c => c.Account.FullName)];
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
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.Unsubscribe(this);
                TreeNodes.Clear();
                DisplayTreeNodes.Clear();
                _allPermissionNodes.Clear();
                Collaborators.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Data Loading

        private async Task LoadMenuHierarchyAsync()
        {
            var (fragment, query) = _menuHierarchyQuery.Value;

            ExpandoObject variables = new GraphQLVariables()
                .For(fragment, "pagination", new { pageSize = -1 })
                .Build();

            PageType<MenuModuleGraphQLModel> result = await _menuModuleService.GetPageAsync(query, variables);
            _fullMenuHierarchy = [.. result.Entries];
        }

        private async Task LoadPermissionDefinitionsAsync()
        {
            var (fragment, query) = _permissionDefinitionsQuery.Value;

            ExpandoObject variables = new GraphQLVariables()
                .For(fragment, "pagination", new { pageSize = -1 })
                .Build();

            PageType<PermissionDefinitionGraphQLModel> result = await _permissionDefinitionService.GetPageAsync(query, variables);
            _allPermissionDefinitions = [.. result.Entries];
        }

        private async Task LoadCompanyPermissionDefaultsAsync()
        {
            var (fragment, query) = _companyPermDefaultsQuery.Value;

            ExpandoObject variables = new GraphQLVariables()
                .For(fragment, "pagination", new { pageSize = -1 })
                .Build();

            PageType<CompanyPermissionDefaultGraphQLModel> result = await _companyPermDefaultService.GetPageAsync(query, variables);
            _allCompanyDefaults = [.. result.Entries];
        }

        private async Task LoadUserPermissionsAsync(int accountId)
        {
            try
            {
                IsBusy = true;

                var (fragment, query) = _userPermissionsQuery.Value;

                dynamic filters = new ExpandoObject();
                filters.accountId = accountId;

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "filters", filters)
                    .For(fragment, "pagination", new { pageSize = -1 })
                    .Build();

                PageType<UserPermissionGraphQLModel> result = await _userPermissionService.GetPageAsync(query, variables);
                _currentUserPermissions = [.. result.Entries];

                BuildTree();
                ModuleFilters = [.. TreeNodes];
                SelectedModuleFilter = null;
                ApplyTreeFilter();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(LoadUserPermissionsAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Tree Building

        private void BuildTree()
        {
            TreeNodes.Clear();
            _allPermissionNodes.Clear();

            Dictionary<int, List<PermissionDefinitionGraphQLModel>> permsByMenuItem = _allPermissionDefinitions
                .Where(pd => pd.MenuItem != null)
                .GroupBy(pd => pd.MenuItem!.Id)
                .ToDictionary(g => g.Key, g => g.OrderBy(pd => pd.DisplayOrder).ToList());

            Dictionary<int, CompanyPermissionDefaultGraphQLModel> companyDefaultsByPermId = _allCompanyDefaults
                .Where(cd => cd.PermissionDefinition != null)
                .ToDictionary(cd => cd.PermissionDefinition!.Id);

            Dictionary<int, UserPermissionGraphQLModel> userPermsByPermId = _currentUserPermissions
                .Where(up => up.PermissionDefinition != null)
                .ToDictionary(up => up.PermissionDefinition!.Id);

            foreach (MenuModuleGraphQLModel module in _fullMenuHierarchy.OrderBy(m => m.DisplayOrder))
            {
                UserPermissionTreeNodeDTO moduleNode = new()
                {
                    Id = module.Id,
                    Name = module.Name,
                    NodeType = UserPermissionTreeNodeType.Module
                };

                foreach (MenuItemGroupGraphQLModel group in module.MenuItemGroups.OrderBy(g => g.DisplayOrder))
                {
                    UserPermissionTreeNodeDTO groupNode = new()
                    {
                        Id = group.Id,
                        Name = group.Name,
                        NodeType = UserPermissionTreeNodeType.Group,
                        Parent = moduleNode
                    };

                    foreach (MenuItemGraphQLModel item in group.MenuItems.OrderBy(i => i.DisplayOrder))
                    {
                        if (!item.IsActive) continue;
                        if (!permsByMenuItem.TryGetValue(item.Id, out List<PermissionDefinitionGraphQLModel>? permsForItem)) continue;

                        UserPermissionTreeNodeDTO itemNode = new()
                        {
                            Id = item.Id,
                            Name = item.Name,
                            NodeType = UserPermissionTreeNodeType.Item,
                            Parent = groupNode
                        };

                        List<PermissionDefinitionGraphQLModel> actionPerms = permsForItem.Where(p => p.PermissionType == "ACTION").ToList();
                        List<PermissionDefinitionGraphQLModel> fieldPerms = permsForItem.Where(p => p.PermissionType == "FIELD").ToList();

                        if (actionPerms.Count > 0)
                        {
                            UserPermissionTreeNodeDTO actionGroupNode = new()
                            {
                                Name = "Acciones",
                                NodeType = UserPermissionTreeNodeType.PermissionTypeGroup,
                                Parent = itemNode
                            };
                            AddPermissionNodes(actionGroupNode, actionPerms, companyDefaultsByPermId, userPermsByPermId);
                            itemNode.Children.Add(actionGroupNode);
                        }

                        if (fieldPerms.Count > 0)
                        {
                            UserPermissionTreeNodeDTO fieldGroupNode = new()
                            {
                                Name = "Campos obligatorios",
                                NodeType = UserPermissionTreeNodeType.PermissionTypeGroup,
                                Parent = itemNode
                            };
                            AddPermissionNodes(fieldGroupNode, fieldPerms, companyDefaultsByPermId, userPermsByPermId);
                            itemNode.Children.Add(fieldGroupNode);
                        }

                        groupNode.Children.Add(itemNode);
                    }

                    if (groupNode.Children.Count > 0)
                        moduleNode.Children.Add(groupNode);
                }

                if (moduleNode.Children.Count > 0)
                    TreeNodes.Add(moduleNode);
            }
        }

        private void AddPermissionNodes(
            UserPermissionTreeNodeDTO parentNode,
            List<PermissionDefinitionGraphQLModel> perms,
            Dictionary<int, CompanyPermissionDefaultGraphQLModel> companyDefaultsByPermId,
            Dictionary<int, UserPermissionGraphQLModel> userPermsByPermId)
        {
            foreach (PermissionDefinitionGraphQLModel permDef in perms)
            {
                // Resolve effective default: company → system
                string effectiveDefault = permDef.SystemDefault;
                if (companyDefaultsByPermId.TryGetValue(permDef.Id, out CompanyPermissionDefaultGraphQLModel? compDefault))
                    effectiveDefault = compDefault.DefaultValue;

                // Resolve user permission
                userPermsByPermId.TryGetValue(permDef.Id, out UserPermissionGraphQLModel? userPerm);

                UserPermissionValue? userValue = userPerm?.Value switch
                {
                    "ALLOWED" => UserPermissionValue.Allowed,
                    "DENIED" => UserPermissionValue.Denied,
                    "REQUIRED" => UserPermissionValue.Required,
                    "OPTIONAL" => UserPermissionValue.Optional,
                    _ => null
                };

                DateTime? expiresAt = null;
                if (!string.IsNullOrEmpty(userPerm?.ExpiresAt) &&
                    DateTime.TryParse(userPerm.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                    expiresAt = parsed;

                UserPermissionTreeNodeDTO permNode = new()
                {
                    Id = permDef.Id,
                    Name = permDef.Name,
                    Code = permDef.Code,
                    PermissionType = permDef.PermissionType,
                    EffectiveDefault = effectiveDefault,
                    UserPermissionId = userPerm?.Id,
                    UserValue = userValue,
                    OriginalUserValue = userValue,
                    ExpiresAt = expiresAt,
                    OriginalExpiresAt = expiresAt,
                    NodeType = UserPermissionTreeNodeType.Permission,
                    Parent = parentNode
                };

                permNode.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(UserPermissionTreeNodeDTO.HasChanged))
                    {
                        NotifyOfPropertyChange(nameof(HasChanges));
                        NotifyOfPropertyChange(nameof(CanSave));
                    }
                };

                parentNode.Children.Add(permNode);
                _allPermissionNodes.Add(permNode);
            }
        }

        private async Task RefreshUserPermissionValuesAsync(int accountId)
        {
            try
            {
                IsBusy = true;

                var (fragment, query) = _userPermissionsQuery.Value;

                dynamic filters = new ExpandoObject();
                filters.accountId = accountId;

                ExpandoObject variables = new GraphQLVariables()
                    .For(fragment, "filters", filters)
                    .For(fragment, "pagination", new { pageSize = -1 })
                    .Build();

                PageType<UserPermissionGraphQLModel> result = await _userPermissionService.GetPageAsync(query, variables);

                Dictionary<int, UserPermissionGraphQLModel> userPermsByPermId = result.Entries
                    .Where(up => up.PermissionDefinition != null)
                    .ToDictionary(up => up.PermissionDefinition!.Id);

                foreach (UserPermissionTreeNodeDTO node in _allPermissionNodes)
                {
                    userPermsByPermId.TryGetValue(node.Id, out UserPermissionGraphQLModel? userPerm);

                    UserPermissionValue? userValue = userPerm?.Value switch
                    {
                        "ALLOWED" => UserPermissionValue.Allowed,
                        "DENIED" => UserPermissionValue.Denied,
                        "REQUIRED" => UserPermissionValue.Required,
                        "OPTIONAL" => UserPermissionValue.Optional,
                        _ => null
                    };

                    DateTime? expiresAt = null;
                    if (!string.IsNullOrEmpty(userPerm?.ExpiresAt) &&
                        DateTime.TryParse(userPerm.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                        expiresAt = parsed;

                    node.UserPermissionId = userPerm?.Id;
                    node.UserValue = userValue;
                    node.OriginalUserValue = userValue;
                    node.ExpiresAt = expiresAt;
                    node.OriginalExpiresAt = expiresAt;
                }

                NotifyOfPropertyChange(nameof(HasChanges));
                NotifyOfPropertyChange(nameof(CanSave));
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(RefreshUserPermissionValuesAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ClearTree()
        {
            TreeNodes.Clear();
            DisplayTreeNodes = [];
            _allPermissionNodes.Clear();
            NotifyOfPropertyChange(nameof(HasChanges));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Filters

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
                DisplayTreeNodes = [.. TreeNodes];
                return;
            }

            if (SelectedGroupFilter is not null)
            {
                UserPermissionTreeNodeDTO moduleWrapper = new()
                {
                    Id = SelectedModuleFilter.Id,
                    Name = SelectedModuleFilter.Name,
                    NodeType = UserPermissionTreeNodeType.Module
                };
                UserPermissionTreeNodeDTO groupWrapper = new()
                {
                    Id = SelectedGroupFilter.Id,
                    Name = SelectedGroupFilter.Name,
                    NodeType = UserPermissionTreeNodeType.Group,
                    Parent = moduleWrapper
                };
                foreach (UserPermissionTreeNodeDTO item in SelectedGroupFilter.Children)
                    groupWrapper.Children.Add(item);
                moduleWrapper.Children.Add(groupWrapper);
                DisplayTreeNodes = [moduleWrapper];
            }
            else
            {
                DisplayTreeNodes = [SelectedModuleFilter];
            }
        }

        #endregion

        #region Save / Undo

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;

                (GraphQLQueryFragment createFragment, string createQuery) = _createMutation.Value;
                (GraphQLQueryFragment updateFragment, string updateQuery) = _updateMutation.Value;
                (GraphQLQueryFragment deleteFragment, string deleteQuery) = _deleteMutation.Value;

                int accountId = SelectedCollaborator!.Account.Id;

                foreach (UserPermissionTreeNodeDTO node in _allPermissionNodes)
                {
                    if (!node.HasChanged) continue;

                    if (node.OriginalUserValue == null && node.UserValue != null)
                    {
                        // CREATE
                        string valueStr = ToGraphQLValue(node.UserValue.Value);
                        dynamic variables = new GraphQLVariables()
                            .For(createFragment, "input", new
                            {
                                accountId,
                                permissionDefinitionId = node.Id,
                                value = valueStr,
                                expiresAt = node.ExpiresAt == null
                                ? null
                                : new DateTimeOffset(DateTime.SpecifyKind(node.ExpiresAt.Value, DateTimeKind.Utc)).ToString("O")
                            })
                            .Build();
                        UpsertResponseType<UserPermissionGraphQLModel> result =
                            await _userPermissionService.CreateAsync<UpsertResponseType<UserPermissionGraphQLModel>>(createQuery, variables);
                        if (!result.Success)
                        {
                            ThemedMessageBox.Show("Atención !", $"Error al crear permiso '{node.Name}'.\n\n{result.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        node.UserPermissionId = result.Entity.Id;
                    }
                    else if (node.OriginalUserValue != null && node.UserValue != null)
                    {
                        // UPDATE
                        string valueStr = ToGraphQLValue(node.UserValue.Value);
                        dynamic variables = new GraphQLVariables()
                            .For(updateFragment, "id", node.UserPermissionId)
                            .For(updateFragment, "data", new
                            {
                                value = valueStr,
                                expiresAt = node.ExpiresAt == null
                                ? null
                                : new DateTimeOffset(DateTime.SpecifyKind(node.ExpiresAt.Value, DateTimeKind.Utc)).ToString("O")
                            })
                            .Build();
                        UpsertResponseType<UserPermissionGraphQLModel> result =
                            await _userPermissionService.UpdateAsync<UpsertResponseType<UserPermissionGraphQLModel>>(updateQuery, variables);
                        if (!result.Success)
                        {
                            ThemedMessageBox.Show("Atención !", $"Error al actualizar permiso '{node.Name}'.\n\n{result.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else if (node.OriginalUserValue != null && node.UserValue == null)
                    {
                        // DELETE
                        dynamic variables = new GraphQLVariables()
                            .For(deleteFragment, "id", node.UserPermissionId)
                            .Build();
                        DeleteResponseType result = await _userPermissionService.DeleteAsync<DeleteResponseType>(deleteQuery, variables);
                        if (!result.Success)
                        {
                            ThemedMessageBox.Show("Atención !", $"Error al eliminar permiso '{node.Name}'.\n\n{result.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        node.UserPermissionId = null;
                        node.ExpiresAt = null;
                    }

                    node.OriginalUserValue = node.UserValue;
                    node.OriginalExpiresAt = node.ExpiresAt;
                }

                _notificationService.ShowSuccess("Permisos del usuario actualizados correctamente");
                NotifyOfPropertyChange(nameof(HasChanges));
                NotifyOfPropertyChange(nameof(CanSave));
                await _eventAggregator.PublishOnCurrentThreadAsync(new UserPermissionChangedMessage());
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UndoChanges()
        {
            foreach (UserPermissionTreeNodeDTO node in _allPermissionNodes)
            {
                node.UserValue = node.OriginalUserValue;
                node.ExpiresAt = node.OriginalExpiresAt;
            }
        }

        private static string ToGraphQLValue(UserPermissionValue value) => value switch
        {
            UserPermissionValue.Allowed => "ALLOWED",
            UserPermissionValue.Denied => "DENIED",
            UserPermissionValue.Required => "REQUIRED",
            UserPermissionValue.Optional => "OPTIONAL",
            _ => "ALLOWED"
        };

        public async Task BatchAssignAsync()
        {
            try
            {
                IsBusy = true;
                await Task.Yield();
                BatchUserPermissionViewModel batchVm = new(_userPermissionService, _eventAggregator, _joinableTaskFactory);
                batchVm.SetData(_fullMenuHierarchy, _allPermissionDefinitions, _collaboratorCache.Items);
                IsBusy = false;

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    batchVm.DialogWidth = parentView.ActualWidth * 0.85;
                    batchVm.DialogHeight = parentView.ActualHeight * 0.90;
                }

                bool? result = await _dialogService.ShowDialogAsync(batchVm, "Asignación masiva de permisos");
                if (result == true)
                {
                    _notificationService.ShowSuccess("Permisos asignados masivamente");
                    if (SelectedCollaborator != null)
                        await RefreshUserPermissionValuesAsync(SelectedCollaborator.Account.Id);
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(BatchAssignAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Event Handlers

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(nameof(HasEditPermission));
            NotifyOfPropertyChange(nameof(HasBatchAssignPermission));
            NotifyOfPropertyChange(nameof(CanSave));
            NotifyOfPropertyChange(nameof(CanBatchAssign));
            return Task.CompletedTask;
        }

        #endregion

        #region GraphQL Queries

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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _permissionDefinitionsQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<PermissionDefinitionGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Code)
                    .Field(e => e.Name)
                    .Field(e => e.PermissionType)
                    .Field(e => e.SystemDefault)
                    .Field(e => e.DisplayOrder)
                    .Select(e => e.MenuItem, mi => mi
                        .Field(m => m!.Id)))
                .Build();

            GraphQLQueryParameter pagination = new("pagination", "Pagination");
            GraphQLQueryFragment fragment = new("permissionDefinitionsPage", [pagination], fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _companyPermDefaultsQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<CompanyPermissionDefaultGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.DefaultValue)
                    .Select(e => e.PermissionDefinition, pd => pd
                        .Field(p => p!.Id)))
                .Build();

            GraphQLQueryParameter pagination = new("pagination", "Pagination");
            GraphQLQueryFragment fragment = new("companyPermissionDefaultsPage", [pagination], fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _userPermissionsQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<UserPermissionGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Value)
                    .Field(e => e.ExpiresAt)
                    .Select(e => e.PermissionDefinition, pd => pd
                        .Field(p => p!.Id)))
                .Build();

            GraphQLQueryParameter filters = new("filters", "UserPermissionFilters");
            GraphQLQueryParameter pagination = new("pagination", "Pagination");
            GraphQLQueryFragment fragment = new("userPermissionsPage", [filters, pagination], fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createMutation = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<UpsertResponseType<UserPermissionGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "userPermission", nested: sq => sq
                    .Field(e => e.Id))
                .Field(f => f.Success)
                .Field(f => f.Message)
                .SelectList(f => f.Errors, sq => sq
                    .Field(e => e.Message))
                .Build();

            GraphQLQueryFragment fragment = new("createUserPermission",
                [new("input", "CreateUserPermissionInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateMutation = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<UpsertResponseType<UserPermissionGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "userPermission", nested: sq => sq
                    .Field(e => e.Id))
                .Field(f => f.Success)
                .Field(f => f.Message)
                .SelectList(f => f.Errors, sq => sq
                    .Field(e => e.Message))
                .Build();

            GraphQLQueryFragment fragment = new("updateUserPermission",
                [new("id", "ID!"), new("data", "UpdateUserPermissionInput!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteMutation = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Build();

            GraphQLQueryFragment fragment = new("deleteUserPermission",
                [new("id", "ID!")],
                fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
