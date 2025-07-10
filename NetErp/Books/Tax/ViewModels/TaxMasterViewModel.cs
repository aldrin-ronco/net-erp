using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Books.Tax.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;

namespace NetErp.Books.Tax.ViewModels
{
    public class TaxMasterViewModel : Screen,
         IHandle<TaxDeleteMessage>,
        IHandle<TaxUpdateMessage>,
        IHandle<TaxCreateMessage>
    {
        public TaxViewModel Context { get; set; }
        public IGenericDataAccess<TaxGraphQLModel> TaxService { get; set; } = IoC.Get<IGenericDataAccess<TaxGraphQLModel>>();

        public TaxMasterViewModel(TaxViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
            _ = Task.Run(() => InitializeAsync());
        }

        public async Task InitializeAsync()
        {
            _ = Task.Run(this.LoadTaxs);

        }
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);

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
        private bool _isActive = true;
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));

                    PageIndex = 1;
                    _ = Task.Run(this.LoadTaxs);

                }
            }
        }
        #region Paginacion

        /// <summary>
        /// TotalCount
        /// </summary>
        private int _totalCount = 0;
        public int TotalCount
        {
            get { return _totalCount; }
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
            }
        }

        /// <summary>
        /// PageIndex
        /// </summary>
        private int _pageIndex = 1; // DevExpress first page is index zero
        public int PageIndex
        {
            get { return _pageIndex; }
            set
            {
                if (_pageIndex != value)
                {
                    _pageIndex = value;
                    NotifyOfPropertyChange(nameof(PageIndex));
                }
            }
        }

        private int _pageSize = 50; // Default PageSize 50
        public int PageSize
        {
            get { return _pageSize; }
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    NotifyOfPropertyChange(nameof(PageSize));
                }
            }

        }

        #endregion
        // Filtro de busqueda
        private string _filterSearch = "";
        public string FilterSearch
        {
            get { return _filterSearch; }
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 2)
                    {
                        _ = Task.Run(this.LoadTaxs);
                    }
                    ;
                }
            }
        }
        private ObservableCollection<TaxGraphQLModel> _Taxs;

        public ObservableCollection<TaxGraphQLModel> Taxs
        {
            get { return _Taxs; }
            set
            {
                if (_Taxs != value)
                {
                    _Taxs = value;
                    NotifyOfPropertyChange(nameof(Taxs));
                }
            }
        }
        private TaxGraphQLModel? _selectedTaxGraphQLModel;
        public TaxGraphQLModel? SelectedTaxGraphQLModel
        {
            get { return _selectedTaxGraphQLModel; }
            set
            {
                if (_selectedTaxGraphQLModel != value)
                {
                    _selectedTaxGraphQLModel = value;
                    NotifyOfPropertyChange(nameof(CanDeleteTax));
                    NotifyOfPropertyChange(nameof(SelectedTaxGraphQLModel));
                }
            }
        }

        #region Command
        private ICommand _newCommand;

        public ICommand NewCommand
        {
            get
            {
                if (_newCommand is null) _newCommand = new AsyncCommand(NewAsync);
                return _newCommand;
            }
        }
        private ICommand _deleteTaxCommand;
        public ICommand DeleteTaxCommand
        {
            get
            {
                if (_deleteTaxCommand is null) _deleteTaxCommand = new AsyncCommand(DeleteTax, CanDeleteTax);
                return _deleteTaxCommand;
            }
        }
        #endregion
        public bool CanDeleteTax
        {
            get
            {
                if (SelectedTaxGraphQLModel is null) return false;
                return true;
            }
        }
        public async Task NewAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteActivateDetailViewForEdit());
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "NewtTaxEntity" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }




        public async Task DeleteTax()
        {
            try
            {
                IsBusy = true;
                int id = SelectedTaxGraphQLModel.Id;

                string query = @"
                query($id:Int!) {
                  CanDeleteModel: canDeleteTax(id:$id) {
                    canDelete
                    message
                  }
                }";
                object variables = new { Id = id };

                var validation = await this.TaxService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Atención!", text: $"¿Confirma que desea eliminar el registro {SelectedTaxGraphQLModel.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                        (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }


                Refresh();
                var deletedTax = await ExecuteDeleteTax(id);

                await Context.EventAggregator.PublishOnCurrentThreadAsync(new TaxDeleteMessage() { DeletedTax = deletedTax });

                // Desactivar opcion de eliminar registros
                NotifyOfPropertyChange(nameof(CanDeleteTax));
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
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteTax" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<TaxGraphQLModel> ExecuteDeleteTax(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteTax(id: $id) {
                    id
                  }
                }";
                object variables = new { Id = id };
                var deletedTax = await this.TaxService.Delete(query, variables);
                this.SelectedTaxGraphQLModel = null;
                return deletedTax;
            }

            catch (Exception ex)
            {
                throw;
            }

        }
        public async Task LoadTaxs()
        {
            try
            {
                IsBusy = true;
                string query = @"
			    query($filter : TaxFilterInput!){
     
                   pageResponse : taxPage(filter : $filter){
        
                       count,
                       rows {
                        id
                        name
                        margin
                        taxType {
                          id
                          name
                        }
                        generatedTaxAccount {id name},
                        generatedTaxRefundAccount {id name},
                        deductibleTaxAccount {id name},
                        deductibleTaxRefundAccount { id name}
                        isActive
                        formula
                        alternativeFormula
                       }
      
      
                    }
       
                }
                ";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;


                variables.filter.and = new ExpandoObject[]
               {
                     new(),
                     new(),
                     new()
               };
                if (FilterSearch.Length > 0)
                {
                    variables.filter.and[0].name = new ExpandoObject();
                    variables.filter.and[0].name.@operator = "like";
                    variables.filter.and[0].name.value = FilterSearch;

                }
                if (IsActive)
                {
                    variables.filter.and[1].isActive = new ExpandoObject();
                    variables.filter.and[1].isActive.@operator = "=";
                    variables.filter.and[1].isActive.value = true;

                }

                var result = await TaxService.GetPage(query, variables);

                Taxs = Context.AutoMapper.Map<ObservableCollection<TaxGraphQLModel>>(result.PageResponse.Rows);
                TotalCount = result.PageResponse.Count;
                
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task EditTax()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteActivateDetailViewForEdit());

                SelectedTaxGraphQLModel = null;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "EditTax" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task ExecuteActivateDetailViewForEdit()
        {
            await Context.ActivateDetailViewForEdit(SelectedTaxGraphQLModel);
        }

        public Task HandleAsync(TaxDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {

                return LoadTaxs();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task HandleAsync(TaxCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
               
                return LoadTaxs();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task HandleAsync(TaxUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {

                return LoadTaxs();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
