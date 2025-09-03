using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NetErp.Books.AccountingEntries.ViewModels
{
    public class AccountingEntriesDocumentPreviewViewModel : Screen
    {
        private IEventAggregator _eventAggregator;
        private readonly IRepository<AccountingEntryMasterGraphQLModel> _accountingEntryMasterService;
        private readonly IRepository<AccountingEntryDraftMasterGraphQLModel> _accountingEntryDraftMasterService;


        private ObservableCollection<AccountingEntryDetailGraphQLModel> _accountingEntries;

        public ObservableCollection<AccountingEntryDetailGraphQLModel> AccountingEntries
        {
            get { return _accountingEntries; }
            set
            {
                if (_accountingEntries != value)
                {
                    _accountingEntries = value;
                    NotifyOfPropertyChange(nameof(AccountingEntries));
                }
            }
        }

        public AccountingEntriesViewModel Context { get; set; }

        private AccountingEntryMasterDTO _selectedAccountingEntry;
        public AccountingEntryMasterDTO SelectedAccountingEntry
        {
            get { return _selectedAccountingEntry; }
            set
            {
                if (_selectedAccountingEntry != value)
                {
                    _selectedAccountingEntry = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntry));
                }
            }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanEditAccountingEntry));
                }
            }
        }

        private string _busyContent = string.Empty;
        public string BusyContent
        {
            get { return _busyContent; }
            set
            {
                if (_busyContent != value)
                {
                    _busyContent = value;
                    NotifyOfPropertyChange(nameof(BusyContent));
                }
            }
        }

        private AccountingEntryMasterGraphQLModel _selectedAccountingEntryMaster;
        public AccountingEntryMasterGraphQLModel SelectedAccountingEntryMaster
        {
            get { return _selectedAccountingEntryMaster; }
            set
            {
                if (_selectedAccountingEntryMaster != value)
                {
                    _selectedAccountingEntryMaster = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntryMaster));
                }
            }
        }

        private ICommand _goBackCommand;

        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBackAsync, CanGoBack);
                return _goBackCommand;
            }
        }

        private ICommand _editAccountingEntryCommand;

        public ICommand EditAccountingEntryCommand
        {
            get
            {
                if (_editAccountingEntryCommand is null) _editAccountingEntryCommand = new AsyncCommand(EditAccountingEntryAsync, CanEditAccountingEntry);
                return _editAccountingEntryCommand;
            }
        }

        private ICommand _cancelAccountingEntryCommand;

        public ICommand CancelAccountingEntryCommand
        {
            get
            {
                if (_cancelAccountingEntryCommand is null) _cancelAccountingEntryCommand = new AsyncCommand(CancelAccountingEntry, CanCancelAccountingEntry);
                return _cancelAccountingEntryCommand;
            }
        }

        private ICommand _deleteAccountingEntryCommand;

        public ICommand DeleteAccountingEntryCommand
        {
            get
            {
                if (_deleteAccountingEntryCommand is null) _deleteAccountingEntryCommand = new AsyncCommand(DeleteAccountingEntryAsync, CanDeleteAccountingEntry);
                return _deleteAccountingEntryCommand;
            }
        }



        public async Task InitializeAsync()
        {
            try
            {
                string query = @"
                query($masterId:ID){
                  SingleItemResponse: accountingEntryMaster(masterId: $masterId) {
                    id
                    documentNumber
                    description
                    accountingBook {
                      id
                      name
                    }
                    costCenter {
                      id
                      name
                    }
                    accountingSource {
                      id
                      name
                    }
                    totals {
                      debit
                      credit
                    }
                    state
                    annulment
                    draftMasterId
                    documentDate                
                    createdAt
                    description
                    createdBy
                    cancelledBy
                    accountingEntriesDetail{
                        id    
                        masterId
                        accountingAccount {
                          id
                          code
                          name
                        }
                        accountingEntity {
                          id
                          identificationNumber
                          verificationDigit
                          searchName
                        }
                        costCenter {
                          id
                          name
                        }
                        recordDetail
                        debit
                        credit
                        base
                    }
                    }   
                  }";

                dynamic variables = new ExpandoObject();
                //variables.filter = new ExpandoObject();
                variables.MasterId = SelectedAccountingEntry.Id;
                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                var result = await this._accountingEntryMasterService.FindByIdAsync(query, variables);

                stopwatch.Stop();
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                this.AccountingEntries = new ObservableCollection<AccountingEntryDetailGraphQLModel>(result.AccountingEntriesDetail);
                this.SelectedAccountingEntryMaster = result;
                //this.TotalCount = result.AccountingEntryDetailPage.PageResponse.Count;

            }
            catch (Exception)
            {
                throw;
            }
        }

        public AccountingEntriesDocumentPreviewViewModel(AccountingEntriesViewModel context, AccountingEntryMasterDTO selectedAccountingEntry, IRepository<AccountingEntryMasterGraphQLModel> accountingEntryMasterService, IRepository<AccountingEntryDraftMasterGraphQLModel> accountingEntryDraftMasterService)
        {
            this.Context = context;
            this._accountingEntryMasterService = accountingEntryMasterService;
            this.SelectedAccountingEntry = selectedAccountingEntry;
            this._accountingEntryDraftMasterService = accountingEntryDraftMasterService;

            // Mensajes
            this._eventAggregator = IoC.Get<IEventAggregator>();
            this._eventAggregator.SubscribeOnUIThread(this);

            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await InitializeAsync());
           
        }

        // Print
        public void Print()
        {
            App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", "Esta función aun no está implementada", MessageBoxButton.OK, MessageBoxImage.Information));
        }

        // Copy
        public void Copy()
        {
            App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", "Esta función aun no está implementada", MessageBoxButton.OK, MessageBoxImage.Information));
        }

        // Cancel Accounting Entry
        public async Task CancelAccountingEntry()
        {
            if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea anular el comprobante?", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No) return;

            try
            {
                this.IsBusy = true;
                this.Refresh();
                var result = await ExecuteCancelAccountingEntryAsync();
                await this.Context.EventAggregator.PublishOnUIThreadAsync(new AccountingEntryMasterCancellationMessage() { CancelledAccountingEntry = result });
                await this.Context.ActivateMasterViewAsync();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (ArgumentException ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool CanCancelAccountingEntry
        {
            get
            {
                // Puedo anular el documento si, y solo si ...
                // El sistema no esta procesando otra tarea
                // Este documento no tenga un borrador
                // Este documento no esté ya anulado
                // El documento no sea un documento de anulacion
                return !this.IsBusy && (this.SelectedAccountingEntry.DraftMasterId is null)
                    && string.IsNullOrEmpty(this.SelectedAccountingEntry.State.Trim())
                    && !this.SelectedAccountingEntry.Annulment;
            }
        }

        public async Task<AccountingEntryMasterGraphQLModel> ExecuteCancelAccountingEntryAsync()
        {
            try
            {
                string query = @"
                mutation($data: CancelAccountingEntryMasterInput!) {
                  UpdateResponse: cancelAccountingEntryMaster(data:$data) {
                    id
                    draftMasterId
                    documentNumber
                    accountingBook {
                      id
                      name
                    }
                    costCenter {
                      id
                      name
                    }
                    accountingSource {
                      id
                      name
                    }
                    documentDate
                    documentTime
                    description
                    createdBy
                    createdAt
                    annulment
                    state    
                  }
                }";

                object variables = new
                {
                    Data = new
                    {
                        MasterId = this.SelectedAccountingEntry.Id,
                        CancelledBy = SessionInfo.UserEmail
                    }
                };

                var result = await this._accountingEntryMasterService.UpdateAsync(query, variables);

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // Delete Accounting Entry
        public async Task DeleteAccountingEntryAsync()
        {
            if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea eliminar el comprobante?", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No) return;

            try
            {
                this.IsBusy = true;
                this.Refresh();

                var deletedRecord = await Task.Run(() => this.ExecuteDeleteAccountingEntryAsync());

                if (deletedRecord > 0)
                {
                    // Notificar la eliminacion del registro
                    await this.Context.EventAggregator.PublishOnUIThreadAsync(new AccountingEntryMasterDeleteMessage() { Id = this.SelectedAccountingEntry.Id });
                    await this.Context.ActivateMasterViewAsync();
                }
                else
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", "El registro no ha sido eliminado", MessageBoxButton.OK, MessageBoxImage.Error));
                }
                this.IsBusy = false;
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task<int> ExecuteDeleteAccountingEntryAsync()
        {
            try
            {
                string query = @"
                    mutation($masterIds:[ID!]!) {
                    bulkDeleteAccountingEntryMaster(masterIds:$masterIds) {
                    count
                    }
                }";
                object variables = new
                {
                    MasterIds = new List<BigInteger>() { this.SelectedAccountingEntry.Id }
                };
                var result = await this._accountingEntryMasterService.GetDataContextAsync<BulkDeleteAccountingEntryMaster>(query, variables);
                return result.Count;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool CanDeleteAccountingEntry
        {
            get
            {
                // Puedo eliminar el documento si, y solo si ...
                // El sistema no esta procesando otra tarea
                // Este documento no tenga un borrador
                // Este documento no esté ya anulado
                return !this.IsBusy
                       && this.SelectedAccountingEntry.DraftMasterId is null
                       && string.IsNullOrEmpty(this.SelectedAccountingEntry.State.Trim())
                       && !this.SelectedAccountingEntry.Annulment;
            }
        }

        // Back Button
        public async Task GoBackAsync()
        {
            await this.Context.ActivateMasterViewAsync();
        }

        public bool CanGoBack => true;

        // Edit Accounting Entry
        public async Task EditAccountingEntryAsync()
        {
            try
            {
                this.BusyContent = @"Generando borrador, por favor espere ...";
                this.IsBusy = true;

                this.Refresh();
                var result = await this.ExecuteEditAccountingEntryAsync();

                // Informar a la vista de que la entrada ahora tiene un draft
                await this._eventAggregator.PublishOnUIThreadAsync(result);

                if (result != null)
                {
                    this.BusyContent = @"Iniciando edición de borrador ...";
                    await this.Context.ActivateDetailViewForEdit(result);
                }
                else
                {
                    // Mensaje de error
                }
                this.BusyContent = "";
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                this.IsBusy = false;
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        public bool CanEditAccountingEntry
        {
            get
            {
                return !this.IsBusy
                       && string.IsNullOrEmpty(this.SelectedAccountingEntry.State.Trim())
                       && !this.SelectedAccountingEntry.Annulment;
            }
        }

        public async Task<AccountingEntryDraftMasterGraphQLModel> ExecuteEditAccountingEntryAsync()
        {
            string query = "";
            object variables;

            try
            {
                if (this.SelectedAccountingEntry.DraftMasterId is null)
                {
                    query = @"
                    mutation($masterId: ID!) {
                      CreateResponse: createAccountingEntryDraftMasterFromMaster(masterId:$masterId) {
                        id
                        masterId
                        accountingBook {
                          id
                          name
                        }
                        costCenter {
                          id
                          name
                        }
                        accountingSource {
                          id
                          name
                        }
                        documentDate
                        documentNumber    
                        createdAt
                        description
                        createdBy    
                      }
                    }";
                    variables = new
                    {
                        MasterId = this.SelectedAccountingEntryMaster.Id
                    };
                    var result = await this._accountingEntryDraftMasterService.CreateAsync(query, variables);
                    return result;
                }
                else
                {
                    query = @"
                query($draftMasterId:ID!) {
                  SingleItemResponse: accountingEntryDraftMaster(draftMasterId:$draftMasterId) {
                    id
                    masterId
                    accountingBook {
                      id
                      name
                    }
                    costCenter {
                      id
                      name
                    }
                    accountingSource {
                      id
                      name
                    }
                    documentNumber
                    documentDate
                    createdAt
                    description
                    createdBy    
                    }
                    }";
                    variables = new
                    {
                        this.SelectedAccountingEntry.DraftMasterId
                    };

                    var result = await this._accountingEntryDraftMasterService.FindByIdAsync(query, variables);
                    return result;
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        #region Paginacion Comprobantes
        /// <summary>
        /// PageIndex
        /// </summary>
        private int _pageIndex = 1; // DefaultPageIndex = 1
        public int PageIndex
        {
            get { return _pageIndex; }
            set
            {
                if (_pageIndex != value)
                {
                    _pageIndex = value;
                    NotifyOfPropertyChange(() => PageIndex);
                }
            }
        }
        /// <summary>
        /// PageSize
        /// </summary>
        private int _pageSize = 50; // Default PageSize 50
        public int PageSize
        {
            get { return _pageSize; }
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    NotifyOfPropertyChange(() => PageSize);
                }
            }
        }
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
                    NotifyOfPropertyChange(() => TotalCount);
                }
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
        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) this._paginationCommand = new RelayCommand(CanExecutePaginationChangeIndex, ExecutePaginationChangeIndex);
                return _paginationCommand;
            }
        }
        private void ExecutePaginationChangeIndex(object parameter)
        {
            Task.Run(() => 0);
        }
        private bool CanExecutePaginationChangeIndex(object parameter)
        {
            return true;
        }
        #endregion
    }
}
