using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Inventory;
using NetErp.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    public class SubCategoryDetailViewModel: Screen, INotifyDataErrorInfo
    {
        private readonly IRepository<ItemSubCategoryGraphQLModel> _itemSubCategoryService;

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
                    ValidateProperty(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
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
            Context.EnableOnActivateAsync = false;
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
                Context.EnableOnActivateAsync = false;
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
                var result = IsNewRecord ? await _itemSubCategoryService.CreateAsync(query, variables) : await _itemSubCategoryService.UpdateAsync(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public bool CanSave => _errors.Count <= 0;



        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(nameof(Name));
        }

        public void CleanUpControls()
        {
            Id = 0;
            Name = string.Empty;
        }

        #region "Errors"


        Dictionary<string, List<string>> _errors;

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

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
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(Name):
                        if (string.IsNullOrEmpty(Name)) AddError(propertyName, "El nombre de la subcategoría no puede estar vacío");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
        private void ValidateProperties()
        {
            ValidateProperty(nameof(Name), Name);
        }
        #endregion
        
        public SubCategoryDetailViewModel(
            CatalogViewModel context,
            IRepository<ItemSubCategoryGraphQLModel> itemSubCategoryService)
        {
            Context = context;
            _itemSubCategoryService = itemSubCategoryService;
            _errors = new Dictionary<string, List<string>>();
            ValidateProperty(nameof(Name), Name);
        }
    }
}
