using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Books;
using System.Windows.Input;
using Microsoft.VisualStudio.Threading;
using System.Dynamic;
using System.Runtime.CompilerServices;
using DevExpress.Mvvm;
using DevExpress.Data.Filtering;
using NetErp.Billing.PriceList.ViewModels;
using NetErp.Books.AccountingEntries.DTO;

namespace NetErp.Books.AccountingEntries.ViewModels
{
    public class AccountingEntriesDetailViewModel : Screen,
        INotifyDataErrorInfo,
        IHandle<AccountingAccountUpdateMessage>,
        IHandle<AccountingAccountCreateListMessage>,
        IHandle<AccountingAccountDeleteMessage>,
        IHandle<CostCenterCreateMessage>
    {
        Dictionary<string, List<string>> _errors;
        private readonly Helpers.Services.INotificationService _notificationService = IoC.Get<Helpers.Services.INotificationService>();
        #region Propiedades

        // Context
        public AccountingEntriesViewModel Context { get; set; }

        // Parent record reference
        public AccountingEntryDraftMasterGraphQLModel SelectedAccountingEntryDraftMaster { get; set; } = null;

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
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                    // Con esto evito que vaya a la API en la primera asignación
                    if (this.SelectedAccountingEntryDraftMaster != null && this.SelectedAccountingEntryDraftMaster.DocumentDate != value)
                        _ = Task.Run(() => UpdateAccountingEntryDraftMaster(nameof(DocumentDate)));
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
                    ValidateProperty(nameof(Description), value, 0);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                    // Con esto evito que vaya a la API en la primera asignación
                    if (this.SelectedAccountingEntryDraftMaster != null && this.SelectedAccountingEntryDraftMaster.Description != value && !string.IsNullOrEmpty(Description))
                        _ = Task.Run(() => UpdateAccountingEntryDraftMaster(nameof(Description)));
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
        public int SelectedAccountingBookId
        {
            get { return _selectedAccountingBookId; }
            set
            {
                if (_selectedAccountingBookId != value)
                {
                    _selectedAccountingBookId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingBookId));
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                }
            }
        }

        private int _selectedCostCenterId = 0;
        public int SelectedCostCenterId
        {
            get { return _selectedCostCenterId; }
            set
            {
                if (_selectedCostCenterId != value)
                {
                    _selectedCostCenterId = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenterId));
                    ValidateProperty(nameof(SelectedCostCenterId), null, value);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
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
        public int SelectedAccountingSourceId
        {
            get { return _selectedAccountingSourceId; }
            set
            {
                if (_selectedAccountingSourceId != value)
                {
                    _selectedAccountingSourceId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingSourceId));
                    ValidateProperty(nameof(SelectedAccountingSourceId), null, value);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                }
            }
        }

        private ICommand _publishAccountingEntryCommand;

        public ICommand PublishAccountingEntryCommand
        {
            get
            {
                if (_publishAccountingEntryCommand is null) _publishAccountingEntryCommand = new AsyncCommand(PublishAccountingEntry, CanPublishAccountingEntry);
                return _publishAccountingEntryCommand;
            }
        }


        private ICommand _deleteAccountingEntriesCommand;

        public ICommand DeleteAccountingEntriesCommand
        {
            get
            {
                if (_deleteAccountingEntriesCommand is null) _deleteAccountingEntriesCommand = new AsyncCommand(DeleteAccountingEntries, CanDeleteAccountingEntries);
                return _deleteAccountingEntriesCommand;
            }
        }

        #endregion

        #region Metodos

        public async Task PublishAccountingEntry()
        {
            try
            {

                if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea finalizar la edición del comprobante?", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No) return;

                this.IsBusy = true;
                this.Refresh();
                var createdEntry = await this.ExecutePublishAccountingEntry();
                await this.Context.EventAggregator.PublishOnUIThreadAsync(createdEntry);
                if (this.DraftMasterId != 0) await this.Context.EventAggregator.PublishOnUIThreadAsync(new AccountingEntryDraftMasterDeleteMessage { Id = this.DraftMasterId });
                if (createdEntry != null)
                {
                    await this.Context.ActivateMasterView();
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

        public async Task<AccountingEntryMasterGraphQLModel> ExecutePublishAccountingEntry()
        {
            AccountingEntryMasterGraphQLModel entry = null;
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
                      CreateResponse: createAccountingEntryMasterFromDraft(draftMasterId:$draftMasterId) {
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
                    entry = await this.Context.AccountingEntryMasterService.Create(query, variables);
                }
                else
                {
                    string query = @"
                    mutation($draftMasterId:ID!) {
                      UpdateResponse: updateAccountingEntryMasterFromDraft(draftMasterId:$draftMasterId) {
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
                    entry = await this.Context.AccountingEntryMasterService.Update(query, variables);
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
                // Aqui deben haber mas validaciones, que no hayan cosas vacias, etc..
                return (TotalDiference == 0 && TotalCredit > 0);
            }
        }

        public async Task EndRowEditing()
        {
            try
            {
                IsBusy = true;
                string query = @"
                mutation($data:UpdateAccountingEntryDraftDetailInput!, $id:Int!) {
                  UpdateResponse: updateAccountingEntryDraftDetail(data:$data, id:$id) {
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
                      verificationDigit
                      businessName
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
                }";


                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Id = SelectedAccountingEntryDraftDetail.Id;
                variables.Data.AccountingAccountId = SelectedAccountingEntryDraftDetail.AccountingAccount.Id;
                variables.Data.AccountingEntityId = SelectedAccountingEntryDraftDetail.AccountingEntity.Id;
                variables.Data.CostCenterId = SelectedAccountingEntryDraftDetail.CostCenter.Id;
                variables.Data.RecordDetail = SelectedAccountingEntryDraftDetail.RecordDetail;
                variables.Data.Debit = SelectedAccountingEntryDraftDetail.Debit;
                variables.Data.Credit = SelectedAccountingEntryDraftDetail.Credit;
                variables.Data.Base = SelectedAccountingEntryDraftDetail.Base;

                AccountingEntryDraftDetailDTO updatedEntry = Context.Mapper.Map<AccountingEntryDraftDetailDTO>(await this.Context.AccountingEntryDraftDetailService.Update(query, variables)) ?? throw new Exception("No se pudo actualizar el registro");
                // Actualizo el registro en la lista
                var entryToUpdate = this.AccountingEntries.FirstOrDefault(x => x.Id == updatedEntry.Id);
                if (entryToUpdate != null)
                {
                    entryToUpdate.AccountingAccount = updatedEntry.AccountingAccount;
                    entryToUpdate.AccountingEntity = updatedEntry.AccountingEntity;
                    entryToUpdate.CostCenter = updatedEntry.CostCenter;
                    entryToUpdate.RecordDetail = updatedEntry.RecordDetail;
                    entryToUpdate.Debit = updatedEntry.Debit;
                    entryToUpdate.Credit = updatedEntry.Credit;
                    entryToUpdate.Base = updatedEntry.Base;
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
                var result = await this.Context.AccountingEntryMasterService.GetDataContext<AccountingEntriesDraftDetailDataContext>(query, variables);
                this.TotalCredit = result.AccountingEntryDraftTotals.Credit;
                this.TotalDebit = result.AccountingEntryDraftTotals.Debit;
                this.IsBusy = false;
                _notificationService.ShowSuccess("Actualización exitosa");
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

        public async Task DeleteAccountingEntries()
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

                var response = await this.Context.AccountingEntryDraftDetailService.MutationContext<SuccessResponseDataWrapper>(query, variables);

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

                var result = await this.Context.AccountingEntryMasterService.GetDataContext<AccountingEntriesDraftDetailDataContext>(query, variables);

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

        public async void SearchForAccountingEntityMatch()
        {
            try
            {
                this.IsBusy = true;
                this.Refresh();

                if (this.IsFilterSearchAccountinEntityOnEditMode)
                {
                    await Task.Run(() => this.ExecuteSearchForAccountingEntityMatch());
                    App.Current.Dispatcher.Invoke(() => this.SetFocus(nameof(SelectedAccountingEntityOnEntryId)));
                }
                else
                {
                    await Task.Run(() =>
                    {
                        this.IsFilterSearchAccountinEntityOnEditMode = true;
                        App.Current.Dispatcher.Invoke(() => this.SetFocus(nameof(FilterSearchAccountingEntity)));
                    });
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
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


        private async Task Initialize()
        {
            try
            {
                IsBusy = true;
                // Validaciones
                this._errors = new Dictionary<string, List<string>>();
                this.Context.EventAggregator.SubscribeOnUIThread(this);

                if (this.Context.AccountingAccounts == null) // Solo debe suceder una vez
                {
                    string query = @"
                    query ($accountingAccountFilter: AccountingAccountFilterInput) {
                      ListResponse: accountingAccounts(filter: $accountingAccountFilter) {
                        id
                        code
                        name
                        margin
                        marginBasis
                      }
                    }";
                    dynamic variables = new ExpandoObject();
                    variables.AccountingAccountFilter = new ExpandoObject();
                    variables.AccountingAccountFilter.Code = new ExpandoObject();
                    variables.AccountingAccountFilter.Code.@operator = new List<string>() { "length", ">=" };
                    variables.AccountingAccountFilter.Code.value = 8;
                    var result = await this.Context.AccountingAccountService.GetList(query, variables);
                    this.Context.AccountingAccounts = new ObservableCollection<AccountingAccountGraphQLModel>(result);
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

        public AccountingEntriesDetailViewModel(AccountingEntriesViewModel context)
        {
            this.Context = context;
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await Initialize());
        }

        public async Task ExecuteSearchForAccountingEntityMatch()
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
                var accountingEntities = await this.Context.AccountingEntityService.GetList(query, variables);
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
        }

        public async Task<AccountingEntryDraftMasterGraphQLModel> UpdateAccountingEntryDraftMaster(string field)
        {
            try
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
                DateTime? parsedDocumentDate = DateTimeHelper.DateTimeKindUTC(DocumentDate);
                object variables = field switch
                {
                    nameof(DocumentDate) => new
                    {
                        Data = new { DocumentDate = parsedDocumentDate },
                        Id = this.DraftMasterId
                    },
                    nameof(Description) => new
                    {
                        Data = new { Description },
                        Id = this.DraftMasterId
                    },
                    _ => new { },
                };
                var result = await this.Context.AccountingEntryDraftMasterService.Update(query, variables);
                var message = new AccountingEntryDraftMasterUpdateMessage() { UpdatedAccountingEntryDraftMaster = this.Context.Mapper.Map<AccountingEntryDraftMasterDTO>(result) };
                await this.Context.EventAggregator.PublishOnUIThreadAsync(message);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region Commands
        public async Task AddRecord()
        {
            try
            {
                this.IsBusy = true;
                this.Refresh();
                await ExecuteAddRecord();
                CleanEntry();
                this.Refresh();
                this.SetFocus(nameof(this.Context.AccountingAccounts));
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

        public async Task ExecuteAddRecord()
        {
            try
            {
                string query;
                object variables;
                if (this.DraftMasterId == 0)
                {
                    query = @"
                    mutation ($data: CreateAccountingEntryDraftMasterInput!) {
                      CreateResponse: createAccountingEntryDraftMaster(data: $data) {
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

                    variables = new
                    {
                        Data = new
                        {
                            CostCenterId = this.SelectedCostCenterId,
                            AccountingSourceId = this.SelectedAccountingSourceId,
                            AccountingBookId = this.SelectedAccountingBookId,
                            this.DocumentDate,
                            this.Description,
                            CreatedBy = SessionInfo.UserEmail,
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
                    AccountingEntryDraftMasterGraphQLModel result = await this.Context.AccountingEntryDraftMasterService.Create(query, variables);
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
                    var entry = await this.Context.AccountingEntryDraftDetailService.Create(query, variables);
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
            catch (Exception)
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
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBack, CanGoBack);
                return _goBackCommand;
            }
        }


        public async Task GoBack()
        {
            try
            {
                await this.Context.ActivateMasterView();
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
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ValidateProperty(string propertyName, string stringValue, int intValue = 0, decimal decimalValue = 0)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(SelectedCostCenterId):
                    if (intValue <= 0) AddError(propertyName, "El centro de costo no puede estar vacío");
                    break;
                case nameof(SelectedAccountingSourceId):
                    if (intValue <= 0) AddError(propertyName, "La fuente contable no puede estar vacía");
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
            ValidateProperty(nameof(SelectedCostCenterId), null, SelectedCostCenterId);
            ValidateProperty(nameof(SelectedAccountingSourceId), null, SelectedAccountingSourceId);
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
                this.Context.AccountingAccounts.Replace(message.UpdatedAccountingAccount);
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
                        var item = this.Context.AccountingAccounts.Where(x => x.Id == account.Id).FirstOrDefault();
                        if (item is null)
                        {
                            this.Context.AccountingAccounts.Add(account);
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
                var accountToDelete = this.Context.AccountingAccounts.Where(account => account.Id == message.DeletedAccountingAccount.Id).FirstOrDefault();
                if (accountToDelete != null) this.Context.AccountingAccounts.Remove(accountToDelete);
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
                var costCenter = this.Context.CostCenters.Where(costCenter => costCenter.Id == message.CreatedCostCenter.Id).FirstOrDefault();
                if (costCenter is null) this.Context.CostCenters.Add(message.CreatedCostCenter);
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
