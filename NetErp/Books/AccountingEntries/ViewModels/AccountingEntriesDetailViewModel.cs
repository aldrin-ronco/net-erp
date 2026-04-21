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

using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingEntries.DTO;
using NetErp.Global.Modals.ViewModels;
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
        private readonly IRepository<DraftAccountingEntryGraphQLModel> _draftAccountingEntryService; 
        private readonly IRepository<DraftAccountingEntryLineGraphQLModel> _draftAccountingEntryLineService;
        private readonly Helpers.IDialogService _dialogService;



        #region Propiedades

        public AccountingEntriesViewModel Context { get; set; }

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
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        // Colecciones para combos y grid
        private ObservableCollection<DraftAccountingEntryLineDTO> _accountingEntries;
        public ObservableCollection<DraftAccountingEntryLineDTO> AccountingEntries
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

        private DraftAccountingEntryLineDTO _selectedDraftAccountingEntryLine;
        public DraftAccountingEntryLineDTO SelectedDraftAccountingEntryLine
        {
            get { return _selectedDraftAccountingEntryLine; }
            set
            {
                if (this._selectedDraftAccountingEntryLine != value)
                {
                    _selectedDraftAccountingEntryLine = value;
                    NotifyOfPropertyChange(nameof(this.SelectedDraftAccountingEntryLine));
                }
            }
        }

        #region DraftEntry

        public DraftAccountingEntryGraphQLModel SelectedDraftAccountingEntry { get; set; } = null;

        public int DescriptionMaxLength => Context.StringLengthCache.GetMaxLength<DraftAccountingEntryGraphQLModel>(nameof(DraftAccountingEntryGraphQLModel.Description));

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

        public bool IsNewRecord
        {
            get { return this.DraftMasterId == 0; }
        }

        public bool IsEditingRecord
        {
            get { return this.DraftMasterId > 0; }
        }

        private DateTime? _documentDate = DateTime.Now;
        [IsoDate]
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

        private int? _selectedAccountingBookId;
        [ExpandoPath("accountingBookId")]
        public int? SelectedAccountingBookId
        {
            get { return _selectedAccountingBookId; }
            set
            {
                if (_selectedAccountingBookId != value)
                {
                    _selectedAccountingBookId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingBookId));
                    this.TrackChange(nameof(SelectedAccountingBookId));
                    ValidateProperty(nameof(SelectedAccountingBookId), null, value.GetValueOrDefault());
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                    NotifyOfPropertyChange(nameof(CanPublishAccountingEntry));
                }
            }
        }

        private int? _selectedCostCenterId;
        [ExpandoPath("costCenterId")]
        public int? SelectedCostCenterId
        {
            get { return _selectedCostCenterId; }
            set
            {
                if (_selectedCostCenterId != value)
                {
                    _selectedCostCenterId = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenterId));
                    this.TrackChange(nameof(SelectedCostCenterId));
                    ValidateProperty(nameof(SelectedCostCenterId), null, value.GetValueOrDefault());
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                    NotifyOfPropertyChange(nameof(CanPublishAccountingEntry));
                }
            }
        }

        private int? _selectedAccountingSourceId;
        [ExpandoPath("accountingSourceId")]
        public int? SelectedAccountingSourceId
        {
            get { return _selectedAccountingSourceId; }
            set
            {
                if (_selectedAccountingSourceId != value)
                {
                    _selectedAccountingSourceId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingSourceId));
                    this.TrackChange(nameof(SelectedAccountingSourceId));
                    ValidateProperty(nameof(SelectedAccountingSourceId), null, value.GetValueOrDefault());
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                    NotifyOfPropertyChange(nameof(CanPublishAccountingEntry));
                }
            }
        }

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

        #region DraftLine

        public int RecordDetailMaxLength => Context.StringLengthCache.GetMaxLength<DraftAccountingEntryLineGraphQLModel>(nameof(DraftAccountingEntryLineGraphQLModel.RecordDetail));

        private AccountingEntityGraphQLModel? _selectedAccountingEntity;
        public AccountingEntityGraphQLModel? SelectedAccountingEntity
        {
            get => _selectedAccountingEntity;
            set
            {
                if (_selectedAccountingEntity != value)
                {
                    _selectedAccountingEntity = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntity));
                    NotifyOfPropertyChange(nameof(SelectedAccountingEntityDisplay));
                    this.TrackChange(nameof(SelectedAccountingEntity));
                    ValidateProperty(nameof(SelectedAccountingEntity), null);
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                }
            }
        }

        public string SelectedAccountingEntityDisplay =>
            SelectedAccountingEntity is null
                ? string.Empty
                : $"{SelectedAccountingEntity.IdentificationNumber} — {SelectedAccountingEntity.SearchName}";

        private int? _selectedAccountingAccountOnEntryId;
        public int? SelectedAccountingAccountOnEntryId
        {
            get { return _selectedAccountingAccountOnEntryId; }
            set
            {
                if (_selectedAccountingAccountOnEntryId != value)
                {
                    _selectedAccountingAccountOnEntryId = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountingAccountOnEntryId));
                    ValidateProperty(nameof(SelectedAccountingAccountOnEntryId), null, value.GetValueOrDefault());
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                }
            }
        }

        private int? _selectedCostCenterOnEntryId;
        public int? SelectedCostCenterOnEntryId
        {
            get { return _selectedCostCenterOnEntryId; }
            set
            {
                if (_selectedCostCenterOnEntryId != value)
                {
                    _selectedCostCenterOnEntryId = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenterOnEntryId));
                    ValidateProperty(nameof(SelectedCostCenterOnEntryId), null, value.GetValueOrDefault());
                    NotifyOfPropertyChange(nameof(CanAddRecord));
                }
            }
        }

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

        public bool IsEnableDebit
        {
            get { return this.Credit == 0; }
        }

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

        public bool IsEnableCredit { get { return this.Debit == 0; } }

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

        #endregion

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

                // Si hay cambios pendientes en el header del borrador, persistirlos antes
                // de finalizar. Solo aplica cuando el borrador ya existe (DraftMasterId > 0);
                // si es un borrador nuevo (DraftMasterId == 0) no hay nada que actualizar
                // porque los datos del header se enviarán dentro de createAccountingEntryDraft
                // junto con la primera línea — ese flujo no aplica aquí porque PublishAccountingEntry
                // requiere un borrador ya creado.
                if (DraftMasterId != 0 && HasHeaderChanges())
                {
                    await UpdateDraftHeaderAsync();
                }

                var createdEntry = await this.ExecutePublishAccountingEntryAsync();
                await this.Context.EventAggregator.PublishOnUIThreadAsync(createdEntry);
                if (this.DraftMasterId != 0) await this.Context.EventAggregator.PublishOnUIThreadAsync(new DraftAccountingEntryFinalizeMessage { DraftId = this.DraftMasterId });
                if (createdEntry != null)
                {
                    await this.Context.ActivateMasterViewAsync();
                }

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

        /// <summary>
        /// Finaliza el borrador vigente vía <c>finalizeAccountingEntryDraft(input: FinalizeAccountingEntryDraftInput!)</c>.
        /// El schema ya no diferencia "crear publicado" vs "re-publicar": siempre es una única mutación
        /// que transforma el borrador en un <c>AccountingEntry</c> publicado y retorna ese entry.
        /// </summary>
        public async Task<AccountingEntryGraphQLModel> ExecutePublishAccountingEntryAsync()
        {
            var (fragment, query) = _finalizeDraftQuery.Value;
            object variables = new GraphQLVariables()
                .For(fragment, "input", new { draftId = (int)this.DraftMasterId })
                .Build();

            var payload = await this._accountingEntryMasterService
                .CreateAsync<UpsertResponseType<AccountingEntryGraphQLModel>>(query, variables);

            if (!payload.Success)
                throw new Exception(payload.Message);

            return payload.Entity;
        }

        /// <summary>
        /// Campos del header que deben estar libres de errores para permitir publicar.
        /// Los campos del line (cuenta contable, tercero, centro de costo del entry, detalle,
        /// débito, crédito) solo afectan a <see cref="CanAddRecord"/>.
        /// </summary>
        private static readonly string[] _headerFields =
        [
            nameof(SelectedAccountingBookId),
            nameof(SelectedCostCenterId),
            nameof(SelectedAccountingSourceId),
            nameof(DocumentDate),
            nameof(Description)
        ];

        private bool HasHeaderErrors => _headerFields.Any(f => _errors.ContainsKey(f));

        public bool CanPublishAccountingEntry
        {
            get
            {
                if (HasHeaderErrors) return false;
                if (TotalDiference != 0) return false;
                if (TotalCredit <= 0) return false;
                return true;
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
        //        variables.Id = SelectedDraftAccountingEntryLine.Id;
        //        variables.Data.AccountingAccountId = SelectedDraftAccountingEntryLine.AccountingAccount.Id;
        //        variables.Data.AccountingEntityId = SelectedDraftAccountingEntryLine.AccountingEntity.Id;
        //        variables.Data.CostCenterId = SelectedDraftAccountingEntryLine.CostCenter.Id;
        //        variables.Data.RecordDetail = SelectedDraftAccountingEntryLine.RecordDetail;
        //        variables.Data.Debit = SelectedDraftAccountingEntryLine.Debit;
        //        variables.Data.Credit = SelectedDraftAccountingEntryLine.Credit;
        //        variables.Data.Base = SelectedDraftAccountingEntryLine.Base;
        //
        //        DraftAccountingEntryLineDTO updatedEntry = Context.Mapper.Map<DraftAccountingEntryLineDTO>(await this._draftAccountingEntryLineService.UpdateAsync(query, variables)) ?? throw new Exception("No se pudo actualizar el registro");
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

                int[] lineIds = this.AccountingEntries
                    .Where(e => e.IsChecked)
                    .Select(e => (int)e.Id)
                    .ToArray();

                if (lineIds.Length == 0) return;

                var (fragment, query) = _deleteDraftLinesQuery.Value;
                object variables = new GraphQLVariables()
                    .For(fragment, "input", new
                    {
                        draftId = (int)this.DraftMasterId,
                        lineIds
                    })
                    .Build();

                var payload = await this._draftAccountingEntryLineService
                    .MutationContextAsync<DeleteDraftLinesPayloadWrapper>(query, variables);
                var response = payload?.DeleteResponse
                    ?? throw new Exception("No se recibió respuesta al eliminar las líneas.");

                if (!response.Success)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.DeleteAccountingEntriesAsync\r\n{response.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }

                // Recargar líneas del borrador desde el API y recalcular totales client-side.
                await ReloadDraftLinesAsync();
                NotifyOfPropertyChange(nameof(CanDeleteAccountingEntries));
                _notificationService.ShowSuccess(response.Message);
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
                this.SelectedAccountingAccountOnEntryId = null;
                this.RecordDetail = string.Empty;
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
            // Sin sentinelas: los combos usan NullText vía DevExpress cuando el valor es null.
            CostCenters = [.. _costCenterCache.Items];
            this.AccountingBooks = [.. _accountingBookCache.Items];
            this.AccountingSources = [.. _notAnnulledAccountingSourceCache.Items];
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
            // Header — sin pre-selección automática; los combos muestran su NullText.
            SelectedDraftAccountingEntry = null;
            DraftMasterId = 0;
            SelectedAccountingBookId = null;
            SelectedCostCenterId = null;
            SelectedAccountingSourceId = null;
            SelectedCostCenterOnEntryId = null;
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
            SelectedAccountingAccountOnEntryId = null;
            SelectedAccountingEntity = null;
            SelectedCostCenterOnEntryId = null;
            RecordDetail = "";
            Debit = 0;
            Credit = 0;
            Base = 0;
            SeedDefaultValues();
        }

        /// <summary>
        /// Inicializa el ViewModel para editar un borrador existente.
        /// Llamado por el Conductor ANTES de ActivateItemAsync, después de cargar las líneas
        /// y los totales del borrador.
        /// </summary>
        public void SetForEdit(DraftAccountingEntryGraphQLModel model,
                               IEnumerable<DraftAccountingEntryLineDTO> entries,
                               decimal totalDebit,
                               decimal totalCredit,
                               string responseTime)
        {
            // Header
            SelectedDraftAccountingEntry = model;
            DraftMasterId = model.Id;
            SelectedAccountingBookId = model.AccountingBook.Id;
            SelectedCostCenterId = model.CostCenter.Id;
            SelectedAccountingSourceId = model.AccountingSource.Id;
            DocumentDate = model.DocumentDate;
            Description = model.Description;

            // Líneas y totales
            AccountingEntries = new ObservableCollection<DraftAccountingEntryLineDTO>(entries);
            TotalDebit = totalDebit;
            TotalCredit = totalCredit;
            EntriesResponseTime = responseTime;

            // Entry Point (formulario de captura de líneas)
            SelectedAccountingAccountOnEntryId = null;
            SelectedAccountingEntity = null;
            SelectedCostCenterOnEntryId = null;
            RecordDetail = "";
            Debit = 0;
            Credit = 0;
            Base = 0;
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
            IRepository<AccountingEntryGraphQLModel> accountingEntryMasterService,
            IRepository<AccountingEntityGraphQLModel> accountingEntityService,
            IRepository<DraftAccountingEntryGraphQLModel> accountingEntryDraftMasterService,
            IRepository<DraftAccountingEntryLineGraphQLModel> accountingEntryDraftLineService,
            CostCenterCache costCenterCache,
            AccountingBookCache accountingBookCache,
            NotAnnulledAccountingSourceCache notAnnulledAccountingSourceCache,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            Helpers.IDialogService dialogService,
            IGraphQLClient graphQLClient)
        {
            this.Context = context;
            _costCenterCache = costCenterCache;
            _accountingBookCache = accountingBookCache;
            _notAnnulledAccountingSourceCache = notAnnulledAccountingSourceCache;
            this._accountingEntryMasterService = accountingEntryMasterService;
            this._accountingEntityService = accountingEntityService;
            this._draftAccountingEntryService = accountingEntryDraftMasterService;
            this._draftAccountingEntryLineService = accountingEntryDraftLineService;
            this._auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _dialogService = dialogService;
            _graphQLClient = graphQLClient;

            // Suscribir para recibir handlers de AccountingAccount / CostCenter + modal tercero.
            this.Context.EventAggregator.SubscribeOnUIThread(this);
            Messenger.Default.Register<ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel>>(
                this,
                SearchWithTwoColumnsGridMessageToken.AccountingEntryAccountingEntity,
                false,
                OnFindAccountingEntityMessage);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                this.Context.EventAggregator.Unsubscribe(this);
                Messenger.Default.Unregister(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        /// <summary>
        /// Abre la modal genérica de búsqueda de terceros con dos columnas
        /// (identificación + nombre). La modal retorna el tercero seleccionado
        /// vía Messenger.Default y el handler OnFindAccountingEntityMessage
        /// lo asigna a SelectedAccountingEntity.
        /// </summary>
        public async Task OpenAccountingEntitySearchAsync()
        {
            string query = GetSearchAccountingEntityQuery();

            var viewModel = new SearchWithTwoColumnsGridViewModel<AccountingEntityGraphQLModel>(
                query,
                fieldHeader1: "Identificación",
                fieldHeader2: "Nombre / Razón Social",
                fieldData1: "IdentificationNumberWithVerificationDigit",
                fieldData2: "SearchName",
                variables: null,
                SearchWithTwoColumnsGridMessageToken.AccountingEntryAccountingEntity,
                _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Búsqueda de terceros");
        }

        private static string GetSearchAccountingEntityQuery()
        {
            var fields = FieldSpec<PageType<AccountingEntityGraphQLModel>>
                .Create()
                .Field(f => f.PageNumber)
                .Field(f => f.TotalEntries)
                .Field(f => f.PageSize)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.IdentificationNumber)
                    .Field(e => e.VerificationDigit)
                    .Field(e => e.SearchName))
                .Build();

            var filterParameter = new GraphQLQueryParameter("filters", "AccountingEntityFilters");
            var paginationParameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("accountingEntitiesPage", [filterParameter, paginationParameter], fields, "PageResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        private void OnFindAccountingEntityMessage(ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel> message)
        {
            if (message?.ReturnedData is null) return;
            SelectedAccountingEntity = message.ReturnedData;
        }

        private static readonly string[] _nonHeaderProperties = [nameof(SelectedAccountingEntity)];

        private static readonly HashSet<string> _headerProperties =
        [
            nameof(Description),
            nameof(DocumentDate),
            nameof(SelectedAccountingBookId),
            nameof(SelectedCostCenterId),
            nameof(SelectedAccountingSourceId),
        ];

        public bool HasHeaderChanges()
        {
            return this.GetChangedProperties().Any(_headerProperties.Contains);
        }

        public async Task<DraftAccountingEntryGraphQLModel> UpdateDraftHeaderAsync()
        {
            var (fragment, query) = _updateDraftQuery.Value;

            dynamic variables = ChangeCollector.CollectChanges(this,
                prefix: "updateResponseInput",
                excludeProperties: _nonHeaderProperties);
            variables.updateResponseInput.draftId = (int)this.DraftMasterId;

            var result = await this._draftAccountingEntryService
                .UpdateAsync<UpsertResponseType<DraftAccountingEntryGraphQLModel>>(query, variables);

            if (!result.Success)
                throw new Exception(result.Message);

            var message = new DraftAccountingEntryUpdateMessage
            {
                UpdatedDraftAccountingEntry = result.Entity
            };
            await this.Context.EventAggregator.PublishOnUIThreadAsync(message);
            this.AcceptChanges();
            return result.Entity;
        }

        #endregion

        #region GraphQL Queries

        /// <summary>
        /// <c>createAccountingEntryDraft(input: CreateAccountingEntryDraftInput!)</c>.
        /// Crea el encabezado del borrador (sin líneas). Las líneas se persisten aparte
        /// vía <c>upsertDraftLines</c>. El payload del schema usa <c>draft</c> como campo
        /// de la entidad; aquí se mapea al alias <c>entity</c> de <see cref="UpsertResponseType{T}"/>.
        /// </summary>
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createDraftQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<DraftAccountingEntryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "draft", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.DocumentDate)
                    .Field(e => e.DocumentNumber)
                    .Select(e => e.AccountingBook, b => b.Field(x => x.Id).Field(x => x.Name))
                    .Select(e => e.CostCenter, c => c.Field(x => x.Id).Field(x => x.Name))
                    .Select(e => e.AccountingSource, s => s.Field(x => x.Id).Field(x => x.Name))
                    .Select(e => e.CreatedBy, u => u.Field(x => x.Id).Field(x => x.FullName)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createDraftAccountingEntry",
                [new("input", "CreateDraftAccountingEntryInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        /// <summary>
        /// <c>updateDraft(input: UpdateDraftInput!)</c>.
        /// Actualiza campos del header del borrador. <c>draftId</c> viaja DENTRO del input wrapper.
        /// </summary>
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateDraftQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<DraftAccountingEntryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "draft", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.DocumentDate)
                    .Field(e => e.DocumentNumber)
                    .Select(e => e.AccountingBook, b => b.Field(x => x.Id).Field(x => x.Name))
                    .Select(e => e.CostCenter, c => c.Field(x => x.Id).Field(x => x.Name))
                    .Select(e => e.AccountingSource, s => s.Field(x => x.Id).Field(x => x.Name))
                    .Select(e => e.CreatedBy, u => u.Field(x => x.Id).Field(x => x.FullName)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateDraft",
                [new("input", "UpdateDraftInput!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        /// <summary>
        /// <c>upsertDraftLines(input: UpsertDraftLinesInput!)</c>.
        /// Crea y/o actualiza múltiples líneas del borrador en una sola llamada.
        /// El payload solo retorna contadores y success/errors, no las líneas; por eso
        /// <see cref="ReloadDraftLinesAsync"/> recarga el borrador después.
        /// </summary>
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _upsertDraftLinesQuery = new(() =>
        {
            var fields = FieldSpec<UpsertDraftLinesPayloadWrapper.UpsertDraftLinesResponse>
                .Create()
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Field(f => f.InsertedCount)
                .Field(f => f.UpdatedCount)
                .SelectList(f => f.Errors, sq => sq
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("upsertDraftLines",
                [new("input", "UpsertDraftLinesInput!")],
                fields, "UpsertResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        /// <summary>
        /// <c>deleteDraftLines(input: DeleteDraftLinesInput!)</c>.
        /// Elimina líneas específicas del borrador por id.
        /// </summary>
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteDraftLinesQuery = new(() =>
        {
            var fields = FieldSpec<DeleteDraftLinesPayloadWrapper.DeleteDraftLinesResponse>
                .Create()
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Field(f => f.DeletedCount)
                .SelectList(f => f.Errors, sq => sq
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("deleteDraftLines",
                [new("input", "DeleteDraftLinesInput!")],
                fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        /// <summary>
        /// <c>finalizeAccountingEntryDraft(input: FinalizeAccountingEntryDraftInput!)</c>.
        /// Convierte el borrador en un comprobante publicado (<c>AccountingEntry</c>).
        /// </summary>
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _finalizeDraftQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccountingEntryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingEntry", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.DocumentDate)
                    .Field(e => e.DocumentNumber)
                    .Select(e => e.AccountingBook, b => b.Field(x => x.Id).Field(x => x.Name))
                    .Select(e => e.CostCenter, c => c.Field(x => x.Id).Field(x => x.Name))
                    .Select(e => e.AccountingSource, s => s.Field(x => x.Id).Field(x => x.Name))
                    .Select(e => e.CreatedBy, u => u.Field(x => x.Id).Field(x => x.FullName)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("finalizeDraftAccountingEntry",
                [new("input", "FinalizeDraftAccountingEntryInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        /// <summary>
        /// <c>accountingEntryDraft(id: ID!)</c>.
        /// Recarga el borrador entero con sus líneas (incluidas las relaciones: cuenta,
        /// tercero, centro de costo). Usado tras cada mutación de líneas para refrescar
        /// la colección local y recalcular totales.
        /// </summary>
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadDraftLinesQuery = new(() =>
        {
            var fields = FieldSpec<DraftAccountingEntryGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.Description)
                .Field(f => f.DocumentDate)
                .Field(f => f.DocumentNumber)
                .SelectList(f => f.Lines, l => l
                    .Field(x => x.Id)
                    .Field(x => x.RecordDetail)
                    .Field(x => x.Debit)
                    .Field(x => x.Credit)
                    .Field(x => x.Base)
                    .Select(x => x.AccountingAccount, a => a.Field(y => y.Id).Field(y => y.Code).Field(y => y.Name))
                    .Select(x => x.AccountingEntity, a => a.Field(y => y.Id).Field(y => y.IdentificationNumber).Field(y => y.SearchName))
                    .Select(x => x.CostCenter, c => c.Field(y => y.Id).Field(y => y.Name)))
                .Build();

            var fragment = new GraphQLQueryFragment("draftAccountingEntry",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion

        #region Commands
        public async Task AddRecordAsync()
        {
            try
            {
                this.IsBusy = true;
                await ExecuteAddRecordAsync();
                CleanEntry();
                NotifyOfPropertyChange(nameof(CanAddRecord));
                NotifyOfPropertyChange(nameof(CanPublishAccountingEntry));
                this.SetFocus(nameof(this.AccountingAccounts));
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

        /// <summary>
        /// Agrega una línea al borrador. Flujo alineado al schema actual:
        /// 1. Si es el primer registro (DraftMasterId == 0) → createAccountingEntryDraft(header only)
        ///    para materializar el borrador, y luego upsertDraftLines(lines: [firstLine]).
        /// 2. Si ya existe borrador → upsertDraftLines(lines: [newLine]) directamente.
        /// Los totales se recalculan client-side desde la colección local.
        /// </summary>
        public async Task ExecuteAddRecordAsync()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            if (this.DraftMasterId == 0)
            {
                // Paso 1: crear el borrador con solo el header.
                // CanAddRecord garantiza que los tres IDs del header no son null al llegar aquí.
                var (createFragment, createQuery) = _createDraftQuery.Value;
                object createVariables = new GraphQLVariables()
                    .For(createFragment, "input", new
                    {
                        accountingBookId = this.SelectedAccountingBookId!.Value,
                        accountingSourceId = this.SelectedAccountingSourceId!.Value,
                        costCenterId = this.SelectedCostCenterId!.Value,
                        description = this.Description,
                        documentDate = this.DocumentDate.ToIsoDate()
                    })
                    .Build();

                var draftPayload = await this._draftAccountingEntryService
                    .CreateAsync<UpsertResponseType<DraftAccountingEntryGraphQLModel>>(createQuery, createVariables);

                if (!draftPayload.Success)
                    throw new Exception(draftPayload.Message);

                this.DraftMasterId = draftPayload.Entity.Id;
                this.SelectedDraftAccountingEntry = draftPayload.Entity;
                this.AccountingEntries = [];

                // Publicar mensaje de creación del borrador para que el Master lo agregue a su lista.
                await this.Context.EventAggregator.PublishOnUIThreadAsync(draftPayload.Entity);

                // Re-seedear: los valores del header ya están persistidos en BD.
                // Sin esto, DocumentDate y Description quedan como "changed" en el tracker
                // porque sus seeds tienen valor real (no null) y TrackChange sin currentValue
                // compara Equals(seed, null) → false.
                SeedCurrentValues();
            }

            // Paso 2 (siempre): upsert de la línea nueva contra el borrador vigente.
            var (upsertFragment, upsertQuery) = _upsertDraftLinesQuery.Value;
            object upsertVariables = new GraphQLVariables()
                .For(upsertFragment, "input", new
                {
                    draftId = (int)this.DraftMasterId,
                    lines = new[]
                    {
                        new
                        {
                            accountingAccountId = this.SelectedAccountingAccountOnEntryId!.Value,
                            accountingEntityId = (int)this.SelectedAccountingEntity!.Id,
                            costCenterId = this.SelectedCostCenterOnEntryId!.Value,
                            recordDetail = this.RecordDetail,
                            debit = this.Debit,
                            credit = this.Credit,
                            @base = this.Base
                        }
                    }
                })
                .Build();

            var upsertPayload = await this._draftAccountingEntryLineService
                .MutationContextAsync<UpsertDraftLinesPayloadWrapper>(upsertQuery, upsertVariables);
            var upsertResult = upsertPayload.UpsertResponse;

            if (!upsertResult.Success)
                throw new Exception(upsertResult.Message);

            stopwatch.Stop();
            this.EntriesResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";

            // Recargar las líneas del borrador desde el API para tener los datos completos con relaciones.
            await ReloadDraftLinesAsync();
        }

        /// <summary>
        /// Recarga todas las líneas del borrador actual desde el API.
        /// Necesario después de cualquier mutación sobre líneas (upsert/delete) porque
        /// ni upsertDraftLines ni deleteDraftLines retornan los objetos completos con relaciones.
        /// También recalcula los totales client-side desde la colección recargada.
        /// </summary>
        private async Task ReloadDraftLinesAsync()
        {
            var (fragment, query) = _loadDraftLinesQuery.Value;
            object variables = new GraphQLVariables()
                .For(fragment, "id", (int)this.DraftMasterId)
                .Build();

            var draft = await this._draftAccountingEntryService.FindByIdAsync(query, variables);
            IEnumerable<DraftAccountingEntryLineGraphQLModel> rawLines = draft?.Lines ?? [];
            var mappedLines = this.Context.Mapper.Map<IEnumerable<DraftAccountingEntryLineDTO>>(rawLines);

            App.Current.Dispatcher.Invoke(() =>
            {
                this.AccountingEntries = new ObservableCollection<DraftAccountingEntryLineDTO>(mappedLines);
            });

            RecalculateTotals();
        }

        /// <summary>
        /// Recalcula TotalDebit y TotalCredit sumando los valores de la colección local de líneas.
        /// Reemplaza la antigua query <c>accountingEntryDraftTotals</c> del API (ya no existe).
        /// </summary>
        private void RecalculateTotals()
        {
            this.TotalDebit = this.AccountingEntries?.Sum(e => e.Debit) ?? 0;
            this.TotalCredit = this.AccountingEntries?.Sum(e => e.Credit) ?? 0;
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
                case nameof(SelectedAccountingEntity):
                    if (SelectedAccountingEntity is null) AddError(propertyName, "El tercero del registro no puede estar vacío");
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
            ValidateProperty(nameof(SelectedAccountingBookId), null, SelectedAccountingBookId.GetValueOrDefault());
            ValidateProperty(nameof(SelectedCostCenterId), null, SelectedCostCenterId.GetValueOrDefault());
            ValidateProperty(nameof(SelectedAccountingSourceId), null, SelectedAccountingSourceId.GetValueOrDefault());
            ValidateProperty(nameof(DocumentDate), null);
            ValidateProperty(nameof(SelectedAccountingAccountOnEntryId), null, SelectedAccountingAccountOnEntryId.GetValueOrDefault());
            ValidateProperty(nameof(SelectedAccountingEntity), null);
            ValidateProperty(nameof(SelectedCostCenterOnEntryId), null, SelectedCostCenterOnEntryId.GetValueOrDefault());
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
                    this.AccountingEntries = new ObservableCollection<DraftAccountingEntryLineDTO>(this.AccountingEntries);
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
