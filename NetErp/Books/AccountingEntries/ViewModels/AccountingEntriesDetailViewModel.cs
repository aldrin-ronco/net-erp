using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Data.Filtering;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.XtraEditors.Controls;
using Extensions.Books;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Billing.PriceList.ViewModels;
using NetErp.Books.AccountingEntries.DTO;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static DevExpress.Data.Utils.SafeProcess;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingEntries.ViewModels
{
    public class AccountingEntriesDetailViewModel : Screen,
        INotifyDataErrorInfo,
        IHandle<AccountingAccountUpdateMessage>,
        IHandle<AccountingAccountCreateListMessage>,
        IHandle<AccountingAccountDeleteMessage>,
        IHandle<CostCenterCreateMessage>
    {
        private readonly IGraphQLClient _graphQLClient;
        private readonly CostCenterCache _costCenterCache;
        private readonly AccountingBookCache _accountingBookCache;
        private readonly NotAnnulledAccountingSourceCache _notAnnulledAccountingSourceCache;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;

        Dictionary<string, List<string>> _errors = [];
        private readonly Helpers.Services.INotificationService _notificationService = IoC.Get<Helpers.Services.INotificationService>();
        private readonly IRepository<AccountingEntryGraphQLModel> _accountingEntryMasterService;
        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;
        private readonly IRepository<AccountingEntryDraftGraphQLModel> _accountingEntryDraftMasterService; 
        private readonly IRepository<AccountingEntryDraftDetailGraphQLModel> _accountingEntryDraftDetailService;

        


        #region Propiedades

        // Context
        public AccountingEntriesViewModel Context { get; set; }

        // StringLength constraints (backed by StringLengthCache via el Conductor).
        // MaxLength = 0 en DevExpress TextEdit significa sin límite, por lo que si el
        // cache no tiene la entrada aún, el binding no restringe la entrada.
        public int DescriptionMaxLength => Context.StringLengthCache.GetMaxLength<AccountingEntryDraftGraphQLModel>(nameof(AccountingEntryDraftGraphQLModel.Description));
        public int RecordDetailMaxLength => Context.StringLengthCache.GetMaxLength<AccountingEntryDraftDetailGraphQLModel>(nameof(AccountingEntryDraftDetailGraphQLModel.RecordDetail));

        // Parent record reference
        public AccountingEntryDraftGraphQLModel SelectedAccountingEntryDraftMaster { get; set; } = null;

        // Accounting Entries
        private ObservableCollection<AccountingEntryDraftDetailDTO> _accountingEntries;
        public ObservableCollection<AccountingEntryDraftDetailDTO> AccountingEntries
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
        // Cuentas Contables
        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccounts;
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts
        {
            get { return _accountingAccounts; }
            set
            {
                if (_accountingAccounts != value)
                {
                    _accountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                }
            }
        }
        //CostCenters
        private ObservableCollection<CostCenterGraphQLModel> _costCenters;
        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get { return _costCenters; }
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }
        // Libros contables
        private ObservableCollection<AccountingBookGraphQLModel> _accountingBooks;
        public ObservableCollection<AccountingBookGraphQLModel> AccountingBooks
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
        // AccountingSources
        private ObservableCollection<AccountingSourceGraphQLModel> _accountingSources;
        public ObservableCollection<AccountingSourceGraphQLModel> AccountingSources
        {
            get { return _accountingSources; }
            set
            {
                if (_accountingSources != value)
                {
                    _accountingSources = value;
                    NotifyOfPropertyChange(nameof(AccountingSources));
                }
            }
        }
        // Selected Accounting Entry
        private AccountingEntryDraftDetailDTO _selectedAccountingEntryDraftDetail;
        public AccountingEntryDraftDetailDTO SelectedAccountingEntryDraftDetail
        {
            get { return _selectedAccountingEntryDraftDetail; }
            set
            {
                if (this._selectedAccountingEntryDraftDetail != value)
                {
                    _selectedAccountingEntryDraftDetail = value;
                    NotifyOfPropertyChange(nameof(this.SelectedAccountingEntryDraftDetail));
                }
            }
        }

        // Draft Master Id
        private BigInteger _draftMasterId = 0;
        public BigInteger DraftMasterId
        {
            get { return _draftMasterId; }
            set
            {
                if (_draftMasterId != value)
                {
                    _draftMasterId = value;
                    NotifyOfPropertyChange(nameof(DraftMasterId));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        // Resultados de busqueda de terceros
        private ObservableCollection<AccountingEntityGraphQLModel> _accountingEntitiesSearchResults = new ObservableCollection<AccountingEntityGraphQLModel>();
        public ObservableCollection<AccountingEntityGraphQLModel> AccountingEntitiesSearchResults
        {
            get { return _accountingEntitiesSearchResults; }
            set
            {
                if (_accountingEntitiesSearchResults != value)
                {
                    _accountingEntitiesSearchResults = value;
                    NotifyOfPropertyChange(nameof(AccountingEntitiesSearchResults));
                }
            }
        }

        // IsBusy
        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                    NotifyOfPropertyChange(nameof(CanSearchForAccountingEntityMatch));
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        // Filtro de busqueda de tercero
        private string _filterSearchAccountingEntity = "";
        public string FilterSearchAccountingEntity
        {
            get { return _filterSearchAccountingEntity; }
            set
            {
                if (_filterSearchAccountingEntity != value)
                {
                    _filterSearchAccountingEntity = value;
                    NotifyOfPropertyChange(nameof(FilterSearchAccountingEntity));
                    NotifyOfPropertyChange(nameof(CanSearchForAccountingEntityMatch));
                }
            }
        }

        // Estado de filtro de tercero
        private bool _isFilterSearchAccountinEntityOnEditMode = true;
        public bool IsFilterSearchAccountinEntityOnEditMode
        {
            get { return _isFilterSearchAccountinEntityOnEditMode; }
            set
            {
                if (_isFilterSearchAccountinEntityOnEditMode != value)
                {
                    _isFilterSearchAccountinEntityOnEditMode = value;
                    NotifyOfPropertyChange(nameof(IsFilterSearchAccountinEntityOnEditMode));
                    NotifyOfPropertyChange(nameof(CanSearchForAccountingEntityMatch));
                }
            }
        }

        // Document Date
        private DateTime? _documentDate = DateTime.Now;
        public DateTime? DocumentDate
        {
            get { return _documentDate; }
            set
            {
                if (_documentDate != value)
                {
                    _documentDate = value;
                    NotifyOfPropertyChange(nameof(DocumentDate));
                    this.TrackChange(nameof(DocumentDate));
                    ValidateProperty(nameof(DocumentDate), null);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                    NotifyOfPropertyChange(nameof(CanPublishAccountingEntry));
                }
            }
        }

        // Descrcipcion del comprobante
        private string _description = string.Empty;
        public string Description
        {
            get { return _description; }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    NotifyOfPropertyChange(nameof(Description));
                    this.TrackChange(nameof(Description));
                    ValidateProperty(nameof(Description), value, 0);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                    NotifyOfPropertyChange(nameof(CanPublishAccountingEntry));
                }
            }
        }

        // Detalle del registro
        private string _recordDetail = string.Empty;
        public string RecordDetail
        {
            get { return _recordDetail; }
            set
            {
                if (_recordDetail != value)
                {
                    _recordDetail = value;
                    NotifyOfPropertyChange(nameof(RecordDetail));
                    ValidateProperty(nameof(RecordDetail), value, 0);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                }
            }
        }

        // Debito
        private decimal _debit = 0;
        public decimal Debit
        {
            get { return _debit; }
            set
            {
                if (_debit != value)
                {
                    _debit = value;
                    NotifyOfPropertyChange(nameof(Debit));
                    ValidateProperty(nameof(Debit), null, 0, value);
                    ValidateProperty(nameof(Credit), null, 0, this.Credit);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                    NotifyOfPropertyChange(nameof(IsEnableCredit));
                    NotifyOfPropertyChange(nameof(IsEnableDebit));
                }
            }
        }

        // Is Enable Debit
        public bool IsEnableDebit
        {
            get { return this.Credit == 0; }
        }

        // Credit
        private decimal _credit = 0;
        public decimal Credit
        {
            get { return _credit; }
            set
            {
                if (_credit != value)
                {
                    _credit = value;
                    NotifyOfPropertyChange(nameof(Credit));
                    ValidateProperty(nameof(Credit), null, 0, value);
                    ValidateProperty(nameof(Debit), null, 0, this.Debit);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                    NotifyOfPropertyChange(nameof(IsEnableCredit));
                    NotifyOfPropertyChange(nameof(IsEnableDebit));
                }
            }
        }

        // Is Enable Credit
        public bool IsEnableCredit { get { return this.Debit == 0; } }

        // Base
        private decimal _base = 0;
        public decimal Base
        {
            get { return _base; }
            set
            {
                if (_base != value)
                {
                    _base = value;
                    NotifyOfPropertyChange(nameof(Base));
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                }
            }
        }

        // IsNewRcord
        public bool IsNewRecord
        {
            get { return this.DraftMasterId == 0; }
        }

        // IsEditingRecord
        public bool IsEditingRecord
        {
            get { return this.DraftMasterId > 0; }
        }

        // Habilitar Search Entity
        public bool CanSearchForAccountingEntityMatch
        {
            get
            {
                if (this.IsFilterSearchAccountinEntityOnEditMode)
                {
                    return (this.FilterSearchAccountingEntity.Trim().Length >= 3) && !this.IsBusy;
                }
                else
                {
                    return true;
                }
            }
        }

        // Total Debito
        private Decimal _totalDebit = 0;
        public Decimal TotalDebit
        {
            get { return _totalDebit; }
            set
            {
                if (_totalDebit != value)
                {
                    _totalDebit = value;
                    NotifyOfPropertyChange(nameof(TotalDebit));
                    NotifyOfPropertyChange(nameof(TotalDiference));
                    NotifyOfPropertyChange(nameof(CanPublishAccountingEntry));
                }
            }
        }

        // Total Credit
        private decimal _totalCredit;
        public decimal TotalCredit
        {
            get { return _totalCredit; }
            set
            {
                if (_totalCredit != value)
                {
                    _totalCredit = value;
                    NotifyOfPropertyChange(nameof(TotalCredit));
                    NotifyOfPropertyChange(nameof(TotalDiference));
                    NotifyOfPropertyChange(nameof(CanPublishAccountingEntry));
                }
            }
        }

        public decimal TotalDiference { get { return this._totalDebit - this._totalCredit; } }

        #endregion

        #region Selected Items

        // Selected Accounting Book
        private int _selectedAccountingBookId;
        [ExpandoPath("accountingBookId")]
        public int SelectedAccountingBookId
        {
            get { return _selectedAccountingBookId; }
            set
            {
                if (_selectedAccountingBookId != value)
                {
                    _selectedAccountingBookId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingBookId));
                    this.TrackChange(nameof(SelectedAccountingBookId));
                    ValidateProperty(nameof(SelectedAccountingBookId), null, value);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                    NotifyOfPropertyChange(nameof(CanPublishAccountingEntry));
                }
            }
        }

        private int _selectedCostCenterId = 0;
        [ExpandoPath("costCenterId")]
        public int SelectedCostCenterId
        {
            get { return _selectedCostCenterId; }
            set
            {
                if (_selectedCostCenterId != value)
                {
                    _selectedCostCenterId = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenterId));
                    this.TrackChange(nameof(SelectedCostCenterId));
                    ValidateProperty(nameof(SelectedCostCenterId), null, value);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                    NotifyOfPropertyChange(nameof(CanPublishAccountingEntry));
                }
            }
        }

        private int _selectedCostCenterOnEntryId = 0;
        public int SelectedCostCenterOnEntryId
        {
            get { return _selectedCostCenterOnEntryId; }
            set
            {
                if (_selectedCostCenterOnEntryId != value)
                {
                    _selectedCostCenterOnEntryId = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenterOnEntryId));
                    ValidateProperty(nameof(SelectedCostCenterOnEntryId), null, value);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                }
            }
        }

        // Cuenta Contable
        private int _selectedAccountingAccountOnEntryId = 0;
        public int SelectedAccountingAccountOnEntryId
        {
            get { return _selectedAccountingAccountOnEntryId; }
            set
            {
                if (_selectedAccountingAccountOnEntryId != value)
                {
                    _selectedAccountingAccountOnEntryId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingAccountOnEntryId));
                    ValidateProperty(nameof(SelectedAccountingAccountOnEntryId), null, value);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                }
            }
        }

        // Tercero
        private int _selectedAccountingEntityOnEntryId;
        public int SelectedAccountingEntityOnEntryId
        {
            get { return _selectedAccountingEntityOnEntryId; }
            set
            {
                if (_selectedAccountingEntityOnEntryId != value)
                {
                    _selectedAccountingEntityOnEntryId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntityOnEntryId));
                    ValidateProperty(nameof(SelectedAccountingEntityOnEntryId), null, value);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                }

            }
        }

        // Selected Accounting Source
        private int _selectedAccountingSourceId = 0;
        [ExpandoPath("accountingSourceId")]
        public int SelectedAccountingSourceId
        {
            get { return _selectedAccountingSourceId; }
            set
            {
                if (_selectedAccountingSourceId != value)
                {
                    _selectedAccountingSourceId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingSourceId));
                    this.TrackChange(nameof(SelectedAccountingSourceId));
                    ValidateProperty(nameof(SelectedAccountingSourceId), null, value);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                    NotifyOfPropertyChange(nameof(CanPublishAccountingEntry));
                }
            }
        }

        private ICommand _publishAccountingEntryCommand;

        public ICommand PublishAccountingEntryCommand
        {
            get
            {
                if (_publishAccountingEntryCommand is null) _publishAccountingEntryCommand = new AsyncCommand(PublishAccountingEntryAsync, CanPublishAccountingEntry);
                return _publishAccountingEntryCommand;
            }
        }


        private ICommand _deleteAccountingEntriesCommand;

        public ICommand DeleteAccountingEntriesCommand
        {
            get
            {
                if (_deleteAccountingEntriesCommand is null) _deleteAccountingEntriesCommand = new AsyncCommand(DeleteAccountingEntriesAsync, CanDeleteAccountingEntries);
                return _deleteAccountingEntriesCommand;
            }
        }

        #endregion

        #region Metodos

        public async Task PublishAccountingEntryAsync()
        {
            try
            {

                if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea finalizar la edición del comprobante?", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No) return;

                this.IsBusy = true;
                this.Refresh();

                // Si hay cambios pendientes en el header del borrador, persistirlos antes
                // de finalizar. Solo aplica cuando el borrador ya existe (DraftMasterId > 0);
                // si es un borrador nuevo (DraftMasterId == 0) no hay nada que actualizar
                // porque los datos del header se enviarán dentro de createAccountingEntryDraft
                // junto con la primera línea — ese flujo no aplica aquí porque PublishAccountingEntry
                // requiere un borrador ya creado.
                if (DraftMasterId != 0 && this.HasChanges())
                {
                    await UpdateDraftHeaderAsync();
                }

                var createdEntry = await this.ExecutePublishAccountingEntryAsync();
                await this.Context.EventAggregator.PublishOnUIThreadAsync(createdEntry);
                if (this.DraftMasterId != 0) await this.Context.EventAggregator.PublishOnUIThreadAsync(new AccountingEntryDraftMasterDeleteMessage { Id = this.DraftMasterId });
                if (createdEntry != null)
                {
                    await this.Context.ActivateMasterViewAsync();
                }

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
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

        public async Task<AccountingEntryGraphQLModel> ExecutePublishAccountingEntryAsync()
        {
            AccountingEntryGraphQLModel? entry = null;
            object variables = new
            {
                this.DraftMasterId
            };

            try
            {
                if (this.SelectedAccountingEntryDraftMaster is null || this.SelectedAccountingEntryDraftMaster.MasterId is null)
                {
                    string query = @"
                    mutation($draftMasterId:ID!) {
                      CreateResponse: finalize_accounting_entry_draft(draftId:$draftMasterId) {
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
                        createdAt
                        createdBy
                        description    
                      }
                    }";
                    entry = await this._accountingEntryMasterService.CreateAsync(query, variables);
                }
                else
                {
                    string query = @"
                    mutation($draftMasterId:ID!) {
                      UpdateResponse: finalize_accounting_entry_draft (draftId:$draftMasterId) {
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
                        description
                        createdBy
                        createdAt
                      }
                    }";
                    entry = await this._accountingEntryMasterService.UpdateAsync(query, variables);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return entry;
        }

        public bool CanPublishAccountingEntry
        {
            get
            {
                // Domain rules: las líneas deben cuadrar y haber al menos un crédito.
                // Adicional: el header no debe tener errores de validación pendientes
                // (descripción vacía, libro/fuente sin seleccionar, etc.).
                return (TotalDiference == 0 && TotalCredit > 0) && !this.HasErrors;
            }
        }

        // TODO: refactor pendiente — edición inline por celda deshabilitada.
        // Este método persistía cada celda inmediatamente al API. Requiere análisis de UX e
        // integridad transaccional antes de re-habilitarse (mismo patrón que el auto-save de
        // DocumentDate/Description en los setters del header). Se conserva el código completo
        // como referencia y se reactivará en una iteración posterior del refactor.
        // El binding en AccountingEntriesDetailView.xaml también está comentado.
        //public async Task EndRowEditingAsync()
        //{
        //    try
        //    {
        //        IsBusy = true;
        //        string query = @"
        //        mutation($data:UpdateAccountingEntryDraftDetailInput!, $id:Int!) {
        //          UpdateResponse: updateAccountingEntryDraftDetail(data:$data, id:$id) {
        //            id
        //            draftMasterId
        //            accountingAccount {
        //              id
        //              code
        //              name
        //            }
        //            accountingEntity {
        //              id
        //              identificationNumber
        //              verificationDigit
        //              businessName
        //              searchName
        //            }
        //            costCenter {
        //              id
        //              name
        //            }
        //            recordDetail
        //            debit
        //            credit
        //            base
        //          }
        //        }";
        //
        //
        //        dynamic variables = new ExpandoObject();
        //        variables.Data = new ExpandoObject();
        //        variables.Id = SelectedAccountingEntryDraftDetail.Id;
        //        variables.Data.AccountingAccountId = SelectedAccountingEntryDraftDetail.AccountingAccount.Id;
        //        variables.Data.AccountingEntityId = SelectedAccountingEntryDraftDetail.AccountingEntity.Id;
        //        variables.Data.CostCenterId = SelectedAccountingEntryDraftDetail.CostCenter.Id;
        //        variables.Data.RecordDetail = SelectedAccountingEntryDraftDetail.RecordDetail;
        //        variables.Data.Debit = SelectedAccountingEntryDraftDetail.Debit;
        //        variables.Data.Credit = SelectedAccountingEntryDraftDetail.Credit;
        //        variables.Data.Base = SelectedAccountingEntryDraftDetail.Base;
        //
        //        AccountingEntryDraftDetailDTO updatedEntry = Context.Mapper.Map<AccountingEntryDraftDetailDTO>(await this._accountingEntryDraftDetailService.UpdateAsync(query, variables)) ?? throw new Exception("No se pudo actualizar el registro");
        //        // Actualizo el registro en la lista
        //        var entryToUpdate = this.AccountingEntries.FirstOrDefault(x => x.Id == updatedEntry.Id);
        //        if (entryToUpdate != null)
        //        {
        //            entryToUpdate.AccountingAccount = updatedEntry.AccountingAccount;
        //            entryToUpdate.AccountingEntity = updatedEntry.AccountingEntity;
        //            entryToUpdate.CostCenter = updatedEntry.CostCenter;
        //            entryToUpdate.RecordDetail = updatedEntry.RecordDetail;
        //            entryToUpdate.Debit = updatedEntry.Debit;
        //            entryToUpdate.Credit = updatedEntry.Credit;
        //            entryToUpdate.Base = updatedEntry.Base;
        //        }
        //        // Totals
        //        query = @"
        //            query($draftMasterId:ID!){
        //                accountingEntryDraftTotals(draftMasterId:$draftMasterId) {
        //                debit
        //                credit
        //                }
        //            }";
        //        variables = new
        //        {
        //            this.DraftMasterId
        //        };
        //        var result = await this._accountingEntryMasterService.GetDataContextAsync<AccountingEntriesDraftDetailDataContext>(query, variables);
        //        this.TotalCredit = result.AccountingEntryDraftTotals.Credit;
        //        this.TotalDebit = result.AccountingEntryDraftTotals.Debit;
        //        this.IsBusy = false;
        //        _notificationService.ShowSuccess("Actualización exitosa");
        //    }
        //    catch (Exception ex)
        //    {
        //        App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
        //    }
        //    finally
        //    {
        //        IsBusy = false;
        //    }
        //}

        public bool CanDeleteAccountingEntries
        {
            get
            {
                if (this.AccountingEntries == null) return false;
                BigInteger[] ids = (from e in this.AccountingEntries
                                    where e.IsChecked
                                    select e.Id).ToArray();
                return ids.Length > 0;
            }
        }

        public void OnChecked()
        {
            NotifyOfPropertyChange(() => CanDeleteAccountingEntries);
        }

        public void OnUnchecked()
        {
            NotifyOfPropertyChange(() => CanDeleteAccountingEntries);
        }

        public async Task DeleteAccountingEntriesAsync()
        {
            try
            {
                if (ThemedMessageBox.Show("Atención !", "¿ Confirma que desea eliminar los registros seleccionados ?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

                this.IsBusy = true;
                this.Refresh();

                IEnumerable<BigInteger> ids = from e in this.AccountingEntries
                                              where e.IsChecked
                                              select e.Id;

                string query = @"
                mutation($ids: [Int!]!){
                  Data: deleteListAccountingEntryDraftDetail(ids:$ids){
                    success
                    message
                  }
                }  ";

                object variables = new
                {
                    Ids = ids
                };

                var response = await this._accountingEntryDraftDetailService.MutationContextAsync<SuccessResponseDataWrapper>(query, variables);

                if(!response.Data.Success)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{response.Data.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }
                // Totals
                query = @"
                    query($draftMasterId:ID!){
                        accountingEntryDraftTotals(draftMasterId:$draftMasterId) {
                        debit
                        credit
                        }
                    }";

                variables = new
                {
                    this.DraftMasterId
                };

                var result = await this._accountingEntryMasterService.GetDataContextAsync<AccountingEntriesDraftDetailDataContext>(query, variables);

                this.IsBusy = false;

                this.TotalCredit = result.AccountingEntryDraftTotals.Credit;
                this.TotalDebit = result.AccountingEntryDraftTotals.Debit;

                var itemsToDelete = this.AccountingEntries.Where(x => x.IsChecked).ToList();

                foreach (var item in itemsToDelete)
                {
                    this.AccountingEntries.Remove(item); // Queda asi o debo llamar a la pagina ? quien sabe ....
                }
                NotifyOfPropertyChange(nameof(CanDeleteAccountingEntries));
                _notificationService.ShowSuccess("Eliminación exitosa");
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void CleanEntry()
        {
            try
            {
                this.SelectedAccountingAccountOnEntryId = 0;
                this.Debit = 0;
                this.Credit = 0;
                this.Base = 0;
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

       


        public Task InitializeAsync()
        {
            // Los caches ya fueron cargados centralizadamente por el Conductor en
            // OnInitializedAsync vía CacheBatchLoader. Aquí solo se materializan las
            // colecciones locales que la vista bindea.
            // La selección por defecto la maneja SetForNew/SetForEdit, llamados por el Conductor
            // ANTES de ActivateItemAsync (siguiendo el patrón estándar Seller/Customer).
            CostCenters = [.. _costCenterCache.Items];
            this.CostCenters.Insert(0, new CostCenterGraphQLModel() { Id = 0, Name = "SELECCIONE CENTRO DE COSTO" });
            this.AccountingBooks = [.. _accountingBookCache.Items];
            this.AccountingSources = [.. _notAnnulledAccountingSourceCache.Items];
            this.AccountingSources.Insert(0, new AccountingSourceGraphQLModel() { Id = 0, Name = "SELECCIONE FUENTE CONTABLE" });
            this.AccountingAccounts = new ObservableCollection<AccountingAccountGraphQLModel>(_auxiliaryAccountingAccountCache.Items);
            return Task.CompletedTask;
        }

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync();
            await base.OnInitializedAsync(cancellationToken);
        }

        /// <summary>
        /// Inicializa el ViewModel para crear un nuevo borrador de comprobante contable.
        /// Llamado por el Conductor ANTES de ActivateItemAsync. Asume que los caches ya
        /// fueron cargados por el Master (CacheBatchLoader en Master.OnInitializedAsync).
        /// </summary>
        public void SetForNew()
        {
            // Header
            SelectedAccountingEntryDraftMaster = null;
            DraftMasterId = 0;
            SelectedAccountingBookId = _accountingBookCache.Items.FirstOrDefault()?.Id ?? 0;
            SelectedCostCenterId = _costCenterCache.Items.FirstOrDefault()?.Id ?? 0;
            SelectedAccountingSourceId = _notAnnulledAccountingSourceCache.Items.FirstOrDefault()?.Id ?? 0;
            SelectedCostCenterOnEntryId = _costCenterCache.Items.FirstOrDefault()?.Id ?? 0;
            AccountingEntries = [];
            EntriesPageIndex = 1;
            EntriesPageSize = 50;
            EntriesTotalCount = 0;
            EntriesResponseTime = "";
            TotalDebit = 0;
            TotalCredit = 0;
            DocumentDate = DateTime.Now.Date;
            Description = "";

            // Entry Point (formulario de captura de líneas)
            SelectedAccountingAccountOnEntryId = 0;
            SelectedAccountingEntityOnEntryId = 0;
            RecordDetail = "";
            Debit = 0;
            Credit = 0;
            Base = 0;
            IsFilterSearchAccountinEntityOnEditMode = true;
            FilterSearchAccountingEntity = "";

            SeedDefaultValues();
        }

        /// <summary>
        /// Inicializa el ViewModel para editar un borrador existente.
        /// Llamado por el Conductor ANTES de ActivateItemAsync, después de cargar las líneas
        /// y los totales del borrador.
        /// </summary>
        public void SetForEdit(AccountingEntryDraftGraphQLModel model,
                               IEnumerable<AccountingEntryDraftDetailDTO> entries,
                               decimal totalDebit,
                               decimal totalCredit,
                               string responseTime)
        {
            // Header
            SelectedAccountingEntryDraftMaster = model;
            DraftMasterId = model.Id;
            SelectedAccountingBookId = model.AccountingBook.Id;
            SelectedCostCenterId = model.CostCenter.Id;
            SelectedAccountingSourceId = model.AccountingSource.Id;
            DocumentDate = model.DocumentDate;
            Description = model.Description;

            // Líneas y totales
            AccountingEntries = new ObservableCollection<AccountingEntryDraftDetailDTO>(entries);
            TotalDebit = totalDebit;
            TotalCredit = totalCredit;
            EntriesResponseTime = responseTime;

            // Entry Point (formulario de captura de líneas)
            SelectedAccountingAccountOnEntryId = 0;
            SelectedAccountingEntityOnEntryId = 0;
            SelectedCostCenterOnEntryId = 0;
            RecordDetail = "";
            Debit = 0;
            Credit = 0;
            Base = 0;
            IsFilterSearchAccountinEntityOnEditMode = true;
            FilterSearchAccountingEntity = "";

            SeedCurrentValues();
        }

        /// <summary>
        /// Para CREATE: limpia seeds previos, siembra solo defaults significativos y acepta cambios.
        /// Para los borradores, los defaults son las selecciones iniciales del header.
        /// </summary>
        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(SelectedAccountingBookId), SelectedAccountingBookId);
            this.SeedValue(nameof(SelectedCostCenterId), SelectedCostCenterId);
            this.SeedValue(nameof(SelectedAccountingSourceId), SelectedAccountingSourceId);
            this.SeedValue(nameof(DocumentDate), DocumentDate);
            this.SeedValue(nameof(Description), Description);
            this.AcceptChanges();
        }

        /// <summary>
        /// Para EDIT: siembra TODOS los campos editables del header con su valor actual del borrador.
        /// Sin ClearSeeds aquí: AcceptChanges al final limpia el HashSet de cambios pero conserva los seeds.
        /// </summary>
        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(SelectedAccountingBookId), SelectedAccountingBookId);
            this.SeedValue(nameof(SelectedCostCenterId), SelectedCostCenterId);
            this.SeedValue(nameof(SelectedAccountingSourceId), SelectedAccountingSourceId);
            this.SeedValue(nameof(DocumentDate), DocumentDate);
            this.SeedValue(nameof(Description), Description);
            this.AcceptChanges();
        }

        public AccountingEntriesDetailViewModel(AccountingEntriesViewModel context,
            IRepository<AccountingEntryGraphQLModel>accountingEntryMasterService,
            IRepository<AccountingEntityGraphQLModel> accountingEntityService,
            IRepository<AccountingEntryDraftGraphQLModel> accountingEntryDraftMasterService,
            IRepository<AccountingEntryDraftDetailGraphQLModel> accountingEntryDraftDetailService,
             CostCenterCache costCenterCache,
             AccountingBookCache accountingBookCache,
             NotAnnulledAccountingSourceCache notAnnulledAccountingSourceCache,
             AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
             IGraphQLClient graphQLClient)
        {
            this.Context = context;
            _costCenterCache = costCenterCache;
            _accountingBookCache = accountingBookCache;
            _notAnnulledAccountingSourceCache = notAnnulledAccountingSourceCache;
            this._accountingEntryMasterService = accountingEntryMasterService;
            this._accountingEntityService = accountingEntityService;
            this._accountingEntryDraftMasterService = accountingEntryDraftMasterService;
            this._accountingEntryDraftDetailService = accountingEntryDraftDetailService;
            this._auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _graphQLClient = graphQLClient;

            // Suscribir para recibir los handlers de AccountingAccount / CostCenter
            // (mantener colecciones locales sincronizadas con cambios de otros módulos).
            this.Context.EventAggregator.SubscribeOnUIThread(this);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                // Solo al cerrar definitivamente (no al cambiar de pantalla interna).
                this.Context.EventAggregator.Unsubscribe(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        /*public async Task ExecuteSearchForAccountingEntityMatchAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(this.FilterSearchAccountingEntity))
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        this.AccountingEntitiesSearchResults.Clear();
                    });
                    return;
                }

                string query = @"
                    query ($filter: AccountingEntityFilterInput) {
                      ListResponse: accountingEntities(filter: $filter) {
                        id
                        searchName
                        identificationNumber
                        verificationDigit
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.searchName = new ExpandoObject();
                variables.filter.searchName.@operator = "like";
                variables.filter.searchName.value = this.FilterSearchAccountingEntity.Replace(" ", "%").Trim().RemoveExtraSpaces();
                var accountingEntities = await this._accountingEntityService.GetListAsync(query, variables);
                this.AccountingEntitiesSearchResults = new ObservableCollection<AccountingEntityGraphQLModel>(accountingEntities);
                App.Current.Dispatcher.Invoke(() =>
                {
                    AccountingEntitiesSearchResults.Insert(0, new AccountingEntityGraphQLModel() { Id = 0, SearchName = "SELECCIONE UN TERCERO" });
                    if (AccountingEntitiesSearchResults.ToList().Count == 2) AccountingEntitiesSearchResults = AccountingEntitiesSearchResults.Where(x => x.Id != 0).ToObservableCollection();
                });

                this.IsFilterSearchAccountinEntityOnEditMode = (this.AccountingEntitiesSearchResults.Count == 0);
                this.SelectedAccountingEntityOnEntryId = -1; // Necesario para que siempre se ejecute el property change
                this.SelectedAccountingEntityOnEntryId = this.AccountingEntitiesSearchResults.FirstOrDefault().Id;
            }
            catch (Exception)
            {

                throw;
            }
        }*/
        public async Task SearchForAccountingEntityMatchAsync()
        {
           
            try
            {
                if (string.IsNullOrEmpty(this.FilterSearchAccountingEntity))
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        this.AccountingEntitiesSearchResults.Clear();
                    });
                    return;
                }
                if (this.IsFilterSearchAccountinEntityOnEditMode)
                {
                    string query = GetSearchForAccountingEntityMatchQuery();
                    dynamic variables = new ExpandoObject();
                    variables.pageResponsePagination = new ExpandoObject();
                    variables.pageResponsePagination.page = 1;
                    variables.pageResponsePagination.pageSize = 10;

                    variables.pageResponseFilters = new ExpandoObject();
                    variables.pageResponseFilters.matching = this.FilterSearchAccountingEntity.Replace(" ", "%").Trim().RemoveExtraSpaces();
                   
                    PageType<AccountingEntityGraphQLModel> result = await _accountingEntityService.GetPageAsync(query, variables);
                    this.AccountingEntitiesSearchResults = [.. result.Entries];
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        AccountingEntitiesSearchResults.Insert(0, new AccountingEntityGraphQLModel() { Id = 0, SearchName = "SELECCIONE UN TERCERO" });
                        if (AccountingEntitiesSearchResults.ToList().Count == 2) AccountingEntitiesSearchResults = AccountingEntitiesSearchResults.Where(x => x.Id != 0).ToObservableCollection();
                    });

                    this.IsFilterSearchAccountinEntityOnEditMode = (this.AccountingEntitiesSearchResults.Count == 0);
                    this.SelectedAccountingEntityOnEntryId = -1; // Necesario para que siempre se ejecute el property change
                    this.SelectedAccountingEntityOnEntryId = this.AccountingEntitiesSearchResults.FirstOrDefault().Id;
                  
                }
                else
                {
                    await Task.Run(() =>
                    {
                        this.IsFilterSearchAccountinEntityOnEditMode = true;
                        App.Current.Dispatcher.Invoke(() => this.SetFocus(nameof(FilterSearchAccountingEntity)));
                    });
                }

                IsBusy = true;

               
                

               

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
        public string GetSearchForAccountingEntityMatchQuery()
        {


            var accountingEntityFields = FieldSpec<PageType<AccountingEntityGraphQLModel>>
            .Create()
            .SelectList(it => it.Entries, entries => entries
                .Field(e => e.Id)
                .Field(e => e.SearchName)
                .Field(e => e.VerificationDigit)
                .Field(e => e.IdentificationNumber)
                .Field(e => e.InsertedAt)
            )
            .Field(o => o.PageNumber)
            .Field(o => o.PageSize)
            .Field(o => o.TotalPages)
            .Field(o => o.TotalEntries)
            .Build();


            var accountingEntityPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var accountingEntityfilterParameters = new GraphQLQueryParameter("filters", "AccountingEntityFilters");

            var accountingEntityFragment = new GraphQLQueryFragment("accountingEntitiesPage", [accountingEntityPagParameters, accountingEntityfilterParameters], accountingEntityFields, "PageResponse");

            var builder = new GraphQLQueryBuilder([accountingEntityFragment]);
            return builder.GetQuery();
        }

        /// <summary>
        /// Persiste los cambios pendientes del header del borrador (descripción, fecha, libro,
        /// centro de costo, fuente). Solo manda los campos modificados gracias a ChangeCollector
        /// + ChangeTracker. Llamado desde PublishAccountingEntryAsync antes de finalize cuando
        /// hay cambios y existe un borrador (DraftMasterId > 0).
        /// </summary>
        public async Task<AccountingEntryDraftGraphQLModel> UpdateDraftHeaderAsync()
        {
            string query = @"
            mutation($data:UpdateAccountingEntryDraftMasterInput!, $id:Int!) {
                UpdateResponse: updateAccountingEntryDraftMaster(data:$data, id:$id) {
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

            dynamic variables = ChangeCollector.CollectChanges(this, prefix: "data");

            // ChangeCollector serializa DateTime sin Kind. La API requiere Kind=Utc para
            // documentDate, igual que el código original (DateTimeHelper.DateTimeKindUTC).
            var dataDict = (IDictionary<string, object>)variables.data;
            if (dataDict.TryGetValue("documentDate", out var dateVal) && dateVal is DateTime dt)
            {
                dataDict["documentDate"] = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }

            variables.id = (int)this.DraftMasterId;

            var result = await this._accountingEntryDraftMasterService.UpdateAsync(query, (object)variables);
            var message = new AccountingEntryDraftMasterUpdateMessage() { UpdatedAccountingEntryDraftMaster = this.Context.Mapper.Map<AccountingEntryDraftMasterDTO>(result) };
            await this.Context.EventAggregator.PublishOnUIThreadAsync(message);
            this.AcceptChanges();
            return result;
        }

        #endregion

        #region Commands
        public async Task AddRecordAsync()
        {
            try
            {
                this.IsBusy = true;
                this.Refresh();
                await ExecuteAddRecordAsync();
                CleanEntry();
                this.Refresh();
                this.SetFocus(nameof(this.AccountingAccounts));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
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

        public async Task ExecuteAddRecordAsync()
        {
            try
            {
                string query;
                object variables;
                if (this.DraftMasterId == 0)
                {
                    query = @"
                    mutation ($data: CreateAccountingEntryDraftMasterInput!) {
                      CreateResponse: createAccountingEntryDraft(data: $data) {
                        id
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
                          code
                          name
                        },
                        accountingEntriesDraftDetail {
                            id
                            draftMasterId
                            accountingAccount {
                              id
                              code
                              name
                            }
                            accountingEntity {
                              id
                              identificationNumber
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
                        masterId
                        documentNumber
                        documentDate
                        createdAt
                        createdBy
                        description
                        totals {
                          debit
                          credit
                        }
                      }  
                    }";
                    var DocumentDate = this.DocumentDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    variables = new
                    {
                        Input = new
                        {
                            CostCenterId = this.SelectedCostCenterId,
                            AccountingSourceId = this.SelectedAccountingSourceId,
                            AccountingBookId = this.SelectedAccountingBookId,
                            DocumentDate,
                            this.Description,
                            createdById = SessionInfo.SessionId,
                            EntriesDraftDetail = new List<Object>()
                            {
                                new
                                {
                                    AccountingAccountId = this.SelectedAccountingAccountOnEntryId,
                                    AccountingEntityId = this.SelectedAccountingEntityOnEntryId,
                                    CostCenterId = this.SelectedCostCenterId,
                                    this.RecordDetail,
                                    this.Debit,
                                    this.Credit,
                                    this.Base
                                }
                            }
                        }
                    };

                    // Iniciar cronometro
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    AccountingEntryDraftGraphQLModel result = await this._accountingEntryDraftMasterService.CreateAsync(query, variables);
                    stopwatch.Stop();

                    // Message
                    await this.Context.EventAggregator.PublishOnUIThreadAsync(result);

                    this.TotalCredit = result.Totals.Credit;
                    this.TotalDebit = result.Totals.Debit;
                    this.EntriesResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
                    this.AccountingEntries = new ObservableCollection<AccountingEntryDraftDetailDTO>(this.Context.Mapper.Map<IEnumerable<AccountingEntryDraftDetailDTO>>(result.AccountingEntriesDraftDetail));
                    //this.EntriesTotalCount = result.AccountingEntryDraftDetailPage.PageResponse.Count;
                    this.DraftMasterId = result.Id;
                }
                else
                {
                    query = @"
                    mutation($data: CreateAccountingEntryDraftDetailInput!) {
                      CreateResponse: createAccountingEntryDraftDetail(data: $data) {
                        id
                        draftMasterId
                        accountingAccount {
                          id
                          code
                          name
                        }
                        costCenter {
                          id
                          name
                        }
                        accountingEntity {
                          id
                          identificationNumber
                          searchName
                        }
                        recordDetail
                        debit
                        credit
                        base
                        totals {
                          debit
                          credit
                        }
                      }
                    }";

                    variables = new
                    {
                        Data = new
                        {
                            this.DraftMasterId,
                            AccountingAccountId = this.SelectedAccountingAccountOnEntryId,
                            AccountingEntityId = this.SelectedAccountingEntityOnEntryId,
                            CostCenterId = this.SelectedCostCenterOnEntryId,
                            this.RecordDetail,
                            this.Debit,
                            this.Credit,
                            this.Base
                        }
                    };
                    
                    // Iniciar cronometro
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var entry = await this._accountingEntryDraftDetailService.CreateAsync(query, variables);
                    stopwatch.Stop();

                    this.TotalCredit = entry.Totals.Credit;
                    this.TotalDebit = entry.Totals.Debit;
                    this.EntriesResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        this.AccountingEntries.Add(this.Context.Mapper.Map<AccountingEntryDraftDetailDTO>(entry));
                    });
                }
            }
            catch (Exception ex)
            {
               throw;
            }
        }

        public bool CanAddRecord
        {
            get
            {
                return !this.HasErrors && !this.IsBusy;
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


        public async Task GoBackAsync()
        {
            try
            {
                await this.Context.ActivateMasterViewAsync();
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public bool CanGoBack()
        {
            return true;
        }

        #endregion

        #region Override

        protected override void OnViewReady(object view)
        {
            try
            {
                base.OnViewReady(view);
                ValidateProperties();
                if (this.DraftMasterId == 0)
                {
                    this.SetFocus(nameof(this.SelectedAccountingBookId));
                }
                else
                {
                    this.SetFocus(nameof(this.SelectedAccountingAccountOnEntryId));
                }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public CriteriaOperator ExcludeCostCenterIdZeroFilter => new BinaryOperator("Id", 0, BinaryOperatorType.NotEqual);
        #endregion

        #region Validaciones
        public bool HasErrors => (_errors.Count > 0);

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                RaiseErrorsChanged(propertyName);
                NotifyOfPropertyChange(nameof(CanAddRecord));
                NotifyOfPropertyChange(nameof(CanPublishAccountingEntry));
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
                NotifyOfPropertyChange(nameof(CanAddRecord));
                NotifyOfPropertyChange(nameof(CanPublishAccountingEntry));
            }
        }

        private void ValidateProperty(string propertyName, string stringValue, int intValue = 0, decimal decimalValue = 0)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(SelectedAccountingBookId):
                    if (intValue <= 0) AddError(propertyName, "El libro contable no puede estar vacío");
                    break;
                case nameof(SelectedCostCenterId):
                    if (intValue <= 0) AddError(propertyName, "El centro de costo no puede estar vacío");
                    break;
                case nameof(SelectedAccountingSourceId):
                    if (intValue <= 0) AddError(propertyName, "La fuente contable no puede estar vacía");
                    break;
                case nameof(DocumentDate):
                    if (DocumentDate is null) AddError(propertyName, "La fecha del documento no puede estar vacía");
                    break;
                case nameof(SelectedAccountingAccountOnEntryId):
                    if (intValue <= 0) AddError(propertyName, "La cuenta contable no puede estar vacía");
                    break;
                case nameof(SelectedAccountingEntityOnEntryId):
                    if (intValue <= 0) AddError(propertyName, "El tercero del registro no puede estar vacío");
                    break;
                case nameof(SelectedCostCenterOnEntryId):
                    if (intValue <= 0) AddError(propertyName, "El centro de costo del registro no puede estar vacío");
                    break;
                case nameof(Description):
                    if (string.IsNullOrEmpty(stringValue)) AddError(propertyName, "La descripción del comprobante no puede estar vacía");
                    break;
                case nameof(RecordDetail):
                    if (string.IsNullOrEmpty(stringValue)) AddError(propertyName, "El detalle del registro no puede estar vacío");
                    break;
                case nameof(Debit):
                    if (decimalValue <= 0 && this.Credit <= 0) AddError(propertyName, "El valor del débito o credito debe ser mayor que cero");
                    break;
                case nameof(Credit):
                    if (decimalValue <= 0 && this.Debit <= 0) AddError(propertyName, "El valor del débito o credito debe ser mayor que cero");
                    break;
                default:
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(SelectedAccountingBookId), null, SelectedAccountingBookId);
            ValidateProperty(nameof(SelectedCostCenterId), null, SelectedCostCenterId);
            ValidateProperty(nameof(SelectedAccountingSourceId), null, SelectedAccountingSourceId);
            ValidateProperty(nameof(DocumentDate), null);
            ValidateProperty(nameof(SelectedAccountingAccountOnEntryId), null, SelectedAccountingAccountOnEntryId);
            ValidateProperty(nameof(SelectedAccountingEntityOnEntryId), null, SelectedAccountingEntityOnEntryId);
            ValidateProperty(nameof(SelectedCostCenterOnEntryId), null, SelectedCostCenterOnEntryId);
            ValidateProperty(nameof(Description), Description);
            ValidateProperty(nameof(RecordDetail), RecordDetail);
            ValidateProperty(nameof(Debit), null, 0, this.Debit);
            ValidateProperty(nameof(Credit), null, 0, this.Credit);
        }

        #endregion

        #region Paginacion Entries

        /// <summary>
        /// PageIndex
        /// </summary>
        private int _entriesPageIndex = 1; // DefaultPageIndex = 1
        public int EntriesPageIndex
        {
            get { return _entriesPageIndex; }
            set
            {
                if (_entriesPageIndex != value)
                {
                    _entriesPageIndex = value;
                    NotifyOfPropertyChange(() => EntriesPageIndex);
                }
            }
        }

        /// <summary>
        /// PageSize
        /// </summary>
        private int _entriesPageSize = 50; // Default PageSize 50
        public int EntriesPageSize
        {
            get { return _entriesPageSize; }
            set
            {
                if (_entriesPageSize != value)
                {
                    _entriesPageSize = value;
                    NotifyOfPropertyChange(() => EntriesPageSize);
                }
            }
        }

        /// <summary>
        /// TotalCount
        /// </summary>
        private int _entriesTotalCount = 0;
        public int EntriesTotalCount
        {
            get { return _entriesTotalCount; }
            set
            {
                if (_entriesTotalCount != value)
                {
                    _entriesTotalCount = value;
                    NotifyOfPropertyChange(() => EntriesTotalCount);
                }
            }
        }

        // Tiempo de respuesta
        private string _entriesResponseTime;
        public string EntriesResponseTime
        {
            get { return _entriesResponseTime; }
            set
            {
                if (_entriesResponseTime != value)
                {
                    _entriesResponseTime = value;
                    NotifyOfPropertyChange(() => EntriesResponseTime);
                }
            }
        }


        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _entriesPaginationCommand;
        public ICommand EntriesPaginationCommand
        {
            get
            {
                if (_entriesPaginationCommand == null) this._entriesPaginationCommand = new RelayCommand(CanExecutePaginationEntriesChangeIndex, ExecutePaginationEntriesChangeIndex);
                return _entriesPaginationCommand;
            }
        }

        private void ExecutePaginationEntriesChangeIndex(object parameter)
        {
            Task.Run(() => 0);
        }

        private bool CanExecutePaginationEntriesChangeIndex(object parameter)
        {
            return true;
        }

        public Task HandleAsync(AccountingAccountUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                // Actualizar PUC general para los ViewModels
                this.AccountingAccounts.Replace(message.UpdatedAccountingAccount);
                // Actualizar las cuentas que puedan haber en el comprobante contable
                var matches = this.AccountingEntries.Where(x => x.AccountingAccount.Id == message.UpdatedAccountingAccount.Id);
                if (matches.ToList().Count > 0)
                {
                    foreach (var entry in matches)
                    {
                        entry.AccountingAccount = message.UpdatedAccountingAccount;
                    }
                    this.AccountingEntries = new ObservableCollection<AccountingEntryDraftDetailDTO>(this.AccountingEntries);
                }

            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingAccountCreateListMessage message, CancellationToken cancellationToken)
        {
            try
            {
                foreach (var account in message.CreatedAccountingAccountList)
                {
                    if (account.Code.Trim().Length >= 8)
                    {
                        var item = this.AccountingAccounts.Where(x => x.Id == account.Id).FirstOrDefault();
                        if (item is null)
                        {
                            this.AccountingAccounts.Add(account);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingAccountDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var accountToDelete = this.AccountingAccounts.Where(account => account.Id == message.DeletedAccountingAccount.Id).FirstOrDefault();
                if (accountToDelete != null) this.AccountingAccounts.Remove(accountToDelete);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(CostCenterCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var costCenter = this.CostCenters.Where(costCenter => costCenter.Id == message.CreatedCostCenter.Entity.Id).FirstOrDefault();
                if (costCenter is null) this.CostCenters.Add(message.CreatedCostCenter.Entity);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            return Task.CompletedTask;
        }

        #endregion

    }
}
