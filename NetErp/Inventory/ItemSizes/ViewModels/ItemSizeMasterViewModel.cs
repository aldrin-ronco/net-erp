using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Data;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Inventory;
using NetErp.Books.AccountingAccounts.DTO;
using NetErp.Helpers;
using NetErp.Inventory.ItemSizes.DTO;
using Services.Inventory.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.ItemSizes.ViewModels
{
    public class ItemSizeMasterViewModel : Screen,
        IHandle<ItemSizeDetailDeleteMessage>,
        IHandle<ItemSizeDetailUpdateMessage>,
        IHandle<ItemSizeDetailCreateMessage>,
        IHandle<ItemSizeMasterUpdateMessage>,
        IHandle<ItemSizeMasterCreateMessage>,
        IHandle<ItemSizeMasterDeleteMessage>
    {
        #region "ItemSizeMaster"

        #region "Properties"

        private readonly IRepository<ItemSizeCategoryGraphQLModel> _itemSizeCategoryService;
        private readonly Helpers.Services.INotificationService _notificationService;

        private ObservableCollection<ItemSizeCategoryDTO> _itemSizesMaster = [];
        public ObservableCollection<ItemSizeCategoryDTO> ItemSizesMaster
        {
            get { return _itemSizesMaster; }
            set
            {
                if (_itemSizesMaster != value)
                {
                    _itemSizesMaster = value;
                    NotifyOfPropertyChange(nameof(ItemSizesMaster));
                }
            }
        }
        #endregion

        #region "Commands"

        private ICommand _editItemSizeMasterCommand;

        public ICommand EditItemSizeMasterCommand
        {
            get
            {
                if (_editItemSizeMasterCommand is null) _editItemSizeMasterCommand = new AsyncCommand(EditItemSizeMaster, CanEditItemSizeMaster);
                return _editItemSizeMasterCommand;
            }
        }

        public bool CanEditItemSizeMaster => true;

        private ICommand _deleteItemSizeMasterCommand;

        public ICommand DeleteItemSizeMasterCommand
        {
            get
            {
                if (_deleteItemSizeMasterCommand is null) _deleteItemSizeMasterCommand = new AsyncCommand(DeleteItemSizeMaster, CanDeleteItemSizeMaster);
                return _deleteItemSizeMasterCommand;
            }
        }
        public bool CanDeleteItemSizeMaster => true;

        #endregion

        #region "Methods"

        public async Task LoadItemSizesMaster()
        {
            try
            {
                Refresh();
                string query = @"
                query($pageResponsePagination: PaginationInput) {
                    PageResponse: itemSizeCategoriesPage(pagination: $pageResponsePagination) {
                        entries {
                            id
                            name
                            itemSizeValues {
                                id
                                name
                                displayOrder
                                itemSizeCategory {
                                    id
                                }
                            }
                        }
                    }
                }";
                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.pageSize = -1;
                PageType<ItemSizeCategoryGraphQLModel> result = await _itemSizeCategoryService.GetPageAsync(query, variables);
                ItemSizesMaster = Context.AutoMapper.Map<ObservableCollection<ItemSizeCategoryDTO>>(result.Entries);

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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadItemSizesMaster" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }
        public async Task DeleteItemSizeMaster()
        {
            try
            {
                ItemSizeCategoryDTO selectedItem = ((ItemSizeCategoryDTO)SelectedItem);
                string query = @"
                query($id: ID){
                  CanDeleteModel: canDeleteItemSizeCategory(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { selectedItem.Id };

                var validation = await this._itemSizeCategoryService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Atención!", text: $"¿Confirma que desea eliminar el grupo de tallaje: {selectedItem.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "Este tallaje no puede ser eliminado" +
                        (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }
                var deletedItemSizeMaster = await this.ExecuteDeleteItemSizeMaster(selectedItem.Id);
                await Context.EventAggregator.PublishOnCurrentThreadAsync(new ItemSizeMasterDeleteMessage() { DeletedItemSizeMaster = deletedItemSizeMaster });
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
        public async Task<ItemSizeCategoryGraphQLModel> ExecuteDeleteItemSizeMaster(int id)
        {
            try
            {
                string query = @"
                mutation ($id: ID) {
                  DeleteResponse: deleteItemSizeCategory (id: $id) {
                    id
                    name
                  }
                }";

                object variables = new
                {
                    Id = (int)id
                };

                var deletedItemSizeMaster = await _itemSizeCategoryService.DeleteAsync(query, variables);
                return deletedItemSizeMaster;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task EditItemSizeMaster()
        {
            try
            {
                TextBoxName = ((ItemSizeCategoryDTO)SelectedItem).Name;
                ((ItemSizeCategoryDTO)SelectedItem).IsEditing = true;
                _ = this.SetFocus(nameof(TextBoxName));
                IsUpdate = true;
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task<ItemSizeCategoryGraphQLModel> UpdateItemSizeMaster()
        {
            try
            {
                string query = @"
                mutation($data: UpdateItemSizeCategoryInput!, $id: ID){
                  UpdateResponse: updateItemSizeCategory(data: $data, id: $id){
                    id
                    name
                    itemSizeValues {
                        id
                        name
                        itemSizeCategory {
                            id
                        }
                        displayOrder
                    }
                  }
                }
                ";
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.Name = TextBoxName;
                variables.Id = ((ItemSizeCategoryDTO)SelectedItem).Id;

                ItemSizeCategoryGraphQLModel result = await _itemSizeCategoryService.UpdateAsync(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ItemSizeCategoryGraphQLModel> SaveItemSizeMaster()
        {
            try
            {
                string query = @"
                mutation($data: CreateItemSizeCategoryInput!){
                  CreateResponse: createItemSizeCategory(data: $data){
                    id
                    name
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.Name = TextBoxName;

                ItemSizeCategoryGraphQLModel result = await _itemSizeCategoryService.CreateAsync(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        #endregion

        #region "Messages"


        public Task HandleAsync(ItemSizeMasterUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                ItemSizeCategoryDTO itemSizeCategoryDTO = ItemSizesMaster.Where(x => x.Id == message.UpdatedItemSizeMaster.Id).First();
                int indexToUpdate = ItemSizesMaster.IndexOf(itemSizeCategoryDTO);
                _ = Application.Current.Dispatcher.Invoke(() => ItemSizesMaster[indexToUpdate] = Context.AutoMapper.Map<ItemSizeCategoryDTO>(message.UpdatedItemSizeMaster));
                _notificationService.ShowSuccess("Grupo de tallaje actualizado correctamente");
            }
            catch (Exception)
            {

                throw;
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSizeMasterDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                ItemSizeCategoryDTO itemSizeCategoryDTO = ItemSizesMaster.Where(x => x.Id == message.DeletedItemSizeMaster.Id).First();
                _ = Application.Current.Dispatcher.Invoke(() => ItemSizesMaster.Remove(itemSizeCategoryDTO));
                _notificationService.ShowSuccess("Grupo de tallaje eliminado correctamente");
            }
            catch (Exception)
            {

                throw;
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSizeMasterCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ItemSizeCategoryDTO? selectedItemSizeCategoryDTO = ItemSizesMaster.Where(x => x.Id == 0).FirstOrDefault();
                    if (selectedItemSizeCategoryDTO != null)
                    {
                        SelectedItem = selectedItemSizeCategoryDTO;
                        ((ItemSizeCategoryDTO)SelectedItem).Id = message.CreatedItemSizeMaster.Id;
                        ((ItemSizeCategoryDTO)SelectedItem).Name = message.CreatedItemSizeMaster.Name;
                        ((ItemSizeCategoryDTO)SelectedItem).IsEditing = false;
                    }
                    else
                    {
                        ItemSizesMaster.Add(Context.AutoMapper.Map<ItemSizeCategoryDTO>(message.CreatedItemSizeMaster));
                    }

                });
                _notificationService.ShowSuccess("Grupo de tallaje creado correctamente");
            }
            catch (Exception)
            {

                throw;
            }
            return Task.CompletedTask;
        }

        #endregion

        #endregion

        #region "ItemSizeDetail"

        #region "Properties"

        private readonly IRepository<ItemSizeValueGraphQLModel> _itemSizeValueService;

        #endregion

        #region "Commands"

        private ICommand _deleteItemSizeDetailCommand;

        public ICommand DeleteItemSizeDetailCommand
        {
            get
            {
                if (_deleteItemSizeDetailCommand is null) _deleteItemSizeDetailCommand = new AsyncCommand(DeleteItemSizeDetail, CanDeleteItemSizeDetail);
                return _deleteItemSizeDetailCommand;
            }
        }
        public bool CanDeleteItemSizeDetail => true;

        private ICommand _editItemSizeDetailCommand;

        public ICommand EditItemSizeDetailCommand
        {
            get
            {
                if (_editItemSizeDetailCommand is null) _editItemSizeDetailCommand = new AsyncCommand(EditItemSizeDetail, CanEditItemSizeDetail);
                return _editItemSizeDetailCommand;
            }
        }
        public bool CanEditItemSizeDetail => true;



        #endregion

        #region "Methods"

        public async Task DeleteItemSizeDetail()
        {
            try
            {
                ItemSizeValueDTO selectedItem = ((ItemSizeValueDTO)SelectedItem);
                string query = @"
                query($id: ID){
                  CanDeleteModel: canDeleteItemSizeValue(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { selectedItem.Id };

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
                var deletedItemSizeDetail = await this.ExecuteDeleteItemSizeDetail(selectedItem.Id);
                await Context.EventAggregator.PublishOnCurrentThreadAsync(new ItemSizeDetailDeleteMessage() { DeletedItemSizeDetail = deletedItemSizeDetail });
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

        public async Task<ItemSizeValueGraphQLModel> ExecuteDeleteItemSizeDetail(int id)
        {
            try
            {
                string query = @"
                mutation ($id: ID) {
                  DeleteResponse: deleteItemSizeValue (id: $id) {
                    id
                    name
                    itemSizeCategory {
                        id
                    }
                    displayOrder
                  }
                }";

                object variables = new
                {
                    Id = (int)id
                };

                var deletedItemSizeDetail = await _itemSizeValueService.DeleteAsync(query, variables);
                return deletedItemSizeDetail;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ItemSizeValueGraphQLModel> UpdateItemSizeDetail()
        {
            try
            {
                string query = @"
                mutation($data: UpdateItemSizeValueInput!, $id: ID){
                  UpdateResponse: updateItemSizeValue(data: $data, id: $id){
                    id
                    name
                    itemSizeCategory {
                        id
                    }
                    displayOrder
                  }
                }
                ";
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.Name = TextBoxName;
                variables.Data.DisplayOrder = ((ItemSizeValueDTO)SelectedItem).DisplayOrder;
                variables.Id = ((ItemSizeValueDTO)SelectedItem).Id;

                ItemSizeValueGraphQLModel result = await _itemSizeValueService.UpdateAsync(query, variables);
                return result;

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task EditItemSizeDetail()
        {
            try
            {
                TextBoxName = ((ItemSizeValueDTO)SelectedItem).Name;
                ((ItemSizeValueDTO)SelectedItem).IsEditing = true;
                _ = this.SetFocus(nameof(TextBoxName));
                IsUpdate = true;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ItemSizeValueGraphQLModel> SaveItemSizeDetail()
        {
            try
            {
                ItemSizeCategoryDTO masterDTO = ItemSizesMaster.FirstOrDefault(x => x.Id == ((ItemSizeValueDTO)SelectedItem).ItemSizeCategoryId);
                string query = @"
                mutation($data: CreateItemSizeValueInput!){
                  CreateResponse: createItemSizeValue(data: $data){
                    id
                    name
                    itemSizeCategory {
                        id
                    }
                    displayOrder
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.Name = TextBoxName;
                variables.Data.ItemSizeCategoryId = masterDTO.Id;
                variables.Data.DisplayOrder = masterDTO.ItemSizeValues.Count - 1;

                ItemSizeValueGraphQLModel result = await _itemSizeValueService.CreateAsync(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        #endregion

        #region "Messages"

        public Task HandleAsync(ItemSizeDetailDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                ItemSizeValueDTO itemDetailDTO = ItemSizesMaster.Where(x => x.Id == message.DeletedItemSizeDetail.ItemSizeCategory.Id).FirstOrDefault().ItemSizeValues.First(x => x.Id == message.DeletedItemSizeDetail.Id);
                _ = Application.Current.Dispatcher.Invoke(() => ItemSizesMaster.Where(x => x.Id == itemDetailDTO.ItemSizeCategoryId).FirstOrDefault().ItemSizeValues.Remove(itemDetailDTO));
                _notificationService.ShowSuccess("Tallaje eliminado correctamente");
            }
            catch (Exception)
            {

                throw;
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSizeDetailUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                ItemSizeValueDTO itemSizeValueDTO = ItemSizesMaster.Where(x => x.Id == message.UpdatedItemSizeDetail.ItemSizeCategory.Id).First().ItemSizeValues.First(x => x.Id == message.UpdatedItemSizeDetail.Id);
                int indexToUpdate = ItemSizesMaster.Where(x => x.Id == message.UpdatedItemSizeDetail.ItemSizeCategory.Id).First().ItemSizeValues.IndexOf(itemSizeValueDTO);
                _ = Application.Current.Dispatcher.Invoke(() => ItemSizesMaster.Where(x => x.Id == itemSizeValueDTO.ItemSizeCategoryId).First()
                                                                .ItemSizeValues[indexToUpdate] = Context.AutoMapper.Map<ItemSizeValueDTO>(message.UpdatedItemSizeDetail));
                _notificationService.ShowSuccess("Tallaje actualizado correctamente");
            }
            catch (Exception)
            {

                throw;
            }
            return Task.CompletedTask;
        }


        public Task HandleAsync(ItemSizeDetailCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ItemSizeValueDTO? selectedItemSizeValueDTO = ItemSizesMaster.Where(x => x.Id == message.CreatedItemSizeDetail.ItemSizeCategory.Id).First().ItemSizeValues.FirstOrDefault(x => x.Id == 0);
                    if (selectedItemSizeValueDTO != null)
                    {
                        SelectedItem = selectedItemSizeValueDTO;
                        ((ItemSizeValueDTO)SelectedItem).Id = message.CreatedItemSizeDetail.Id;
                        ((ItemSizeValueDTO)SelectedItem).Name = message.CreatedItemSizeDetail.Name;
                        ((ItemSizeValueDTO)SelectedItem).IsEditing = false;
                    }
                    else
                    {
                        ItemSizesMaster.Where(x => x.Id == message.CreatedItemSizeDetail.ItemSizeCategory.Id).First().ItemSizeValues.Add(Context.AutoMapper.Map<ItemSizeValueDTO>(message.CreatedItemSizeDetail));
                    }
                });
                _notificationService.ShowSuccess("Tallaje creado correctamente");
            }
            catch (Exception)
            {

                throw;
            }
            return Task.CompletedTask;
        }
        #endregion

        #endregion

        #region "Global"

        #region "Properties"

        private bool _isUpdate;

        public bool IsUpdate
        {
            get { return _isUpdate; }
            set
            {
                if (_isUpdate != value)
                {
                    _isUpdate = value;
                    NotifyOfPropertyChange(nameof(IsUpdate));
                }
            }
        }

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
        private string _TextBoxName;

        public string TextBoxName
        {
            get { return _TextBoxName; }
            set
            {
                if (_TextBoxName != value)
                {
                    _TextBoxName = value;
                    this.TrackChange(nameof(TextBoxName));

                    NotifyOfPropertyChange(nameof(TextBoxName));
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
        #endregion

        #region "Commands"

        private ICommand _onPressedEnterKeyCommand;

        public ICommand OnPressedEnterKeyCommand
        {
            get
            {
                if (_onPressedEnterKeyCommand is null) _onPressedEnterKeyCommand = new AsyncCommand(OnPressedEnterKeyAsync, CanOnPressedEnterKey);
                return _onPressedEnterKeyCommand;
            }
        }

        public bool CanOnPressedEnterKey => true;


        #endregion

        #region "Methods"
        public async Task OnPressedEnterKeyAsync()
        {
            try
            {
                if (IsUpdate is true)
                {
                    if (SelectedItem is ItemSizeValueDTO)
                    {
                        var updatedItemSizeDetail = await UpdateItemSizeDetail();
                        await Context.EventAggregator.PublishOnCurrentThreadAsync(new ItemSizeDetailUpdateMessage() { UpdatedItemSizeDetail = updatedItemSizeDetail });
                    }
                    else
                    {
                        var updatedItemSizeMaster = await UpdateItemSizeMaster();
                        await Context.EventAggregator.PublishOnCurrentThreadAsync(new ItemSizeMasterUpdateMessage() { UpdatedItemSizeMaster = updatedItemSizeMaster });
                    }
                }
                else
                {
                    if (SelectedItem is ItemSizeValueDTO)
                    {
                        var createdItemSizeDetail = await SaveItemSizeDetail();
                        await Context.EventAggregator.PublishOnCurrentThreadAsync(new ItemSizeDetailCreateMessage() { CreatedItemSizeDetail = createdItemSizeDetail });
                    }
                    else
                    {
                        var createdItemSizeMaster = await SaveItemSizeMaster();
                        await Context.EventAggregator.PublishOnCurrentThreadAsync(new ItemSizeMasterCreateMessage() { CreatedItemSizeMaster = createdItemSizeMaster });
                    }
                }

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{graphQLError.Errors[0].Extensions.Message} {graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "Save" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = LoadItemSizesMaster();
        }
        #endregion

        #region "Constructor"
        public ItemSizeMasterViewModel(
            ItemSizeViewModel context,
            IRepository<ItemSizeCategoryGraphQLModel> itemSizeMasterService,
            IRepository<ItemSizeValueGraphQLModel> itemSizeDetailService,
            Helpers.Services.INotificationService notificationService)
        {
            Context = context;
            _itemSizeCategoryService = itemSizeMasterService;
            _itemSizeValueService = itemSizeDetailService;
            _notificationService = notificationService;
            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            Context.EventAggregator.Unsubscribe(this);
            await base.OnDeactivateAsync(close, cancellationToken);
        }
        #endregion

        #endregion


    }
}
