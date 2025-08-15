using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.Inventory.CatalogItems.Views;
using Services.Inventory.DAL.PostgreSQL;
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
    public class CatalogDetailViewModel: Screen, INotifyDataErrorInfo
    {
        private readonly IRepository<CatalogGraphQLModel> _catalogService;

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
                CatalogGraphQLModel result = await ExecuteSave();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new CatalogCreateMessage() { CreatedCatalog = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new CatalogUpdateMessage() { UpdatedCatalog = result });
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

        public async Task<CatalogGraphQLModel> ExecuteSave()
        {
            string query = string.Empty;

            try
            {
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();

                if (!IsNewRecord) variables.Id = Id;

                variables.Data.Name = Name;

                query = IsNewRecord
                    ? @"
                    mutation($data: CreateCatalogInput!){
                        CreateResponse: createCatalog(data: $data){
                            id
                            name
                            itemsTypes{
                                id
                                name
                                prefixChar
                                stockControl
                                catalog{
                                    id
                                    }
                                }
                            }           
                        }" : @"
                    mutation($data: UpdateCatalogInput!, $id: Int!){
                        UpdateResponse: updateCatalog(data: $data, id: $id){
                            id
                            name
                            }
                        }";
                var result = IsNewRecord ? await _catalogService.CreateAsync(query, variables) : await _catalogService.UpdateAsync(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public bool CanSave => _errors.Count <= 0;

        public void CleanUpControls()
        {
            Id = 0;
            Name = string.Empty;
        }

        public CatalogDetailViewModel(
            CatalogViewModel context,
            IRepository<CatalogGraphQLModel> catalogService)
        {
            Context = context;
            _catalogService = catalogService;
            _errors = [];

            ValidateProperty(nameof(Name), Name);
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = this.SetFocus(nameof(Name));
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
                        if (string.IsNullOrEmpty(Name)) AddError(propertyName, "El nombre del catálogo no puede estar vacío");
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
        #endregion
    }
}
