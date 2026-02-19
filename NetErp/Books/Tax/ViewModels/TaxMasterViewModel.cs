using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Common.Validators;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Books.Tax.ViewModels;
using NetErp.Helpers.GraphQLQueryBuilder;
using Services.Billing.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static DevExpress.Data.Utils.SafeProcess;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.Tax.ViewModels
{
    public class TaxMasterViewModel : Screen,
         IHandle<TaxDeleteMessage>,
        IHandle<TaxUpdateMessage>,
        IHandle<TaxCreateMessage>
    {
        public TaxViewModel Context { get; set; }
      

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<TaxGraphQLModel> _taxService;

        public TaxMasterViewModel(TaxViewModel context, Helpers.Services.INotificationService notificationService, IRepository<TaxGraphQLModel> taxService)
        {
            Context = context;
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _taxService = taxService ?? throw new ArgumentNullException(nameof(taxService));
            Context.EventAggregator.SubscribeOnUIThread(this);
            _ =  InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            await this.LoadTaxesAsync();

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
                    _ = this.LoadTaxesAsync();

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
                    _filterSearch = value == null? "" : value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 2)
                    {
                        _ = this.LoadTaxesAsync();
                    }
                    ;
                }
            }
        }
        private ObservableCollection<TaxGraphQLModel> _taxes;

        public ObservableCollection<TaxGraphQLModel> Taxes
        {
            get { return _taxes; }
            set
            {
                if (_taxes != value)
                {
                    _taxes = value;
                    NotifyOfPropertyChange(nameof(Taxes));
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
        private ObservableCollection<TaxCategoryGraphQLModel> _taxCategories;

        
       
        // Tiempo de respuesta
        private string _responseTime;
        public string ResponseTime
        {
            get { return _responseTime; }
            set
            {
                if (_responseTime != value)
                {
                    _responseTime = value;
                    NotifyOfPropertyChange(nameof(ResponseTime));
                }
            }
        }
        #region Command
        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) this._paginationCommand = new RelayCommand(CanExecuteChangeIndex, ExecuteChangeIndex);
                return _paginationCommand;
            }
        }
        private async void ExecuteChangeIndex(object parameter)
        {
            await LoadTaxesAsync();
        }

        private bool CanExecuteChangeIndex(object parameter)
        {
            return true;
        }
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
                if (_deleteTaxCommand is null) _deleteTaxCommand = new AsyncCommand(DeleteTaxAsync, CanDeleteTax);
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
                SelectedTaxGraphQLModel = null;
                await ExecuteActivateDetailViewForNewAsync();
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




        public async Task DeleteTaxAsync()
        {
            try
            {
                if (SelectedTaxGraphQLModel is null) return;
                int id = SelectedTaxGraphQLModel.Id;
                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteTaxQuery();

                object variables = new { canDeleteResponseId = SelectedTaxGraphQLModel.Id };

                var validation = await _taxService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea eliminar el registro seleccionado?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención !", "El registro no puede ser eliminado" +
                        (char)13 + (char)13 + validation.Message, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                //elimionar esta linea al habilitar candelete
                IsBusy = true;
                DeleteResponseType deletedTax = await Task.Run(() => this.ExecuteDeleteTaxAsync(id));

                if (!deletedTax.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedTax.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new TaxDeleteMessage { DeletedTax = deletedTax });

                NotifyOfPropertyChange(nameof(CanDeleteTax));
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

        public async Task<DeleteResponseType> ExecuteDeleteTaxAsync(int id)
        {
            try
            {

                string query = GetDeleteTaxQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _taxService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
           

        }
        public string GetDeleteTaxQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteTax", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public async Task LoadTaxesAsync()
        {
            try
            {

                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.page = 1;
                variables.pageResponsePagination.pageSize = 10;

                variables.pageResponseFilters = new ExpandoObject();
                if (IsActive)
                {
                    variables.pageResponseFilters.isActive = IsActive;
                }
                if (FilterSearch?.Length > 0)
                {
                    variables.pageResponseFilters.name = FilterSearch.Trim().RemoveExtraSpaces();
                }
                    
                string query = GetLoadTaxesQuery();
              

                    PageType<TaxGraphQLModel> result = await _taxService.GetPageAsync(query, variables);
                    this.Taxes = result.Entries;
                    PageIndex = result.PageNumber;
                    PageSize = result.PageSize;
                    TotalCount = result.TotalEntries;
                


               
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
        public string GetLoadTaxesQuery()
        {
            var taxFields = FieldSpec<PageType<TaxGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Rate)
                    .Field(e => e.IsActive)
                    .Field(e => e.Formula)
                    .Select(e => e.TaxCategory, cat => cat
                    .Field(c => c.Id)
                    .Field(c => c.Name)
                    .Field(c => c.GeneratedTaxRefundAccountIsRequired)
                    .Field(c => c.GeneratedTaxAccountIsRequired)
                    .Field(c => c.DeductibleTaxRefundAccountIsRequired)
                    .Field(c => c.DeductibleTaxAccountIsRequired)
                    )
                    .Select(e => e.GeneratedTaxAccount, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                        )
                    .Select(e => e.GeneratedTaxRefundAccount, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                        )
                    .Select(e => e.DeductibleTaxRefundAccount, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                        )
                    .Select(e => e.DeductibleTaxAccount, cat => cat
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                    )
                    
                )
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();

 
            var taxPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var taxfilterParameters = new GraphQLQueryParameter("filters", "TaxFilters");

            var taxFragment = new GraphQLQueryFragment("taxesPage", [taxPagParameters, taxfilterParameters], taxFields, "PageResponse");
     
            var builder =    new GraphQLQueryBuilder([taxFragment]);
            return builder.GetQuery();
        }
        public async Task EditTaxAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await ExecuteActivateDetailViewForEditAsync();

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
        public string GetCanDeleteTaxQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteTax", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }
        public async Task ExecuteActivateDetailViewForEditAsync()
        {
            await Context.ActivateDetailViewForEditAsync(SelectedTaxGraphQLModel.Id);
        }
        public async Task ExecuteActivateDetailViewForNewAsync()
        {
            await Context.ActivateDetailViewForNewAsync();
        }
        public Task HandleAsync(TaxDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _notificationService.ShowSuccess(message.DeletedTax.Message);
                return LoadTaxesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task HandleAsync(TaxCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadTaxesAsync();
                _notificationService.ShowSuccess(message.CreatedTax.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task HandleAsync(TaxUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _notificationService.ShowSuccess(message.UpdatedTax.Message);
                await LoadTaxesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
            }
            await base.OnDeactivateAsync(close, cancellationToken);
        }
        
    }
}
