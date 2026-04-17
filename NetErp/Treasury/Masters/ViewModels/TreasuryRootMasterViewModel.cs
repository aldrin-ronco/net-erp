using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using Models.Global;
using Models.Treasury;
using NetErp.Global.CostCenters.DTO;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Treasury.Masters.DTO;
using NetErp.Treasury.Masters.PanelEditors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Treasury.Masters.ViewModels
{
    public class TreasuryRootMasterViewModel : Screen,
        IHandle<TreasuryCashDrawerCreateMessage>,
        IHandle<TreasuryCashDrawerDeleteMessage>,
        IHandle<TreasuryCashDrawerUpdateMessage>,
        IHandle<BankCreateMessage>,
        IHandle<BankUpdateMessage>,
        IHandle<BankDeleteMessage>,
        IHandle<BankAccountCreateMessage>,
        IHandle<BankAccountDeleteMessage>,
        IHandle<BankAccountUpdateMessage>,
        IHandle<FranchiseCreateMessage>,
        IHandle<FranchiseDeleteMessage>,
        IHandle<FranchiseUpdateMessage>
    {
        public TreasuryRootViewModel Context { get; set; }

        private readonly IRepository<CashDrawerGraphQLModel> _cashDrawerService;
        private readonly IRepository<BankGraphQLModel> _bankService;
        private readonly IRepository<BankAccountGraphQLModel> _bankAccountService;
        private readonly IRepository<FranchiseGraphQLModel> _franchiseService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IGraphQLClient _graphQLClient;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly CompanyLocationCache _companyLocationCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly BankAccountCache _bankAccountCache;
        private readonly MajorCashDrawerCache _majorCashDrawerCache;
        private readonly MinorCashDrawerCache _minorCashDrawerCache;
        private readonly AuxiliaryCashDrawerCache _auxiliaryCashDrawerCache;
        private readonly BankCache _bankCache;
        private readonly FranchiseCache _franchiseCache;

        #region Panel Editors

        public MajorCashDrawerPanelEditor MajorCashDrawerEditor { get; private set; }
        public MinorCashDrawerPanelEditor MinorCashDrawerEditor { get; private set; }
        public AuxiliaryCashDrawerPanelEditor AuxiliaryCashDrawerEditor { get; private set; }
        public BankPanelEditor BankEditor { get; private set; }
        public BankAccountPanelEditor BankAccountEditor { get; private set; }
        public FranchisePanelEditor FranchiseEditor { get; private set; }

        private ITreasuryMastersPanelEditor? _currentPanelEditor;
        public ITreasuryMastersPanelEditor? CurrentPanelEditor
        {
            get => _currentPanelEditor;
            private set
            {
                if (_currentPanelEditor != value)
                {
                    _currentPanelEditor = value;
                    NotifyOfPropertyChange(nameof(CurrentPanelEditor));
                }
            }
        }

        #endregion

        public ObservableCollection<object> DummyItems { get; set; } = [];

        private bool _isNewRecord = false;

        public bool IsNewRecord
        {
            get { return _isNewRecord; }
            set
            {
                if (_isNewRecord != value)
                {
                    _isNewRecord = value;
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        private ITreasuryTreeMasterSelectedItem? _selectedItem;

        public ITreasuryTreeMasterSelectedItem? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(ContentControlVisibility));
                    HandleSelectedItemChanged();
                }
            }
        }

        public void HandleSelectedItemChanged()
        {
            if (_selectedItem != null)
            {
                // Determine which Panel Editor to use based on selected item type
                CurrentPanelEditor = _selectedItem switch
                {
                    MajorCashDrawerMasterTreeDTO => MajorCashDrawerEditor,
                    MinorCashDrawerMasterTreeDTO => MinorCashDrawerEditor,
                    TreasuryAuxiliaryCashDrawerMasterTreeDTO => AuxiliaryCashDrawerEditor,
                    TreasuryBankMasterTreeDTO => BankEditor,
                    TreasuryBankAccountMasterTreeDTO => BankAccountEditor,
                    TreasuryFranchiseMasterTreeDTO => FranchiseEditor,
                    _ => null
                };

                if (!IsNewRecord)
                {
                    IsEditing = false;
                    CanEdit = true;
                    CanUndo = false;
                    SelectedIndex = 0;
                    if (_selectedItem is MajorCashDrawerMasterTreeDTO majorCashDrawerMasterTreeDTO)
                    {
                        MajorCashDrawerEditor.SetForEdit(majorCashDrawerMasterTreeDTO);
                        return;
                    }
                    if (_selectedItem is MinorCashDrawerMasterTreeDTO minorCashDrawerMasterTreeDTO)
                    {
                        MinorCashDrawerEditor.SetForEdit(minorCashDrawerMasterTreeDTO);
                        return;
                    }
                    if (_selectedItem is TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawer)
                    {
                        AuxiliaryCashDrawerEditor.SetForEdit(auxiliaryCashDrawer);
                        return;
                    }
                    if (_selectedItem is TreasuryBankMasterTreeDTO bank)
                    {
                        BankEditor.SetForEdit(bank);
                        return;
                    }
                    if (_selectedItem is TreasuryBankAccountMasterTreeDTO bankAccount)
                    {
                        BankAccountEditor.SetForEdit(bankAccount);
                        return;
                    }
                    if (_selectedItem is TreasuryFranchiseMasterTreeDTO franchise)
                    {
                        FranchiseEditor.SetForEdit(franchise);
                        return;
                    }
                }
                else
                {
                    IsEditing = true;
                    CanUndo = true;
                    CanEdit = false;
                    if (_selectedItem is MajorCashDrawerMasterTreeDTO)
                    {
                        MajorCashDrawerEditor.SetForNew(MajorCostCenterBeforeNewCashDrawer);
                        return;
                    }
                    if (_selectedItem is MinorCashDrawerMasterTreeDTO)
                    {
                        MinorCashDrawerEditor.SetForNew(MinorCostCenterBeforeNewCashDrawer);
                        return;
                    }
                    if (_selectedItem is TreasuryAuxiliaryCashDrawerMasterTreeDTO)
                    {
                        AuxiliaryCashDrawerEditor.SetForNew(MajorCashDrawerBeforeNewAuxiliary);
                        return;
                    }
                    if (_selectedItem is TreasuryBankMasterTreeDTO)
                    {
                        BankEditor.SetForNew(null!);
                        return;
                    }
                    if (_selectedItem is TreasuryBankAccountMasterTreeDTO)
                    {
                        BankAccountEditor.SetForNew(BankBeforeNewBankAccount);
                        return;
                    }
                    if (_selectedItem is TreasuryFranchiseMasterTreeDTO)
                    {
                        FranchiseEditor.SetForNew(null!);
                        return;
                    }
                }
            }
            else
            {
                CurrentPanelEditor = null;
            }
        }
        public bool TreeViewIsEnable => !IsEditing;

        private bool _isEditing = false;

        public bool IsEditing
        {
            get { return _isEditing; }
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    NotifyOfPropertyChange(nameof(IsEditing));
                    NotifyOfPropertyChange(nameof(TreeViewIsEnable));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ICommand _editCommand;
        public ICommand EditCommand
        {
            get
            {
                if (_editCommand is null) _editCommand = new DelegateCommand(Edit);
                return _editCommand;
            }
        }

        public void Edit()
        {
            IsEditing = true;
            CanUndo = true;
            CanEdit = false;

            // Set Panel Editor's IsEditing property
            if (CurrentPanelEditor != null)
            {
                CurrentPanelEditor.IsEditing = true;
            }

            if (SelectedItem is MajorCashDrawerMasterTreeDTO) this.SetFocus("MajorCashDrawerName");
            if (SelectedItem is MinorCashDrawerMasterTreeDTO) this.SetFocus("MinorCashDrawerName");
            if (SelectedItem is TreasuryAuxiliaryCashDrawerMasterTreeDTO) this.SetFocus("AuxiliaryCashDrawerName");
        }

        private bool _canEdit = true;

        public bool CanEdit
        {
            get { return _canEdit; }
            set
            {
                if (_canEdit != value)
                {
                    _canEdit = value;
                    NotifyOfPropertyChange(nameof(CanEdit));
                }
            }
        }

        private ICommand _undoCommand;

        public ICommand UndoCommand
        {
            get
            {
                if (_undoCommand is null) _undoCommand = new DelegateCommand(Undo);
                return _undoCommand;
            }
        }

        public void Undo()
        {
            // Call Panel Editor's Undo - restores original values
            CurrentPanelEditor?.Undo();

            if (IsNewRecord)
            {
                SelectedItem = null;
            }
            IsEditing = false;
            CanUndo = false;
            CanEdit = true;
            IsNewRecord = false;
            SelectedIndex = 0;
        }

        private bool _canUndo = false;
        public bool CanUndo
        {
            get { return _canUndo; }
            set
            {
                if (_canUndo != value)
                {
                    _canUndo = value;
                    NotifyOfPropertyChange(nameof(CanUndo));
                }
            }
        }

        private ICommand _createMajorCashDrawerCommand;
        public ICommand CreateMajorCashDrawerCommand
        {
            get
            {
                if (_createMajorCashDrawerCommand is null) _createMajorCashDrawerCommand = new AsyncCommand(CreateMajorCashDrawer, CanCreateMajorCashDrawer);
                return _createMajorCashDrawerCommand;
            }
        }

        public async Task CreateMajorCashDrawer()
        {
            IsNewRecord = true;
            SelectedIndex = 0;
            SelectedItem = new MajorCashDrawerMasterTreeDTO();

            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.SetFocus("MajorCashDrawerName");
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public bool CanCreateMajorCashDrawer => true;



        private ICommand _createMinorCashDrawerCommand;
        public ICommand CreateMinorCashDrawerCommand
        {
            get
            {
                if (_createMinorCashDrawerCommand is null) _createMinorCashDrawerCommand = new AsyncCommand(CreateMinorCashDrawer, CanCreateMinorCashDrawer);
                return _createMinorCashDrawerCommand;
            }
        }

        public async Task CreateMinorCashDrawer()
        {
            IsNewRecord = true;
            SelectedIndex = 0;
            SelectedItem = new MinorCashDrawerMasterTreeDTO();

            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.SetFocus("MinorCashDrawerName");
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public bool CanCreateMinorCashDrawer => true;


        private ICommand _createAuxiliaryCashDrawerCommand;
        public ICommand CreateAuxiliaryCashDrawerCommand
        {
            get
            {
                if (_createAuxiliaryCashDrawerCommand is null) _createAuxiliaryCashDrawerCommand = new AsyncCommand(CreateAuxiliaryCashDrawer, CanCreateAuxiliaryCashDrawer);
                return _createAuxiliaryCashDrawerCommand;
            }
        }

        public async Task CreateAuxiliaryCashDrawer()
        {
            IsNewRecord = true;
            SelectedIndex = 0;
            SelectedItem = new TreasuryAuxiliaryCashDrawerMasterTreeDTO();

            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.SetFocus("AuxiliaryCashDrawerName");
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public bool CanCreateAuxiliaryCashDrawer => true;

        private ICommand _createBankCommand;
        public ICommand CreateBankCommand
        {
            get
            {
                if (_createBankCommand is null) _createBankCommand = new AsyncCommand(CreateBank, CanCreateBank);
                return _createBankCommand;
            }
        }

        public async Task CreateBank()
        {
            IsNewRecord = true;
            SelectedIndex = 0;
            SelectedItem = new TreasuryBankMasterTreeDTO();

            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.SetFocus("BankCode");
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public bool CanCreateBank => true;

        private ICommand _createBankAccountCommand;
        public ICommand CreateBankAccountCommand
        {
            get
            {
                if (_createBankAccountCommand is null) _createBankAccountCommand = new AsyncCommand(CreateBankAccount, CanCreateBankAccount);
                return _createBankAccountCommand;
            }
        }

        public async Task CreateBankAccount()
        {
            IsNewRecord = true;
            SelectedIndex = 0;
            SelectedItem = new TreasuryBankAccountMasterTreeDTO();

            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.SetFocus("BankAccountNumber");
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public bool CanCreateBankAccount => true;

        private ICommand _createFranchiseCommand;
        public ICommand CreateFranchiseCommand
        {
            get
            {
                if (_createFranchiseCommand is null) _createFranchiseCommand = new DelegateCommand(CreateFranchise);
                return _createFranchiseCommand;
            }
        }

        public void CreateFranchise()
        {
            IsNewRecord = true;
            SelectedIndex = 0;
            SelectedItem = new TreasuryFranchiseMasterTreeDTO();
            _ = Application.Current.Dispatcher.BeginInvoke(()  =>
                {
                this.SetFocus("FranchiseName");
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private ObservableCollection<AccountingAccountGraphQLModel> _cashDrawerAccountingAccounts;

        public ObservableCollection<AccountingAccountGraphQLModel> CashDrawerAccountingAccounts
        {
            get { return _cashDrawerAccountingAccounts; }
            set
            {
                if (_cashDrawerAccountingAccounts != value)
                {
                    _cashDrawerAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(CashDrawerAccountingAccounts));
                }
            }
        }


        private int _selectedIndex = 0;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    NotifyOfPropertyChange(nameof(SelectedIndex));
                }
            }
        }

        public bool ContentControlVisibility
        {
            get
            {
                if (_selectedItem != null && _selectedItem.AllowContentControlVisibility)
                {
                    if (_selectedItem is MajorCashDrawerMasterTreeDTO majorcashDrawer) MajorCashDrawerBeforeNewAuxiliary = majorcashDrawer;
                    if (_selectedItem is TreasuryBankMasterTreeDTO bank) BankBeforeNewBankAccount = bank;
                    return true;
                }
                if (_selectedItem is CashDrawerCostCenterDTO costCenterDTO)
                {
                    if (costCenterDTO.Type == CashDrawerType.Major) MajorCostCenterBeforeNewCashDrawer = costCenterDTO;
                    else if (costCenterDTO.Type == CashDrawerType.Minor) MinorCostCenterBeforeNewCashDrawer = costCenterDTO;
                }
                SelectedItem = null;
                return false;
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

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(Save);
                return _saveCommand;
            }
        }

        public async Task Save()
        {
            if (CurrentPanelEditor == null) return;

            try
            {
                IsBusy = true;
                Refresh();

                // Delegate save to the current Panel Editor
                bool saveSuccessful = await CurrentPanelEditor.SaveAsync();

                if (saveSuccessful)
                {
                    IsEditing = false;
                    CanUndo = false;
                    CanEdit = true;
                    IsNewRecord = false;
                    SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                // Panel Editor already handles GraphQL errors, this catches any remaining exceptions
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.Save \r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool CanSave => CurrentPanelEditor?.CanSave ?? false;

        public void RefreshCanSave() => NotifyOfPropertyChange(nameof(CanSave));

        private async Task DeleteEntityAsync<TModel>(
            string displayName,
            int id,
            string canDeleteFragmentName,
            string deleteFragmentName,
            IRepository<TModel> service,
            Func<DeleteResponseType, Task> publishMessage)
        {
            try
            {
                IsBusy = true;
                Refresh();

                string canDeleteQuery = BuildCanDeleteQuery(canDeleteFragmentName);
                object canDeleteVariables = new { canDeleteResponseId = id };
                var validation = await service.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(
                        title: "Confirme...",
                        text: $"¿Confirma que desea eliminar el registro {displayName}?",
                        messageBoxButtons: MessageBoxButton.YesNo,
                        image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"El registro no puede ser eliminado\n\n{validation.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                DeleteResponseType deleteResult = await ExecuteDeleteAsync(service, deleteFragmentName, id);

                if (!deleteResult.Success)
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"No se pudo eliminar el registro.\n\n{deleteResult.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return;
                }

                await publishMessage(deleteResult);
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(
                    exGraphQL.Content?.ToString() ?? "");
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"{GetType().Name}.DeleteEntityAsync \r\n{graphQLError.Errors[0].Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
                }
                else { throw; }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.DeleteEntityAsync \r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private ICommand _deleteMajorCashDrawerCommand;
        public ICommand DeleteMajorCashDrawerCommand
        {
            get
            {
                if (_deleteMajorCashDrawerCommand is null) _deleteMajorCashDrawerCommand = new AsyncCommand(DeleteMajorCashDrawer);
                return _deleteMajorCashDrawerCommand;
            }
        }

        public async Task DeleteMajorCashDrawer()
        {
            var selected = (MajorCashDrawerMasterTreeDTO)SelectedItem;
            await DeleteEntityAsync(selected.Name, selected.Id,
                "canDeleteCashDrawer", "deleteCashDrawer", _cashDrawerService,
                async result => await Context.EventAggregator.PublishOnUIThreadAsync(
                    new TreasuryCashDrawerDeleteMessage { DeletedCashDrawer = result }));
        }

        #region Delete Query Builders

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _canDeleteQueryCache = new();

        private static string BuildCanDeleteQuery(string fragmentName)
        {
            return _canDeleteQueryCache.GetOrAdd(fragmentName, name =>
            {
                var fields = FieldSpec<CanDeleteType>.Create()
                    .Field(f => f.CanDelete)
                    .Field(f => f.Message)
                    .Build();

                var parameter = new GraphQLQueryParameter("id", "ID!");
                var fragment = new GraphQLQueryFragment(name, [parameter], fields, "CanDeleteResponse");
                return new GraphQLQueryBuilder([fragment]).GetQuery();
            });
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _deleteMutationQueryCache = new();

        private static string BuildDeleteMutationQuery(string fragmentName)
        {
            return _deleteMutationQueryCache.GetOrAdd(fragmentName, name =>
            {
                var fields = FieldSpec<DeleteResponseType>.Create()
                    .Field(f => f.DeletedId)
                    .Field(f => f.Message)
                    .Field(f => f.Success)
                    .Build();

                var parameter = new GraphQLQueryParameter("id", "ID!");
                var fragment = new GraphQLQueryFragment(name, [parameter], fields, "DeleteResponse");
                return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
            });
        }

        #endregion

        #region Execute Delete Methods

        private async Task<DeleteResponseType> ExecuteDeleteAsync<TModel>(
            IRepository<TModel> service, string deleteFragmentName, int id)
        {
            string query = BuildDeleteMutationQuery(deleteFragmentName);
            object variables = new { deleteResponseId = id };
            DeleteResponseType result = await service.DeleteAsync<DeleteResponseType>(query, variables);
            SelectedItem = null;
            return result;
        }

        #endregion

        private ICommand _deleteMinorCashDrawerCommand;
        public ICommand DeleteMinorCashDrawerCommand
        {
            get
            {
                if (_deleteMinorCashDrawerCommand is null) _deleteMinorCashDrawerCommand = new AsyncCommand(DeleteMinorCashDrawer);
                return _deleteMinorCashDrawerCommand;
            }
        }

        public async Task DeleteMinorCashDrawer()
        {
            var selected = (MinorCashDrawerMasterTreeDTO)SelectedItem;
            await DeleteEntityAsync(selected.Name, selected.Id,
                "canDeleteCashDrawer", "deleteCashDrawer", _cashDrawerService,
                async result => await Context.EventAggregator.PublishOnUIThreadAsync(
                    new TreasuryCashDrawerDeleteMessage { DeletedCashDrawer = result }));
        }

        private ICommand _deleteAuxiliaryCashDrawerCommand;
        public ICommand DeleteAuxiliaryCashDrawerCommand
        {
            get
            {
                if (_deleteAuxiliaryCashDrawerCommand is null) _deleteAuxiliaryCashDrawerCommand = new AsyncCommand(DeleteAuxiliaryCashDrawer);
                return _deleteAuxiliaryCashDrawerCommand;
            }
        }

        public async Task DeleteAuxiliaryCashDrawer()
        {
            var selected = (TreasuryAuxiliaryCashDrawerMasterTreeDTO)SelectedItem;
            await DeleteEntityAsync(selected.Name, selected.Id,
                "canDeleteCashDrawer", "deleteCashDrawer", _cashDrawerService,
                async result => await Context.EventAggregator.PublishOnUIThreadAsync(
                    new TreasuryCashDrawerDeleteMessage { DeletedCashDrawer = result }));
        }

        private ICommand _deleteBankCommand;
        public ICommand DeleteBankCommand
        {
            get
            {
                if (_deleteBankCommand is null) _deleteBankCommand = new AsyncCommand(DeleteBank);
                return _deleteBankCommand;
            }
        }

        public async Task DeleteBank()
        {
            var selected = (TreasuryBankMasterTreeDTO)SelectedItem;
            await DeleteEntityAsync(selected.AccountingEntity.SearchName, selected.Id,
                "canDeleteBank", "deleteBank", _bankService,
                async result => await Context.EventAggregator.PublishOnUIThreadAsync(
                    new BankDeleteMessage { DeletedBank = result }));
        }

        private ICommand _deleteBankAccountCommand;
        public ICommand DeleteBankAccountCommand
        {
            get
            {
                if (_deleteBankAccountCommand is null) _deleteBankAccountCommand = new AsyncCommand(DeleteBankAccount);
                return _deleteBankAccountCommand;
            }
        }

        public async Task DeleteBankAccount()
        {
            if (SelectedItem is not TreasuryBankAccountMasterTreeDTO selected) return;
            await DeleteEntityAsync(selected.Description, selected.Id,
                "canDeleteBankAccount", "deleteBankAccount", _bankAccountService,
                async result => await Context.EventAggregator.PublishOnUIThreadAsync(
                    new BankAccountDeleteMessage { DeletedBankAccount = result }));
        }

        private ICommand _deleteFranchiseCommand;
        public ICommand DeleteFranchiseCommand
        {
            get
            {
                if (_deleteFranchiseCommand is null) _deleteFranchiseCommand = new AsyncCommand(DeleteFranchise);
                return _deleteFranchiseCommand;
            }
        }

        public async Task DeleteFranchise()
        {
            var selected = (TreasuryFranchiseMasterTreeDTO)SelectedItem;
            await DeleteEntityAsync(selected.Name, selected.Id,
                "canDeleteFranchise", "deleteFranchise", _franchiseService,
                async result => await Context.EventAggregator.PublishOnUIThreadAsync(
                    new FranchiseDeleteMessage { DeletedFranchise = result }));
        }

        #region "MajorCashDrawer Context Properties"

        public CashDrawerCostCenterDTO MajorCostCenterBeforeNewCashDrawer { get; set; } = new() { Type = CashDrawerType.Major };

        // Collection used by MajorCashDrawerPanelEditor
        private ObservableCollection<CashDrawerGraphQLModel> _majorAutoTransferCashDrawerCashDrawers;
        public ObservableCollection<CashDrawerGraphQLModel> MajorAutoTransferCashDrawerCashDrawers
        {
            get { return _majorAutoTransferCashDrawerCashDrawers; }
            set
            {
                if (_majorAutoTransferCashDrawerCashDrawers != value)
                {
                    _majorAutoTransferCashDrawerCashDrawers = value;
                    NotifyOfPropertyChange(nameof(MajorAutoTransferCashDrawerCashDrawers));
                }
            }
        }

        #endregion

        #region "MinorCashDrawer Context Properties"

        public CashDrawerCostCenterDTO MinorCostCenterBeforeNewCashDrawer { get; set; } = new() { Type = CashDrawerType.Minor };

        #endregion

        #region "AuxiliaryCashDrawer Context Properties"

        public MajorCashDrawerMasterTreeDTO? MajorCashDrawerBeforeNewAuxiliary { get; set; }

        // Collection used by AuxiliaryCashDrawerPanelEditor
        private ObservableCollection<CashDrawerGraphQLModel> _auxiliaryAutoTransferCashDrawerCashDrawers = [];
        public ObservableCollection<CashDrawerGraphQLModel> AuxiliaryAutoTransferCashDrawerCashDrawers
        {
            get { return _auxiliaryAutoTransferCashDrawerCashDrawers; }
            set
            {
                if (_auxiliaryAutoTransferCashDrawerCashDrawers != value)
                {
                    _auxiliaryAutoTransferCashDrawerCashDrawers = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryAutoTransferCashDrawerCashDrawers));
                }
            }
        }

        #endregion

        #region "BankAccount Context Properties"

        public TreasuryBankMasterTreeDTO BankBeforeNewBankAccount { get; set; } = new();

        // Collections used by BankAccountPanelEditor
        private ObservableCollection<AccountingAccountGraphQLModel> _bankAccountAccountingAccounts;
        public ObservableCollection<AccountingAccountGraphQLModel> BankAccountAccountingAccounts
        {
            get { return _bankAccountAccountingAccounts; }
            set
            {
                if (_bankAccountAccountingAccounts != value)
                {
                    _bankAccountAccountingAccounts = value;
                    NotifyOfPropertyChange(nameof(BankAccountAccountingAccounts));
                }
            }
        }

        private ObservableCollection<TreasuryBankAccountCostCenterDTO> _bankAccountCostCenters;
        public ObservableCollection<TreasuryBankAccountCostCenterDTO> BankAccountCostCenters
        {
            get { return _bankAccountCostCenters; }
            set
            {
                if (_bankAccountCostCenters != value)
                {
                    _bankAccountCostCenters = value;
                    NotifyOfPropertyChange(nameof(BankAccountCostCenters));
                }
            }
        }

        #endregion

        #region "Franchise Collections"

        // Collections used by FranchisePanelEditor
        private ObservableCollection<AccountingAccountGraphQLModel> _franchiseAccountingAccountsCommission;
        public ObservableCollection<AccountingAccountGraphQLModel> FranchiseAccountingAccountsCommission
        {
            get { return _franchiseAccountingAccountsCommission; }
            set
            {
                if (_franchiseAccountingAccountsCommission != value)
                {
                    _franchiseAccountingAccountsCommission = value;
                    NotifyOfPropertyChange(nameof(FranchiseAccountingAccountsCommission));
                }
            }
        }

        private ObservableCollection<BankAccountGraphQLModel> _franchiseBankAccounts;
        public ObservableCollection<BankAccountGraphQLModel> FranchiseBankAccounts
        {
            get { return _franchiseBankAccounts; }
            set
            {
                if (_franchiseBankAccounts != value)
                {
                    _franchiseBankAccounts = value;
                    NotifyOfPropertyChange(nameof(FranchiseBankAccounts));
                }
            }
        }

        private ObservableCollection<TreasuryFranchiseCostCenterDTO> _franchiseCostCenters;
        public ObservableCollection<TreasuryFranchiseCostCenterDTO> FranchiseCostCenters
        {
            get { return _franchiseCostCenters; }
            set
            {
                if (_franchiseCostCenters != value)
                {
                    _franchiseCostCenters = value;
                    NotifyOfPropertyChange(nameof(FranchiseCostCenters));
                }
            }
        }

        #endregion

        /// <summary>
        /// Reconstruye el árbol completo desde los caches ya hidratados.
        /// Sustituye la carga perezosa previa: ya no se disparan queries cuando el usuario expande nodos.
        /// Debe ejecutarse en el hilo de UI.
        /// </summary>
        private void BuildTreeFromCaches()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CashDrawerDummyDTO? majorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Major);
                CashDrawerDummyDTO? minorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Minor);
                BankDummyDTO? bankDummy = DummyItems.OfType<BankDummyDTO>().FirstOrDefault();
                FranchiseDummyDTO? franchiseDummy = DummyItems.OfType<FranchiseDummyDTO>().FirstOrDefault();

                if (majorDummy != null)
                {
                    majorDummy.Locations.Clear();
                    foreach (var loc in _companyLocationCache.Items)
                    {
                        var locationDTO = Context.AutoMapper.Map<CashDrawerCompanyLocationDTO>(loc);
                        locationDTO.Context = this;
                        locationDTO.DummyParent = majorDummy;
                        locationDTO.Type = CashDrawerType.Major;
                        locationDTO.CostCenters.Clear();

                        foreach (var cc in _costCenterCache.Items.Where(c => c.CompanyLocation != null && c.CompanyLocation.Id == loc.Id))
                        {
                            var costCenterDTO = Context.AutoMapper.Map<CashDrawerCostCenterDTO>(cc);
                            costCenterDTO.Context = this;
                            costCenterDTO.Location = locationDTO;
                            costCenterDTO.Type = CashDrawerType.Major;
                            costCenterDTO.CashDrawers.Clear();

                            foreach (var major in _majorCashDrawerCache.Items.Where(m =>
                                m.CostCenter != null && m.CostCenter.Id == cc.Id && !m.IsPettyCash && m.Parent == null))
                            {
                                var majorDTO = Context.AutoMapper.Map<MajorCashDrawerMasterTreeDTO>(major);
                                majorDTO.Context = this;
                                majorDTO.AuxiliaryCashDrawers.Clear();

                                foreach (var aux in _auxiliaryCashDrawerCache.Items.Where(a =>
                                    a.Parent != null && a.Parent.Id == major.Id))
                                {
                                    var auxDTO = Context.AutoMapper.Map<TreasuryAuxiliaryCashDrawerMasterTreeDTO>(aux);
                                    auxDTO.Context = this;
                                    majorDTO.AuxiliaryCashDrawers.Add(auxDTO);
                                }
                                costCenterDTO.CashDrawers.Add(majorDTO);
                            }
                            locationDTO.CostCenters.Add(costCenterDTO);
                        }
                        majorDummy.Locations.Add(locationDTO);
                    }
                }

                if (minorDummy != null)
                {
                    minorDummy.Locations.Clear();
                    foreach (var loc in _companyLocationCache.Items)
                    {
                        var locationDTO = Context.AutoMapper.Map<CashDrawerCompanyLocationDTO>(loc);
                        locationDTO.Context = this;
                        locationDTO.DummyParent = minorDummy;
                        locationDTO.Type = CashDrawerType.Minor;
                        locationDTO.CostCenters.Clear();

                        foreach (var cc in _costCenterCache.Items.Where(c => c.CompanyLocation != null && c.CompanyLocation.Id == loc.Id))
                        {
                            var costCenterDTO = Context.AutoMapper.Map<CashDrawerCostCenterDTO>(cc);
                            costCenterDTO.Context = this;
                            costCenterDTO.Location = locationDTO;
                            costCenterDTO.Type = CashDrawerType.Minor;
                            costCenterDTO.CashDrawers.Clear();

                            foreach (var minor in _minorCashDrawerCache.Items.Where(m =>
                                m.CostCenter != null && m.CostCenter.Id == cc.Id && m.IsPettyCash && m.Parent == null))
                            {
                                var minorDTO = Context.AutoMapper.Map<MinorCashDrawerMasterTreeDTO>(minor);
                                minorDTO.Context = this;
                                costCenterDTO.CashDrawers.Add(minorDTO);
                            }
                            locationDTO.CostCenters.Add(costCenterDTO);
                        }
                        minorDummy.Locations.Add(locationDTO);
                    }
                }

                if (bankDummy != null)
                {
                    bankDummy.Banks.Clear();
                    foreach (var bank in _bankCache.Items)
                    {
                        var bankDTO = Context.AutoMapper.Map<TreasuryBankMasterTreeDTO>(bank);
                        bankDTO.Context = this;
                        bankDTO.DummyParent = bankDummy;
                        bankDTO.BankAccounts.Clear();

                        foreach (var ba in _bankAccountCache.Items.Where(x => x.Bank != null && x.Bank.Id == bank.Id))
                        {
                            var bankAccountDTO = Context.AutoMapper.Map<TreasuryBankAccountMasterTreeDTO>(ba);
                            bankAccountDTO.Context = this;
                            bankDTO.BankAccounts.Add(bankAccountDTO);
                        }
                        bankDummy.Banks.Add(bankDTO);
                    }
                }

                if (franchiseDummy != null)
                {
                    franchiseDummy.Franchises.Clear();
                    foreach (var franchise in _franchiseCache.Items)
                    {
                        var franchiseDTO = Context.AutoMapper.Map<TreasuryFranchiseMasterTreeDTO>(franchise);
                        franchiseDTO.Context = this;
                        franchiseDTO.DummyParent = franchiseDummy;
                        franchiseDummy.Franchises.Add(franchiseDTO);
                    }
                }
            });
        }


        /// <summary>
        /// Puebla las colecciones usadas por los PanelEditors (combos de cuentas contables,
        /// centros de costo, cuentas bancarias y cajas de auto-transferencia) a partir de los caches.
        /// Mantenida porque los callers externos (handlers de create con side-effects de la API)
        /// la invocan después de <c>_auxiliaryAccountingAccountCache.Clear()</c> para refrescar los combos.
        /// </summary>
        public async Task LoadComboBoxesAsync()
        {
            try
            {
                // Asegura que todos los caches estén cargados (paralelo, un solo round-trip HTTP)
                await CacheBatchLoader.LoadAsync(
                    _graphQLClient, default,
                    _auxiliaryAccountingAccountCache,
                    _companyLocationCache,
                    _costCenterCache,
                    _bankAccountCache,
                    _majorCashDrawerCache,
                    _minorCashDrawerCache,
                    _auxiliaryCashDrawerCache,
                    _bankCache,
                    _franchiseCache);

                // Cuentas contables auxiliares
                CashDrawerAccountingAccounts = new ObservableCollection<AccountingAccountGraphQLModel>(_auxiliaryAccountingAccountCache.Items);
                BankAccountAccountingAccounts = new ObservableCollection<AccountingAccountGraphQLModel>(_auxiliaryAccountingAccountCache.Items);

                FranchiseAccountingAccountsCommission = new ObservableCollection<AccountingAccountGraphQLModel>(_auxiliaryAccountingAccountCache.Items);

                // Centros de costo
                BankAccountCostCenters = Context.AutoMapper.Map<ObservableCollection<TreasuryBankAccountCostCenterDTO>>(_costCenterCache.Items);
                FranchiseCostCenters = Context.AutoMapper.Map<ObservableCollection<TreasuryFranchiseCostCenterDTO>>(_costCenterCache.Items);
                FranchiseCostCenters.Insert(0, new TreasuryFranchiseCostCenterDTO() { Id = 0, Name = "[ APLICACIÓN GENERAL ]" });

                // Cuentas bancarias
                FranchiseBankAccounts = new ObservableCollection<BankAccountGraphQLModel>(_bankAccountCache.Items);

                // Cajas para auto-transferencia
                MajorAutoTransferCashDrawerCashDrawers = new ObservableCollection<CashDrawerGraphQLModel>(_majorCashDrawerCache.Items);

                AuxiliaryAutoTransferCashDrawerCashDrawers = new ObservableCollection<CashDrawerGraphQLModel>(_majorCashDrawerCache.Items);
            }
            catch (AsyncException ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.LoadComboBoxesAsync {ex.Message}\r\n{ex.InnerException?.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.LoadComboBoxesAsync \r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
            }
        }

        public TreasuryRootMasterViewModel(
            TreasuryRootViewModel context,
            IRepository<CashDrawerGraphQLModel> cashDrawerService,
            IRepository<BankGraphQLModel> bankService,
            IRepository<BankAccountGraphQLModel> bankAccountService,
            IRepository<FranchiseGraphQLModel> franchiseService,
            Helpers.IDialogService dialogService,
            Helpers.Services.INotificationService notificationService,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            CompanyLocationCache companyLocationCache,
            CostCenterCache costCenterCache,
            BankAccountCache bankAccountCache,
            MajorCashDrawerCache majorCashDrawerCache,
            MinorCashDrawerCache minorCashDrawerCache,
            AuxiliaryCashDrawerCache auxiliaryCashDrawerCache,
            BankCache bankCache,
            FranchiseCache franchiseCache,
            IGraphQLClient graphQLClient)
        {
            _cashDrawerService = cashDrawerService;
            _bankService = bankService;
            _bankAccountService = bankAccountService;
            _franchiseService = franchiseService;
            _dialogService = dialogService;
            _notificationService = notificationService;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _companyLocationCache = companyLocationCache;
            _costCenterCache = costCenterCache;
            _bankAccountCache = bankAccountCache;
            _majorCashDrawerCache = majorCashDrawerCache;
            _minorCashDrawerCache = minorCashDrawerCache;
            _auxiliaryCashDrawerCache = auxiliaryCashDrawerCache;
            _bankCache = bankCache;
            _franchiseCache = franchiseCache;
            _graphQLClient = graphQLClient;

            // Los DummyItems se crean con colecciones vacías; BuildTreeFromCaches las llena en OnActivatedAsync.
            DummyItems = [
                new CashDrawerDummyDTO() { Id = 1, Name = "CAJA GENERAL", Type = CashDrawerType.Major, Context = this },
                new CashDrawerDummyDTO() { Id = 2, Name = "CAJA MENOR", Type = CashDrawerType.Minor, Context = this },
                new BankDummyDTO() { Id = 3, Name = "BANCOS", Context = this },
                new FranchiseDummyDTO() { Id = 4, Name = "FRANQUICIAS", Context = this }
            ];
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);

            // Initialize Panel Editors
            MajorCashDrawerEditor = new MajorCashDrawerPanelEditor(this, _cashDrawerService);
            MinorCashDrawerEditor = new MinorCashDrawerPanelEditor(this, _cashDrawerService);
            AuxiliaryCashDrawerEditor = new AuxiliaryCashDrawerPanelEditor(this, _cashDrawerService);
            BankEditor = new BankPanelEditor(this, _bankService, _dialogService);
            BankAccountEditor = new BankAccountPanelEditor(this, _bankAccountService);
            FranchiseEditor = new FranchisePanelEditor(this, _franchiseService, _notificationService);
        }

        protected override async Task OnActivatedAsync(CancellationToken cancellationToken)
        {
            await CacheBatchLoader.LoadAsync(
                _graphQLClient, cancellationToken,
                _auxiliaryAccountingAccountCache,
                _companyLocationCache,
                _costCenterCache,
                _bankAccountCache,
                _majorCashDrawerCache,
                _minorCashDrawerCache,
                _auxiliaryCashDrawerCache,
                _bankCache,
                _franchiseCache);

            BuildTreeFromCaches();
            await LoadComboBoxesAsync();

            await base.OnActivatedAsync(cancellationToken);
        }

        public async Task HandleAsync(TreasuryCashDrawerCreateMessage message, CancellationToken cancellationToken)
        {
            var createdCashDrawer = message.CreatedCashDrawer.Entity;
            IsNewRecord = false;

            // Recargar combos antes de asignar SelectedItem (que dispara SetForEdit),
            // porque la API puede crear cuentas contables como side-effect
            _auxiliaryAccountingAccountCache.Clear();
            await LoadComboBoxesAsync();

            // Caja general (major)
            if (!createdCashDrawer.IsPettyCash && createdCashDrawer.Parent is null)
            {
                MajorCashDrawerMasterTreeDTO majorDTO = Context.AutoMapper.Map<MajorCashDrawerMasterTreeDTO>(createdCashDrawer);
                majorDTO.Context = this;
                ITreasuryTreeMasterSelectedItem? inserted = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CashDrawerDummyDTO? majorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Major);
                    if (majorDummy is null) return;
                    CashDrawerCompanyLocationDTO? location = majorDummy.Locations.FirstOrDefault(x => x.Id == createdCashDrawer.CostCenter.CompanyLocation.Id);
                    if (location is null) return;
                    CashDrawerCostCenterDTO? costCenter = location.CostCenters.FirstOrDefault(x => x.Id == createdCashDrawer.CostCenter.Id);
                    if (costCenter is null) return;
                    costCenter.CashDrawers.Add(majorDTO);
                    majorDummy.IsExpanded = true;
                    location.IsExpanded = true;
                    costCenter.IsExpanded = true;
                    inserted = majorDTO;
                });
                if (inserted != null) SelectedItem = inserted;
                _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
                return;
            }

            // Caja auxiliar
            if (!createdCashDrawer.IsPettyCash && createdCashDrawer.Parent != null)
            {
                TreasuryAuxiliaryCashDrawerMasterTreeDTO auxDTO = Context.AutoMapper.Map<TreasuryAuxiliaryCashDrawerMasterTreeDTO>(createdCashDrawer);
                auxDTO.Context = this;
                ITreasuryTreeMasterSelectedItem? inserted = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CashDrawerDummyDTO? majorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Major);
                    if (majorDummy is null) return;
                    CashDrawerCompanyLocationDTO? location = majorDummy.Locations.FirstOrDefault(x => x.Id == createdCashDrawer.Parent.CostCenter.CompanyLocation.Id);
                    if (location is null) return;
                    CashDrawerCostCenterDTO? costCenter = location.CostCenters.FirstOrDefault(x => x.Id == createdCashDrawer.Parent.CostCenter.Id);
                    if (costCenter is null) return;
                    MajorCashDrawerMasterTreeDTO? parent = costCenter.CashDrawers.OfType<MajorCashDrawerMasterTreeDTO>().FirstOrDefault(x => x.Id == createdCashDrawer.Parent.Id);
                    if (parent is null) return;
                    parent.AuxiliaryCashDrawers.Add(auxDTO);
                    majorDummy.IsExpanded = true;
                    location.IsExpanded = true;
                    costCenter.IsExpanded = true;
                    parent.IsExpanded = true;
                    inserted = auxDTO;
                });
                if (inserted != null) SelectedItem = inserted;
                _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
                return;
            }

            // Caja menor
            MinorCashDrawerMasterTreeDTO minorDTO = Context.AutoMapper.Map<MinorCashDrawerMasterTreeDTO>(createdCashDrawer);
            minorDTO.Context = this;
            ITreasuryTreeMasterSelectedItem? insertedMinor = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                CashDrawerDummyDTO? minorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Minor);
                if (minorDummy is null) return;
                CashDrawerCompanyLocationDTO? location = minorDummy.Locations.FirstOrDefault(x => x.Id == createdCashDrawer.CostCenter.CompanyLocation.Id);
                if (location is null) return;
                CashDrawerCostCenterDTO? costCenter = location.CostCenters.FirstOrDefault(x => x.Id == createdCashDrawer.CostCenter.Id);
                if (costCenter is null) return;
                costCenter.CashDrawers.Add(minorDTO);
                minorDummy.IsExpanded = true;
                location.IsExpanded = true;
                costCenter.IsExpanded = true;
                insertedMinor = minorDTO;
            });
            if (insertedMinor != null) SelectedItem = insertedMinor;
            _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
        }

        public Task HandleAsync(TreasuryCashDrawerDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                int? deletedId = message.DeletedCashDrawer.DeletedId;

                // Buscar en cajas generales y auxiliares
                CashDrawerDummyDTO? majorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Major);
                if (majorDummy != null)
                {
                    foreach (var location in majorDummy.Locations)
                    {
                        foreach (var costCenter in location.CostCenters)
                        {
                            var majorCashDrawer = costCenter.CashDrawers.FirstOrDefault(x => x.Id == deletedId);
                            if (majorCashDrawer != null)
                            {
                                costCenter.CashDrawers.Remove(majorCashDrawer);
                                SelectedItem = null;
                                return;
                            }

                            foreach (var major in costCenter.CashDrawers.OfType<MajorCashDrawerMasterTreeDTO>())
                            {
                                var auxiliary = major.AuxiliaryCashDrawers.FirstOrDefault(x => x.Id == deletedId);
                                if (auxiliary != null)
                                {
                                    major.AuxiliaryCashDrawers.Remove(auxiliary);
                                    SelectedItem = null;
                                    return;
                                }
                            }
                        }
                    }
                }

                // Buscar en cajas menores
                CashDrawerDummyDTO? minorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Minor);
                if (minorDummy != null)
                {
                    foreach (var location in minorDummy.Locations)
                    {
                        foreach (var costCenter in location.CostCenters)
                        {
                            var minorCashDrawer = costCenter.CashDrawers.FirstOrDefault(x => x.Id == deletedId);
                            if (minorCashDrawer != null)
                            {
                                costCenter.CashDrawers.Remove(minorCashDrawer);
                                SelectedItem = null;
                                return;
                            }
                        }
                    }
                }
            });
            _notificationService.ShowSuccess(message.DeletedCashDrawer.Message);
            return Task.CompletedTask;
        }

        public async Task HandleAsync(TreasuryCashDrawerUpdateMessage message, CancellationToken cancellationToken)
        {
            var updatedCashDrawer = message.UpdatedCashDrawer.Entity;

            if (updatedCashDrawer.IsPettyCash is false && updatedCashDrawer.Parent is null)
            {
                MajorCashDrawerMasterTreeDTO cashDrawerDTO = Context.AutoMapper.Map<MajorCashDrawerMasterTreeDTO>(updatedCashDrawer);
                CashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Major) ?? throw new Exception("");
                if (majorCashDrawerDummyDTO is null) return;
                CashDrawerCompanyLocationDTO majorCashDrawerCompanyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == updatedCashDrawer.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
                if (majorCashDrawerCompanyLocation is null) return;
                CashDrawerCostCenterDTO majorCashDrawerCostCenter = majorCashDrawerCompanyLocation.CostCenters.FirstOrDefault(x => x.Id == updatedCashDrawer.CostCenter.Id) ?? throw new Exception("");
                if (majorCashDrawerCostCenter is null) return;
                MajorCashDrawerMasterTreeDTO cashDrawerToUpdate = majorCashDrawerCostCenter.CashDrawers.OfType<MajorCashDrawerMasterTreeDTO>().FirstOrDefault(x => x.Id == updatedCashDrawer.Id) ?? throw new Exception("");
                if (cashDrawerToUpdate is null) return;
                cashDrawerToUpdate.Id = cashDrawerDTO.Id;
                cashDrawerToUpdate.Name = cashDrawerDTO.Name;
                cashDrawerToUpdate.CashAccountingAccount = cashDrawerDTO.CashAccountingAccount;
                cashDrawerToUpdate.CheckAccountingAccount = cashDrawerDTO.CheckAccountingAccount;
                cashDrawerToUpdate.CardAccountingAccount = cashDrawerDTO.CardAccountingAccount;
                cashDrawerToUpdate.CashReviewRequired = cashDrawerDTO.CashReviewRequired;
                cashDrawerToUpdate.AutoAdjustBalance = cashDrawerDTO.AutoAdjustBalance;
                cashDrawerToUpdate.AutoTransfer = cashDrawerDTO.AutoTransfer;
                cashDrawerToUpdate.AutoTransferCashDrawer = cashDrawerDTO.AutoTransferCashDrawer;
                await Task.Run(() => MajorCashDrawerEditor.SetForEdit(cashDrawerToUpdate), cancellationToken);
                _notificationService.ShowSuccess(message.UpdatedCashDrawer.Message);
                return;
            }
            if(updatedCashDrawer.IsPettyCash is false && updatedCashDrawer.Parent != null)
            {
                TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawer = Context.AutoMapper.Map<TreasuryAuxiliaryCashDrawerMasterTreeDTO>(updatedCashDrawer);
                CashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Major) ?? throw new Exception("");
                if (majorCashDrawerDummyDTO is null) return;
                CashDrawerCompanyLocationDTO majorCashDrawerCompanyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == updatedCashDrawer.Parent.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
                if (majorCashDrawerCompanyLocation is null) return;
                CashDrawerCostCenterDTO majorCashDrawerCostCenter = majorCashDrawerCompanyLocation.CostCenters.FirstOrDefault(x => x.Id == updatedCashDrawer.Parent.CostCenter.Id) ?? throw new Exception("");
                if (majorCashDrawerCostCenter is null) return;
                MajorCashDrawerMasterTreeDTO majorCashDrawer = majorCashDrawerCostCenter.CashDrawers.OfType<MajorCashDrawerMasterTreeDTO>().FirstOrDefault(x => x.Id == updatedCashDrawer.Parent.Id) ?? throw new Exception("");
                if (majorCashDrawer is null) return;
                TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawerToUpdate = majorCashDrawer.AuxiliaryCashDrawers.FirstOrDefault(x => x.Id == updatedCashDrawer.Id) ?? throw new Exception("");
                if (auxiliaryCashDrawerToUpdate is null) return;
                auxiliaryCashDrawerToUpdate.Id = auxiliaryCashDrawer.Id;
                auxiliaryCashDrawerToUpdate.Name = auxiliaryCashDrawer.Name;
                auxiliaryCashDrawerToUpdate.CashAccountingAccount = auxiliaryCashDrawer.CashAccountingAccount;
                auxiliaryCashDrawerToUpdate.CheckAccountingAccount = auxiliaryCashDrawer.CheckAccountingAccount;
                auxiliaryCashDrawerToUpdate.CardAccountingAccount = auxiliaryCashDrawer.CardAccountingAccount;
                auxiliaryCashDrawerToUpdate.CashReviewRequired = auxiliaryCashDrawer.CashReviewRequired;
                auxiliaryCashDrawerToUpdate.AutoAdjustBalance = auxiliaryCashDrawer.AutoAdjustBalance;
                auxiliaryCashDrawerToUpdate.AutoTransfer = auxiliaryCashDrawer.AutoTransfer;
                auxiliaryCashDrawerToUpdate.AutoTransferCashDrawer = auxiliaryCashDrawer.AutoTransferCashDrawer;
                auxiliaryCashDrawerToUpdate.ComputerName = auxiliaryCashDrawer.ComputerName;
                await Task.Run(() => AuxiliaryCashDrawerEditor.SetForEdit(auxiliaryCashDrawerToUpdate), cancellationToken);
                _notificationService.ShowSuccess(message.UpdatedCashDrawer.Message);
                return;
            }
            MinorCashDrawerMasterTreeDTO minorCashDrawerMasterTreeDTO = Context.AutoMapper.Map<MinorCashDrawerMasterTreeDTO>(updatedCashDrawer);
            CashDrawerDummyDTO minorCashDrawerDummyDTO = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Minor) ?? throw new Exception("");
            if (minorCashDrawerDummyDTO is null) return;
            CashDrawerCompanyLocationDTO minorCashDrawerCompanyLocation = minorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == updatedCashDrawer.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
            if (minorCashDrawerCompanyLocation is null) return;
            CashDrawerCostCenterDTO minorCashDrawerCostCenter = minorCashDrawerCompanyLocation.CostCenters.FirstOrDefault(x => x.Id == updatedCashDrawer.CostCenter.Id) ?? throw new Exception("");
            if (minorCashDrawerCostCenter is null) return;
            MinorCashDrawerMasterTreeDTO minorCashDrawerToUpdate = minorCashDrawerCostCenter.CashDrawers.OfType<MinorCashDrawerMasterTreeDTO>().FirstOrDefault(x => x.Id == updatedCashDrawer.Id) ?? throw new Exception("");
            if (minorCashDrawerToUpdate is null) return;
            minorCashDrawerToUpdate.Id = minorCashDrawerMasterTreeDTO.Id;
            minorCashDrawerToUpdate.Name = minorCashDrawerMasterTreeDTO.Name;
            minorCashDrawerToUpdate.CashAccountingAccount = minorCashDrawerMasterTreeDTO.CashAccountingAccount;
            minorCashDrawerToUpdate.CashReviewRequired = minorCashDrawerMasterTreeDTO.CashReviewRequired;
            minorCashDrawerToUpdate.AutoAdjustBalance = minorCashDrawerMasterTreeDTO.AutoAdjustBalance;
            await Task.Run(() => MinorCashDrawerEditor.SetForEdit(minorCashDrawerToUpdate), cancellationToken);
            _notificationService.ShowSuccess(message.UpdatedCashDrawer.Message);
            return;
        }

        public Task HandleAsync(BankCreateMessage message, CancellationToken cancellationToken)
        {
            var createdBank = message.CreatedBank.Entity;
            IsNewRecord = false;

            TreasuryBankMasterTreeDTO bankDTO = Context.AutoMapper.Map<TreasuryBankMasterTreeDTO>(createdBank);
            bankDTO.Context = this;
            ITreasuryTreeMasterSelectedItem? inserted = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                BankDummyDTO? bankDummy = DummyItems.OfType<BankDummyDTO>().FirstOrDefault();
                if (bankDummy is null) return;
                bankDTO.DummyParent = bankDummy;
                bankDummy.Banks.Add(bankDTO);
                bankDummy.IsExpanded = true;
                inserted = bankDTO;
            });
            if (inserted != null) SelectedItem = inserted;
            _notificationService.ShowSuccess(message.CreatedBank.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(BankUpdateMessage message, CancellationToken cancellationToken)
        {
            var updatedBank = message.UpdatedBank.Entity;
            TreasuryBankMasterTreeDTO bankDTO = Context.AutoMapper.Map<TreasuryBankMasterTreeDTO>(updatedBank);
            BankDummyDTO bankDummyDTO = DummyItems.FirstOrDefault(x => x is BankDummyDTO) as BankDummyDTO ?? throw new Exception("");
            if (bankDummyDTO is null) return Task.CompletedTask;
            TreasuryBankMasterTreeDTO bankToUpdate = bankDummyDTO.Banks.FirstOrDefault(x => x.Id == updatedBank.Id) ?? throw new Exception("");
            if (bankToUpdate is null) return Task.CompletedTask;
            bankToUpdate.Id = bankDTO.Id;
            bankToUpdate.AccountingEntity = bankDTO.AccountingEntity;
            bankToUpdate.PaymentMethodPrefix = bankDTO.PaymentMethodPrefix;
            _notificationService.ShowSuccess(message.UpdatedBank.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(BankDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                BankDummyDTO bankDummyDTO = DummyItems.FirstOrDefault(x => x is BankDummyDTO) as BankDummyDTO ?? throw new Exception("");
                if (bankDummyDTO is null) return;
                bankDummyDTO.Banks.Remove(bankDummyDTO.Banks.Where(x => x.Id == message.DeletedBank.DeletedId).First());
            });
            _notificationService.ShowSuccess(message.DeletedBank.Message);
            return Task.CompletedTask;
        }

        public async Task HandleAsync(BankAccountCreateMessage message, CancellationToken cancellationToken)
        {
            var createdBankAccount = message.CreatedBankAccount.Entity;
            IsNewRecord = false;

            // Recargar combos antes de asignar SelectedItem (que dispara SetForEdit),
            // porque la API puede crear cuentas contables como side-effect
            _auxiliaryAccountingAccountCache.Clear();
            await LoadComboBoxesAsync();

            TreasuryBankAccountMasterTreeDTO bankAccountDTO = Context.AutoMapper.Map<TreasuryBankAccountMasterTreeDTO>(createdBankAccount);
            bankAccountDTO.Context = this;
            ITreasuryTreeMasterSelectedItem? inserted = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                BankDummyDTO? bankDummy = DummyItems.OfType<BankDummyDTO>().FirstOrDefault();
                if (bankDummy is null) return;
                TreasuryBankMasterTreeDTO? bank = bankDummy.Banks.FirstOrDefault(x => x.Id == createdBankAccount.Bank.Id);
                if (bank is null) return;
                bank.BankAccounts.Add(bankAccountDTO);
                bankDummy.IsExpanded = true;
                bank.IsExpanded = true;
                inserted = bankAccountDTO;
            });
            if (inserted != null) SelectedItem = inserted;
            _notificationService.ShowSuccess(message.CreatedBankAccount.Message);
        }

        public async Task HandleAsync(BankAccountDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var bankDummyDTO = DummyItems.OfType<BankDummyDTO>().FirstOrDefault();
                if (bankDummyDTO is null) return;

                foreach (var bank in bankDummyDTO.Banks)
                {
                    var bankAccount = bank.BankAccounts.FirstOrDefault(x => x.Id == message.DeletedBankAccount.DeletedId);
                    if (bankAccount != null)
                    {
                        bank.BankAccounts.Remove(bankAccount);
                        break;
                    }
                }
            });
            await LoadComboBoxesAsync();
            _notificationService.ShowSuccess(message.DeletedBankAccount.Message);
        }

        public async Task HandleAsync(BankAccountUpdateMessage message, CancellationToken cancellationToken)
        {
            var updatedBankAccount = message.UpdatedBankAccount.Entity;
            TreasuryBankAccountMasterTreeDTO bankAccountDTO = Context.AutoMapper.Map<TreasuryBankAccountMasterTreeDTO>(updatedBankAccount);
            BankDummyDTO bankDummyDTO = DummyItems.FirstOrDefault(x => x is BankDummyDTO) as BankDummyDTO ?? throw new Exception("");
            if (bankDummyDTO is null) return;
            TreasuryBankMasterTreeDTO bankDTO = bankDummyDTO.Banks.FirstOrDefault(x => x.Id == updatedBankAccount.Bank.Id) ?? throw new Exception("");
            if (bankDTO is null) return;
            TreasuryBankAccountMasterTreeDTO bankAccountToUpdate = bankDTO.BankAccounts.FirstOrDefault(x => x.Id == updatedBankAccount.Id) ?? throw new Exception("");
            if (bankAccountToUpdate is null) return;
            bankAccountToUpdate.Id = bankAccountDTO.Id;
            bankAccountToUpdate.Type = bankAccountDTO.Type;
            bankAccountToUpdate.Number = bankAccountDTO.Number;
            bankAccountToUpdate.IsActive = bankAccountDTO.IsActive;
            bankAccountToUpdate.Description = bankAccountDTO.Description;
            bankAccountToUpdate.Reference = bankAccountDTO.Reference;
            bankAccountToUpdate.DisplayOrder = bankAccountDTO.DisplayOrder;
            bankAccountToUpdate.AccountingAccount = bankAccountDTO.AccountingAccount;
            bankAccountToUpdate.Bank = bankAccountDTO.Bank;
            bankAccountToUpdate.Provider = bankAccountDTO.Provider;
            bankAccountToUpdate.PaymentMethod = bankAccountDTO.PaymentMethod;
            bankAccountToUpdate.AllowedCostCenters = bankAccountDTO.AllowedCostCenters;
            await Task.Run(() => BankAccountEditor.SetForEdit(bankAccountToUpdate));
            _notificationService.ShowSuccess(message.UpdatedBankAccount.Message);
            return;
        }

        public Task HandleAsync(FranchiseCreateMessage message, CancellationToken cancellationToken)
        {
            var createdFranchise = message.CreatedFranchise.Entity;
            IsNewRecord = false;
            TreasuryFranchiseMasterTreeDTO franchiseDTO = Context.AutoMapper.Map<TreasuryFranchiseMasterTreeDTO>(createdFranchise);
            franchiseDTO.Context = this;
            ITreasuryTreeMasterSelectedItem? inserted = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                FranchiseDummyDTO? franchiseDummy = DummyItems.OfType<FranchiseDummyDTO>().FirstOrDefault();
                if (franchiseDummy is null) return;
                franchiseDTO.DummyParent = franchiseDummy;
                franchiseDummy.Franchises.Add(franchiseDTO);
                franchiseDummy.IsExpanded = true;
                inserted = franchiseDTO;
            });
            if (inserted != null) SelectedItem = inserted;
            _notificationService.ShowSuccess(message.CreatedFranchise.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(FranchiseDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                FranchiseDummyDTO franchiseDummyDTO = DummyItems.FirstOrDefault(x => x is FranchiseDummyDTO) as FranchiseDummyDTO ?? throw new Exception("");
                if (franchiseDummyDTO is null) return;
                franchiseDummyDTO.Franchises.Remove(franchiseDummyDTO.Franchises.Where(x => x.Id == message.DeletedFranchise.DeletedId).First());
            });
            _notificationService.ShowSuccess(message.DeletedFranchise.Message);
            return Task.CompletedTask;
        }

        public async Task HandleAsync(FranchiseUpdateMessage message, CancellationToken cancellationToken)
        {
            var updatedFranchise = message.UpdatedFranchise.Entity;
            TreasuryFranchiseMasterTreeDTO franchiseDTO = Context.AutoMapper.Map<TreasuryFranchiseMasterTreeDTO>(updatedFranchise);
            FranchiseDummyDTO franchiseDummyDTO = DummyItems.FirstOrDefault(x => x is FranchiseDummyDTO) as FranchiseDummyDTO ?? throw new Exception("");
            if (franchiseDummyDTO is null) return;
            TreasuryFranchiseMasterTreeDTO franchiseToUpdate = franchiseDummyDTO.Franchises.FirstOrDefault(x => x.Id == updatedFranchise.Id) ?? throw new Exception("");
            if (franchiseToUpdate is null) return;
            franchiseToUpdate.Id = franchiseDTO.Id;
            franchiseToUpdate.Name = franchiseDTO.Name;
            franchiseToUpdate.FormulaCommission = franchiseDTO.FormulaCommission;
            franchiseToUpdate.FormulaReteiva = franchiseDTO.FormulaReteiva;
            franchiseToUpdate.FormulaReteica = franchiseDTO.FormulaReteica;
            franchiseToUpdate.FormulaRetefte = franchiseDTO.FormulaRetefte;
            franchiseToUpdate.CommissionRate = franchiseDTO.CommissionRate;
            franchiseToUpdate.ReteivaRate = franchiseDTO.ReteivaRate;
            franchiseToUpdate.ReteicaRate = franchiseDTO.ReteicaRate;
            franchiseToUpdate.RetefteRate = franchiseDTO.RetefteRate;
            franchiseToUpdate.TaxRate = franchiseDTO.TaxRate;
            franchiseToUpdate.BankAccount = franchiseDTO.BankAccount;
            franchiseToUpdate.CommissionAccountingAccount = franchiseDTO.CommissionAccountingAccount;
            franchiseToUpdate.FranchisesByCostCenter = franchiseDTO.FranchisesByCostCenter;
            await Task.Run(() => FranchiseEditor.SetForEdit(franchiseToUpdate));
            _notificationService.ShowSuccess(message.UpdatedFranchise.Message);
            return;
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Messenger.Default.Unregister<ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel>>(this);
                Context.EventAggregator.Unsubscribe(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
