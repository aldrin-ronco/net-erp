using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;

using Microsoft.VisualStudio.Threading;
using Models.Books;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using static Models.Global.GraphQLResponseTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NetErp.Books.AccountingEntries.ViewModels
{
    public class AccountingEntriesDocumentPreviewViewModel : Screen
    {
        private readonly IRepository<AccountingEntryGraphQLModel> _accountingEntryMasterService;
        private readonly IRepository<DraftAccountingEntryGraphQLModel> _draftAccountingEntryService;


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

        private AccountingEntryGraphQLModel _selectedAccountingEntry;
        public AccountingEntryGraphQLModel SelectedAccountingEntry
        {
            get { return _selectedAccountingEntry; }
            set
            {
                if (_selectedAccountingEntry != value)
                {
                    _selectedAccountingEntry = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntry));
                    NotifyOfPropertyChange(nameof(CanEditAccountingEntry));
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
                _cancelAccountingEntryCommand ??= new DelegateCommand(CancelAccountingEntry);
                return _cancelAccountingEntryCommand;
            }
        }

        private ICommand _copyCommand;
        public ICommand CopyCommand
        {
            get
            {
                _copyCommand ??= new DelegateCommand(Copy);
                return _copyCommand;
            }
        }

        private ICommand _printCommand;
        public ICommand PrintCommand
        {
            get
            {
                _printCommand ??= new DelegateCommand(Print);
                return _printCommand;
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
                .Field(e => e.Status)
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

        public AccountingEntriesDocumentPreviewViewModel(AccountingEntriesViewModel context, AccountingEntryGraphQLModel selectedAccountingEntry, IRepository<AccountingEntryGraphQLModel> accountingEntryMasterService, IRepository<DraftAccountingEntryGraphQLModel> accountingEntryDraftMasterService)
        {
            this.Context = context;
            this._accountingEntryMasterService = accountingEntryMasterService;
            this.SelectedAccountingEntry = selectedAccountingEntry;
            this._draftAccountingEntryService = accountingEntryDraftMasterService;
            // Nota: este VM no implementa IHandle<> de ningún mensaje, por lo que
            // NO se suscribe al EventAggregator. Solo publica (cancel/delete/edit→draft).
        }

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync();
            await base.OnInitializedAsync(cancellationToken);
        }

        public void Print()
        {
            ThemedMessageBox.Show("Atención !", "Estamos trabajando en esta implementación", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void Copy()
        {
            ThemedMessageBox.Show("Atención !", "Estamos trabajando en esta implementación", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void CancelAccountingEntry()
        {
            ThemedMessageBox.Show("Atención !", "Estamos trabajando en esta implementación", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteEntryQuery = new(() =>
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var fragment = new GraphQLQueryFragment("deleteAccountingEntry",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        public async Task DeleteAccountingEntryAsync()
        {
            if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea eliminar el comprobante?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

            try
            {
                this.IsBusy = true;
                var (fragment, query) = _deleteEntryQuery.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "id", (int)SelectedAccountingEntry.Id)
                    .Build();

                var result = await _accountingEntryMasterService.DeleteAsync<DeleteResponseType>(query, variables);

                if (!result.Success)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", result.Message, MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }

                await this.Context.EventAggregator.PublishOnUIThreadAsync(new AccountingEntryDeleteMessage { DeletedAccountingEntry = result });
                await this.Context.ActivateMasterViewAsync();
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

        public bool CanDeleteAccountingEntry
        {
            get
            {
                return !this.IsBusy
                       && this.SelectedAccountingEntry.Status is "ACTIVE" or "" or null
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
            catch (Exception ex)
            {
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
                       && this.SelectedAccountingEntry.Status is "ACTIVE" or "" or null
                       && !this.SelectedAccountingEntry.Annulment;
            }
        }

        /// <summary>
        /// Mutation <c>createDraftFromAccountingEntry(input: CreateDraftFromAccountingEntryInput!)</c>.
        /// Genera un borrador editable a partir del comprobante publicado actual.
        /// Solo pide el <c>Id</c> del draft; <see cref="AccountingEntriesViewModel.ActivateDetailViewForEditAsync"/>
        /// recarga el borrador completo con líneas.
        /// </summary>
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createDraftFromEntryQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<DraftAccountingEntryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "draft", nested: sq => sq
                    .Field(e => e.Id))
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("createDraftFromAccountingEntry",
                [new("input", "CreateDraftFromAccountingEntryInput!")],
                fields, "CreateDraftFromAccountingEntryPayload");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        public async Task<DraftAccountingEntryGraphQLModel> ExecuteEditAccountingEntryAsync()
        {
            var (fragment, query) = _createDraftFromEntryQuery.Value;
            object variables = new GraphQLVariables()
                .For(fragment, "input", new { accountingEntryId = (int)this.SelectedAccountingEntry.Id })
                .Build();

            var payload = await this._draftAccountingEntryService
                .CreateAsync<UpsertResponseType<DraftAccountingEntryGraphQLModel>>(query, variables);

            if (!payload.Success)
                throw new Exception(payload.Message);

            return payload.Entity;
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
