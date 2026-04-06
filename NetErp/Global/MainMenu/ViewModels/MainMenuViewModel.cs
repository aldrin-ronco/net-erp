using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Global.MainMenu.Models;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Login.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.MainMenu.ViewModels
{
    public class MainMenuViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IRepository<MenuItemGraphQLModel> _menuItemService;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private bool _isMenuLoaded;

        public int SelectedIndex
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedIndex));
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

        public ObservableCollection<MenuModuleDisplayModel> MenuModules
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(MenuModules));
                }
            }
        } = [];

        public MainMenuViewModel(
            IEventAggregator eventAggregator,
            IRepository<MenuItemGraphQLModel> menuItemService,
            JoinableTaskFactory joinableTaskFactory)
        {
            _eventAggregator = eventAggregator;
            _menuItemService = menuItemService;
            _joinableTaskFactory = joinableTaskFactory;
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            if (!_isMenuLoaded)
            {
                await LoadMenuAsync();
                _isMenuLoaded = true;
            }
            await base.OnActivateAsync(cancellationToken);
        }

        private async Task LoadMenuAsync()
        {
            try
            {
                (GraphQLQueryFragment fragment, string query) = _loadMenuQuery.Value;

                dynamic variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = -1 })
                    .Build();

                MenuDataContext result = await _menuItemService.GetDataContextAsync<MenuDataContext>(query, variables);
                MenuModules = BuildMenuStructure(result);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Error al cargar menú",
                    text: $"{GetType().Name}.{nameof(LoadMenuAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
                MenuModules = BuildFallbackMenu();
            }
        }

        private ObservableCollection<MenuModuleDisplayModel> BuildMenuStructure(MenuDataContext data)
        {
            ObservableCollection<MenuModuleDisplayModel> modules = [];

            foreach (MenuModuleGraphQLModel module in data.MenuModules.Entries.OrderBy(m => m.DisplayOrder))
            {
                MenuModuleDisplayModel displayModule = new()
                {
                    Name = module.Name,
                    Icon = module.Icon
                };

                List<MenuItemGroupGraphQLModel> groups = module.MenuItemGroups
                    .OrderBy(g => g.DisplayOrder)
                    .ToList();

                bool isFirstGroup = true;
                foreach (MenuItemGroupGraphQLModel group in groups)
                {
                    List<MenuItemGraphQLModel> items = group.MenuItems
                        .OrderBy(i => i.DisplayOrder)
                        .ToList();

                    if (items.Count == 0)
                        continue;

                    // Insert separator between groups (not before the first)
                    if (!isFirstGroup)
                    {
                        displayModule.Items.Add(new MenuItemDisplayModel { IsSeparator = true });
                    }

                    foreach (MenuItemGraphQLModel item in items)
                    {
                        displayModule.Items.Add(new MenuItemDisplayModel
                        {
                            Name = item.Name,
                            ItemKey = item.ItemKey,
                            Icon = item.Icon,
                            IsSeparator = false,
                            Command = new AsyncCommand(() => OpenMenuItemAsync(item.ItemKey, item.Name))
                        });
                    }

                    isFirstGroup = false;
                }

                // Only add modules that have items
                if (displayModule.Items.Count > 0)
                    modules.Add(displayModule);
            }

            // Add "Sistema" module programmatically
            modules.Add(BuildSystemModule());

            return modules;
        }

        private ObservableCollection<MenuModuleDisplayModel> BuildFallbackMenu()
        {
            return [BuildSystemModule()];
        }

        private MenuModuleDisplayModel BuildSystemModule()
        {
            return new MenuModuleDisplayModel
            {
                Name = "Sistema",
                Icon = string.Empty,
                Items =
                [
                    new MenuItemDisplayModel
                    {
                        Name = "Volver a la selección de empresa",
                        ItemKey = "ReturnToCompanySelection",
                        IsSeparator = false,
                        Command = new DelegateCommand(ReturnToCompanySelection)
                    }
                ]
            };
        }

        private async Task OpenMenuItemAsync(string itemKey, string displayName)
        {
            try
            {
                IsBusy = true;
                await Task.Yield();

                Type? vmType = typeof(MainMenuViewModel).Assembly.GetTypes()
                    .FirstOrDefault(t => t.IsClass && t.Name == itemKey);

                if (vmType == null)
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"No se encontró el módulo '{itemKey}'.",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Warning);
                    return;
                }

                IScreen instance = (IScreen)IoC.GetInstance(vmType, null);
                instance.DisplayName = displayName;
                await ActivateItemAsync(instance, new CancellationToken());
                int newIndex = Items.IndexOf(instance);
                if (newIndex >= 0) SelectedIndex = newIndex;
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(OpenMenuItemAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async void ReturnToCompanySelection()
        {
            try
            {
                Items.Clear();
                await _eventAggregator.PublishOnCurrentThreadAsync(new ReturnToCompanySelectionMessage(), CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(ReturnToCompanySelection)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
        }

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadMenuQuery = new(() =>
        {
            var fields = FieldSpec<PageType<MenuModuleGraphQLModel>>
                .Create()
                .SelectList(selector: p => p.Entries, nested: module => module
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Icon)
                    .Field(f => f.DisplayOrder)
                    .Field(f => f.IsActive)
                    .SelectList(f => f.MenuItemGroups, group => group
                        .Field(g => g.Id)
                        .Field(g => g.Name)
                        .Field(g => g.DisplayOrder)
                        .Field(g => g.IsActive)
                        .SelectList(g => g.MenuItems, item => item
                            .Field(i => i.Id)
                            .Field(i => i.ItemKey)
                            .Field(i => i.Name)
                            .Field(i => i.Icon)
                            .Field(i => i.DisplayOrder)
                            .Field(i => i.IsLockable)
                            .Field(i => i.IsActive))))
                .Build();

            var fragment = new GraphQLQueryFragment(
                "menuModulesPage",
                [new("pagination", "Pagination")],
                fields,
                "menuModules");

            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
    }
}
