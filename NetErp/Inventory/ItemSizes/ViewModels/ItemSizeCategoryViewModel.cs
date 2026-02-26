using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Inventory.ItemSizes.DTO;
using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Extensions.Global;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.ItemSizes.ViewModels
{
    public class ItemSizeCategoryViewModel : Screen,
        IHandle<ItemSizeValueDeleteMessage>,
        IHandle<ItemSizeValueUpdateMessage>,
        IHandle<ItemSizeValueCreateMessage>,
        IHandle<ItemSizeCategoryUpdateMessage>,
        IHandle<ItemSizeCategoryCreateMessage>,
        IHandle<ItemSizeCategoryDeleteMessage>
    {
        #region Fields

        private readonly IRepository<ItemSizeCategoryGraphQLModel> _itemSizeCategoryService;
        private readonly IRepository<ItemSizeValueGraphQLModel> _itemSizeValueService;
        private readonly Helpers.Services.INotificationService _notificationService;

        #endregion

        #region Properties

        private ItemSizeViewModel _context;
        public ItemSizeViewModel Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        private ObservableCollection<ItemSizeCategoryDTO> _itemSizeCategories = [];
        public ObservableCollection<ItemSizeCategoryDTO> ItemSizeCategories
        {
            get { return _itemSizeCategories; }
            set
            {
                if (_itemSizeCategories != value)
                {
                    _itemSizeCategories = value;
                    NotifyOfPropertyChange(nameof(ItemSizeCategories));
                }
            }
        }

        private ItemSizeType? _selectedItem;
        public ItemSizeType? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(DetailPanelVisibility));
                    HandleSelectedItemChanged();
                }
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        // State properties

        private bool _isEditing;
        public bool IsEditing
        {
            get { return _isEditing; }
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    NotifyOfPropertyChange(nameof(IsEditing));
                    NotifyOfPropertyChange(nameof(TreeViewIsEnable));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _isNewRecord;
        public bool IsNewRecord
        {
            get { return _isNewRecord; }
            set
            {
                if (_isNewRecord != value)
                {
                    _isNewRecord = value;
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        private bool _canEdit;
        public bool CanEdit
        {
            get { return _canEdit; }
            set
            {
                if (_canEdit != value)
                {
                    _canEdit = value;
                    NotifyOfPropertyChange(nameof(CanEdit));
                }
            }
        }

        private bool _canUndo;
        public bool CanUndo
        {
            get { return _canUndo; }
            set
            {
                if (_canUndo != value)
                {
                    _canUndo = value;
                    NotifyOfPropertyChange(nameof(CanUndo));
                }
            }
        }

        public bool CanSave => IsEditing && !string.IsNullOrWhiteSpace(EditingName) && this.HasChanges();

        public bool TreeViewIsEnable => !IsEditing;

        public bool DetailPanelVisibility => SelectedItem != null || IsNewRecord;

        private string _breadcrumbCategoryName = string.Empty;
        public string BreadcrumbCategoryName
        {
            get { return _breadcrumbCategoryName; }
            set
            {
                if (_breadcrumbCategoryName != value)
                {
                    _breadcrumbCategoryName = value;
                    NotifyOfPropertyChange(nameof(BreadcrumbCategoryName));
                }
            }
        }

        private string _breadcrumbValueName = string.Empty;
        public string BreadcrumbValueName
        {
            get { return _breadcrumbValueName; }
            set
            {
                if (_breadcrumbValueName != value)
                {
                    _breadcrumbValueName = value;
                    NotifyOfPropertyChange(nameof(BreadcrumbValueName));
                }
            }
        }

        private bool _breadcrumbHasValue;
        public bool BreadcrumbHasValue
        {
            get { return _breadcrumbHasValue; }
            set
            {
                if (_breadcrumbHasValue != value)
                {
                    _breadcrumbHasValue = value;
                    NotifyOfPropertyChange(nameof(BreadcrumbHasValue));
                }
            }
        }

        // Editing fields

        private string _editingName = string.Empty;
        public string EditingName
        {
            get { return _editingName; }
            set
            {
                if (_editingName != value)
                {
                    _editingName = value;
                    this.TrackChange(nameof(EditingName));
                    NotifyOfPropertyChange(nameof(EditingName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _editingDisplayOrder;
        public int EditingDisplayOrder
        {
            get { return _editingDisplayOrder; }
            set
            {
                if (_editingDisplayOrder != value)
                {
                    _editingDisplayOrder = value;
                    this.TrackChange(nameof(EditingDisplayOrder));
                    NotifyOfPropertyChange(nameof(EditingDisplayOrder));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _isValueSelected;
        public bool IsValueSelected
        {
            get { return _isValueSelected; }
            set
            {
                if (_isValueSelected != value)
                {
                    _isValueSelected = value;
                    NotifyOfPropertyChange(nameof(IsValueSelected));
                }
            }
        }

        private int _selectedCategoryIdForNewValue;
        public int SelectedCategoryIdForNewValue
        {
            get { return _selectedCategoryIdForNewValue; }
            set
            {
                if (_selectedCategoryIdForNewValue != value)
                {
                    _selectedCategoryIdForNewValue = value;
                    NotifyOfPropertyChange(nameof(SelectedCategoryIdForNewValue));
                }
            }
        }

        #endregion

        #region Commands

        // Create Category
        private ICommand _createCategoryCommand;
        public ICommand CreateCategoryCommand
        {
            get
            {
                if (_createCategoryCommand is null) _createCategoryCommand = new DelegateCommand(SetForNewCategory);
                return _createCategoryCommand;
            }
        }

        // Create Value
        private ICommand _createValueCommand;
        public ICommand CreateValueCommand
        {
            get
            {
                if (_createValueCommand is null) _createValueCommand = new DelegateCommand(SetForNewValue);
                return _createValueCommand;
            }
        }

        // Edit
        private ICommand _editCommand;
        public ICommand EditCommand
        {
            get
            {
                if (_editCommand is null) _editCommand = new DelegateCommand(Edit);
                return _editCommand;
            }
        }

        // Save
        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        // Undo
        private ICommand _undoCommand;
        public ICommand UndoCommand
        {
            get
            {
                if (_undoCommand is null) _undoCommand = new DelegateCommand(Undo);
                return _undoCommand;
            }
        }

        // Delete Category
        private ICommand _deleteItemSizeCategoryCommand;
        public ICommand DeleteItemSizeCategoryCommand
        {
            get
            {
                if (_deleteItemSizeCategoryCommand is null) _deleteItemSizeCategoryCommand = new AsyncCommand(DeleteItemSizeCategoryAsync);
                return _deleteItemSizeCategoryCommand;
            }
        }

        // Delete Value
        private ICommand _deleteItemSizeValueCommand;
        public ICommand DeleteItemSizeValueCommand
        {
            get
            {
                if (_deleteItemSizeValueCommand is null) _deleteItemSizeValueCommand = new AsyncCommand(DeleteItemSizeValueAsync);
                return _deleteItemSizeValueCommand;
            }
        }

        #endregion

        #region Command Methods

        public void SetForNewCategory()
        {
            IsNewRecord = true;
            IsEditing = true;
            IsValueSelected = false;
            CanEdit = false;
            CanUndo = true;

            EditingName = string.Empty;
            EditingDisplayOrder = 0;
            BreadcrumbCategoryName = "Nuevo grupo";
            BreadcrumbValueName = string.Empty;
            BreadcrumbHasValue = false;

            this.SeedValue(nameof(EditingName), string.Empty);
            this.SeedValue(nameof(EditingDisplayOrder), 0);
            this.AcceptChanges();

            NotifyOfPropertyChange(nameof(DetailPanelVisibility));
            NotifyOfPropertyChange(nameof(CanSave));
            this.SetFocus(nameof(EditingName));
        }

        public void SetForNewValue()
        {
            if (SelectedItem is ItemSizeCategoryDTO category)
            {
                SelectedCategoryIdForNewValue = category.Id;
            }
            else if (SelectedItem is ItemSizeValueDTO value)
            {
                SelectedCategoryIdForNewValue = value.ItemSizeCategoryId;
            }
            else
            {
                return;
            }

            IsNewRecord = true;
            IsEditing = true;
            IsValueSelected = true;
            CanEdit = false;
            CanUndo = true;

            EditingName = string.Empty;
            EditingDisplayOrder = 0;
            SetNewBreadcrumb();

            this.SeedValue(nameof(EditingName), string.Empty);
            this.SeedValue(nameof(EditingDisplayOrder), 0);
            this.AcceptChanges();

            NotifyOfPropertyChange(nameof(DetailPanelVisibility));
            NotifyOfPropertyChange(nameof(CanSave));
            this.SetFocus(nameof(EditingName));
        }

        public void Edit()
        {
            if (SelectedItem == null) return;

            CanEdit = false;
            CanUndo = true;
            IsEditing = true;

            if (SelectedItem is ItemSizeCategoryDTO category)
            {
                EditingName = category.Name;
                IsValueSelected = false;

                this.SeedValue(nameof(EditingName), category.Name);
                this.AcceptChanges();
            }
            else if (SelectedItem is ItemSizeValueDTO value)
            {
                EditingName = value.Name;
                EditingDisplayOrder = value.DisplayOrder;
                IsValueSelected = true;

                this.SeedValue(nameof(EditingName), value.Name);
                this.SeedValue(nameof(EditingDisplayOrder), value.DisplayOrder);
                this.AcceptChanges();
            }

            this.SetFocus(nameof(EditingName));
        }

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;

                if (IsNewRecord)
                {
                    if (IsValueSelected)
                    {
                        var result = await CreateItemSizeValueAsync();
                        if (!result.Success)
                        {
                            ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                            return;
                        }
                        await Context.EventAggregator.PublishOnCurrentThreadAsync(new ItemSizeValueCreateMessage() { CreatedItemSizeValue = result });
                    }
                    else
                    {
                        var result = await CreateItemSizeCategoryAsync();
                        if (!result.Success)
                        {
                            ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                            return;
                        }
                        await Context.EventAggregator.PublishOnCurrentThreadAsync(new ItemSizeCategoryCreateMessage() { CreatedItemSizeCategory = result });
                    }
                }
                else
                {
                    if (SelectedItem is ItemSizeValueDTO)
                    {
                        var result = await UpdateItemSizeValueAsync();
                        if (!result.Success)
                        {
                            ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                            return;
                        }
                        await Context.EventAggregator.PublishOnCurrentThreadAsync(new ItemSizeValueUpdateMessage() { UpdatedItemSizeValue = result });
                    }
                    else if (SelectedItem is ItemSizeCategoryDTO)
                    {
                        var result = await UpdateItemSizeCategoryAsync();
                        if (!result.Success)
                        {
                            ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                            return;
                        }
                        await Context.EventAggregator.PublishOnCurrentThreadAsync(new ItemSizeCategoryUpdateMessage() { UpdatedItemSizeCategory = result });
                    }
                }

                ResetEditingState();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{graphQLError.Errors[0].Extensions.Message} {graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "SaveAsync" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void Undo()
        {
            if (IsNewRecord)
            {
                IsNewRecord = false;
                IsEditing = false;
                CanUndo = false;
                CanEdit = SelectedItem != null;

                EditingName = string.Empty;
                EditingDisplayOrder = 0;
                IsValueSelected = SelectedItem is ItemSizeValueDTO;
                this.AcceptChanges();

                // Repopulate form from current selection if any
                UpdateBreadcrumb();
                if (SelectedItem is ItemSizeCategoryDTO category)
                {
                    EditingName = category.Name;
                    this.SeedValue(nameof(EditingName), category.Name);
                    this.AcceptChanges();
                }
                else if (SelectedItem is ItemSizeValueDTO value)
                {
                    EditingName = value.Name;
                    EditingDisplayOrder = value.DisplayOrder;
                    this.SeedValue(nameof(EditingName), value.Name);
                    this.SeedValue(nameof(EditingDisplayOrder), value.DisplayOrder);
                    this.AcceptChanges();
                }

                NotifyOfPropertyChange(nameof(DetailPanelVisibility));
                NotifyOfPropertyChange(nameof(CanSave));
            }
            else
            {
                // Restore original values from current selection
                if (SelectedItem is ItemSizeCategoryDTO category)
                {
                    EditingName = category.Name;
                    this.SeedValue(nameof(EditingName), category.Name);
                }
                else if (SelectedItem is ItemSizeValueDTO value)
                {
                    EditingName = value.Name;
                    EditingDisplayOrder = value.DisplayOrder;
                    this.SeedValue(nameof(EditingName), value.Name);
                    this.SeedValue(nameof(EditingDisplayOrder), value.DisplayOrder);
                }

                this.AcceptChanges();
                IsEditing = false;
                CanUndo = false;
                CanEdit = true;

                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private void HandleSelectedItemChanged()
        {
            if (IsEditing || IsNewRecord) return;

            UpdateBreadcrumb();

            if (SelectedItem is ItemSizeCategoryDTO category)
            {
                EditingName = category.Name;
                IsValueSelected = false;
                CanEdit = true;
                CanUndo = false;

                this.SeedValue(nameof(EditingName), category.Name);
                this.AcceptChanges();
            }
            else if (SelectedItem is ItemSizeValueDTO value)
            {
                EditingName = value.Name;
                EditingDisplayOrder = value.DisplayOrder;
                IsValueSelected = true;
                CanEdit = true;
                CanUndo = false;

                this.SeedValue(nameof(EditingName), value.Name);
                this.SeedValue(nameof(EditingDisplayOrder), value.DisplayOrder);
                this.AcceptChanges();
            }
            else
            {
                CanEdit = false;
                CanUndo = false;
            }

            NotifyOfPropertyChange(nameof(CanSave));
        }

        private void ResetEditingState()
        {
            IsNewRecord = false;
            IsEditing = false;
            CanUndo = false;
            CanEdit = SelectedItem != null;
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        private void UpdateBreadcrumb()
        {
            if (SelectedItem is ItemSizeCategoryDTO category)
            {
                BreadcrumbCategoryName = category.Name ?? string.Empty;
                BreadcrumbValueName = string.Empty;
                BreadcrumbHasValue = false;
            }
            else if (SelectedItem is ItemSizeValueDTO value)
            {
                var parent = ItemSizeCategories.FirstOrDefault(c => c.Id == value.ItemSizeCategoryId);
                BreadcrumbCategoryName = parent?.Name ?? string.Empty;
                BreadcrumbValueName = value.Name ?? string.Empty;
                BreadcrumbHasValue = true;
            }
            else
            {
                BreadcrumbCategoryName = string.Empty;
                BreadcrumbValueName = string.Empty;
                BreadcrumbHasValue = false;
            }
        }

        private void SetNewBreadcrumb()
        {
            if (IsValueSelected)
            {
                var parent = ItemSizeCategories.FirstOrDefault(c => c.Id == SelectedCategoryIdForNewValue);
                BreadcrumbCategoryName = parent?.Name ?? string.Empty;
                BreadcrumbValueName = "...";
                BreadcrumbHasValue = true;
            }
            else
            {
                BreadcrumbCategoryName = "Nuevo grupo";
                BreadcrumbValueName = string.Empty;
                BreadcrumbHasValue = false;
            }
        }

        #endregion

        #region API Methods

        public async Task LoadItemSizeCategoriesAsync()
        {
            try
            {
                Refresh();
                string query = GetLoadItemSizeCategoriesQuery();
                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.pageSize = -1;
                PageType<ItemSizeCategoryGraphQLModel> result = await _itemSizeCategoryService.GetPageAsync(query, variables);
                ItemSizeCategories = Context.AutoMapper.Map<ObservableCollection<ItemSizeCategoryDTO>>(result.Entries);
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadItemSizeCategoriesAsync" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        // Category CRUD

        public async Task<UpsertResponseType<ItemSizeCategoryGraphQLModel>> CreateItemSizeCategoryAsync()
        {
            string query = GetCreateItemSizeCategoryQuery();
            dynamic variables = new ExpandoObject();
            variables.createResponseInput = new ExpandoObject();
            variables.createResponseInput.Name = EditingName;

            return await _itemSizeCategoryService.CreateAsync<UpsertResponseType<ItemSizeCategoryGraphQLModel>>(query, variables);
        }

        public async Task<UpsertResponseType<ItemSizeCategoryGraphQLModel>> UpdateItemSizeCategoryAsync()
        {
            string query = GetUpdateItemSizeCategoryQuery();
            dynamic variables = new ExpandoObject();
            variables.updateResponseData = new ExpandoObject();
            variables.updateResponseData.Name = EditingName;
            variables.updateResponseId = ((ItemSizeCategoryDTO)SelectedItem).Id;

            return await _itemSizeCategoryService.UpdateAsync<UpsertResponseType<ItemSizeCategoryGraphQLModel>>(query, variables);
        }

        public async Task DeleteItemSizeCategoryAsync()
        {
            try
            {
                ItemSizeCategoryDTO selectedItem = ((ItemSizeCategoryDTO)SelectedItem);
                string query = GetCanDeleteItemSizeCategoryQuery();

                object variables = new { canDeleteResponseId = selectedItem.Id };

                var validation = await this._itemSizeCategoryService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Atención!", text: $"¿Confirma que desea eliminar el grupo de tallaje: {selectedItem.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "Este grupo de tallaje no puede ser eliminado" +
                        (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }
                DeleteResponseType deletedItemSizeCategory = await this.ExecuteDeleteItemSizeCategoryAsync(selectedItem.Id);
                if (!deletedItemSizeCategory.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedItemSizeCategory.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(new ItemSizeCategoryDeleteMessage() { DeletedItemSizeCategory = deletedItemSizeCategory });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "Delete" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task<DeleteResponseType> ExecuteDeleteItemSizeCategoryAsync(int id)
        {
            string query = GetDeleteItemSizeCategoryQuery();

            object variables = new
            {
                deleteResponseId = id
            };

            return await _itemSizeCategoryService.DeleteAsync<DeleteResponseType>(query, variables);
        }

        // Value CRUD

        public async Task<UpsertResponseType<ItemSizeValueGraphQLModel>> CreateItemSizeValueAsync()
        {
            var parent = ItemSizeCategories.FirstOrDefault(x => x.Id == SelectedCategoryIdForNewValue);
            string query = GetCreateItemSizeValueQuery();
            dynamic variables = new ExpandoObject();
            variables.createResponseInput = new ExpandoObject();
            variables.createResponseInput.Name = EditingName;
            variables.createResponseInput.ItemSizeCategoryId = SelectedCategoryIdForNewValue;
            variables.createResponseInput.DisplayOrder = parent?.ItemSizeValues?.Count ?? 0;

            return await _itemSizeValueService.CreateAsync<UpsertResponseType<ItemSizeValueGraphQLModel>>(query, variables);
        }

        public async Task<UpsertResponseType<ItemSizeValueGraphQLModel>> UpdateItemSizeValueAsync()
        {
            string query = GetUpdateItemSizeValueQuery();
            dynamic variables = new ExpandoObject();
            variables.updateResponseData = new ExpandoObject();
            variables.updateResponseData.Name = EditingName;
            variables.updateResponseData.DisplayOrder = EditingDisplayOrder;
            variables.updateResponseId = ((ItemSizeValueDTO)SelectedItem).Id;

            return await _itemSizeValueService.UpdateAsync<UpsertResponseType<ItemSizeValueGraphQLModel>>(query, variables);
        }

        public async Task DeleteItemSizeValueAsync()
        {
            try
            {
                ItemSizeValueDTO selectedItem = ((ItemSizeValueDTO)SelectedItem);
                string query = GetCanDeleteItemSizeValueQuery();

                object variables = new { canDeleteResponseId = selectedItem.Id };

                var validation = await this._itemSizeValueService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Atención!", text: $"¿Confirma que desea eliminar el tallaje: {selectedItem.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "Este tallaje no puede ser eliminado" +
                        (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }
                DeleteResponseType deletedItemSizeValue = await this.ExecuteDeleteItemSizeValueAsync(selectedItem.Id);
                if (!deletedItemSizeValue.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedItemSizeValue.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(new ItemSizeValueDeleteMessage() { DeletedItemSizeValue = deletedItemSizeValue });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "Delete" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task<DeleteResponseType> ExecuteDeleteItemSizeValueAsync(int id)
        {
            string query = GetDeleteItemSizeValueQuery();

            object variables = new
            {
                deleteResponseId = id
            };

            return await _itemSizeValueService.DeleteAsync<DeleteResponseType>(query, variables);
        }

        #endregion

        #region Message Handlers

        public Task HandleAsync(ItemSizeCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            var entity = message.CreatedItemSizeCategory.Entity;
            var newDTO = new ItemSizeCategoryDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                ItemSizeValues = []
            };
            ItemSizeCategories.Add(newDTO);
            SelectedItem = newDTO;
            _notificationService.ShowSuccess(message.CreatedItemSizeCategory.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSizeCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            var entity = message.UpdatedItemSizeCategory.Entity;
            var existing = ItemSizeCategories.FirstOrDefault(c => c.Id == entity.Id);
            if (existing != null)
            {
                existing.Name = entity.Name;
            }
            _notificationService.ShowSuccess(message.UpdatedItemSizeCategory.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSizeCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            var existing = ItemSizeCategories.FirstOrDefault(c => c.Id == message.DeletedItemSizeCategory.DeletedId);
            if (existing != null)
            {
                ItemSizeCategories.Remove(existing);
                if (SelectedItem == existing) SelectedItem = null;
            }
            _notificationService.ShowSuccess(message.DeletedItemSizeCategory.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSizeValueCreateMessage message, CancellationToken cancellationToken)
        {
            var entity = message.CreatedItemSizeValue.Entity;
            var parent = ItemSizeCategories.FirstOrDefault(c => c.Id == entity.ItemSizeCategory.Id);
            if (parent != null)
            {
                if (parent.ItemSizeValues == null) parent.ItemSizeValues = [];
                var newDTO = new ItemSizeValueDTO
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    DisplayOrder = entity.DisplayOrder,
                    ItemSizeCategoryId = entity.ItemSizeCategory.Id
                };
                parent.ItemSizeValues.Add(newDTO);
                parent.IsExpanded = true;
                SelectedItem = newDTO;
            }
            _notificationService.ShowSuccess(message.CreatedItemSizeValue.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSizeValueUpdateMessage message, CancellationToken cancellationToken)
        {
            var entity = message.UpdatedItemSizeValue.Entity;
            foreach (var category in ItemSizeCategories)
            {
                var existing = category.ItemSizeValues?.FirstOrDefault(v => v.Id == entity.Id);
                if (existing != null)
                {
                    existing.Name = entity.Name;
                    existing.DisplayOrder = entity.DisplayOrder;
                    break;
                }
            }
            _notificationService.ShowSuccess(message.UpdatedItemSizeValue.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSizeValueDeleteMessage message, CancellationToken cancellationToken)
        {
            foreach (var category in ItemSizeCategories)
            {
                var existing = category.ItemSizeValues?.FirstOrDefault(v => v.Id == message.DeletedItemSizeValue.DeletedId);
                if (existing != null)
                {
                    category.ItemSizeValues.Remove(existing);
                    if (SelectedItem == existing) SelectedItem = null;
                    break;
                }
            }
            _notificationService.ShowSuccess(message.DeletedItemSizeValue.Message);
            return Task.CompletedTask;
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = LoadItemSizeCategoriesAsync();
        }

        #endregion

        #region QueryBuilder Methods

        // ItemSizeCategory queries

        public static string GetLoadItemSizeCategoriesQuery()
        {
            var fields = FieldSpec<PageType<ItemSizeCategoryGraphQLModel>>
                .Create()
                .SelectList(p => p.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .SelectList(e => e.ItemSizeValues, sv => sv
                        .Field(v => v.Id)
                        .Field(v => v.Name)
                        .Field(v => v.DisplayOrder)
                        .Select(v => v.ItemSizeCategory, cat => cat
                            .Field(c => c.Id))))
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("itemSizeCategoriesPage", [parameter], fields, alias: "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public static string GetCanDeleteItemSizeCategoryQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteItemSizeCategory", [parameter], fields, alias: "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public static string GetDeleteItemSizeCategoryQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteItemSizeCategory", [parameter], fields, alias: "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        public static string GetCreateItemSizeCategoryQuery()
        {
            var fields = FieldSpec<UpsertResponseType<ItemSizeCategoryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "itemSizeCategory", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateItemSizeCategoryInput!");
            var fragment = new GraphQLQueryFragment("createItemSizeCategory", [parameter], fields, alias: "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        public static string GetUpdateItemSizeCategoryQuery()
        {
            var fields = FieldSpec<UpsertResponseType<ItemSizeCategoryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "itemSizeCategory", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .SelectList(f => f.ItemSizeValues, sv => sv
                        .Field(v => v.Id)
                        .Field(v => v.Name)
                        .Select(v => v.ItemSizeCategory, cat => cat
                            .Field(c => c.Id))
                        .Field(v => v.DisplayOrder)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var dataParam = new GraphQLQueryParameter("data", "UpdateItemSizeCategoryInput!");
            var idParam = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("updateItemSizeCategory", [dataParam, idParam], fields, alias: "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        // ItemSizeValue queries

        public static string GetCanDeleteItemSizeValueQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("canDeleteItemSizeValue", [parameter], fields, alias: "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        public static string GetDeleteItemSizeValueQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("deleteItemSizeValue", [parameter], fields, alias: "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        public static string GetCreateItemSizeValueQuery()
        {
            var fields = FieldSpec<UpsertResponseType<ItemSizeValueGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "itemSizeValue", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Select(f => f.ItemSizeCategory, cat => cat
                        .Field(c => c.Id))
                    .Field(f => f.DisplayOrder))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateItemSizeValueInput!");
            var fragment = new GraphQLQueryFragment("createItemSizeValue", [parameter], fields, alias: "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        public static string GetUpdateItemSizeValueQuery()
        {
            var fields = FieldSpec<UpsertResponseType<ItemSizeValueGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "itemSizeValue", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Select(f => f.ItemSizeCategory, cat => cat
                        .Field(c => c.Id))
                    .Field(f => f.DisplayOrder))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var dataParam = new GraphQLQueryParameter("data", "UpdateItemSizeValueInput!");
            var idParam = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("updateItemSizeValue", [dataParam, idParam], fields, alias: "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        }

        #endregion

        #region Constructor

        public ItemSizeCategoryViewModel(
            ItemSizeViewModel context,
            IRepository<ItemSizeCategoryGraphQLModel> itemSizeCategoryService,
            IRepository<ItemSizeValueGraphQLModel> itemSizeValueService,
            Helpers.Services.INotificationService notificationService)
        {
            Context = context;
            _itemSizeCategoryService = itemSizeCategoryService;
            _itemSizeValueService = itemSizeValueService;
            _notificationService = notificationService;
            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close) Context.EventAggregator.Unsubscribe(this);
            await base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion
    }
}
