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

        public List<ItemSizeMasterGraphQLModel> itemsSizesMaster = [];

        public readonly IGenericDataAccess<ItemSizeMasterGraphQLModel> ItemSizeMasterService = IoC.Get<IGenericDataAccess<ItemSizeMasterGraphQLModel>>();

        private ObservableCollection<ItemSizeMasterDTO> _itemSizesMaster = [];
        public ObservableCollection<ItemSizeMasterDTO> ItemSizesMaster
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
                query($filter:ItemSizeMasterFilterInput){
                    ListResponse: itemsSizesMaster(filter: $filter){
                    id
                    name
                    sizes{
                       id
                       name
                       presentationOrder
                       itemSizeMasterId
                    }
                    }
                }";
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.Name = "";
                IEnumerable<ItemSizeMasterGraphQLModel> source = await ItemSizeMasterService.GetList(query, variables);
                ItemSizesMaster = Context.AutoMapper.Map<ObservableCollection<ItemSizeMasterDTO>>(source);

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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadAccountingEntities" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }
        public async Task DeleteItemSizeMaster()
        {
            try
            {
                ItemSizeMasterDTO selectedItem = ((ItemSizeMasterDTO)SelectedItem);
                string query = @"
                query($id: ID){
                  CanDeleteModel: canDeleteItemSizeMaster(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { selectedItem.Id };

                var validation = await this.ItemSizeMasterService.CanDelete(query, variables);

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
                var deletedItemSizeMaster = await Task.Run(() => this.ExecuteDeleteItemSizeMaster(selectedItem.Id));
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
        public async Task<ItemSizeMasterGraphQLModel> ExecuteDeleteItemSizeMaster(int id)
        {
            try
            {
                string query = @"
                mutation ($id: ID) {
                  DeleteResponse: deleteItemSizeMaster (id: $id) {
                    id
                    name
                  }
                }";

                object variables = new
                {
                    Id = (int)id
                };

                var deletedItemSizeMaster = await ItemSizeMasterService.Delete(query, variables);
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
                TextBoxName = ((ItemSizeMasterDTO)SelectedItem).Name;
                ((ItemSizeMasterDTO)SelectedItem).IsEditing = true;
                _ = this.SetFocus(nameof(TextBoxName));
                IsUpdate = true;
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task<ItemSizeMasterGraphQLModel> UpdateItemSizeMaster()
        {
            try
            {
                string query = @"
                mutation($data: UpdateItemSizeMasterInput!, $id: ID){
                  UpdateResponse: updateItemSizeMaster(data: $data, id: $id){
                    id
                    name
                    sizes{
                        id
                        name
                        itemSizeMasterId
                        presentationOrder
                    }
                  }
                }
                ";
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.Name = TextBoxName;
                variables.Id = ((ItemSizeMasterDTO)SelectedItem).Id;

                ItemSizeMasterGraphQLModel result = await ItemSizeMasterService.Update(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ItemSizeMasterGraphQLModel> SaveItemSizeMaster()
        {
            try
            {
                string query = @"
                mutation($data: CreateItemSizeMasterInput!){
                  CreateResponse: createItemSizeMaster(data: $data){
                    id
                    name
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.Name = TextBoxName;

                ItemSizeMasterGraphQLModel result = await ItemSizeMasterService.Create(query, variables);
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
                ItemSizeMasterDTO itemSizeMasterDTO = ItemSizesMaster.Where(x => x.Id == message.UpdatedItemSizeMaster.Id).First();
                int indexToUpdate = ItemSizesMaster.IndexOf(itemSizeMasterDTO);
                _ = Application.Current.Dispatcher.Invoke(() => ItemSizesMaster[indexToUpdate] = Context.AutoMapper.Map<ItemSizeMasterDTO>(message.UpdatedItemSizeMaster));
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
                ItemSizeMasterDTO itemSizeMasterDTO = ItemSizesMaster.Where(x => x.Id == message.DeletedItemSizeMaster.Id).First();
                _ = Application.Current.Dispatcher.Invoke(() => ItemSizesMaster.Remove(itemSizeMasterDTO));
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
                    ItemSizeMasterDTO? selectedItemSizeMasterDTO = ItemSizesMaster.Where(x => x.Id == 0).FirstOrDefault();
                    if (selectedItemSizeMasterDTO != null)
                    {
                        SelectedItem = selectedItemSizeMasterDTO;
                        ((ItemSizeMasterDTO)SelectedItem).Id = message.CreatedItemSizeMaster.Id;
                        ((ItemSizeMasterDTO)SelectedItem).Name = message.CreatedItemSizeMaster.Name;
                        ((ItemSizeMasterDTO)SelectedItem).IsEditing = false;
                    }
                    else
                    {
                        ItemSizesMaster.Add(Context.AutoMapper.Map<ItemSizeMasterDTO>(message.CreatedItemSizeMaster));
                    }

                });
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

        public readonly IGenericDataAccess<ItemSizeDetailGraphQLModel> ItemSizeDetailService = IoC.Get<IGenericDataAccess<ItemSizeDetailGraphQLModel>>();

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
                ItemSizeDetailDTO selectedItem = ((ItemSizeDetailDTO)SelectedItem);
                string query = @"
                query($id: ID){
                  CanDeleteModel: canDeleteItemSizeDetail(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { selectedItem.Id };

                var validation = await this.ItemSizeDetailService.CanDelete(query, variables);

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
                var deletedItemSizeDetail = await Task.Run(() => this.ExecuteDeleteItemSizeDetail(selectedItem.Id));
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

        public async Task<ItemSizeDetailGraphQLModel> ExecuteDeleteItemSizeDetail(int id)
        {
            try
            {
                string query = @"
                mutation ($id: ID) {
                  DeleteResponse: deleteItemSizeDetail (id: $id) {
                    id
                    name
                    itemSizeMasterId
                    presentationOrder
                  }
                }";

                object variables = new
                {
                    Id = (int)id
                };

                var deletedItemSizeDetail = await ItemSizeDetailService.Delete(query, variables);
                return deletedItemSizeDetail;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ItemSizeDetailGraphQLModel> UpdateItemSizeDetail()
        {
            try
            {
                string query = @"
                mutation($data: UpdateItemSizeDetailInput!, $id: ID){
                  UpdateResponse: updateItemSizeDetail(data: $data, id: $id){
                    id
                    itemSizeMasterId
                    name
                    presentationOrder
                  }
                }
                ";
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.Name = TextBoxName;
                variables.Data.PresentationOrder = ((ItemSizeDetailDTO)SelectedItem).PresentationOrder;
                variables.Id = ((ItemSizeDetailDTO)SelectedItem).Id;

                ItemSizeDetailGraphQLModel result = await ItemSizeDetailService.Update(query, variables);
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
                TextBoxName = ((ItemSizeDetailDTO)SelectedItem).Name;
                ((ItemSizeDetailDTO)SelectedItem).IsEditing = true;
                _ = this.SetFocus(nameof(TextBoxName));
                IsUpdate = true;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ItemSizeDetailGraphQLModel> SaveItemSizeDetail()
        {
            try
            {
                ItemSizeMasterDTO masterDTO = ItemSizesMaster.FirstOrDefault(x => x.Id == ((ItemSizeDetailDTO)SelectedItem).ItemSizeMasterId);
                string query = @"
                mutation($data: CreateItemSizeDetailInput!){
                  CreateResponse: createItemSizeDetail(data: $data){
                    id
                    itemSizeMasterId
                    name
                    presentationOrder
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.Name = TextBoxName;
                variables.Data.ItemSizeMasterId = masterDTO.Id;
                variables.Data.PresentationOrder = masterDTO.Sizes.Count - 1;

                ItemSizeDetailGraphQLModel result = await ItemSizeDetailService.Create(query, variables);
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
                ItemSizeDetailDTO itemDetailDTO = ItemSizesMaster.Where(x => x.Id == message.DeletedItemSizeDetail.ItemSizeMasterId).FirstOrDefault().Sizes.First(x => x.Id == message.DeletedItemSizeDetail.Id);
                _ = Application.Current.Dispatcher.Invoke(() => ItemSizesMaster.Where(x => x.Id == itemDetailDTO.ItemSizeMasterId).FirstOrDefault().Sizes.Remove(itemDetailDTO));
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
                ItemSizeDetailDTO itemSizeDetailDTO = ItemSizesMaster.Where(x => x.Id == message.UpdatedItemSizeDetail.ItemSizeMasterId).First().Sizes.First(x => x.Id == message.UpdatedItemSizeDetail.Id);
                int indexToUpdate = ItemSizesMaster.Where(x => x.Id == message.UpdatedItemSizeDetail.ItemSizeMasterId).First().Sizes.IndexOf(itemSizeDetailDTO);
                _ = Application.Current.Dispatcher.Invoke(() => ItemSizesMaster.Where(x => x.Id == itemSizeDetailDTO.ItemSizeMasterId).First()
                                                                .Sizes[indexToUpdate] = Context.AutoMapper.Map<ItemSizeDetailDTO>(message.UpdatedItemSizeDetail));
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
                    ItemSizeDetailDTO? selectedItemSizeDetailDTO = ItemSizesMaster.Where(x => x.Id == message.CreatedItemSizeDetail.ItemSizeMasterId).First().Sizes.FirstOrDefault(x => x.Id == 0);
                    if (selectedItemSizeDetailDTO != null)
                    {
                        SelectedItem = selectedItemSizeDetailDTO;
                        ((ItemSizeDetailDTO)SelectedItem).Id = message.CreatedItemSizeDetail.Id;
                        ((ItemSizeDetailDTO)SelectedItem).Name = message.CreatedItemSizeDetail.Name;
                        ((ItemSizeDetailDTO)SelectedItem).IsEditing = false;
                    }
                    else
                    {
                        ItemSizesMaster.Where(x => x.Id == message.CreatedItemSizeDetail.ItemSizeMasterId).First().Sizes.Add(Context.AutoMapper.Map<ItemSizeDetailDTO>(message.CreatedItemSizeDetail));
                    }

                });

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
                    if (SelectedItem is ItemSizeDetailDTO)
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
                    if (SelectedItem is ItemSizeDetailDTO)
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
            _ = Task.Run(() => LoadItemSizesMaster());
        }
        #endregion

        #region "Constructor"
        public ItemSizeMasterViewModel(ItemSizeViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
        }
        #endregion

        #endregion


    }
}
