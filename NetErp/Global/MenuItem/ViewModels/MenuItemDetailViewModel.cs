using Caliburn.Micro;
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.MenuItem.ViewModels
{
    public class MenuItemDetailViewModel(
        IRepository<MenuItemGraphQLModel> menuItemService,
        IRepository<MenuItemGroupGraphQLModel> menuItemGroupService,
        IEventAggregator eventAggregator,
        MenuModuleCache menuModuleCache,
        StringLengthCache stringLengthCache,
        JoinableTaskFactory joinableTaskFactory) : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<MenuItemGraphQLModel> _menuItemService = menuItemService ?? throw new ArgumentNullException(nameof(menuItemService));
        private readonly IRepository<MenuItemGroupGraphQLModel> _menuItemGroupService = menuItemGroupService ?? throw new ArgumentNullException(nameof(menuItemGroupService));
        private readonly IEventAggregator _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        private readonly MenuModuleCache _menuModuleCache = menuModuleCache ?? throw new ArgumentNullException(nameof(menuModuleCache));
        private readonly StringLengthCache _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
        private readonly JoinableTaskFactory _joinableTaskFactory = joinableTaskFactory;

        #endregion

        #region Dialog Size

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
        } = 500;

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
        } = 450;

        #endregion

        #region MaxLength Properties

        public int ItemKeyMaxLength => _stringLengthCache.GetMaxLength<MenuItemGraphQLModel>(nameof(MenuItemGraphQLModel.ItemKey));
        public int NameMaxLength => _stringLengthCache.GetMaxLength<MenuItemGraphQLModel>(nameof(MenuItemGraphQLModel.Name));
        public int IconMaxLength => _stringLengthCache.GetMaxLength<MenuItemGraphQLModel>(nameof(MenuItemGraphQLModel.Icon));

        #endregion

        #region State

        public bool IsNewRecord => Id == 0;

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

        #endregion

        #region ComboBox Sources

        public ObservableCollection<MenuModuleGraphQLModel> Modules
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Modules));
                }
            }
        } = [];

        private List<MenuItemGroupGraphQLModel> _allGroups = [];

        public ObservableCollection<MenuItemGroupGraphQLModel> Groups
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Groups));
                }
            }
        } = [];

        #endregion

        #region Form Properties

        public int Id
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        public int? SelectedModuleId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedModuleId));
                    UpdateGroupsForSelectedModule();
                    if (SelectedGroupId.HasValue)
                    {
                        bool groupBelongsToModule = Groups.Any(g => g.Id == SelectedGroupId.Value);
                        if (!groupBelongsToModule) SelectedGroupId = null;
                    }
                }
            }
        }

        [ExpandoPath("menuItemGroupId")]
        public int? SelectedGroupId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedGroupId));
                    this.TrackChange(nameof(SelectedGroupId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public string ItemKey
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ItemKey));
                    ValidateProperty(nameof(ItemKey), value);
                    this.TrackChange(nameof(ItemKey));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Name));
                    ValidateProperty(nameof(Name), value);
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Icon
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Icon));
                    this.TrackChange(nameof(Icon));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public bool IsLockable
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsLockable));
                    this.TrackChange(nameof(IsLockable));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public new bool IsActive
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.TrackChange(nameof(IsActive));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region Button States

        public bool CanSave => !HasErrors && this.HasChanges()
                               && !string.IsNullOrEmpty(ItemKey)
                               && !string.IsNullOrEmpty(Name)
                               && SelectedGroupId is > 0;

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

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Initialize

        public async Task InitializeAsync()
        {
            await _menuModuleCache.EnsureLoadedAsync();
            Modules = new ObservableCollection<MenuModuleGraphQLModel>(_menuModuleCache.Items);

            (GraphQLQueryFragment fragment, string query) = _loadGroupsQuery.Value;
            dynamic variables = new GraphQLVariables()
                .For(fragment, "pagination", new { Page = 1, PageSize = -1 })
                .Build();

            PageType<MenuItemGroupGraphQLModel> result = await _menuItemGroupService.GetPageAsync(query, variables);
            _allGroups = [.. result.Entries.OrderBy(g => g.DisplayOrder)];
        }

        private void UpdateGroupsForSelectedModule()
        {
            if (SelectedModuleId == null)
            {
                Groups = new ObservableCollection<MenuItemGroupGraphQLModel>(_allGroups);
                return;
            }

            List<MenuItemGroupGraphQLModel> filtered = [.. _allGroups.Where(g => g.MenuModule?.Id == SelectedModuleId.Value)];

            Groups = new ObservableCollection<MenuItemGroupGraphQLModel>(filtered);
        }

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            Id = 0;
            ItemKey = string.Empty;
            Name = string.Empty;
            Icon = string.Empty;
            IsLockable = false;
            IsActive = true;
            SelectedModuleId = null;
            SelectedGroupId = null;
            Groups = new ObservableCollection<MenuItemGroupGraphQLModel>(_allGroups);
            SeedDefaultValues();
        }

        public void SetForEdit(MenuItemGraphQLModel entity)
        {
            Id = entity.Id;
            ItemKey = entity.ItemKey;
            Name = entity.Name;
            Icon = entity.Icon;
            IsLockable = entity.IsLockable;
            IsActive = entity.IsActive;

            if (entity.MenuItemGroup?.MenuModule != null)
            {
                SelectedModuleId = entity.MenuItemGroup.MenuModule.Id;
            }
            SelectedGroupId = entity.MenuItemGroup?.Id;

            SeedCurrentValues();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(IsLockable), IsLockable);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(ItemKey), ItemKey);
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(Icon), Icon);
            this.SeedValue(nameof(IsLockable), IsLockable);
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(SelectedGroupId), SelectedGroupId);
            this.AcceptChanges();
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<MenuItemGraphQLModel> result = await ExecuteSaveAsync();

                if (!result.Success)
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new MenuItemCreateMessage { CreatedMenuItem = result }
                        : new MenuItemUpdateMessage { UpdatedMenuItem = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"Error al realizar operación.\r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(SaveAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<MenuItemGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    (GraphQLQueryFragment _, string query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _menuItemService.CreateAsync<UpsertResponseType<MenuItemGraphQLModel>>(query, variables);
                }
                else
                {
                    (GraphQLQueryFragment _, string query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _menuItemService.UpdateAsync<UpsertResponseType<MenuItemGraphQLModel>>(query, variables);
                }
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadGroupsQuery = new(() =>
        {
            var fields = FieldSpec<PageType<MenuItemGroupGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.DisplayOrder)
                    .Select(e => e.MenuModule, m => m
                        .Field(m => m!.Id)
                        .Field(m => m!.Name)))
                .Build();

            var fragment = new GraphQLQueryFragment("menuItemGroupsPage",
                [new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<MenuItemGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "menuItem", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createMenuItem",
                [new("input", "CreateMenuItemInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<MenuItemGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "menuItem", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateMenuItem",
                [new("data", "UpdateMenuItemInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Validation (INotifyDataErrorInfo)

        private readonly Dictionary<string, List<string>> _errors = [];

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? value)) return Enumerable.Empty<string>();
            return value;
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = [];

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty;
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(ItemKey):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "La clave del ítem no puede estar vacía");
                    break;
                case nameof(Name):
                    if (string.IsNullOrEmpty(value)) AddError(propertyName, "El nombre no puede estar vacío");
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(ItemKey), ItemKey);
            ValidateProperty(nameof(Name), Name);
        }

        #endregion
    }
}
