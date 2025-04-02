using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Global;
using Models.Treasury;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;
using static Models.Treasury.ConceptGraphQLModel;

namespace NetErp.Treasury.Concept.ViewModels
{
    public class ConceptMasterViewModel: Screen,
        IHandle<TreasuryConceptDeleteMessage>,
        IHandle<TreasuryConceptUpdateMessage>
    {
        public IGenericDataAccess<ConceptGraphQLModel> ConceptService { get; set; } = IoC.Get<IGenericDataAccess<ConceptGraphQLModel>>();
        public ConceptViewModel Context { get; set; }
        public ConceptMasterViewModel(ConceptViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
            SelectTypeCommand = new DelegateCommand<string>(type => SelectedType = type);
            Task.Run(() => LoadConceptsAsync());

        }

        private ObservableCollection<ConceptGraphQLModel> _concepts;

        public ObservableCollection<ConceptGraphQLModel> Concepts
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

        private ConceptGraphQLModel _selectedItem = null;

        public ConceptGraphQLModel SelectedItem
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
                _cts.Cancel(); // Cancela cualquier petición anterior
                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                //IsBusy = true; 
                string query = @"
               query($filter: ConceptFilterInput!){
                    PageResponse: conceptPage(filter: $filter){
                    count
                    rows{
                        id
                        name
                        type
                        margin
                        allowMargin
                        marginBasis
                        accountingAccountId
                    }
                   }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.pagination = new ExpandoObject();
                variables.filter.pagination.page = PageIndex;
                variables.filter.pagination.pageSize = PageSize;
                if (!string.IsNullOrEmpty(SelectedType) && SelectedType != "T")
                {
                    variables.filter.type = new ExpandoObject();
                    variables.filter.type.Operator = "=";
                    variables.filter.type.Value = SelectedType;
                }

                var result = await ConceptService.GetPage(query, variables);                
                Concepts = new ObservableCollection<ConceptGraphQLModel>(result.PageResponse.Rows ?? new List<ConceptGraphQLModel>());

            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                //IsBusy = false; 
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

        private ICommand _deleteConceptCommand;

        public ICommand DeleteConceptCommand
        {
            get
            {
                if (_deleteConceptCommand is null) _deleteConceptCommand = new AsyncCommand(DeleteConcept);
                return _deleteConceptCommand;
            }
        }

        public async Task DeleteConcept()
        {
            try
            {
                //IsBusy = true;
                int id = SelectedItem!.Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteConcept(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this.ConceptService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    //IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {SelectedItem.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    //IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }
                //this.IsBusy = true;

                Refresh();

                ConceptGraphQLModel deletedConcept = await ExecuteDeleteConceptAsync(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryConceptDeleteMessage() { DeletedTreasuryConcept = deletedConcept });

                NotifyOfPropertyChange(nameof(CanDeleteConcept));
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                //IsBusy = false;
            }
        }

        public async Task<ConceptGraphQLModel> ExecuteDeleteConceptAsync(int id)
        {
            try
            {
                string query = @"mutation($id:Int!){
                  DeleteResponse: deleteConcept(id: $id){
                    id
                    name
                    type
                    margin
                    allowMargin
                    marginBasis
                    accountingAccountId
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.id = id;
                var result = await ConceptService.Delete(query, variables);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task EditConcept()
        {
            await Context.ActivateDetailView(SelectedItem ?? new ());
        }


        public Task HandleAsync(TreasuryConceptDeleteMessage message, CancellationToken cancellationToken)
        {
            ConceptGraphQLModel conceptToDelete = Concepts.FirstOrDefault(x => x.Id == message.DeletedTreasuryConcept.Id) ?? new ConceptGraphQLModel();
            Concepts.Remove(conceptToDelete);
            SelectedItem = null;
            return Task.CompletedTask;
        }

        public Task HandleAsync(TreasuryConceptUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadConceptsAsync();
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
                
        public ICommand SelectTypeCommand { get; }

    }
}
