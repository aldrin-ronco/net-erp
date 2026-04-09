using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NetErp.Books.AccountingEntries.ViewModels
{
    public class AccountingEntriesDocumentPreviewViewModel : Screen
    {
        private readonly IRepository<AccountingEntryGraphQLModel> _accountingEntryMasterService;
        private readonly IRepository<AccountingEntryDraftGraphQLModel> _accountingEntryDraftMasterService;


        private ObservableCollection<AccountingEntryLineGraphQLModel> _accountingEntries;

        public ObservableCollection<AccountingEntryLineGraphQLModel> AccountingEntries
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

        private AccountingEntryGraphQLModel _selectedAccountingEntryMaster;
        public AccountingEntryGraphQLModel SelectedAccountingEntryMaster
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

        // Totales calculados client-side desde las líneas del comprobante.
        // El schema actual no expone totals en AccountingEntry; se suman al cargar.
        private decimal _totalDebit;
        public decimal TotalDebit
        {
            get => _totalDebit;
            set { if (_totalDebit != value) { _totalDebit = value; NotifyOfPropertyChange(nameof(TotalDebit)); } }
        }

        private decimal _totalCredit;
        public decimal TotalCredit
        {
            get => _totalCredit;
            set { if (_totalCredit != value) { _totalCredit = value; NotifyOfPropertyChange(nameof(TotalCredit)); } }
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



        /// <summary>
        /// Carga el comprobante publicado con sus líneas vía <c>accountingEntry(id: ID!)</c>.
        /// Alineado al schema actual; reemplaza la query legacy <c>accountingEntryMaster(masterId)</c>.
        /// </summary>
        public async Task InitializeAsync()
        {
            var (fragment, query) = _loadAccountingEntryQuery.Value;
            object variables = new GraphQLVariables()
                .For(fragment, "id", (int)SelectedAccountingEntry.Id)
                .Build();

            Stopwatch stopwatch = Stopwatch.StartNew();
            var result = await this._accountingEntryMasterService.FindByIdAsync(query, variables);
            stopwatch.Stop();

            this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            var lines = result?.Lines ?? [];
            this.AccountingEntries = new ObservableCollection<AccountingEntryLineGraphQLModel>(lines);
            this.SelectedAccountingEntryMaster = result;
            this.TotalDebit = lines.Sum(l => l.Debit);
            this.TotalCredit = lines.Sum(l => l.Credit);
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadAccountingEntryQuery = new(() =>
        {
            var fields = FieldSpec<AccountingEntryGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Description)
                .Field(e => e.DocumentDate)
                .Field(e => e.DocumentNumber)
                .Field(e => e.State)
                .Field(e => e.Annulment)
                .Field(e => e.InsertedAt)
                .Select(e => e.AccountingBook, b => b.Field(x => x.Id).Field(x => x.Name))
                .Select(e => e.CostCenter, c => c.Field(x => x.Id).Field(x => x.Name))
                .Select(e => e.AccountingSource, s => s.Field(x => x.Id).Field(x => x.Name))
                .Select(e => e.CreatedBy, u => u.Field(x => x.Id).Field(x => x.FullName))
                .Select(e => e.CancelledBy, u => u.Field(x => x.Id).Field(x => x.FullName))
                .SelectList(e => e.Lines, l => l
                    .Field(x => x.Id)
                    .Field(x => x.RecordDetail)
                    .Field(x => x.Debit)
                    .Field(x => x.Credit)
                    .Field(x => x.Base)
                    .Select(x => x.AccountingAccount, a => a.Field(y => y.Id).Field(y => y.Code).Field(y => y.Name))
                    .Select(x => x.AccountingEntity, a => a.Field(y => y.Id).Field(y => y.IdentificationNumber).Field(y => y.SearchName))
                    .Select(x => x.CostCenter, c => c.Field(y => y.Id).Field(y => y.Name)))
                .Build();

            var fragment = new GraphQLQueryFragment("accountingEntry",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        public AccountingEntriesDocumentPreviewViewModel(AccountingEntriesViewModel context, AccountingEntryMasterDTO selectedAccountingEntry, IRepository<AccountingEntryGraphQLModel> accountingEntryMasterService, IRepository<AccountingEntryDraftGraphQLModel> accountingEntryDraftMasterService)
        {
            this.Context = context;
            this._accountingEntryMasterService = accountingEntryMasterService;
            this.SelectedAccountingEntry = selectedAccountingEntry;
            this._accountingEntryDraftMasterService = accountingEntryDraftMasterService;
            // Nota: este VM no implementa IHandle<> de ningún mensaje, por lo que
            // NO se suscribe al EventAggregator. Solo publica (cancel/delete/edit→draft).
        }

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync();
            await base.OnInitializedAsync(cancellationToken);
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
                await this.Context.EventAggregator.PublishOnUIThreadAsync(new AccountingEntryCancellationMessage() { CancelledAccountingEntry = result });
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
                // TODO (Bloque 10 del refactor): el schema actual NO expone la mutación
                // cancelAccountingEntryMaster. Deshabilitado hasta que el backend la agregue
                // o se defina otro mecanismo de anulación.
                return false;
            }
        }

        public async Task<AccountingEntryGraphQLModel> ExecuteCancelAccountingEntryAsync()
        {
            throw new NotImplementedException(
                "Anulación de comprobantes no está implementada en el schema actual. " +
                "Se aborda en el Bloque 10 del refactor.");
            // Código legacy eliminado: apuntaba a cancelAccountingEntryMaster (no existe).
#pragma warning disable CS0162
            try
            {
                string query = @"
                mutation($data: CancelAccountingEntryMasterInput!) {
                  UpdateResponse: cancelAccountingEntryMaster(data:$data) {
                    id
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
#pragma warning restore CS0162
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
                    await this.Context.EventAggregator.PublishOnUIThreadAsync(new AccountingEntryDeleteMessage() { Id = this.SelectedAccountingEntry.Id });
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

        public Task<int> ExecuteDeleteAccountingEntryAsync()
        {
            // TODO (Bloque 10 del refactor): el schema actual NO expone bulkDeleteAccountingEntryMaster.
            throw new NotImplementedException(
                "Eliminación de comprobantes no está implementada en el schema actual. " +
                "Se aborda en el Bloque 10 del refactor.");
        }

        public bool CanDeleteAccountingEntry
        {
            get
            {
                // TODO (Bloque 10 del refactor): deshabilitado hasta definir flujo en el schema actual.
                return false;
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
                await this.Context.EventAggregator.PublishOnUIThreadAsync(result);

                if (result != null)
                {
                    this.BusyContent = @"Iniciando edición de borrador ...";
                    await this.Context.ActivateDetailViewForEditAsync(result);
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

        public Task<AccountingEntryDraftGraphQLModel> ExecuteEditAccountingEntryAsync()
        {
            // TODO (Bloque 10 del refactor): el schema actual NO expone createAccountingEntryDraftMasterFromMaster
            // ni accountingEntryDraftMaster(draftMasterId). La edición de un comprobante publicado desde
            // el preview queda deshabilitada hasta que el backend exponga el flujo de "re-editar".
            throw new NotImplementedException(
                "Edición de comprobantes publicados (re-generar borrador) no está implementada " +
                "en el schema actual. Se aborda en el Bloque 10 del refactor.");
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
