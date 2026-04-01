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
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Global.UserPermission.ViewModels
{
    public class BatchUserPermissionViewModel : Screen
    {
        #region Dependencies

        private readonly IRepository<UserPermissionGraphQLModel> _userPermissionService;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region Constructor

        public BatchUserPermissionViewModel(
            IRepository<UserPermissionGraphQLModel> userPermissionService,
            JoinableTaskFactory joinableTaskFactory)
        {
            _userPermissionService = userPermissionService;
            _joinableTaskFactory = joinableTaskFactory;
        }

        #endregion

        #region Properties

        public double DialogWidth
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogWidth));
                }
            }
        } = 900;

        public double DialogHeight
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogHeight));
                }
            }
        } = 700;

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

        public ObservableCollection<BatchPermissionNodeDTO> TreeNodes { get; set; } = [];

        public ObservableCollection<BatchPermissionNodeDTO> DisplayTreeNodes
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

        public ObservableCollection<BatchPermissionNodeDTO> ModuleFilters
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

        public ObservableCollection<BatchPermissionNodeDTO> GroupFilters
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

        public BatchPermissionNodeDTO? SelectedModuleFilter
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

        public BatchPermissionNodeDTO? SelectedGroupFilter
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

        public string? SelectedPermissionType
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedPermissionType));
                    NotifyOfPropertyChange(nameof(HasPermissionTypeSelected));
                    NotifyOfPropertyChange(nameof(ApplicableValueOptions));
                    SelectedValue = null;
                    UnselectAllPermissions();
                    ApplyTreeFilter();
                }
            }
        }

        public bool HasPermissionTypeSelected => SelectedPermissionType is not null;

        public IReadOnlyList<UserPermissionValueOption> ApplicableValueOptions =>
            SelectedPermissionType == "ACTION" ? ActionValueOptions : FieldValueOptions;

        public ObservableCollection<BatchCollaboratorDTO> Collaborators { get; set; } = [];

        public bool IsAllCollaboratorsSelected
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsAllCollaboratorsSelected));
                    foreach (BatchCollaboratorDTO collab in Collaborators)
                        collab.IsSelected = value;
                }
            }
        }

        public bool IsDeleteMode
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsDeleteMode));
                    NotifyOfPropertyChange(nameof(IsAssignMode));
                    NotifyOfPropertyChange(nameof(CanApply));
                    if (value)
                    {
                        SelectedValue = null;
                        ExpiresAt = null;
                    }
                }
            }
        }

        public bool IsAssignMode
        {
            get => !IsDeleteMode;
            set => IsDeleteMode = !value;
        }

        public UserPermissionValue? SelectedValue
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedValue));
                    NotifyOfPropertyChange(nameof(CanApply));
                }
            }
        }

        public DateTime? ExpiresAt
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ExpiresAt));
                }
            }
        }

        public int SelectedPermissionCount => _allPermissionNodes.Count(n => n.IsSelected);
        public int SelectedCollaboratorCount => Collaborators.Count(c => c.IsSelected);

        public bool CanApply =>
            SelectedPermissionCount > 0 &&
            SelectedCollaboratorCount > 0 &&
            (IsDeleteMode || SelectedValue != null);

        public static IReadOnlyList<UserPermissionValueOption> ActionValueOptions { get; } =
        [
            new(UserPermissionValue.Allowed, "Permitido"),
            new(UserPermissionValue.Denied, "Denegado")
        ];

        public static IReadOnlyList<UserPermissionValueOption> FieldValueOptions { get; } =
        [
            new(UserPermissionValue.Required, "Requerido"),
            new(UserPermissionValue.Optional, "Opcional")
        ];

        public static IReadOnlyList<PermissionTypeOption> PermissionTypeOptions { get; } =
        [
            new("ACTION", "Acciones"),
            new("FIELD", "Campos obligatorios")
        ];

        #endregion

        #region Private State

        private List<BatchPermissionNodeDTO> _allPermissionNodes = [];

        #endregion

        #region Commands

        private ICommand? _applyCommand;
        public ICommand ApplyCommand
        {
            get
            {
                _applyCommand ??= new AsyncCommand(ApplyAsync);
                return _applyCommand;
            }
        }

        private ICommand? _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand ??= new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        #endregion

        #region Setup

        public void SetData(
            List<MenuModuleGraphQLModel> menuHierarchy,
            List<PermissionDefinitionGraphQLModel> permissionDefinitions,
            ReadOnlyObservableCollection<CollaboratorGraphQLModel> collaborators)
        {
            BuildTree(menuHierarchy, permissionDefinitions);
            ModuleFilters = [.. TreeNodes];
            ApplyTreeFilter();

            Collaborators = [.. collaborators
                .OrderBy(c => c.Account.FullName)
                .Select(c => new BatchCollaboratorDTO
                {
                    AccountId = c.Account.Id,
                    FullName = c.Account.FullName
                })];

            // Subscribe to selection changes for CanApply
            foreach (BatchPermissionNodeDTO node in _allPermissionNodes)
            {
                node.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(BatchPermissionNodeDTO.IsSelected))
                    {
                        NotifyOfPropertyChange(nameof(SelectedPermissionCount));
                        NotifyOfPropertyChange(nameof(CanApply));
                    }
                };
            }

            foreach (BatchCollaboratorDTO collab in Collaborators)
            {
                collab.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(BatchCollaboratorDTO.IsSelected))
                    {
                        NotifyOfPropertyChange(nameof(SelectedCollaboratorCount));
                        NotifyOfPropertyChange(nameof(CanApply));
                    }
                };
            }
        }

        #endregion

        #region Tree Building

        private void BuildTree(
            List<MenuModuleGraphQLModel> menuHierarchy,
            List<PermissionDefinitionGraphQLModel> permissionDefinitions)
        {
            TreeNodes.Clear();
            _allPermissionNodes.Clear();

            Dictionary<int, List<PermissionDefinitionGraphQLModel>> permsByMenuItem = permissionDefinitions
                .Where(pd => pd.MenuItem != null)
                .GroupBy(pd => pd.MenuItem!.Id)
                .ToDictionary(g => g.Key, g => g.OrderBy(pd => pd.DisplayOrder).ToList());

            foreach (MenuModuleGraphQLModel module in menuHierarchy.OrderBy(m => m.DisplayOrder))
            {
                BatchPermissionNodeDTO moduleNode = new()
                {
                    Id = module.Id,
                    Name = module.Name,
                    NodeType = UserPermissionTreeNodeType.Module
                };

                foreach (MenuItemGroupGraphQLModel group in module.MenuItemGroups.OrderBy(g => g.DisplayOrder))
                {
                    BatchPermissionNodeDTO groupNode = new()
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

                        BatchPermissionNodeDTO itemNode = new()
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
                            BatchPermissionNodeDTO actionGroupNode = new()
                            {
                                Name = "Acciones",
                                NodeType = UserPermissionTreeNodeType.PermissionTypeGroup,
                                Parent = itemNode
                            };
                            foreach (PermissionDefinitionGraphQLModel permDef in actionPerms)
                            {
                                BatchPermissionNodeDTO permNode = new()
                                {
                                    Id = permDef.Id,
                                    Name = permDef.Name,
                                    Code = permDef.Code,
                                    PermissionType = permDef.PermissionType,
                                    NodeType = UserPermissionTreeNodeType.Permission,
                                    Parent = actionGroupNode
                                };
                                actionGroupNode.Children.Add(permNode);
                                _allPermissionNodes.Add(permNode);
                            }
                            itemNode.Children.Add(actionGroupNode);
                        }

                        if (fieldPerms.Count > 0)
                        {
                            BatchPermissionNodeDTO fieldGroupNode = new()
                            {
                                Name = "Campos obligatorios",
                                NodeType = UserPermissionTreeNodeType.PermissionTypeGroup,
                                Parent = itemNode
                            };
                            foreach (PermissionDefinitionGraphQLModel permDef in fieldPerms)
                            {
                                BatchPermissionNodeDTO permNode = new()
                                {
                                    Id = permDef.Id,
                                    Name = permDef.Name,
                                    Code = permDef.Code,
                                    PermissionType = permDef.PermissionType,
                                    NodeType = UserPermissionTreeNodeType.Permission,
                                    Parent = fieldGroupNode
                                };
                                fieldGroupNode.Children.Add(permNode);
                                _allPermissionNodes.Add(permNode);
                            }
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
            if (SelectedPermissionType is null)
            {
                DisplayTreeNodes = [];
                return;
            }

            string typeGroupName = SelectedPermissionType == "ACTION" ? "Acciones" : "Campos obligatorios";

            ObservableCollection<BatchPermissionNodeDTO> sourceNodes = SelectedModuleFilter is not null
                ? [SelectedModuleFilter]
                : TreeNodes;

            if (SelectedGroupFilter is not null)
            {
                BatchPermissionNodeDTO moduleWrapper = new()
                {
                    Id = SelectedModuleFilter!.Id,
                    Name = SelectedModuleFilter.Name,
                    NodeType = UserPermissionTreeNodeType.Module
                };
                BatchPermissionNodeDTO groupWrapper = new()
                {
                    Id = SelectedGroupFilter.Id,
                    Name = SelectedGroupFilter.Name,
                    NodeType = UserPermissionTreeNodeType.Group,
                    Parent = moduleWrapper
                };
                foreach (BatchPermissionNodeDTO item in SelectedGroupFilter.Children)
                    groupWrapper.Children.Add(item);
                moduleWrapper.Children.Add(groupWrapper);
                sourceNodes = [moduleWrapper];
            }

            // Filter: only show MenuItems that have the selected PermissionTypeGroup
            ObservableCollection<BatchPermissionNodeDTO> filtered = [];
            foreach (BatchPermissionNodeDTO module in sourceNodes)
            {
                BatchPermissionNodeDTO moduleClone = new()
                {
                    Id = module.Id,
                    Name = module.Name,
                    NodeType = UserPermissionTreeNodeType.Module
                };

                foreach (BatchPermissionNodeDTO group in module.Children)
                {
                    BatchPermissionNodeDTO groupClone = new()
                    {
                        Id = group.Id,
                        Name = group.Name,
                        NodeType = UserPermissionTreeNodeType.Group,
                        Parent = moduleClone
                    };

                    foreach (BatchPermissionNodeDTO item in group.Children)
                    {
                        // Find the PermissionTypeGroup that matches the filter
                        BatchPermissionNodeDTO? typeGroup = item.Children
                            .FirstOrDefault(c => c.NodeType == UserPermissionTreeNodeType.PermissionTypeGroup && c.Name == typeGroupName);

                        if (typeGroup is null) continue;

                        // Create item wrapper with only the matching type group's permissions as direct children
                        BatchPermissionNodeDTO itemClone = new()
                        {
                            Id = item.Id,
                            Name = item.Name,
                            NodeType = UserPermissionTreeNodeType.Item,
                            Parent = groupClone
                        };

                        foreach (BatchPermissionNodeDTO perm in typeGroup.Children)
                            itemClone.Children.Add(perm);

                        groupClone.Children.Add(itemClone);
                    }

                    if (groupClone.Children.Count > 0)
                        moduleClone.Children.Add(groupClone);
                }

                if (moduleClone.Children.Count > 0)
                    filtered.Add(moduleClone);
            }

            DisplayTreeNodes = filtered;
        }

        private void UnselectAllPermissions()
        {
            foreach (BatchPermissionNodeDTO node in _allPermissionNodes)
                node.IsSelected = false;
            NotifyOfPropertyChange(nameof(SelectedPermissionCount));
            NotifyOfPropertyChange(nameof(CanApply));
        }

        #endregion

        #region Apply / Cancel

        public async Task ApplyAsync()
        {
            try
            {
                IsBusy = true;

                List<int> permissionIds = [.. _allPermissionNodes.Where(n => n.IsSelected).Select(n => n.Id)];
                List<int> accountIds = [.. Collaborators.Where(c => c.IsSelected).Select(c => c.AccountId)];

                if (IsDeleteMode)
                {
                    var (fragment, query) = _batchDeleteMutation.Value;

                    ExpandoObject variables = new GraphQLVariables()
                        .For(fragment, "input", new { accountIds, permissionDefinitionIds = permissionIds })
                        .Build();

                    BatchResultGraphQLModel result = await _userPermissionService.BatchAsync<BatchResultGraphQLModel>(query, variables);

                    if (!result.Success)
                    {
                        ThemedMessageBox.Show("Atención !", $"Error en la operación.\n\n{result.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    await TryCloseAsync(true);
                }
                else
                {
                    string valueStr = SelectedValue!.Value switch
                    {
                        UserPermissionValue.Allowed => "ALLOWED",
                        UserPermissionValue.Denied => "DENIED",
                        UserPermissionValue.Required => "REQUIRED",
                        UserPermissionValue.Optional => "OPTIONAL",
                        _ => "ALLOWED"
                    };

                    var (fragment, query) = _batchUpsertMutation.Value;

                    ExpandoObject variables = new GraphQLVariables()
                        .For(fragment, "input", new
                        {
                            accountIds,
                            permissionDefinitionIds = permissionIds,
                            value = valueStr,
                            expiresAt = ExpiresAt == null
                                ? null
                                : new DateTimeOffset(DateTime.SpecifyKind(ExpiresAt.Value, DateTimeKind.Utc)).ToString("O")
                        })
                        .Build();

                    BatchResultGraphQLModel result = await _userPermissionService.BatchAsync<BatchResultGraphQLModel>(query, variables);

                    if (!result.Success)
                    {
                        ThemedMessageBox.Show("Atención !", $"Error en la operación.\n\n{result.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    await TryCloseAsync(true);
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{nameof(ApplyAsync)} \r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _batchUpsertMutation = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<BatchResultGraphQLModel>
                .Create()
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Field(f => f.TotalAffected)
                .Field(f => f.AffectedIds)
                .SelectList(f => f.Errors, sq => sq
                    .Field(e => e.Message))
                .Build();

            GraphQLQueryFragment fragment = new("batchUpsertUserPermissions",
                [new("input", "BatchUpsertUserPermissionsInput!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _batchDeleteMutation = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<BatchResultGraphQLModel>
                .Create()
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Field(f => f.TotalAffected)
                .Field(f => f.AffectedIds)
                .SelectList(f => f.Errors, sq => sq
                    .Field(e => e.Message))
                .Build();

            GraphQLQueryFragment fragment = new("batchDeleteUserPermissions",
                [new("input", "BatchDeleteUserPermissionsInput!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
