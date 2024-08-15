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
using NetErp.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NetErp.Billing.DocumentSequence.ViewModels
{
    public class DocumentSequenceMasterViewModel : Screen,
        IHandle<DocumentSequenceDeleteMessage>,
        IHandle<DocumentSequenceCreateMessage>,
        IHandle<DocumentSequenceUpdateMessage>
    {

        #region Properties

        public DocumentSequenceViewModel Context { get; private set; }

        public readonly IGenericDataAccess<DocumentSequenceMasterGraphQLModel> DocumentSequenceMasterService = IoC.Get<IGenericDataAccess<DocumentSequenceMasterGraphQLModel>>();

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanCreateDocumentSequence));
                }
            }
        }

        private string _filterSearch = "";
        public string FilterSearch
        {
            get => _filterSearch;
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    // Solo ejecutamos la busqueda si esta vacio el filtro o si hay por lo menos 3 caracteres digitados
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = Task.Run(() => LoadDocumentSequences());
                }
            }
        }

        private bool _showActiveDocumentSequenceOnly = true;
        public bool ShowActiveDocumentSequenceOnly
        {
            get => _showActiveDocumentSequenceOnly;
            set
            {
                if (_showActiveDocumentSequenceOnly != value)
                {
                    _showActiveDocumentSequenceOnly = value;
                    NotifyOfPropertyChange(nameof(ShowActiveDocumentSequenceOnly));
                }
            }
        }

        private int _selectedCostCenterId = 0;
        public int SelectedCostCenterId
        {
            get => _selectedCostCenterId;
            set
            {
                if (_selectedCostCenterId != value)
                {
                    _selectedCostCenterId = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenterId));
                    _ = Task.Run(() => LoadDocumentSequences());
                }
            }
        }

        public bool CanDeleteDocumentSequence
        {
            get
            {
                if (SelectedDocumentSequence is null) return false;
                return true;
            }
        }

        public bool CanCreateDocumentSequence
        {
            get
            {
                if (IsBusy) return false;
                return true;
            }
        }


        private DocumentSequenceMasterGraphQLModel _selectedDocumentSequence;
        public DocumentSequenceMasterGraphQLModel SelectedDocumentSequence
        {
            get => _selectedDocumentSequence;
            set
            {
                if (_selectedDocumentSequence != value)
                {
                    _selectedDocumentSequence = value;
                    NotifyOfPropertyChange(nameof(SelectedDocumentSequence));
                    NotifyOfPropertyChange(nameof(CanDeleteDocumentSequence));
                }
            }
        }

        private ObservableCollection<DocumentSequenceMasterGraphQLModel> _documentSequences;
        public ObservableCollection<DocumentSequenceMasterGraphQLModel> DocumentSequences
        {
            get => _documentSequences;
            set
            {
                if (_documentSequences != value)
                {
                    _documentSequences = value;
                    NotifyOfPropertyChange(nameof(DocumentSequences));
                }
            }
        }

        private ObservableCollection<CostCenterGraphQLModel> _costCenters;
        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get => _costCenters;
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        private ICommand _createDocumentSequenceCommand;
        public ICommand CreateDocumentSequenceCommand
        {
            get
            {
                if (_createDocumentSequenceCommand is null) _createDocumentSequenceCommand = new AsyncCommand(CreateDocumentSequence, CanCreateDocumentSequence);
                return _createDocumentSequenceCommand;
            }

        }

        private ICommand _deleteDocumentSequenceCommand;
        public ICommand DeleteDocumentSequenceCommand
        {
            get
            {
                if (_deleteDocumentSequenceCommand is null) _deleteDocumentSequenceCommand = new AsyncCommand(DeleteDocumentSequence, CanDeleteDocumentSequence);
                return _deleteDocumentSequenceCommand;
            }
        }

        #endregion

        #region Methods

        public async Task EditDocumentSequence()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteEditDocumentSequence());
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteEditDocumentSequence()
        {
            await Context.ActivateDetailViewForEdit(SelectedDocumentSequence);
        }

        public void OnChecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteDocumentSequence));
        }

        public void OnUnchecked()
        {
            NotifyOfPropertyChange(nameof(CanDeleteDocumentSequence));
        }

        public async Task LoadDocumentSequences()
        {
            try
            {
                IsBusy = true;

                Refresh();
                string query = @"query ($filter: DocumentSequenceMasterFilterInput!) {
                   pageResponse: documentSequenceMasterPage(filter: $filter) {
                    count
                    rows {
                      id
                      costCenter {
                        id
                        name
                      }
                      number
                      initialDate
                      finalDate
                      prefix
                      initialNumber
                      finalNumber
                      titleLabel
                      sequenceLabel
                      authorizationType
                      authorizationKind
                      reference
                      isActive
                      technicalKey
                      documentSequenceDetail {
                        id
                        documentSequenceMasterId
                        number
                      }
                    }
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.Filter = new ExpandoObject();
                variables.Filter.Pagination = new ExpandoObject();
                variables.Filter.Pagination.Page = PageIndex;
                variables.Filter.Pagination.PageSize = PageSize;
                variables.Filter.Number = FilterSearch;
                if (ShowActiveDocumentSequenceOnly) variables.Filter.IsActive = ShowActiveDocumentSequenceOnly;
                if (!ShowActiveDocumentSequenceOnly) variables.Filter.IsActive = ShowActiveDocumentSequenceOnly;
                variables.Filter.CostCenters = SelectedCostCenterId == 0 ? null : new int[] { SelectedCostCenterId };

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                var result = await DocumentSequenceMasterService.GetPage(query, variables);
                stopwatch.Stop();
                TotalCount = result.PageResponse.Count;
                DocumentSequences = result.PageResponse.Rows;
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task Initialize()
        {
            try
            {
                IsBusy = true;

                Refresh();
                string query = @"query ($filter: DocumentSequenceMasterFilterInput!) {
                  documentSequenceMasterPage(filter: $filter) {
                    count
                    rows {
                      id
                      costCenter {
                        id
                        name
                      }
                      number
                      initialDate
                      finalDate
                      prefix
                      initialNumber
                      finalNumber
                      titleLabel
                      sequenceLabel
                      authorizationType
                      authorizationKind
                      reference
                      isActive
                      technicalKey
                      documentSequenceDetail {
                        id
                        documentSequenceMasterId
                        number
                      }
                    }
                  }
                  costCenters{
                    id
                    name
                  }
                }";

                object variables = new { Filter = new { IsActive = true, Pagination = new { Page = PageIndex, PageSize } }  };
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                var result = await DocumentSequenceMasterService.GetDataContext<DocumentSequenceDataContext>(query, variables);
                stopwatch.Stop();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CostCenters = new ObservableCollection<CostCenterGraphQLModel>(result.CostCenters);
                    Context.CostCenters = new ObservableCollection<CostCenterGraphQLModel>(result.CostCenters);
                    CostCenters.Insert(0, new CostCenterGraphQLModel() { Id = 0, Name = "MOSTRAR TODOS LOS CENTROS DE COSTOS" });
                    SelectedCostCenterId = CostCenters.FirstOrDefault(x => x.Id == 0).Id;
                    DocumentSequences = result.DocumentSequenceMasterPage.Rows;
                });
                TotalCount = result.DocumentSequenceMasterPage.Count;
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CreateDocumentSequence()
        {
            await Context.ActivateDetailViewForNew();
        }

        public DocumentSequenceMasterViewModel(DocumentSequenceViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
            _ = Task.Run(() => Initialize());
        }
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            _ = this.SetFocus(nameof(FilterSearch));
        }

        private async Task ExecuteChangeIndex()
        {
            await Task.CompletedTask;
        }

        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        public async Task DeleteDocumentSequence()
        {
            try
            {
                IsBusy = true;
                int id = SelectedDocumentSequence.Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteDocumentSequence(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this.DocumentSequenceMasterService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar la autorización de numeración: {SelectedDocumentSequence.Description}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) 
                    {
                        SelectedDocumentSequence = null;
                        return; 
                    };
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                Refresh();
                var deletedDocumentSequence = await Task.Run(() => ExecuteDeleteDocumentSequence(id));
                await Context.EventAggregator.PublishOnUIThreadAsync(new DocumentSequenceDeleteMessage() { DeletedDocumentSequence = deletedDocumentSequence });
                NotifyOfPropertyChange(nameof(CanDeleteDocumentSequence));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<DocumentSequenceMasterGraphQLModel> ExecuteDeleteDocumentSequence(int id)
        {
            try
            {
                string query = @"mutation($id:Int!) {
                  deleteResponse: deleteDocumentSequence(id:$id) {
                    id
                  }
                }";

                object variables = new { Id = id };
                var result = await DocumentSequenceMasterService.Delete(query, variables);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task HandleAsync(DocumentSequenceDeleteMessage message, CancellationToken cancellationToken)
        {
            return LoadDocumentSequences();
        }

        public Task HandleAsync(DocumentSequenceCreateMessage message, CancellationToken cancellationToken)
        {
            return LoadDocumentSequences();
        }

        public Task HandleAsync(DocumentSequenceUpdateMessage message, CancellationToken cancellationToken)
        {
            
            return LoadDocumentSequences();
        }

        #endregion

        #region Paginacion

        private string _responseTime;
        public string ResponseTime
        {
            get => _responseTime;
            set
            {
                if (_responseTime != value)
                {
                    _responseTime = value;
                    NotifyOfPropertyChange(nameof(ResponseTime));
                }
            }
        }

        private int _pageIndex = 1; // DefaultPageIndex = 1
        public int PageIndex
        {
            get => _pageIndex;
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
            get => _pageSize;
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    NotifyOfPropertyChange(nameof(PageSize));
                }
            }
        }

        private int _totalCount = 0;
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
            }
        }

        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) _paginationCommand = new AsyncCommand(ExecuteChangeIndex, CanExecuteChangeIndex);
                return _paginationCommand;
            }
        }

        #endregion
    }
}
