using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Global.CompanyPermissionDefault.DTO;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.CompanyPermissionDefault.ViewModels
{
    public class CompanyPermissionDefaultViewModel : Screen
    {
        #region Dependencies

        private readonly IRepository<MenuModuleGraphQLModel> _menuModuleService;
        private readonly IRepository<PermissionDefinitionGraphQLModel> _permissionDefinitionService;
        private readonly IRepository<CompanyPermissionDefaultGraphQLModel> _companyPermDefaultService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region Constructor

        public CompanyPermissionDefaultViewModel(
            IRepository<MenuModuleGraphQLModel> menuModuleService,
            IRepository<PermissionDefinitionGraphQLModel> permissionDefinitionService,
            IRepository<CompanyPermissionDefaultGraphQLModel> companyPermDefaultService,
            Helpers.Services.INotificationService notificationService,
            JoinableTaskFactory joinableTaskFactory)
        {
            _menuModuleService = menuModuleService;
            _permissionDefinitionService = permissionDefinitionService;
            _companyPermDefaultService = companyPermDefaultService;
            _notificationService = notificationService;
            _joinableTaskFactory = joinableTaskFactory;
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

        public ObservableCollection<PermissionTreeNodeDTO> TreeNodes { get; set; } = [];

        public ObservableCollection<PermissionTreeNodeDTO> DisplayTreeNodes
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

        public ObservableCollection<PermissionTreeNodeDTO> ModuleFilters
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

        public ObservableCollection<PermissionTreeNodeDTO> GroupFilters
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

        public PermissionTreeNodeDTO? SelectedModuleFilter
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

        public PermissionTreeNodeDTO? SelectedGroupFilter
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

        public bool HasChanges
        {
            get
            {
                foreach (PermissionTreeNodeDTO node in _allPermissionNodes)
                {
                    if (node.HasChanged) return true;
                }
                return false;
            }
        }

        public bool CanSave => HasChanges;

        #endregion

        #region Private State

        private List<MenuModuleGraphQLModel> _fullMenuHierarchy = [];
        private List<PermissionDefinitionGraphQLModel> _allPermissionDefinitions = [];
        private List<CompanyPermissionDefaultGraphQLModel> _allCompanyDefaults = [];
        private List<PermissionTreeNodeDTO> _allPermissionNodes = [];

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

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                IsBusy = true;
                await LoadMenuHierarchyAsync();
                await LoadPermissionDefinitionsAsync();
                await LoadCompanyPermissionDefaultsAsync();
                BuildTree();
                ModuleFilters = [.. TreeNodes];
                ApplyTreeFilter();
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
            finally
            {
                IsBusy = false;
            }
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                TreeNodes.Clear();
                DisplayTreeNodes.Clear();
                _allPermissionNodes.Clear();
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

        #endregion

        #region Tree Building

        private void BuildTree()
        {
            TreeNodes.Clear();
            _allPermissionNodes.Clear();

            // Index permission definitions by menuItem.Id
            Dictionary<int, List<PermissionDefinitionGraphQLModel>> permsByMenuItem = _allPermissionDefinitions
                .Where(pd => pd.MenuItem != null)
                .GroupBy(pd => pd.MenuItem!.Id)
                .ToDictionary(g => g.Key, g => g.OrderBy(pd => pd.DisplayOrder).ToList());

            // Index company defaults by permissionDefinition.Id
            Dictionary<int, CompanyPermissionDefaultGraphQLModel> companyDefaultsByPermId = _allCompanyDefaults
                .Where(cd => cd.PermissionDefinition != null)
                .ToDictionary(cd => cd.PermissionDefinition!.Id);

            foreach (MenuModuleGraphQLModel module in _fullMenuHierarchy.OrderBy(m => m.DisplayOrder))
            {
                PermissionTreeNodeDTO moduleNode = new()
                {
                    Id = module.Id,
                    Name = module.Name,
                    NodeType = PermissionTreeNodeType.Module
                };

                foreach (MenuItemGroupGraphQLModel group in module.MenuItemGroups.OrderBy(g => g.DisplayOrder))
                {
                    PermissionTreeNodeDTO groupNode = new()
                    {
                        Id = group.Id,
                        Name = group.Name,
                        NodeType = PermissionTreeNodeType.Group,
                        Parent = moduleNode
                    };

                    foreach (MenuItemGraphQLModel item in group.MenuItems.OrderBy(i => i.DisplayOrder))
                    {
                        if (!item.IsActive) continue;
                        if (!permsByMenuItem.TryGetValue(item.Id, out List<PermissionDefinitionGraphQLModel>? permsForItem)) continue;

                        PermissionTreeNodeDTO itemNode = new()
                        {
                            Id = item.Id,
                            Name = item.Name,
                            NodeType = PermissionTreeNodeType.Item,
                            Parent = groupNode
                        };

                        // Group permissions by type (ACTION / FIELD)
                        var actionPerms = permsForItem.Where(p => p.PermissionType == "ACTION").ToList();
                        var fieldPerms = permsForItem.Where(p => p.PermissionType == "FIELD").ToList();

                        if (actionPerms.Count > 0)
                        {
                            PermissionTreeNodeDTO actionGroupNode = new()
                            {
                                Name = "Acciones",
                                NodeType = PermissionTreeNodeType.PermissionTypeGroup,
                                Parent = itemNode
                            };
                            AddPermissionNodes(actionGroupNode, actionPerms, companyDefaultsByPermId);
                            itemNode.Children.Add(actionGroupNode);
                        }

                        if (fieldPerms.Count > 0)
                        {
                            PermissionTreeNodeDTO fieldGroupNode = new()
                            {
                                Name = "Campos obligatorios",
                                NodeType = PermissionTreeNodeType.PermissionTypeGroup,
                                Parent = itemNode
                            };
                            AddPermissionNodes(fieldGroupNode, fieldPerms, companyDefaultsByPermId);
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
            PermissionTreeNodeDTO parentNode,
            List<PermissionDefinitionGraphQLModel> perms,
            Dictionary<int, CompanyPermissionDefaultGraphQLModel> companyDefaultsByPermId)
        {
            foreach (PermissionDefinitionGraphQLModel permDef in perms)
            {
                companyDefaultsByPermId.TryGetValue(permDef.Id, out CompanyPermissionDefaultGraphQLModel? compDefault);

                PermissionDefaultValue? companyValue = compDefault?.DefaultValue switch
                {
                    "ALLOWED" => PermissionDefaultValue.Allowed,
                    "DENIED" => PermissionDefaultValue.Denied,
                    "REQUIRED" => PermissionDefaultValue.Required,
                    "OPTIONAL" => PermissionDefaultValue.Optional,
                    _ => null
                };

                PermissionTreeNodeDTO permNode = new()
                {
                    Id = permDef.Id,
                    Name = permDef.Name,
                    Code = permDef.Code,
                    PermissionType = permDef.PermissionType,
                    SystemDefault = permDef.SystemDefault,
                    CompanyPermissionDefaultId = compDefault?.Id,
                    CompanyDefaultValue = companyValue,
                    OriginalCompanyDefaultValue = companyValue,
                    NodeType = PermissionTreeNodeType.Permission,
                    Parent = parentNode
                };

                permNode.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(PermissionTreeNodeDTO.HasChanged))
                    {
                        NotifyOfPropertyChange(nameof(HasChanges));
                        NotifyOfPropertyChange(nameof(CanSave));
                    }
                };

                parentNode.Children.Add(permNode);
                _allPermissionNodes.Add(permNode);
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
            if (SelectedModuleFilter is null)
            {
                DisplayTreeNodes = TreeNodes;
                return;
            }

            if (SelectedGroupFilter is not null)
            {
                PermissionTreeNodeDTO moduleWrapper = new()
                {
                    Id = SelectedModuleFilter.Id,
                    Name = SelectedModuleFilter.Name,
                    NodeType = PermissionTreeNodeType.Module
                };
                PermissionTreeNodeDTO groupWrapper = new()
                {
                    Id = SelectedGroupFilter.Id,
                    Name = SelectedGroupFilter.Name,
                    NodeType = PermissionTreeNodeType.Group,
                    Parent = moduleWrapper
                };
                foreach (PermissionTreeNodeDTO item in SelectedGroupFilter.Children)
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

                var (_, createQuery) = _createMutation.Value;
                var (_, updateQuery) = _updateMutation.Value;
                var (_, deleteQuery) = _deleteMutation.Value;

                foreach (PermissionTreeNodeDTO node in _allPermissionNodes)
                {
                    if (!node.HasChanged) continue;

                    if (node.OriginalCompanyDefaultValue == null && node.CompanyDefaultValue != null)
                    {
                        // CREATE: was "not set", now has a value
                        string valueStr = ToGraphQLValue(node.CompanyDefaultValue.Value);
                        object variables = new { createResponseInput = new { permissionDefinitionId = node.Id, defaultValue = valueStr } };
                        UpsertResponseType<CompanyPermissionDefaultGraphQLModel> result =
                            await _companyPermDefaultService.CreateAsync<UpsertResponseType<CompanyPermissionDefaultGraphQLModel>>(createQuery, variables);
                        node.CompanyPermissionDefaultId = result.Entity.Id;
                    }
                    else if (node.OriginalCompanyDefaultValue != null && node.CompanyDefaultValue != null)
                    {
                        // UPDATE: changed value
                        string valueStr = ToGraphQLValue(node.CompanyDefaultValue.Value);
                        object variables = new { updateResponseId = node.CompanyPermissionDefaultId, updateResponseData = new { defaultValue = valueStr } };
                        await _companyPermDefaultService.UpdateAsync<UpsertResponseType<CompanyPermissionDefaultGraphQLModel>>(updateQuery, variables);
                    }
                    else if (node.OriginalCompanyDefaultValue != null && node.CompanyDefaultValue == null)
                    {
                        // DELETE: was set, now "not set"
                        object variables = new { deleteResponseId = node.CompanyPermissionDefaultId };
                        await _companyPermDefaultService.DeleteAsync<DeleteResponseType>(deleteQuery, variables);
                        node.CompanyPermissionDefaultId = null;
                    }

                    node.OriginalCompanyDefaultValue = node.CompanyDefaultValue;
                }

                _notificationService.ShowSuccess("Valores predeterminados actualizados correctamente");
                NotifyOfPropertyChange(nameof(HasChanges));
                NotifyOfPropertyChange(nameof(CanSave));
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
            foreach (PermissionTreeNodeDTO node in _allPermissionNodes)
                node.CompanyDefaultValue = node.OriginalCompanyDefaultValue;
        }

        private static string ToGraphQLValue(PermissionDefaultValue value) => value switch
        {
            PermissionDefaultValue.Allowed => "ALLOWED",
            PermissionDefaultValue.Denied => "DENIED",
            PermissionDefaultValue.Required => "REQUIRED",
            PermissionDefaultValue.Optional => "OPTIONAL",
            _ => "ALLOWED"
        };

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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createMutation = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<UpsertResponseType<CompanyPermissionDefaultGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "companyPermissionDefault", nested: sq => sq
                    .Field(e => e.Id))
                .Field(f => f.Success)
                .Build();

            GraphQLQueryFragment fragment = new("createCompanyPermissionDefault",
                [new("input", "CreateCompanyPermissionDefaultInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateMutation = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<UpsertResponseType<CompanyPermissionDefaultGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "companyPermissionDefault", nested: sq => sq
                    .Field(e => e.Id))
                .Field(f => f.Success)
                .Build();

            GraphQLQueryFragment fragment = new("updateCompanyPermissionDefault",
                [new("id", "ID!"), new("data", "UpdateCompanyPermissionDefaultInput!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteMutation = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.Success)
                .Build();

            GraphQLQueryFragment fragment = new("deleteCompanyPermissionDefault",
                [new("id", "ID!")],
                fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
