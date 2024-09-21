using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Inventory;
using Services.Inventory.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    public class CategoryDetailViewModel: Screen
    {

        public readonly IGenericDataAccess<ItemCategoryGraphQLModel> ItemCategoryService = IoC.Get<IGenericDataAccess<ItemCategoryGraphQLModel>>();

        private CatalogViewModel _context;

        public CatalogViewModel Context
        {
            get { return _context; }
            set 
            {
                if(_context != value)
                {
                    _context = value; 
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        private int _id;

        public int Id
        {
            get { return _id; }
            set 
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                }
            }
        }


        private string _name;

        public string Name
        {
            get { return _name; }
            set 
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                }
            }
        }

        private int _itemTypeId;

        public int ItemTypeId
        {
            get { return _itemTypeId; }
            set 
            {
                if (_itemTypeId != value)
                {
                    _itemTypeId = value; 
                    NotifyOfPropertyChange(nameof(ItemTypeId));
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


        private ICommand _goBackCommand;

        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new RelayCommand(CanGoBack, GoBack);
                return _goBackCommand;
            }
        }

        public bool IsNewRecord => Id == 0;

        public void GoBack(object p)
        {
            _ = Task.Run(() => Context.ActivateMasterView());
        }

        public bool CanGoBack(object p)
        {
            return true;
        }

        private ICommand _saveCommand;

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(Save, CanSave);
                return _saveCommand;
            }
        }

        public async Task Save()
        {
            try
            {
                IsBusy = true;
                Refresh();
                ItemCategoryGraphQLModel result = await ExecuteSave();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new ItemCategoryCreateMessage() { CreatedItemCategory = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new ItemCategoryUpdateMessage() { UpdatedItemCategory = result });
                }
                await Context.ActivateMasterView();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<ItemCategoryGraphQLModel> ExecuteSave()
        {
            string query = string.Empty;

            try
            {
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();

                if (!IsNewRecord) variables.Id = Id;

                variables.Data.Name = Name;
                variables.Data.ItemTypeId = ItemTypeId;

                query = IsNewRecord
                    ? @"
                    mutation($data: CreateItemCategoryInput!){
                        CreateResponse: createItemCategory(data: $data){
                            id
                            name
                            itemType{
                                id
                                }
                            }
                        }" : @"
                    mutation($data: UpdateItemCategoryInput!, $id: Int!){
                        UpdateResponse: updateItemCategory(data: $data, id: $id){
                            id
                            name
                            itemType{
                                id
                                }
                            }
                        }";
                var result = IsNewRecord ? await ItemCategoryService.Create(query, variables) : await ItemCategoryService.Update(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public bool CanSave => !IsBusy;


        public CategoryDetailViewModel(CatalogViewModel context) 
        {
            Context = context;
        }

        public void CleanUpControls()
        {
            Id = 0;
            Name = string.Empty;
        }
    }
}
