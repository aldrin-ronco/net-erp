using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Books;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.Primitives;

namespace NetErp.Books.AccountingPresentations.ViewModels
{
    class AccountingPresentationsMasterViewModel : Screen, IHandle<PresentationCreateMessage>,
        IHandle<PresentationUpdateMessage>
    {
        public IGenericDataAccess<AccountingPresentationGraphQLModel> PresentationServices { get; set; } = IoC.Get<IGenericDataAccess<AccountingPresentationGraphQLModel>>();

        private ObservableCollection<AccountingPresentationGraphQLModel>? _accountingPresentations { get; set; }
        public ObservableCollection<AccountingPresentationGraphQLModel> AccountingPresentations
        {
            get { return _accountingPresentations; }
            set
            {
                if (_accountingPresentations != value)
                {
                    _accountingPresentations = value;
                    NotifyOfPropertyChange(nameof(AccountingPresentations));
                }
            }
        }

        private ObservableCollection<AccountingBookGraphQLModel>? _accountingBooks { get; set; }
        public ObservableCollection<AccountingBookGraphQLModel>? AccountingBooks
        {
            get { return _accountingBooks; }
            set
            {
                if (_accountingBooks != value)
                {
                    _accountingBooks = value;
                    NotifyOfPropertyChange(nameof(AccountingBooks));
                }
            }
        }

        private AccountingPresentationGraphQLModel? _selectedItem;
        public AccountingPresentationGraphQLModel? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanDeletePresentation));
                }
            }
        }

        private string _filterSearch;
        public string FilterSearch
        {
            get { return _filterSearch; }
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = Task.Run(() => LoadPresentationsAsync());
                }
            }
        }

        public bool CanDeletePresentation
        {
            get
            {
                return SelectedItem != null;
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if(_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public AccountingPresentationsViewModel Context { get; set; }

        public ICommand? _createPresentationCommand;
        public ICommand CreatePresentationCommand
        {
            get
            {
                if (_createPresentationCommand is null) _createPresentationCommand = new AsyncCommand(CreatePresentationAsync);
                return _createPresentationCommand;
            }

        }
        public async Task CreatePresentationAsync()
        {
            await Context.ActivateDetailForNewAsync(AccountingBooks);
        }
        private ICommand? _deletePresentationCommand;
        public ICommand DeletePresentationCommand
        {
            get
            {
                if (_deletePresentationCommand is null) _deletePresentationCommand = new AsyncCommand(DeletePresentationAsync);
                return _deletePresentationCommand;
            }
        }

        public async Task DeletePresentationAsync()
        {
            try
            {
                string query = @"mutation($id : Int!){
                  DeleteResponse: deleteAccountingPresentation(id: $id){
                    id
                  }
                }";
                MessageBoxResult answer = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {SelectedItem.Name} ?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes) return;

                dynamic variables = new ExpandoObject();
                variables.id = SelectedItem.Id;
                var result = await PresentationServices.Delete(query, variables);
                if (result == null)
                {
                    MessageBox.Show($"No se pudo eliminar la zona: {SelectedItem.Name}, Intente de nuevo.");
                }
                SelectedItem = null;
                await LoadPresentationsAsync();
            }
            catch
            {
                throw;
            }
        }
        public async Task EditZoneAsync()
        {
            await Context.ActivateDetailViewForEditAsync(SelectedItem ?? new(), AccountingBooks);
        }

        public async Task LoadPresentationsAsync()
        {
            try
            {
                IsBusy = true;
                string query = @"query($filter: AccountingPresentationFilterInput){
                 accountingPresentations(filter: $filter)
                 {
                    id
                    name
                    allowsAccountingClosure
                    accountingBookClosure{
                      id
                      name
                    }
                    accountingBooks{
                      id
                      name
                    }
                  }
                  accountingBooks {
                    id
                    name
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.name = new ExpandoObject();
                variables.filter.name.@operator = "like";
                variables.filter.name.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();
                var results = await PresentationServices.GetDataContext<AccountingPresentationDataContext>(query, variables);
                AccountingPresentations = new ObservableCollection<AccountingPresentationGraphQLModel>(results.AccountingPresentations);
                AccountingBooks = new ObservableCollection<AccountingBookGraphQLModel>(results.AccountingBooks);
              
                IsBusy = false;
            }
            catch
            {
                throw;
            }
        }
        public AccountingPresentationsMasterViewModel(AccountingPresentationsViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
            _ = Task.Run(() => LoadPresentationsAsync());
        }

        public Task HandleAsync(PresentationUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadPresentationsAsync();
        }
        public Task HandleAsync(PresentationCreateMessage message, CancellationToken cancellationToken)
        {
            return LoadPresentationsAsync();
        }
    }
}
