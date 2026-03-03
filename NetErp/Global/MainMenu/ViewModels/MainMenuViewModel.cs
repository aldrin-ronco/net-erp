using Caliburn.Micro;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Global;
using NetErp.Global.MainMenu.Models;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Login.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Dynamic;
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
        private bool _isMenuLoaded;

        private int _selectedIndex;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    NotifyOfPropertyChange(nameof(SelectedIndex));
                }
            }
        }

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

        private ObservableCollection<MenuModuleDisplayModel> _menuModules = [];
        public ObservableCollection<MenuModuleDisplayModel> MenuModules
        {
            get => _menuModules;
            set
            {
                _menuModules = value;
                NotifyOfPropertyChange(nameof(MenuModules));
            }
        }

        public MainMenuViewModel(IRepository<MenuItemGraphQLModel> menuItemService)
        {
            _eventAggregator = IoC.Get<IEventAggregator>();
            _menuItemService = menuItemService;
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

        private string BuildMenuQuery()
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
                            .Field(i => i.IsActive)
                        )
                    )
                )
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");

            var fragment = new GraphQLQueryFragment(
                "menuModulesPage",
                [paginationParam],
                fields,
                "menuModules");

            return new NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder([fragment])
                .GetQuery(GraphQLOperations.QUERY);
        }

        private async Task LoadMenuAsync()
        {
            try
            {
                string query = BuildMenuQuery();

                dynamic variables = new ExpandoObject();
                variables.menuModulesPagination = new ExpandoObject();
                variables.menuModulesPagination.pageSize = -1;

                MenuDataContext result = await _menuItemService.GetDataContextAsync<MenuDataContext>(query, variables);
                MenuModules = BuildMenuStructure(result);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    ThemedMessageBox.Show(title: "Error al cargar menú", text: $"{ex.GetType().Name}: {ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                MenuModules = BuildFallbackMenu();
            }
        }

        private ObservableCollection<MenuModuleDisplayModel> BuildMenuStructure(MenuDataContext data)
        {
            var modules = new ObservableCollection<MenuModuleDisplayModel>();

            foreach (var module in data.MenuModules.Entries.OrderBy(m => m.DisplayOrder))
            {
                var displayModule = new MenuModuleDisplayModel
                {
                    Name = module.Name,
                    Icon = module.Icon
                };

                var groups = module.MenuItemGroups
                    .OrderBy(g => g.DisplayOrder)
                    .ToList();

                bool isFirstGroup = true;
                foreach (var group in groups)
                {
                    var items = group.MenuItems
                        .OrderBy(i => i.DisplayOrder)
                        .ToList();

                    if (items.Count == 0)
                        continue;

                    // Insert separator between groups (not before the first)
                    if (!isFirstGroup)
                    {
                        displayModule.Items.Add(new MenuItemDisplayModel { IsSeparator = true });
                    }

                    foreach (var item in items)
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
                    Application.Current.Dispatcher.Invoke(() =>
                        ThemedMessageBox.Show(title: "Atención!", text: $"No se encontró el módulo '{itemKey}'.", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Warning));
                    return;
                }

                var instance = (IScreen)IoC.GetInstance(vmType, null);
                instance.DisplayName = displayName;
                await ActivateItemAsync(instance, new CancellationToken());
                int newIndex = Items.IndexOf(instance);
                if (newIndex >= 0) SelectedIndex = newIndex;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    ThemedMessageBox.Show(title: "Atención!", text: ex.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void ReturnToCompanySelection()
        {
            try
            {
                Items.Clear();
                _eventAggregator.PublishOnUIThreadAsync(new ReturnToCompanySelectionMessage());
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atención !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
