using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Data.Native;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using Ninject.Activation;
using Services.Billing.DAL.PostgreSQL;
using Services.Books.DAL.PostgreSQL;
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
using Xceed.Wpf.Toolkit.Primitives;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.TaxCategory.ViewModels
{
   public class TaxCategoryMasterViewModel : Screen,
         IHandle<TaxCategoryDeleteMessage>,
        IHandle<TaxCategoryUpdateMessage>,
        IHandle<TaxCategoryCreateMessage>
    {
        public TaxCategoryViewModel Context { get; set; }

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<TaxCategoryGraphQLModel> _taxCategoryService;

        public TaxCategoryMasterViewModel(TaxCategoryViewModel context, Helpers.Services.INotificationService notificationService, IRepository<TaxCategoryGraphQLModel> taxCategoryService)
        {
            Context = context;
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _taxCategoryService = taxCategoryService ?? throw new ArgumentNullException(nameof(taxCategoryService));

            Context.EventAggregator.SubscribeOnUIThread(this);
            // Context.EventAggregator.SubscribeOnPublishedThread(this);
         
            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            await this.LoadTaxCategoriesAsync();
           
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
        // Filtro de busqueda
        private string _filterSearch = "";
        public string FilterSearch
        {
            get { return _filterSearch; }
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value == null ? "" : value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 2)
                    {
                        _ = this.LoadTaxCategoriesAsync();
                    }
                    ;
                }
            }
        }
        private ObservableCollection<TaxCategoryGraphQLModel> _taxCategories;

        public ObservableCollection<TaxCategoryGraphQLModel> TaxCategories
        {
            get { return _taxCategories; }
            set
            {
                if (_taxCategories != value)
                {
                    _taxCategories = value;
                    NotifyOfPropertyChange(nameof(TaxCategories));
                }
            }
        }
        private TaxCategoryGraphQLModel? _selectedTaxCategoryGraphQLModel;
        public TaxCategoryGraphQLModel? SelectedTaxCategoryGraphQLModel
        {
            get { return _selectedTaxCategoryGraphQLModel; }
            set
            {
                if (_selectedTaxCategoryGraphQLModel != value)
                {
                    _selectedTaxCategoryGraphQLModel = value;
                    NotifyOfPropertyChange(nameof(CanDeleteTaxCategory));
                    NotifyOfPropertyChange(nameof(SelectedTaxCategoryGraphQLModel));
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
        private ICommand _deleteTaxCategoryCommand;
        public ICommand DeleteTaxCategoryCommand
        {
            get
            {
                if (_deleteTaxCategoryCommand is null) _deleteTaxCategoryCommand = new AsyncCommand(DeleteTaxCategoryAsync, CanDeleteTaxCategory);
                return _deleteTaxCategoryCommand;
            }
        }
      
        #endregion
        public bool CanDeleteTaxCategory { 
            get
            {
                if (SelectedTaxCategoryGraphQLModel is null) return false;
                return true;
            }
        }
        public async Task NewAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                SelectedTaxCategoryGraphQLModel = null;
                await  ExecuteActivateDetailViewForEditAsync();
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "NewtTaxCategoryEntity" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
       



        public async Task DeleteTaxCategoryAsync()
        {
            try
            {
                if (SelectedTaxCategoryGraphQLModel is null) return;
                int id = SelectedTaxCategoryGraphQLModel.Id;
                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteTaxCategory();

                object variables = new { canDeleteResponseId = SelectedTaxCategoryGraphQLModel.Id };

                 var validation = await _taxCategoryService.CanDeleteAsync(query, variables);

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
                
                IsBusy = true;
                DeleteResponseType deletedTaxCategory = await Task.Run(() => this.ExecuteDeleteAsync(id));

                if (!deletedTaxCategory.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedTaxCategory.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new TaxCategoryDeleteMessage { DeletedTaxCategory = deletedTaxCategory });

                NotifyOfPropertyChange(nameof(CanDeleteTaxCategory));
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

        public async Task<DeleteResponseType> ExecuteDeleteAsync(int id)
        {
            try
            {

                string query = GetDeleteTaxCategoryQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _taxCategoryService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
           

        }
        public string GetDeleteTaxCategoryQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteTaxCategory", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetCanDeleteTaxCategory()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteTaxCategory", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }
        public async Task LoadTaxCategoriesAsync()
        {
            try
            {

                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
               
                variables.pagination = new ExpandoObject();
                variables.pageResponseFilters.matching = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();
                string query = GetLoadTaxCategoryQuery();

                PageType<TaxCategoryGraphQLModel> result = await _taxCategoryService.GetPageAsync(query, variables);
               this.TaxCategories = result.Entries;
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
        public string GetLoadTaxCategoryQuery()
        {
            var taxCategoriesFields = FieldSpec<PageType<TaxCategoryGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)

                    .Field(e => e.Name)
                    .Field(e => e.Prefix)
                    .Field(e => e.GeneratedTaxRefundAccountIsRequired)
                    .Field(e => e.GeneratedTaxAccountIsRequired)
                    .Field(e => e.DeductibleTaxRefundAccountIsRequired)
                    .Field(e => e.DeductibleTaxAccountIsRequired)
                    )
                .Build();

            var taxCategoriesParameters = new GraphQLQueryParameter("filters", "TaxCategoryFilters");

            var taxCategoriesFragment = new GraphQLQueryFragment("taxCategoriesPage", [taxCategoriesParameters], taxCategoriesFields, "PageResponse");

            var builder = new GraphQLQueryBuilder([taxCategoriesFragment]);

            return builder.GetQuery();
        }
        public async Task EditTaxCategoryAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await  ExecuteActivateDetailViewForEditAsync();

                SelectedTaxCategoryGraphQLModel = null;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "EditTaxCategory" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task ExecuteActivateDetailViewForEditAsync()
        {
            await Context.ActivateDetailViewForEdit(SelectedTaxCategoryGraphQLModel);
        }

        public async Task HandleAsync(TaxCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadTaxCategoriesAsync();
                _notificationService.ShowSuccess(message.DeletedTaxCategory.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task HandleAsync(TaxCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            await LoadTaxCategoriesAsync();
            _notificationService.ShowSuccess(message.UpdatedTaxCategory.Message);
        }

        public async Task HandleAsync(TaxCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadTaxCategoriesAsync();
                _notificationService.ShowSuccess(message.CreatedTaxCategory.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }
        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {

            // Desconectar eventos para evitar memory leaks
            //Context.EventAggregator.Unsubscribe(this);
            return base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
