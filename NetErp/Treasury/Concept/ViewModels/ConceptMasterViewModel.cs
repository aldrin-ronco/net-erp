using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.Global;
using Models.Treasury;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using Newtonsoft.Json;
using Services.Books.DAL.PostgreSQL;
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
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;
using static Models.Global.GraphQLResponseTypes;
using static Models.Treasury.TreasuryConceptGraphQLModel;

namespace NetErp.Treasury.Concept.ViewModels
{
    public class ConceptMasterViewModel: Screen,
        IHandle<TreasuryConceptDeleteMessage>,
        IHandle<TreasuryConceptUpdateMessage>,
        IHandle<TreasuryConceptCreateMessage>
    {
        private readonly IRepository<TreasuryConceptGraphQLModel> _conceptService;
        private readonly Helpers.Services.INotificationService _notificationService;
        public ConceptViewModel Context { get; set; }
        public ConceptMasterViewModel(
            ConceptViewModel context,
             Helpers.Services.INotificationService notificationService,
            IRepository<TreasuryConceptGraphQLModel> conceptService)
        {
            Context = context;
            _conceptService = conceptService;
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            Context.EventAggregator.SubscribeOnPublishedThread(this);
            SelectTypeCommand = new DelegateCommand<string>(type => SelectedType = type);
            Task.Run(() => LoadConceptsAsync());
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
            }
            
            await base.OnDeactivateAsync(close, cancellationToken);
        }
        
        private string _selectedType = string.Empty;
        public string SelectedType
        {
            get { return _selectedType; }
            set
            {
                if (_selectedType != value)
                {
                    _selectedType = value;
                    NotifyOfPropertyChange(nameof(SelectedType));
                    NotifyOfPropertyChange(nameof(IsTypeD));
                    NotifyOfPropertyChange(nameof(IsTypeI));
                    NotifyOfPropertyChange(nameof(IsTypeE));
                    PageIndex = 1;
                    Execute.OnUIThreadAsync(async () => await LoadConceptsAsync());
                }
            }
        }
        private bool _isTypeD;
        public bool IsTypeD
        {
            get => _isTypeD;
            set
            {
                if (_isTypeD != value)
                {
                    _isTypeD = value;
                    NotifyOfPropertyChange(nameof(IsTypeD));

                    if (SelectedType != "D")
                    {
                        SelectedType = "D";
                    }
                }
            }
        }
        private bool _isTypeI;
        public bool IsTypeI
        {
            get => _isTypeI;
            set
            {
                if (_isTypeI != value)
                {
                    _isTypeI = value;
                    NotifyOfPropertyChange(nameof(IsTypeI));

                    if (SelectedType != "I")
                    {
                        SelectedType = "I";
                    }
                }
            }
        }
        private bool _isTypeE;
        public bool IsTypeE
        {
            get => _isTypeE;
            set
            {
                if (_isTypeE != value)
                {
                    _isTypeE = value;
                    NotifyOfPropertyChange(nameof(IsTypeE));

                    if (SelectedType != "E")
                    {
                        SelectedType = "E";
                    }
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
        public bool CanDeleteConcept
        {
            get
            {
                if (SelectedItem is null) return false;
                return true;
            }
        }

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
                    NotifyOfPropertyChange(() => ResponseTime);
                }
            }
        }

        public ICommand SelectTypeCommand { get; }
        private ICommand _createConceptCommand;
        public ICommand CreateConceptCommand
        {
            get
            {
                if (_createConceptCommand is null) _createConceptCommand = new AsyncCommand(CreateConceptAsync);
                return _createConceptCommand;
            }
            set { _createConceptCommand = value; }
        }
        private ICommand _deleteConceptCommand;
        public ICommand DeleteConceptCommand
        {
            get
            {
                if (_deleteConceptCommand is null) _deleteConceptCommand = new AsyncCommand(DeleteConceptAsync);
                return _deleteConceptCommand;
            }
        }
        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) this._paginationCommand = new AsyncCommand(ExecuteChangeIndexAsync, CanExecuteChangeIndex);
                return _paginationCommand;
            }
        }

        private ObservableCollection<TreasuryConceptGraphQLModel> _concepts;
        public ObservableCollection<TreasuryConceptGraphQLModel> Concepts
        {
            get
            {
                return _concepts;
            }
            set
            {
                if (_concepts != value)
                {
                    _concepts = value;
                    NotifyOfPropertyChange(nameof(Concepts));
                }
            }
        }
        private TreasuryConceptGraphQLModel _selectedItem = null;
        public TreasuryConceptGraphQLModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanDeleteConcept));
                }
            }
        }
        private CancellationTokenSource _cts = new CancellationTokenSource(); //Controlar la cancelación de tareas asincronas.

        public async Task LoadConceptsAsync()
        {
            try
            {

                IsBusy = true;

                dynamic variables = new ExpandoObject();
                //Pagination
                variables.pagination = new ExpandoObject();
                variables.pagination.pageSize = PageSize;
                variables.pagination.page = PageIndex;
                variables.pageResponseFilters = new ExpandoObject();
                if (!string.IsNullOrEmpty(SelectedType) && SelectedType != "T")
                {
                    variables.filter.type = SelectedType;
                }
                string query = GetLoadTreasuryConceptQuery();

                PageType<TreasuryConceptGraphQLModel> result = await _conceptService.GetPageAsync(query, variables);
                TotalCount = result.TotalEntries;
                Concepts = result.Entries;
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
        public string GetLoadTreasuryConceptQuery()
        {
                                     
            var treasuryConceptFields = FieldSpec<PageType<TreasuryConceptGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Type)
                    .Field(e => e.Name)
                    .Field(e => e.Margin)
                    .Field(e => e.AllowMargin)
                    .Field(e => e.MarginBasis)
                    .Select(c => c.AccountingAccount, map => map 
                        .Field(f => f.Id)
                        .Field(f => f.Code)
                        .Field(f => f.Name)
                    )
                    )
                .Build();

            var treasuryConceptParameters = new GraphQLQueryParameter("filters", "TreasuryConceptFilters");
            var treasuryConceptPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var treasuryConceptFragment = new GraphQLQueryFragment("treasuryConceptsPage", [treasuryConceptParameters, treasuryConceptPagParameters], treasuryConceptFields, "PageResponse");

            var builder = new GraphQLQueryBuilder([treasuryConceptFragment]);

            return builder.GetQuery();
        }
        public string GetCanDeleteTreasuryConceptQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteTreasuryConcept", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }
        public string GetDeleteTreasuryConceptQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteTreasuryConcept", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public async Task<DeleteResponseType> ExecuteDeleteTreasuryConceptAsync(int id)
        {
            try
            {

                string query = GetDeleteTreasuryConceptQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _conceptService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task DeleteConceptAsync()
        {
            try
            {

                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteTreasuryConceptQuery();

                object variables = new { canDeleteResponseId = SelectedItem.Id };

                var validation = await _conceptService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedTreasuryConcept = await Task.Run(() => this.ExecuteDeleteTreasuryConceptAsync(SelectedItem.Id));

                if (!deletedTreasuryConcept.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedTreasuryConcept.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryConceptDeleteMessage { DeletedTreasuryConcept = deletedTreasuryConcept });

                NotifyOfPropertyChange(nameof(CanDeleteConcept));
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
        public async Task EditConcept()
        {
            await Context.ActivateDetailViewForEditAsync(SelectedItem ?? new());
        }
        public async Task CreateConceptAsync()
        {
            await Context.ActivateDetailViewForNewAsync();
        }
        private async Task ExecuteChangeIndexAsync()
        {
            await LoadConceptsAsync();
        }
       

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            
        }
        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        public async Task HandleAsync(TreasuryConceptDeleteMessage message, CancellationToken cancellationToken)
        {

            await LoadConceptsAsync();
            _notificationService.ShowSuccess(message.DeletedTreasuryConcept.Message);

        }
        public async Task HandleAsync(TreasuryConceptUpdateMessage message, CancellationToken cancellationToken)
        {
             await LoadConceptsAsync();
            _notificationService.ShowSuccess(message.UpdatedTreasuryConcept.Message);
        }
        public async Task HandleAsync(TreasuryConceptCreateMessage message, CancellationToken cancellationToken)
        {
            await LoadConceptsAsync();
            _notificationService.ShowSuccess(message.CreatedTreasuryConcept.Message);
        }
                
    }
}
