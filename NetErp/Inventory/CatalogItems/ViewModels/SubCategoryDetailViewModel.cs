using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Inventory;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    public class SubCategoryDetailViewModel: Screen
    {
        public readonly IGenericDataAccess<ItemSubCategoryGraphQLModel> ItemSubCategoryService = IoC.Get<IGenericDataAccess<ItemSubCategoryGraphQLModel>>();

        private CatalogViewModel _context;

        public CatalogViewModel Context
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

        private int _itemCategoryId;

        public int ItemCategoryId
        {
            get { return _itemCategoryId; }
            set
            {
                if (_itemCategoryId != value)
                {
                    _itemCategoryId = value;
                    NotifyOfPropertyChange(nameof(ItemCategoryId));
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
                ItemSubCategoryGraphQLModel result = await ExecuteSave();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new ItemSubCategoryCreateMessage() { CreatedItemSubCategory = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new ItemSubCategoryUpdateMessage() { UpdatedItemSubCategory = result });
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

        public async Task<ItemSubCategoryGraphQLModel> ExecuteSave()
        {
            string query = string.Empty;

            try
            {
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();

                if (!IsNewRecord) variables.Id = Id;

                variables.Data.Name = Name;
                variables.Data.ItemCategoryId = ItemCategoryId;

                query = IsNewRecord
                    ? @"
                    mutation($data: CreateItemSubCategoryInput!){
                        CreateResponse: createItemSubCategory(data: $data){
                            id
                            name
                            itemCategory{
                                id
                                itemType{
                                    id
                                    }
                                }
                            }
                        }" : @"
                    mutation($data: UpdateItemSubCategoryInput!, $id: Int!){
                        UpdateResponse: updateItemSubCategory(data: $data, id: $id){
                            id
                            name
                            itemCategory{
                                id
                                itemType{
                                    id
                                    }
                                }
                            }
                        }";
                var result = IsNewRecord ? await ItemSubCategoryService.Create(query, variables) : await ItemSubCategoryService.Update(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public bool CanSave => !IsBusy;


        public SubCategoryDetailViewModel(CatalogViewModel context)
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
