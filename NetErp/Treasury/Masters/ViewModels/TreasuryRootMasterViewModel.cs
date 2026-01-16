using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.Global;
using Models.Inventory;
using Models.Treasury;
using NetErp.Global.CostCenters.DTO;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Treasury.Masters.DTO;
using NetErp.Treasury.Masters.PanelEditors;
using Services.Billing.DAL.PostgreSQL;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.Primitives;
using static Dictionaries.BooksDictionaries;

namespace NetErp.Treasury.Masters.ViewModels
{
    public class TreasuryRootMasterViewModel : Screen, INotifyDataErrorInfo,
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

        Dictionary<string, List<string>> _errors;

        private readonly IRepository<CompanyLocationGraphQLModel> _companyLocationService;
        private readonly IRepository<CostCenterGraphQLModel> _costCenterService;
        private readonly IRepository<CashDrawerGraphQLModel> _cashDrawerService;
        private readonly IRepository<BankGraphQLModel> _bankService;
        private readonly IRepository<BankAccountGraphQLModel> _bankAccountService;
        private readonly IRepository<FranchiseGraphQLModel> _franchiseService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly Helpers.Services.INotificationService _notificationService;

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

        private ObservableCollection<CashDrawerGraphQLModel> _cashDrawers;

        public ObservableCollection<CashDrawerGraphQLModel> CashDrawers
        {
            get { return _cashDrawers; }
            set
            {
                if (_cashDrawers != value)
                {
                    _cashDrawers = value;
                    NotifyOfPropertyChange(nameof(CashDrawers));
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
                        AuxiliaryCashDrawerEditor.SetForNew(MajorCashDrawerIdBeforeNewAuxiliaryCashDrawer);
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

        private ICommand _searchComputerNameCommand;
        public ICommand SearchComputerNameCommand
        {
            get
            {
                if (_searchComputerNameCommand is null) _searchComputerNameCommand = new RelayCommand(CanSearchComputerName, SearchComputerName);
                return _searchComputerNameCommand;
            }
        }

        public void SearchComputerName(object p)
        {
            AuxiliaryCashDrawerEditor.ComputerName = SessionInfo.GetComputerName();
        }

        public bool CanSearchComputerName(object p) => true;

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
                    if (_selectedItem is MajorCashDrawerMasterTreeDTO majorcashDrawer) MajorCashDrawerIdBeforeNewAuxiliaryCashDrawer = majorcashDrawer.Id;
                    if (_selectedItem is TreasuryBankMasterTreeDTO bank) BankBeforeNewBankAccount = bank;
                    return true;
                }
                if (_selectedItem is TreasuryMajorCashDrawerCostCenterMasterTreeDTO treasuryMajorCashDrawerCostCenterMasterTreeDTO) MajorCostCenterBeforeNewCashDrawer = treasuryMajorCashDrawerCostCenterMasterTreeDTO;
                if (_selectedItem is TreasuryMinorCashDrawerCostCenterMasterTreeDTO minorCashDrawerCostCenterMasterTreeDTO) MinorCostCenterBeforeNewCashDrawer = minorCashDrawerCostCenterMasterTreeDTO;
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
                    // Reload combo boxes for cash drawer and bank account entities
                    if (SelectedItem is MajorCashDrawerMasterTreeDTO or
                        MinorCashDrawerMasterTreeDTO or
                        TreasuryAuxiliaryCashDrawerMasterTreeDTO or
                        TreasuryBankAccountMasterTreeDTO)
                    {
                        await LoadComboBoxesAsync();
                    }

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

        private ICommand _deleteMajorCashDrawerCommand;
        public ICommand DeleteMajorCashDrawerCommand
        {
            get
            {
                if (_deleteMajorCashDrawerCommand is null) _deleteMajorCashDrawerCommand = new AsyncCommand(DeleteMajorCashDrawer, CanDeleteMajorCashDrawer);
                return _deleteMajorCashDrawerCommand;
            }
        }

        public async Task DeleteMajorCashDrawer()
        {
            try
            {
                IsBusy = true;
                int id = ((MajorCashDrawerMasterTreeDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteCashDrawer(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this._cashDrawerService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {((MajorCashDrawerMasterTreeDTO)SelectedItem).Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                CashDrawerGraphQLModel deletedCashDrawer = await ExecuteDeleteMajorCashDrawer(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryCashDrawerDeleteMessage() { DeletedCashDrawer = deletedCashDrawer });

                NotifyOfPropertyChange(nameof(CanDeleteMajorCashDrawer));

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteMajorCashDrawer" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }

        }

        public async Task<CashDrawerGraphQLModel> ExecuteDeleteMajorCashDrawer(int id)
        {
            try
            {
                string query = @"
                    mutation($id: Int!){
                      DeleteResponse: deleteCashDrawer(id: $id){
                        id
                        name
                        cashReviewRequired
                        autoAdjustBalance
                        autoTransfer
                        isPettyCash
                        cashDrawerAutoTransfer {
                          id
                          name
                        }
                        costCenter {
                          id
                          name
                          location{
                            id
                          }
                        }
                        accountingAccountCash {
                          id
                          name
                        }
                        accountingAccountCheck {
                          id
                          name
                        }
                        accountingAccountCard {
                          id
                          name
                        }
                      }
                    }";
                object variables = new { Id = id };
                CashDrawerGraphQLModel deletedCashDrawer = await _cashDrawerService.DeleteAsync(query, variables);
                this.SelectedItem = null;
                return deletedCashDrawer;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanDeleteMajorCashDrawer => true;

        private ICommand _deleteMinorCashDrawerCommand;
        public ICommand DeleteMinorCashDrawerCommand
        {
            get
            {
                if (_deleteMinorCashDrawerCommand is null) _deleteMinorCashDrawerCommand = new AsyncCommand(DeleteMinorCashDrawer, CanDeleteMinorCashDrawer);
                return _deleteMinorCashDrawerCommand;
            }
        }

        public async Task DeleteMinorCashDrawer()
        {
            try
            {
                IsBusy = true;
                int id = ((MinorCashDrawerMasterTreeDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteCashDrawer(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this._cashDrawerService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {((MinorCashDrawerMasterTreeDTO)SelectedItem).Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                CashDrawerGraphQLModel deletedCashDrawer = await ExecuteDeleteMinorCashDrawer(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryCashDrawerDeleteMessage() { DeletedCashDrawer = deletedCashDrawer });

                NotifyOfPropertyChange(nameof(CanDeleteMinorCashDrawer));

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteMajorCashDrawer" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }

        }

        public async Task<CashDrawerGraphQLModel> ExecuteDeleteMinorCashDrawer(int id)
        {
            try
            {
                string query = @"
                    mutation($id: Int!){
                      DeleteResponse: deleteCashDrawer(id: $id){
                        id
                        name
                        cashReviewRequired
                        autoAdjustBalance
                        autoTransfer
                        isPettyCash
                        cashDrawerAutoTransfer {
                          id
                          name
                        }
                        costCenter {
                          id
                          name
                          location{
                            id
                          }
                        }
                        accountingAccountCash {
                          id
                          name
                        }
                        accountingAccountCheck {
                          id
                          name
                        }
                        accountingAccountCard {
                          id
                          name
                        }
                      }
                    }";
                object variables = new { Id = id };
                CashDrawerGraphQLModel deletedCashDrawer = await _cashDrawerService.DeleteAsync(query, variables);
                this.SelectedItem = null;
                return deletedCashDrawer;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanDeleteMinorCashDrawer => true;

        private ICommand _deleteAuxiliaryCashDrawerCommand;
        public ICommand DeleteAuxiliaryCashDrawerCommand
        {
            get
            {
                if (_deleteAuxiliaryCashDrawerCommand is null) _deleteAuxiliaryCashDrawerCommand = new AsyncCommand(DeleteAuxiliaryCashDrawer, CanDeleteAuxiliaryCashDrawer);
                return _deleteAuxiliaryCashDrawerCommand;
            }
        }

        public async Task DeleteAuxiliaryCashDrawer()
        {
            try
            {
                IsBusy = true;
                int id = ((TreasuryAuxiliaryCashDrawerMasterTreeDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteCashDrawer(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this._cashDrawerService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {((TreasuryAuxiliaryCashDrawerMasterTreeDTO)SelectedItem).Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                CashDrawerGraphQLModel deletedCashDrawer = await ExecuteDeleteAuxiliaryCashDrawer(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryCashDrawerDeleteMessage() { DeletedCashDrawer = deletedCashDrawer });

                NotifyOfPropertyChange(nameof(CanDeleteAuxiliaryCashDrawer));

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteMajorCashDrawer" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }

        }

        public async Task<CashDrawerGraphQLModel> ExecuteDeleteAuxiliaryCashDrawer(int id)
        {
            try
            {
                string query = @"
                    mutation($id: Int!){
                      DeleteResponse: deleteCashDrawer(id: $id){
                        id
                        name
                        cashReviewRequired
                        autoAdjustBalance
                        autoTransfer
                        isPettyCash
                        cashDrawerAutoTransfer {
                          id
                          name
                        }
                        accountingAccountCash {
                          id
                          name
                        }
                        accountingAccountCheck {
                          id
                          name
                        }
                        accountingAccountCard {
                          id
                          name
                        }
                        parent{
                          id
                          costCenter{
                            id
                            location{
                              id
                             }
                            }
                        }
                        computerName
                      }
                    }";
                object variables = new { Id = id };
                CashDrawerGraphQLModel deletedCashDrawer = await _cashDrawerService.DeleteAsync(query, variables);
                this.SelectedItem = null;
                return deletedCashDrawer;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanDeleteAuxiliaryCashDrawer => true;

        private ICommand _deleteBankCommand;
        public ICommand DeleteBankCommand
        {
            get
            {
                if (_deleteBankCommand is null) _deleteBankCommand = new AsyncCommand(DeleteBank, CanDeleteBank);
                return _deleteBankCommand;
            }
        }

        public async Task DeleteBank()
        {
            try
            {
                IsBusy = true;
                int id = ((TreasuryBankMasterTreeDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteBank(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this._bankService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {((TreasuryBankMasterTreeDTO)SelectedItem).AccountingEntity.SearchName}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                BankGraphQLModel deletedBank = await ExecuteDeleteBank(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new BankDeleteMessage() { DeletedBank = deletedBank });

                NotifyOfPropertyChange(nameof(CanDeleteBank));

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteBank" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }

        }

        public async Task<BankGraphQLModel> ExecuteDeleteBank(int id)
        {
            try
            {
                string query = @"
                    mutation ($id: Int!) {
                      DeleteResponse: deleteBank(id: $id){
                        id
                        paymentMethodPrefix
                        accountingEntity{
                          id
                          searchName
                        }
                      }
                    }";
                object variables = new { Id = id };
                BankGraphQLModel deletedBank = await _bankService.DeleteAsync(query, variables);
                this.SelectedItem = null;
                return deletedBank;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanDeleteBank => true;

        private ICommand _deleteBankAccountCommand;
        public ICommand DeleteBankAccountCommand
        {
            get
            {
                if (_deleteBankAccountCommand is null) _deleteBankAccountCommand = new AsyncCommand(DeleteBankAccount, CanDeleteBankAccount);
                return _deleteBankAccountCommand;
            }
        }

        public async Task DeleteBankAccount()
        {
            try
            {
                IsBusy = true;
                int id = ((TreasuryBankAccountMasterTreeDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteBankAccount(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this._bankService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {((TreasuryBankAccountMasterTreeDTO)SelectedItem).Description}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                BankAccountGraphQLModel deletedBankAccount = await ExecuteDeleteBankAccount(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new BankAccountDeleteMessage() { DeletedBankAccount = deletedBankAccount });

                NotifyOfPropertyChange(nameof(CanDeleteBankAccount));

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteBankAccount" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }

        }

        public async Task<BankAccountGraphQLModel> ExecuteDeleteBankAccount(int id)
        {
            try
            {
                string query = @"
                    mutation($id: Int!){
                        DeleteResponse: deleteBankAccount(id: $id){
                        id
                        type
                        number
                        description
                        isActive
                        reference
                        displayOrder
                        accountingAccount{
                            id
                            code
                            name
                        }
                        bank{
                            id
                        }
                        }
                    }";
                object variables = new { Id = id };
                BankAccountGraphQLModel deletedBankAccount = await _bankAccountService.DeleteAsync(query, variables);
                this.SelectedItem = null;
                return deletedBankAccount;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanDeleteBankAccount => true;

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
            try
            {
                IsBusy = true;
                int id = ((TreasuryFranchiseMasterTreeDTO)SelectedItem).Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteFranchise(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this._franchiseService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {((TreasuryFranchiseMasterTreeDTO)SelectedItem).Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;
                Refresh();

                FranchiseGraphQLModel deletedFranchise = await ExecuteDeleteFranchise(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new FranchiseDeleteMessage() { DeletedFranchise = deletedFranchise });

                NotifyOfPropertyChange(nameof(CanDeleteFranchise));

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteFranchise" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }

        }

        public async Task<FranchiseGraphQLModel> ExecuteDeleteFranchise(int id)
        {
            try
            {
                string query = @"
                mutation($id: Int!){
                    DeleteResponse: deleteFranchise(id: $id){
                    id
                    name
                    type
                    commissionMargin
                    reteivaMargin
                    reteicaMargin
                    retefteMargin
                    ivaMargin
                    accountingAccountCommission{
                        id
                        code
                        name
                    }
                    bankAccount{
                        id
                        description
                    }
                    formulaCommission
                    formulaReteiva
                    formulaReteica
                    formulaRetefte
                    }
                }";
                object variables = new { Id = id };
                FranchiseGraphQLModel deletedFranchise = await _franchiseService.DeleteAsync(query, variables);
                this.SelectedItem = null;
                return deletedFranchise;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanDeleteFranchise => true;

        #region "MajorCashDrawer Context Properties"

        public TreasuryMajorCashDrawerCostCenterMasterTreeDTO MajorCostCenterBeforeNewCashDrawer { get; set; } = new();

        // Collection used by MajorCashDrawerPanelEditor
        private ObservableCollection<CashDrawerGraphQLModel> _majorCashDrawerAutoTransferCashDrawers;
        public ObservableCollection<CashDrawerGraphQLModel> MajorCashDrawerAutoTransferCashDrawers
        {
            get { return _majorCashDrawerAutoTransferCashDrawers; }
            set
            {
                if (_majorCashDrawerAutoTransferCashDrawers != value)
                {
                    _majorCashDrawerAutoTransferCashDrawers = value;
                    NotifyOfPropertyChange(nameof(MajorCashDrawerAutoTransferCashDrawers));
                }
            }
        }

        #endregion

        #region "MinorCashDrawer Context Properties"

        public TreasuryMinorCashDrawerCostCenterMasterTreeDTO MinorCostCenterBeforeNewCashDrawer { get; set; } = new();

        #endregion

        #region "AuxiliaryCashDrawer Context Properties"

        public int MajorCashDrawerIdBeforeNewAuxiliaryCashDrawer { get; set; }

        // Collection used by AuxiliaryCashDrawerPanelEditor
        private ObservableCollection<CashDrawerGraphQLModel> _auxiliaryCashDrawerAutoTransferCashDrawers = [];
        public ObservableCollection<CashDrawerGraphQLModel> AuxiliaryCashDrawerAutoTransferCashDrawers
        {
            get { return _auxiliaryCashDrawerAutoTransferCashDrawers; }
            set
            {
                if (_auxiliaryCashDrawerAutoTransferCashDrawers != value)
                {
                    _auxiliaryCashDrawerAutoTransferCashDrawers = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryCashDrawerAutoTransferCashDrawers));
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

        #region Franchise Simulator Commands

        private ICommand _franchiseResetFormulaReteivaCommand;
        public ICommand FranchiseResetFormulaReteivaCommand
        {
            get
            {
                if (_franchiseResetFormulaReteivaCommand is null) _franchiseResetFormulaReteivaCommand = new RelayCommand(CanSearchBankAccountingEntity, FranchiseResetFormulaReteiva);
                return _franchiseResetFormulaReteivaCommand;
            }
        }

        public void FranchiseResetFormulaReteiva(object p)
        {
            if (FranchiseEditor != null)
                FranchiseEditor.FormulaReteiva = "[VALOR_IVA]*([MARGEN_RETE_IVA]/100)";
        }

        private ICommand _franchiseResetFormulaCommissionCommand;
        public ICommand FranchiseResetFormulaCommissionCommand
        {
            get
            {
                if (_franchiseResetFormulaCommissionCommand is null) _franchiseResetFormulaCommissionCommand = new RelayCommand(CanFranchiseResetFormulaCommission, FranchiseResetFormulaCommission);
                return _franchiseResetFormulaCommissionCommand;
            }
        }

        public void FranchiseResetFormulaCommission(object p)
        {
            if (FranchiseEditor != null)
                FranchiseEditor.FormulaCommission = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_COMISION]/100)";
        }

        public bool CanFranchiseResetFormulaCommission(object p) => true;

        private ICommand _franchiseResetFormulaReteicaCommand;
        public ICommand FranchiseResetFormulaReteicaCommand
        {
            get
            {
                if (_franchiseResetFormulaReteicaCommand is null) _franchiseResetFormulaReteicaCommand = new RelayCommand(CanFranchiseResetFormulaReteica, FranchiseResetFormulaReteica);
                return _franchiseResetFormulaReteicaCommand;
            }
        }

        public void FranchiseResetFormulaReteica(object p)
        {
            if (FranchiseEditor != null)
                FranchiseEditor.FormulaReteica = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_ICA]/1000)";
        }

        public bool CanFranchiseResetFormulaReteica(object p) => true;

        private ICommand _franchiseResetFormulaRetefteCommand;
        public ICommand FranchiseResetFormulaRetefteCommand
        {
            get
            {
                if (_franchiseResetFormulaRetefteCommand is null) _franchiseResetFormulaRetefteCommand = new RelayCommand(CanFranchiseResetFormulaRetefte, FranchiseResetFormulaRetefte);
                return _franchiseResetFormulaRetefteCommand;
            }
        }

        public void FranchiseResetFormulaRetefte(object p)
        {
            if (FranchiseEditor != null)
                FranchiseEditor.FormulaRetefte = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_FUENTE]/100)";
        }

        public bool CanFranchiseResetFormulaRetefte(object p) => true;

        private ICommand _franchiseSimulatorCommand;
        public ICommand FranchiseSimulatorCommand
        {
            get
            {
                if (_franchiseSimulatorCommand is null) _franchiseSimulatorCommand = new RelayCommand(CanFranchiseSimulator, FranchiseSimulator);
                return _franchiseSimulatorCommand;
            }
        }

        public void FranchiseSimulator(object p)
        {
            if (FranchiseEditor == null) return;

            try
            {
                // Local variables for formula replacement
                string replacedFormulaCommission = FranchiseEditor.FormulaCommission;
                string replacedFormulaReteiva = FranchiseEditor.FormulaReteiva;
                string replacedFormulaReteica = FranchiseEditor.FormulaReteica;
                string replacedFormulaRetefte = FranchiseEditor.FormulaRetefte;

                // Calculate IVA value
                decimal simulatedIvaValue = FranchiseEditor.CardValue - (FranchiseEditor.CardValue / (1 + (FranchiseEditor.IvaMargin / 100)));
                FranchiseEditor.SimulatedIvaValue = simulatedIvaValue;

                // Set up formula variables dictionary
                Dictionary<string, decimal> formulaVariables = new()
                {
                    { "VALOR_TARJETA", FranchiseEditor.CardValue },
                    { "MARGEN_COMISION", FranchiseEditor.CommissionMargin },
                    { "MARGEN_RETE_IVA", FranchiseEditor.ReteivaMargin },
                    { "MARGEN_RETE_ICA", FranchiseEditor.ReteicaMargin },
                    { "MARGEN_RETE_FUENTE", FranchiseEditor.RetefteMargin },
                    { "VALOR_IVA", simulatedIvaValue }
                };

                // Replace variables in formulas
                foreach (var item in formulaVariables)
                {
                    replacedFormulaCommission = replacedFormulaCommission.Replace($"[{item.Key}]", item.Value.ToString(CultureInfo.InvariantCulture));
                    replacedFormulaReteiva = replacedFormulaReteiva.Replace($"[{item.Key}]", item.Value.ToString(CultureInfo.InvariantCulture));
                    replacedFormulaReteica = replacedFormulaReteica.Replace($"[{item.Key}]", item.Value.ToString(CultureInfo.InvariantCulture));
                    replacedFormulaRetefte = replacedFormulaRetefte.Replace($"[{item.Key}]", item.Value.ToString(CultureInfo.InvariantCulture));
                }

                // Calculate formula results
                FranchiseEditor.SimulatedCommission = Convert.ToDecimal(new DataTable().Compute(replacedFormulaCommission, null), CultureInfo.InvariantCulture);
                FranchiseEditor.SimulatedReteiva = Convert.ToDecimal(new DataTable().Compute(replacedFormulaReteiva, null), CultureInfo.InvariantCulture);
                FranchiseEditor.SimulatedReteica = Convert.ToDecimal(new DataTable().Compute(replacedFormulaReteica, null), CultureInfo.InvariantCulture);
                FranchiseEditor.SimulatedRetefte = Convert.ToDecimal(new DataTable().Compute(replacedFormulaRetefte, null), CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"Error al simular la franquicia. \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public bool CanFranchiseSimulator(object p)
        {
            if (FranchiseEditor == null) return false;
            return FranchiseEditor.CardValue != 0
                && !string.IsNullOrEmpty(FranchiseEditor.FormulaCommission)
                && !string.IsNullOrEmpty(FranchiseEditor.FormulaReteiva)
                && !string.IsNullOrEmpty(FranchiseEditor.FormulaReteica)
                && !string.IsNullOrEmpty(FranchiseEditor.FormulaRetefte);
        }

        public void EditFranchiseByCostCenter(int costCenterId)
        {
            if (IsNewRecord || FranchiseEditor == null) return;

            if (costCenterId == 0)
            {
                if (SelectedItem is TreasuryFranchiseMasterTreeDTO selectedFranchise)
                {
                    FranchiseEditor.CommissionMargin = selectedFranchise.CommissionMargin;
                    FranchiseEditor.ReteivaMargin = selectedFranchise.ReteivaMargin;
                    FranchiseEditor.ReteicaMargin = selectedFranchise.ReteicaMargin;
                    FranchiseEditor.RetefteMargin = selectedFranchise.RetefteMargin;
                    FranchiseEditor.IvaMargin = selectedFranchise.IvaMargin;
                    FranchiseEditor.SelectedBankAccount = FranchiseBankAccounts.FirstOrDefault(x => x.Id == selectedFranchise.BankAccount.Id);
                    FranchiseEditor.FormulaCommission = selectedFranchise.FormulaCommission;
                    FranchiseEditor.FormulaReteiva = selectedFranchise.FormulaReteiva;
                    FranchiseEditor.FormulaReteica = selectedFranchise.FormulaReteica;
                    FranchiseEditor.FormulaRetefte = selectedFranchise.FormulaRetefte;
                    FranchiseEditor.CardValue = 0;
                    FranchiseEditor.SimulatedCommission = 0;
                    FranchiseEditor.SimulatedReteiva = 0;
                    FranchiseEditor.SimulatedReteica = 0;
                    FranchiseEditor.SimulatedRetefte = 0;
                    FranchiseEditor.SimulatedIvaValue = 0;
                }
                return;
            }
            var selectedFranchiseSetting = FranchiseEditor.SettingsByCostCenter.FirstOrDefault(x => x.CostCenterId == costCenterId);
            if (selectedFranchiseSetting != null)
            {
                FranchiseEditor.CommissionMargin = selectedFranchiseSetting.CommissionMargin;
                FranchiseEditor.ReteivaMargin = selectedFranchiseSetting.ReteivaMargin;
                FranchiseEditor.ReteicaMargin = selectedFranchiseSetting.ReteicaMargin;
                FranchiseEditor.RetefteMargin = selectedFranchiseSetting.RetefteMargin;
                FranchiseEditor.IvaMargin = selectedFranchiseSetting.IvaMargin;
                FranchiseEditor.SelectedBankAccount = FranchiseBankAccounts.FirstOrDefault(x => x.Id == selectedFranchiseSetting.BankAccountId);
                FranchiseEditor.FormulaCommission = selectedFranchiseSetting.FormulaCommission;
                FranchiseEditor.FormulaReteiva = selectedFranchiseSetting.FormulaReteiva;
                FranchiseEditor.FormulaReteica = selectedFranchiseSetting.FormulaReteica;
                FranchiseEditor.FormulaRetefte = selectedFranchiseSetting.FormulaRetefte;
                FranchiseEditor.CardValue = 0;
                FranchiseEditor.SimulatedCommission = 0;
                FranchiseEditor.SimulatedReteiva = 0;
                FranchiseEditor.SimulatedReteica = 0;
                FranchiseEditor.SimulatedRetefte = 0;
                FranchiseEditor.SimulatedIvaValue = 0;
            }
        }

        #endregion


        private ICommand _searchBankAccountingEntityCommand;
        public ICommand SearchBankAccountingEntityCommand
        {
            get
            {
                if (_searchBankAccountingEntityCommand is null) _searchBankAccountingEntityCommand = new RelayCommand(CanSearchBankAccountingEntity, SearchBankAccountingEntity);
                return _searchBankAccountingEntityCommand;
            }
        }

        public async void SearchBankAccountingEntity(object p)
        {
            string query = @"query($filter: AccountingEntityFilterInput!){
                PageResponse: accountingEntityPage(filter: $filter){
                count
                rows{
                    id
                    searchName
                    identificationNumber
                    verificationDigit
                }
                }
            }";

            string fieldHeader1 = "NIT";
            string fieldHeader2 = "Nombre o razón social";
            string fieldData1 = "IdentificationNumberWithVerificationDigit";
            string fieldData2 = "SearchName";
            var viewModel = new SearchWithTwoColumnsGridViewModel<AccountingEntityGraphQLModel>(query, fieldHeader1, fieldHeader2, fieldData1, fieldData2, null, SearchWithTwoColumnsGridMessageToken.BankAccountingEntity, _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Búsqueda de terceros");
        }

        public bool CanSearchBankAccountingEntity(object p) => true;


        public async Task LoadMajorCashDrawerCompanyLocations()
        {
            try
            {
                MajorCashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.FirstOrDefault(dummy => dummy is MajorCashDrawerDummyDTO) as MajorCashDrawerDummyDTO ?? throw new Exception("");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    majorCashDrawerDummyDTO.Locations.Remove(majorCashDrawerDummyDTO.Locations[0]);
                });
                Refresh();
                string query = @"
                    query{
                        ListResponse: companiesLocations{
                            id
                            name
                        }
                    }";

                IEnumerable<CompanyLocationGraphQLModel> source = await _companyLocationService.GetListAsync(query, new { });
                var locations = Context.AutoMapper.Map<ObservableCollection<TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO>>(source);
                if (locations.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO location in locations)
                        {
                            location.Context = this;
                            location.DummyParent = majorCashDrawerDummyDTO;
                            location.CostCenters.Add(new TreasuryMajorCashDrawerCostCenterMasterTreeDTO() { IsDummyChild = true, Name = "Fucking Dummy"});
                            majorCashDrawerDummyDTO?.Locations.Add(location);
                        }
                    });
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.LoadMajorCashDrawerCompanyLocations \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadMinorCashDrawerCompanyLocations()
        {
            try
            {
                MinorCashDrawerDummyDTO minorCashDrawerDummyDTO = DummyItems.FirstOrDefault(dummy => dummy is MinorCashDrawerDummyDTO) as MinorCashDrawerDummyDTO ?? throw new Exception("");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    minorCashDrawerDummyDTO.Locations.Remove(minorCashDrawerDummyDTO.Locations[0]);
                });
                Refresh();
                string query = @"
                    query{
                        ListResponse: companiesLocations{
                            id
                            name
                        }
                    }";

                IEnumerable<CompanyLocationGraphQLModel> source = await _companyLocationService.GetListAsync(query, new { });
                var locations = Context.AutoMapper.Map<ObservableCollection<TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO>>(source);
                if (locations.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO location in locations)
                        {
                            location.Context = this;
                            location.DummyParent = minorCashDrawerDummyDTO;
                            location.CostCenters.Add(new TreasuryMinorCashDrawerCostCenterMasterTreeDTO() { IsDummyChild = true, Name = "Fucking Dummy" });
                            minorCashDrawerDummyDTO?.Locations.Add(location);
                        }
                    });
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCompanyLocations" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadBanks()
        {
            try
            {
                BankDummyDTO bankDummyDTO = DummyItems.FirstOrDefault(dummy => dummy is BankDummyDTO) as BankDummyDTO ?? throw new Exception("");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    bankDummyDTO.Banks.Remove(bankDummyDTO.Banks[0]);
                });
                Refresh();
                string query = @"
                    query{
                        ListResponse: banks{
                            id
                            paymentMethodPrefix
                            accountingEntity{
                                id
                                searchName
                                captureType
                            }
                        }
                    }";

                var source = await _bankService.GetListAsync(query, new { });
                var banks = Context.AutoMapper.Map<ObservableCollection<TreasuryBankMasterTreeDTO>>(source);
                if (banks.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (TreasuryBankMasterTreeDTO bank in banks)
                        {
                            bank.Context = this;
                            bank.DummyParent = bankDummyDTO;
                            bank.BankAccounts.Add(new TreasuryBankAccountMasterTreeDTO() { IsDummyChild = true, Description = "Fucking Dummy" });
                            bankDummyDTO?.Banks.Add(bank);
                        }
                    });
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCompanyLocations" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadBankAccounts(TreasuryBankMasterTreeDTO bank)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    bank.BankAccounts.Remove(bank.BankAccounts[0]);
                });
                Refresh();
                string query = @"
                query($filter: BankAccountFilterInput!){
                  ListResponse: bankAccounts(filter: $filter){
                    id
                    type
                    number
                    isActive
                    description
                    reference
                    displayOrder
                    paymentMethod{
                        id
                        abbreviation
                        name
                    }
                    accountingAccount{
                      id
                      name
                      code
                    }
                    bank{
                      id
                      accountingEntity{
                        searchName
                        captureType
                      }
                    }
                    provider
                    allowedCostCenters{
                        id
                        name
                        bankAccountId
                    }
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.BankId = new ExpandoObject();
                variables.filter.BankId.@operator = "=";
                variables.filter.BankId.value = bank.Id;

                var source = await _bankAccountService.GetListAsync(query, variables);
                var bankAccounts = Context.AutoMapper.Map<ObservableCollection<TreasuryBankAccountMasterTreeDTO>>(source);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (TreasuryBankAccountMasterTreeDTO bankAccount in bankAccounts)
                    {
                        bankAccount.Context = this;
                        bank.BankAccounts.Add(bankAccount);
                    }
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCostCenters" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadMajorCashDrawerCostCenters(TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO location)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    location.CostCenters.Remove(location.CostCenters[0]);
                });

                List<int> ids = [location.Id];
                string query = @"
                    query($ids: [Int!]!){
                      ListResponse: costCentersByCompaniesLocationsIds(ids: $ids){
                        id
                        name
                        location{
                            id
                        }
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.ids = ids;

                var source = await _costCenterService.GetListAsync(query, variables);
                var CostCenters = Context.AutoMapper.Map<ObservableCollection<TreasuryMajorCashDrawerCostCenterMasterTreeDTO>>(source);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (TreasuryMajorCashDrawerCostCenterMasterTreeDTO costCenter in CostCenters)
                    {
                        costCenter.Context = this;
                        costCenter.CashDrawers.Add(new MajorCashDrawerMasterTreeDTO() { IsDummyChild = true, Name = "Fucking Dummy" });
                        location.CostCenters.Add(costCenter);
                    }
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCostCenters" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadMinorCashDrawerCostCenters(TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO location)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    location.CostCenters.Remove(location.CostCenters[0]);
                });

                List<int> ids = [location.Id];
                string query = @"
                    query($ids: [Int!]!){
                      ListResponse: costCentersByCompaniesLocationsIds(ids: $ids){
                        id
                        name
                        location{
                            id
                        }
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.ids = ids;

                var source = await _costCenterService.GetListAsync(query, variables);
                var CostCenters = Context.AutoMapper.Map<ObservableCollection<TreasuryMinorCashDrawerCostCenterMasterTreeDTO>>(source);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (TreasuryMinorCashDrawerCostCenterMasterTreeDTO costCenter in CostCenters)
                    {
                        costCenter.Context = this;
                        costCenter.CashDrawers.Add(new MinorCashDrawerMasterTreeDTO() { IsDummyChild = true, Name = "Fucking Dummy" });
                        location.CostCenters.Add(costCenter);
                    }
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCostCenters" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadMajorCashDrawers(TreasuryMajorCashDrawerCostCenterMasterTreeDTO costCenterDTO)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    costCenterDTO.CashDrawers.Remove(costCenterDTO.CashDrawers[0]);
                });


                string query = @"
                query($filter: CashDrawerFilterInput!){
                  ListResponse: cashDrawers(filter: $filter){
                    id
                    name
                    costCenter{
                      id
                      name
                    }
                    accountingAccountCash{
                      id  
                      code
                      name
                    }
                    accountingAccountCheck{
                      id  
                      code
                      name
                    }
                    accountingAccountCard{
                      id  
                      code
                      name
                    }
                    cashReviewRequired
                    autoAdjustBalance
                    autoTransfer
                    cashDrawerAutoTransfer{
                      id
                      name
                    }
                    isPettyCash    
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.costCenterId = new ExpandoObject();
                variables.filter.costCenterId.@operator = "=";
                variables.filter.costCenterId.value = costCenterDTO.Id;

                variables.filter.parentId = new ExpandoObject();
                variables.filter.parentId.@operator = "=";
                variables.filter.parentId.value = 0;

                variables.filter.isPettyCash = new ExpandoObject();
                variables.filter.isPettyCash.@operator = "=";
                variables.filter.isPettyCash.value = false;

                var source = await _cashDrawerService.GetListAsync(query, variables);
                var CashDrawers = Context.AutoMapper.Map<ObservableCollection<MajorCashDrawerMasterTreeDTO>>(source);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (MajorCashDrawerMasterTreeDTO cashDrawerDTO in CashDrawers)
                    {
                        cashDrawerDTO.Context = this;
                        cashDrawerDTO.AuxiliaryCashDrawers.Add(new TreasuryAuxiliaryCashDrawerMasterTreeDTO() { IsDummyChild = true, Name = "Fucking Dummy" });
                        costCenterDTO.CashDrawers.Add(cashDrawerDTO);
                    }
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadMajorCashDrawers" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadAuxiliaryCashDrawers(MajorCashDrawerMasterTreeDTO majorCashDrawer)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    majorCashDrawer.AuxiliaryCashDrawers.Remove(majorCashDrawer.AuxiliaryCashDrawers[0]);
                });


                string query = @"
                query($filter: CashDrawerFilterInput!){
                  ListResponse: cashDrawers(filter: $filter){
                    id
                    name
                    accountingAccountCash{
                      id  
                      code
                      name
                    }
                    accountingAccountCheck{
                      id  
                      code
                      name
                    }
                    accountingAccountCard{
                      id  
                      code
                      name
                    }
                    cashReviewRequired
                    autoAdjustBalance
                    autoTransfer
                    cashDrawerAutoTransfer{
                      id
                      name
                    }
                    isPettyCash
                    computerName
                    }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.parentId = new ExpandoObject();
                variables.filter.parentId.@operator = "=";
                variables.filter.parentId.value = majorCashDrawer.Id;

                variables.filter.isPettyCash = new ExpandoObject();
                variables.filter.isPettyCash.@operator = "=";
                variables.filter.isPettyCash.value = false;

                var source = await _cashDrawerService.GetListAsync(query, variables);
                var CashDrawers = Context.AutoMapper.Map<ObservableCollection<TreasuryAuxiliaryCashDrawerMasterTreeDTO>>(source);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawer in CashDrawers)
                    {
                        majorCashDrawer.AuxiliaryCashDrawers.Add(auxiliaryCashDrawer);
                    }
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadMajorCashDrawers" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadMinorCashDrawers(TreasuryMinorCashDrawerCostCenterMasterTreeDTO costCenterDTO)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    costCenterDTO.CashDrawers.Remove(costCenterDTO.CashDrawers[0]);
                });


                string query = @"
                query($filter: CashDrawerFilterInput!){
                  ListResponse: cashDrawers(filter: $filter){
                    id
                    name
                    costCenter{
                      id
                      name
                    }
                    accountingAccountCash{
                      id  
                      code
                      name
                    }
                    cashReviewRequired
                    autoAdjustBalance
                    isPettyCash    
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.costCenterId = new ExpandoObject();
                variables.filter.costCenterId.@operator = "=";
                variables.filter.costCenterId.value = costCenterDTO.Id;

                variables.filter.isPettyCash = new ExpandoObject();
                variables.filter.isPettyCash.@operator = "=";
                variables.filter.isPettyCash.value = true;

                variables.filter.parentId = new ExpandoObject();
                variables.filter.parentId.@operator = "=";
                variables.filter.parentId.value = 0;

                var source = await _cashDrawerService.GetListAsync(query, variables);
                var CashDrawers = Context.AutoMapper.Map<ObservableCollection<MinorCashDrawerMasterTreeDTO>>(source);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (MinorCashDrawerMasterTreeDTO cashDrawerDTO in CashDrawers)
                    {
                        costCenterDTO.CashDrawers.Add(cashDrawerDTO);
                    }
                });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCashDrawers" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadFranchises(FranchiseDummyDTO franchiseDummyDTO)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    franchiseDummyDTO.Franchises.Remove(franchiseDummyDTO.Franchises[0]);
                });
                Refresh();
                string query = @"
                query($filter: FranchiseFilterInput!){
                  ListResponse: franchises(filter: $filter){
                    id
                    name
                    type
                    commissionMargin
                    reteivaMargin
                    reteicaMargin
                    retefteMargin
                    ivaMargin
                    bankAccount{
                      id
                      description
                    }
                    accountingAccountCommission{
                      id
                      name
                    }
                    formulaCommission
                    formulaReteiva
                    formulaReteica
                    formulaRetefte
                    franchiseSettingsByCostCenter{
                        id
                        costCenterId
                        commissionMargin
                        reteivaMargin
                        reteicaMargin
                        retefteMargin
                        ivaMargin
                        bankAccountId
                        formulaCommission
                        formulaReteiva
                        formulaReteica
                        formulaRetefte
                        franchiseId
                    }
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                //TODO : Cambiar por el id de la compañía
                variables.filter.CompanyId = new ExpandoObject();
                variables.filter.CompanyId.@operator = "=";
                variables.filter.CompanyId.value = 1;
                var source = await _franchiseService.GetListAsync(query, variables);
                var franchises = Context.AutoMapper.Map<ObservableCollection<TreasuryFranchiseMasterTreeDTO>>(source);
                if (franchises.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (TreasuryFranchiseMasterTreeDTO franchise in franchises)
                        {
                            franchise.Context = this;
                            franchise.DummyParent = franchiseDummyDTO;
                            franchiseDummyDTO?.Franchises.Add(franchise);
                        }
                    });
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCompanyLocations" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public async Task LoadComboBoxesAsync()
        {
            try
            {
                string query = @"
                query($accountingAccountFilter: AccountingAccountFilterInput!, $cashDrawerFilter: CashDrawerFilterInput!, $bankAccountFilter: BankAccountFilterInput!){
                  accountingAccounts(filter: $accountingAccountFilter){
                    id
                    code
                    name
                  }
                  cashDrawers(filter: $cashDrawerFilter){
                    id
                    name
                  }
                   costCenters{
                    id
                    name
                  }
                    bankAccounts(filter: $bankAccountFilter){
                    id
                    description
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.accountingAccountFilter = new ExpandoObject();
                variables.accountingAccountFilter.code = new ExpandoObject();
                variables.accountingAccountFilter.code.@operator = new List<string>() { "length", ">=" };
                variables.accountingAccountFilter.code.value = 8;

                variables.cashDrawerFilter = new ExpandoObject();
                variables.cashDrawerFilter.isPettyCash = new ExpandoObject();
                variables.cashDrawerFilter.isPettyCash.@operator = "=";
                variables.cashDrawerFilter.isPettyCash.value = false;

                variables.bankAccountFilter = new ExpandoObject();
                variables.bankAccountFilter.allowedTypes = new ExpandoObject();
                variables.bankAccountFilter.allowedTypes.value = "AC";
                variables.bankAccountFilter.allowedTypes.exclude = true;

                var result = await _cashDrawerService.GetDataContextAsync<CashDrawerComboBoxesDataContext>(query, variables);
                CashDrawerAccountingAccounts = new ObservableCollection<AccountingAccountGraphQLModel>(result.AccountingAccounts);
                BankAccountAccountingAccounts = new ObservableCollection<AccountingAccountGraphQLModel>(result.AccountingAccounts);
                BankAccountCostCenters = Context.AutoMapper.Map<ObservableCollection<TreasuryBankAccountCostCenterDTO>>(result.CostCenters);
                FranchiseAccountingAccountsCommission = Context.AutoMapper.Map<ObservableCollection<AccountingAccountGraphQLModel>>(result.AccountingAccounts);
                FranchiseBankAccounts = Context.AutoMapper.Map<ObservableCollection<BankAccountGraphQLModel>>(result.BankAccounts);
                FranchiseCostCenters = Context.AutoMapper.Map<ObservableCollection<TreasuryFranchiseCostCenterDTO>>(result.CostCenters);
                FranchiseCostCenters.Insert(0, new TreasuryFranchiseCostCenterDTO() { Id = 0, Name = "[ APLICACIÓN GENERAL ]" });
                FranchiseBankAccounts.Insert(0, new BankAccountGraphQLModel() { Id = 0, Description = "<< SELECCIONE UNA CUENTA BANCARIA >>" });
                FranchiseAccountingAccountsCommission.Insert(0, new AccountingAccountGraphQLModel() { Id = 0, Name = "<< SELECCIONE UNA CUENTA CONTABLE >>" });
                BankAccountAccountingAccounts.Insert(0, new AccountingAccountGraphQLModel() { Id = 0, Name = "<< SELECCIONE UNA CUENTA CONTABLE >> " });
                CashDrawers = new ObservableCollection<CashDrawerGraphQLModel>(result.CashDrawers);
                CashDrawers.Insert(0, new CashDrawerGraphQLModel() { Id = 0, Name = "<< SELECCIONE UNA CAJA GENERAL >> " });
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCompanyLocations" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }
        public TreasuryRootMasterViewModel(
            TreasuryRootViewModel context,
            IRepository<CompanyLocationGraphQLModel> companyLocationService,
            IRepository<CostCenterGraphQLModel> costCenterService,
            IRepository<CashDrawerGraphQLModel> cashDrawerService,
            IRepository<BankGraphQLModel> bankService,
            IRepository<BankAccountGraphQLModel> bankAccountService,
            IRepository<FranchiseGraphQLModel> franchiseService,
            Helpers.IDialogService dialogService,
            Helpers.Services.INotificationService notificationService)
        {
            _companyLocationService = companyLocationService;
            _costCenterService = costCenterService;
            _cashDrawerService = cashDrawerService;
            _bankService = bankService;
            _bankAccountService = bankAccountService;
            _franchiseService = franchiseService;
            _dialogService = dialogService;
            _notificationService = notificationService;
            
            Messenger.Default.Register<ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel>>(this, SearchWithTwoColumnsGridMessageToken.BankAccountingEntity, false, OnFindBankAccountingEntityMessage);
            DummyItems = [
            new MajorCashDrawerDummyDTO() { 
                Id = 1, Name = "CAJA GENERAL", Locations = [new TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO() { IsDummyChild = true, Name = "Fucking Dummy"}], Context = this 
            },
            new MinorCashDrawerDummyDTO() {
                Id = 2, Name = "CAJA MENOR", Locations = [new TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO() { IsDummyChild = true, Name = "Fucking Dummy", }], Context = this
            },
            new BankDummyDTO(){
                Id = 3, Name = "BANCOS", Banks = [new TreasuryBankMasterTreeDTO() { IsDummyChild = true }], Context = this
            },
            new FranchiseDummyDTO(){
                Id = 4, Name = "FRANQUICIAS", Franchises = [new TreasuryFranchiseMasterTreeDTO() { IsDummyChild = true, Name = "FuckingDummy"}], Context = this
            }
            ];
            Context = context;
            _errors = [];
            Context.EventAggregator.SubscribeOnUIThread(this);

            // Initialize Panel Editors
            MajorCashDrawerEditor = new MajorCashDrawerPanelEditor(this, _cashDrawerService);
            MinorCashDrawerEditor = new MinorCashDrawerPanelEditor(this, _cashDrawerService);
            AuxiliaryCashDrawerEditor = new AuxiliaryCashDrawerPanelEditor(this, _cashDrawerService);
            BankEditor = new BankPanelEditor(this, _bankService);
            BankAccountEditor = new BankAccountPanelEditor(this, _bankAccountService);
            FranchiseEditor = new FranchisePanelEditor(this, _franchiseService);
        }

        public async Task Initialize()
        {
            await LoadComboBoxesAsync();
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);
            await Initialize();
        }

        public void OnFindBankAccountingEntityMessage(ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel> message)
        {
            if (message.ReturnedData is null) return;
            BankEditor.AccountingEntityId = message.ReturnedData.Id;
            BankEditor.AccountingEntityName = message.ReturnedData.SearchName;
        }

        public async Task HandleAsync(TreasuryCashDrawerCreateMessage message, CancellationToken cancellationToken)
        {
            IsNewRecord = false;

            //caja general
            if (message.CreatedCashDrawer.IsPettyCash == false && message.CreatedCashDrawer.Parent is null)
            {
                MajorCashDrawerMasterTreeDTO majorCashDrawerMasterTreeDTO = Context.AutoMapper.Map<MajorCashDrawerMasterTreeDTO>(message.CreatedCashDrawer);
                MajorCashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.FirstOrDefault(x => x is MajorCashDrawerDummyDTO) as MajorCashDrawerDummyDTO ?? throw new Exception("");
                if (majorCashDrawerDummyDTO is null) return;
                TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO majorCashDrawerCompanyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.CreatedCashDrawer.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
                if (majorCashDrawerCompanyLocation is null) return;
                TreasuryMajorCashDrawerCostCenterMasterTreeDTO majorCashDrawerCostCenter = majorCashDrawerCompanyLocation.CostCenters.FirstOrDefault(x => x.Id == message.CreatedCashDrawer.CostCenter.Id) ?? throw new Exception("");
                if (majorCashDrawerCostCenter is null) return;
                if (!majorCashDrawerCostCenter.IsExpanded && majorCashDrawerCostCenter.CashDrawers[0].IsDummyChild)
                {
                    await LoadMajorCashDrawers(majorCashDrawerCostCenter);
                    majorCashDrawerCostCenter.IsExpanded = true;
                    MajorCashDrawerMasterTreeDTO? majorCashDrawer = majorCashDrawerCostCenter.CashDrawers.FirstOrDefault(x => x.Id == majorCashDrawerMasterTreeDTO.Id);
                    if (majorCashDrawer is null) return;
                    SelectedItem = majorCashDrawer;
                    _notificationService.ShowSuccess("Caja General creada correctamente.");
                    return;
                }
                if (!majorCashDrawerCostCenter.IsExpanded)
                {
                    majorCashDrawerCostCenter.IsExpanded = true;
                    majorCashDrawerCostCenter.CashDrawers.Add(majorCashDrawerMasterTreeDTO);
                    SelectedItem = majorCashDrawerMasterTreeDTO;
                    _notificationService.ShowSuccess("Caja General creada correctamente.");
                    return;
                }
                majorCashDrawerCostCenter.CashDrawers.Add(majorCashDrawerMasterTreeDTO);
                SelectedItem = majorCashDrawerMasterTreeDTO;
                _notificationService.ShowSuccess("Caja General creada correctamente.");
                return;
            }
            //caja auxiliar
            if (message.CreatedCashDrawer.IsPettyCash == false && message.CreatedCashDrawer.Parent != null)
            {
                TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawerMasterTreeDTO = Context.AutoMapper.Map<TreasuryAuxiliaryCashDrawerMasterTreeDTO>(message.CreatedCashDrawer);
                MajorCashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.FirstOrDefault(x => x is MajorCashDrawerDummyDTO) as MajorCashDrawerDummyDTO ?? throw new Exception("");
                if (majorCashDrawerDummyDTO is null) return;
                TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO majorCashDrawerCompanyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.CreatedCashDrawer.Parent.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
                if (majorCashDrawerCompanyLocation is null) return;
                TreasuryMajorCashDrawerCostCenterMasterTreeDTO majorCashDrawerCostCenter = majorCashDrawerCompanyLocation.CostCenters.FirstOrDefault(x => x.Id == message.CreatedCashDrawer.Parent.CostCenter.Id) ?? throw new Exception("");
                if (majorCashDrawerCostCenter is null) return;
                MajorCashDrawerMasterTreeDTO majorCashDrawer = majorCashDrawerCostCenter.CashDrawers.FirstOrDefault(x => x.Id == message.CreatedCashDrawer.Parent.Id) ?? throw new Exception("");
                if (majorCashDrawer == null) return;
                if (!majorCashDrawer.IsExpanded && majorCashDrawer.AuxiliaryCashDrawers[0].IsDummyChild)
                {
                    await LoadAuxiliaryCashDrawers(majorCashDrawer);
                    majorCashDrawer.IsExpanded = true;
                    TreasuryAuxiliaryCashDrawerMasterTreeDTO? auxiliaryCashDrawer = majorCashDrawer.AuxiliaryCashDrawers.FirstOrDefault(x => x.Id == auxiliaryCashDrawerMasterTreeDTO.Id);
                    if (auxiliaryCashDrawerMasterTreeDTO is null) return;
                    SelectedItem = auxiliaryCashDrawer;
                    _notificationService.ShowSuccess("Caja Auxiliar creada correctamente.");
                    return;
                }
                if (!majorCashDrawer.IsExpanded)
                {
                    majorCashDrawer.IsExpanded = true;
                    majorCashDrawer.AuxiliaryCashDrawers.Add(auxiliaryCashDrawerMasterTreeDTO);
                    SelectedItem = auxiliaryCashDrawerMasterTreeDTO;
                    _notificationService.ShowSuccess("Caja Auxiliar creada correctamente.");
                    return;
                }
                majorCashDrawer.AuxiliaryCashDrawers.Add(auxiliaryCashDrawerMasterTreeDTO);
                SelectedItem = auxiliaryCashDrawerMasterTreeDTO;
                _notificationService.ShowSuccess("Caja Auxiliar creada correctamente.");
                return;
            }

            //caja menor
            MinorCashDrawerMasterTreeDTO minorCashDrawerMasterTreeDTO = Context.AutoMapper.Map<MinorCashDrawerMasterTreeDTO>(message.CreatedCashDrawer);
            MinorCashDrawerDummyDTO minorCashDrawerDummyDTO = DummyItems.FirstOrDefault(x => x is MinorCashDrawerDummyDTO) as MinorCashDrawerDummyDTO ?? throw new Exception("");
            if (minorCashDrawerDummyDTO is null) return;
            TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO minorCashDrawerCompanyLocation = minorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.CreatedCashDrawer.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
            if (minorCashDrawerCompanyLocation is null) return;
            TreasuryMinorCashDrawerCostCenterMasterTreeDTO minorCashDrawerCostCenter = minorCashDrawerCompanyLocation.CostCenters.FirstOrDefault(x => x.Id == message.CreatedCashDrawer.CostCenter.Id) ?? throw new Exception("");
            if (minorCashDrawerCostCenter is null) return;
            if (!minorCashDrawerCostCenter.IsExpanded && minorCashDrawerCostCenter.CashDrawers[0].IsDummyChild)
            {
                await LoadMinorCashDrawers(minorCashDrawerCostCenter);
                minorCashDrawerCostCenter.IsExpanded = true;
                MinorCashDrawerMasterTreeDTO? minorCasDrawer = minorCashDrawerCostCenter.CashDrawers.FirstOrDefault(x => x.Id == minorCashDrawerMasterTreeDTO.Id);
                if (minorCasDrawer is null) return;
                SelectedItem = minorCasDrawer;
                _notificationService.ShowSuccess("Caja menor creada correctamente.");
                return;
            }
            if (!minorCashDrawerCostCenter.IsExpanded)
            {
                minorCashDrawerCostCenter.IsExpanded = true;
                minorCashDrawerCostCenter.CashDrawers.Add(minorCashDrawerMasterTreeDTO);
                SelectedItem = minorCashDrawerMasterTreeDTO;
                _notificationService.ShowSuccess("Caja menor creada correctamente.");
                return;
            }
            minorCashDrawerCostCenter.CashDrawers.Add(minorCashDrawerMasterTreeDTO);
            SelectedItem = minorCashDrawerMasterTreeDTO;
            _notificationService.ShowSuccess("Caja menor creada correctamente.");
            return;
        }

        public Task HandleAsync(TreasuryCashDrawerDeleteMessage message, CancellationToken cancellationToken)
        {
            if(message.DeletedCashDrawer.IsPettyCash is false && message.DeletedCashDrawer.Parent is null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MajorCashDrawerDummyDTO majorCashDrawerDTO = DummyItems.FirstOrDefault(x => x is MajorCashDrawerDummyDTO) as MajorCashDrawerDummyDTO ?? throw new Exception("");
                    if (majorCashDrawerDTO is null) return;
                    TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO companyLocation = majorCashDrawerDTO.Locations.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
                    if (companyLocation is null) return;
                    TreasuryMajorCashDrawerCostCenterMasterTreeDTO costCenter = companyLocation.CostCenters.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.CostCenter.Id) ?? throw new Exception("");
                    if (costCenter is null) return;
                    costCenter.CashDrawers.Remove(costCenter.CashDrawers.Where(x => x.Id == message.DeletedCashDrawer.Id).First());
                    SelectedItem = null;
                });
                _notificationService.ShowSuccess("Caja General eliminada correctamente.");
                return Task.CompletedTask;
            }
            if(message.DeletedCashDrawer.IsPettyCash is false && message.DeletedCashDrawer.Parent != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MajorCashDrawerDummyDTO majorCashDrawerDTO = DummyItems.FirstOrDefault(x => x is MajorCashDrawerDummyDTO) as MajorCashDrawerDummyDTO ?? throw new Exception("");
                    if (majorCashDrawerDTO is null) return;
                    TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO companyLocation = majorCashDrawerDTO.Locations.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.Parent.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
                    if (companyLocation is null) return;
                    TreasuryMajorCashDrawerCostCenterMasterTreeDTO costCenter = companyLocation.CostCenters.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.Parent.CostCenter.Id) ?? throw new Exception("");
                    if (costCenter is null) return;
                    MajorCashDrawerMasterTreeDTO majorCashDrawer = costCenter.CashDrawers.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.Parent.Id) ?? throw new Exception("");
                    if (majorCashDrawer is null) return;
                    majorCashDrawer.AuxiliaryCashDrawers.Remove(majorCashDrawer.AuxiliaryCashDrawers.Where(x => x.Id == message.DeletedCashDrawer.Id).First());
                    SelectedItem = null;
                });
                _notificationService.ShowSuccess("Caja Auxiliar eliminada correctamente.");
                return Task.CompletedTask;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MinorCashDrawerDummyDTO minorCashDrawerDTO = DummyItems.FirstOrDefault(x => x is MinorCashDrawerDummyDTO) as MinorCashDrawerDummyDTO ?? throw new Exception("");
                if (minorCashDrawerDTO is null) return;
                TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO companyLocation = minorCashDrawerDTO.Locations.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
                if (companyLocation is null) return;
                TreasuryMinorCashDrawerCostCenterMasterTreeDTO costCenter = companyLocation.CostCenters.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.CostCenter.Id) ?? throw new Exception("");
                if (costCenter is null) return;
                costCenter.CashDrawers.Remove(costCenter.CashDrawers.Where(x => x.Id == message.DeletedCashDrawer.Id).First());
                SelectedItem = null;
            });
            _notificationService.ShowSuccess("Caja menor eliminada correctamente.");
            return Task.CompletedTask;
        }

        public async Task HandleAsync(TreasuryCashDrawerUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedCashDrawer.IsPettyCash is false && message.UpdatedCashDrawer.Parent is null)
            {
                MajorCashDrawerMasterTreeDTO cashDrawerDTO = Context.AutoMapper.Map<MajorCashDrawerMasterTreeDTO>(message.UpdatedCashDrawer);
                MajorCashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.FirstOrDefault(x => x is MajorCashDrawerDummyDTO) as MajorCashDrawerDummyDTO ?? throw new Exception("");
                if (majorCashDrawerDummyDTO is null) return;
                TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO majorCashDrawerCompanyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
                if (majorCashDrawerCompanyLocation is null) return;
                TreasuryMajorCashDrawerCostCenterMasterTreeDTO majorCashDrawerCostCenter = majorCashDrawerCompanyLocation.CostCenters.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.CostCenter.Id) ?? throw new Exception("");
                if (majorCashDrawerCostCenter is null) return;
                MajorCashDrawerMasterTreeDTO cashDrawerToUpdate = majorCashDrawerCostCenter.CashDrawers.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.Id) ?? throw new Exception("");
                if (cashDrawerToUpdate is null) return;
                cashDrawerToUpdate.Id = cashDrawerDTO.Id;
                cashDrawerToUpdate.Name = cashDrawerDTO.Name;
                cashDrawerToUpdate.AccountingAccountCash = cashDrawerDTO.AccountingAccountCash;
                cashDrawerToUpdate.AccountingAccountCheck = cashDrawerDTO.AccountingAccountCheck;
                cashDrawerToUpdate.AccountingAccountCard = cashDrawerDTO.AccountingAccountCard;
                cashDrawerToUpdate.CashReviewRequired = cashDrawerDTO.CashReviewRequired;
                cashDrawerToUpdate.AutoAdjustBalance = cashDrawerDTO.AutoAdjustBalance;
                cashDrawerToUpdate.AutoTransfer = cashDrawerDTO.AutoTransfer;
                cashDrawerToUpdate.CashDrawerAutoTransfer = cashDrawerDTO.CashDrawerAutoTransfer;
                await Task.Run(() => MajorCashDrawerEditor.SetForEdit(cashDrawerToUpdate), cancellationToken);
                _notificationService.ShowSuccess("Caja General actualizada correctamente.");
                return;
            }
            if(message.UpdatedCashDrawer.IsPettyCash is false && message.UpdatedCashDrawer.Parent != null)
            {
                TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawer = Context.AutoMapper.Map<TreasuryAuxiliaryCashDrawerMasterTreeDTO>(message.UpdatedCashDrawer);
                MajorCashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.FirstOrDefault(x => x is MajorCashDrawerDummyDTO) as MajorCashDrawerDummyDTO ?? throw new Exception("");
                if (majorCashDrawerDummyDTO is null) return;
                TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO majorCashDrawerCompanyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.Parent.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
                if (majorCashDrawerCompanyLocation is null) return;
                TreasuryMajorCashDrawerCostCenterMasterTreeDTO majorCashDrawerCostCenter = majorCashDrawerCompanyLocation.CostCenters.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.Parent.CostCenter.Id) ?? throw new Exception("");
                if (majorCashDrawerCostCenter is null) return;
                MajorCashDrawerMasterTreeDTO majorCashDrawer = majorCashDrawerCostCenter.CashDrawers.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.Parent.Id) ?? throw new Exception("");
                if (majorCashDrawer is null) return;
                TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawerToUpdate = majorCashDrawer.AuxiliaryCashDrawers.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.Id) ?? throw new Exception("");
                if (auxiliaryCashDrawerToUpdate is null) return;
                auxiliaryCashDrawerToUpdate.Id = auxiliaryCashDrawer.Id;
                auxiliaryCashDrawerToUpdate.Name = auxiliaryCashDrawer.Name;
                auxiliaryCashDrawerToUpdate.AccountingAccountCash = auxiliaryCashDrawer.AccountingAccountCash;
                auxiliaryCashDrawerToUpdate.AccountingAccountCheck = auxiliaryCashDrawer.AccountingAccountCheck;
                auxiliaryCashDrawerToUpdate.AccountingAccountCard = auxiliaryCashDrawer.AccountingAccountCard;
                auxiliaryCashDrawerToUpdate.CashReviewRequired = auxiliaryCashDrawer.CashReviewRequired;
                auxiliaryCashDrawerToUpdate.AutoAdjustBalance = auxiliaryCashDrawer.AutoAdjustBalance;
                auxiliaryCashDrawerToUpdate.AutoTransfer = auxiliaryCashDrawer.AutoTransfer;
                auxiliaryCashDrawerToUpdate.CashDrawerAutoTransfer = auxiliaryCashDrawer.CashDrawerAutoTransfer;
                auxiliaryCashDrawerToUpdate.ComputerName = auxiliaryCashDrawer.ComputerName;
                await Task.Run(() => AuxiliaryCashDrawerEditor.SetForEdit(auxiliaryCashDrawerToUpdate), cancellationToken);
                _notificationService.ShowSuccess("Caja Auxiliar actualizada correctamente.");
                return;
            }
            MinorCashDrawerMasterTreeDTO minorCashDrawerMasterTreeDTO = Context.AutoMapper.Map<MinorCashDrawerMasterTreeDTO>(message.UpdatedCashDrawer);
            MinorCashDrawerDummyDTO minorCashDrawerDummyDTO = DummyItems.FirstOrDefault(x => x is MinorCashDrawerDummyDTO) as MinorCashDrawerDummyDTO ?? throw new Exception("");
            if (minorCashDrawerDummyDTO is null) return;
            TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO minorCashDrawerCompanyLocation = minorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
            if (minorCashDrawerCompanyLocation is null) return;
            TreasuryMinorCashDrawerCostCenterMasterTreeDTO minorCashDrawerCostCenter = minorCashDrawerCompanyLocation.CostCenters.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.CostCenter.Id) ?? throw new Exception("");
            if (minorCashDrawerCostCenter is null) return;
            MinorCashDrawerMasterTreeDTO minorCashDrawerToUpdate = minorCashDrawerCostCenter.CashDrawers.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.Id) ?? throw new Exception("");
            if (minorCashDrawerToUpdate is null) return;
            minorCashDrawerToUpdate.Id = minorCashDrawerMasterTreeDTO.Id;
            minorCashDrawerToUpdate.Name = minorCashDrawerMasterTreeDTO.Name;
            minorCashDrawerToUpdate.AccountingAccountCash = minorCashDrawerMasterTreeDTO.AccountingAccountCash;
            minorCashDrawerToUpdate.CashReviewRequired = minorCashDrawerMasterTreeDTO.CashReviewRequired;
            minorCashDrawerToUpdate.AutoAdjustBalance = minorCashDrawerMasterTreeDTO.AutoAdjustBalance;
            await Task.Run(() => MinorCashDrawerEditor.SetForEdit(minorCashDrawerToUpdate), cancellationToken);
            _notificationService.ShowSuccess("Caja menor actualizada correctamente.");
            return;
        }

        public bool HasErrors => _errors.Count > 0;

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

        private void ClearAllErrors()
        {
            _errors.Clear();
        }

        public async Task HandleAsync(BankCreateMessage message, CancellationToken cancellationToken)
        {
            IsNewRecord = false;

            TreasuryBankMasterTreeDTO bankDTO = Context.AutoMapper.Map<TreasuryBankMasterTreeDTO>(message.CreatedBank);
            BankDummyDTO bankDummyDTO = DummyItems.FirstOrDefault(x => x is BankDummyDTO) as BankDummyDTO ?? throw new Exception("");
            if (bankDummyDTO is null) return;
            if (!bankDummyDTO.IsExpanded && bankDummyDTO.Banks[0].IsDummyChild)
            {
                await LoadBanks();
                bankDummyDTO.IsExpanded = true;
                TreasuryBankMasterTreeDTO? bank = bankDummyDTO.Banks.FirstOrDefault(x => x.Id == bankDTO.Id);
                if (bank is null) return;
                SelectedItem = bank;
                _notificationService.ShowSuccess("Banco creado correctamente.");
                return;
            }
            if (!bankDummyDTO.IsExpanded)
            {
                bankDummyDTO.IsExpanded = true;
                bankDummyDTO.Banks.Add(bankDTO);
                SelectedItem = bankDTO;
                _notificationService.ShowSuccess("Banco creado correctamente.");
                return;
            }
            bankDummyDTO.Banks.Add(bankDTO);
            SelectedItem = bankDTO;
            _notificationService.ShowSuccess("Banco creado correctamente.");
            return;
            
        }

        public Task HandleAsync(BankUpdateMessage message, CancellationToken cancellationToken)
        {
            TreasuryBankMasterTreeDTO bankDTO = Context.AutoMapper.Map<TreasuryBankMasterTreeDTO>(message.UpdatedBank);
            BankDummyDTO bankDummyDTO = DummyItems.FirstOrDefault(x => x is BankDummyDTO) as BankDummyDTO ?? throw new Exception("");
            if (bankDummyDTO is null) return Task.CompletedTask;
            TreasuryBankMasterTreeDTO bankToUpdate = bankDummyDTO.Banks.FirstOrDefault(x => x.Id == message.UpdatedBank.Id) ?? throw new Exception("");
            if (bankToUpdate is null) return Task.CompletedTask;
            bankToUpdate.Id = bankDTO.Id;
            bankToUpdate.AccountingEntity = bankDTO.AccountingEntity;
            bankToUpdate.PaymentMethodPrefix = bankDTO.PaymentMethodPrefix;
            _notificationService.ShowSuccess("Banco actualizado correctamente.");
            return Task.CompletedTask;
        }

        public Task HandleAsync(BankDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                BankDummyDTO bankDummyDTO = DummyItems.FirstOrDefault(x => x is BankDummyDTO) as BankDummyDTO ?? throw new Exception("");
                if (bankDummyDTO is null) return;
                bankDummyDTO.Banks.Remove(bankDummyDTO.Banks.Where(x => x.Id == message.DeletedBank.Id).First());
            });
            _notificationService.ShowSuccess("Banco eliminado correctamente.");
            return Task.CompletedTask;
        }

        public async Task HandleAsync(BankAccountCreateMessage message, CancellationToken cancellationToken)
        {
            IsNewRecord = false;

            TreasuryBankAccountMasterTreeDTO bankAccountDTO = Context.AutoMapper.Map<TreasuryBankAccountMasterTreeDTO>(message.CreatedBankAccount);
            BankDummyDTO bankDummyDTO = DummyItems.FirstOrDefault(x => x is BankDummyDTO) as BankDummyDTO ?? throw new Exception("");
            if (bankDummyDTO is null) return;
            TreasuryBankMasterTreeDTO bankDTO = bankDummyDTO.Banks.FirstOrDefault(x => x.Id == message.CreatedBankAccount.Bank.Id) ?? throw new Exception("");
            if (bankDTO is null) return;
            if (!bankDTO.IsExpanded && bankDTO.BankAccounts[0].IsDummyChild)
            {
                await LoadBankAccounts(bankDTO);
                bankDTO.IsExpanded = true;
                TreasuryBankAccountMasterTreeDTO? bankAccount = bankDTO.BankAccounts.FirstOrDefault(x => x.Id == bankAccountDTO.Id);
                if (bankAccount is null) return;
                SelectedItem = bankAccount;
                _notificationService.ShowSuccess("Cuenta bancaria creada correctamente.");
                return;
            }
            if (!bankDTO.IsExpanded)
            {
                bankDTO.IsExpanded = true;
                bankDTO.BankAccounts.Add(bankAccountDTO);
                SelectedItem = bankAccountDTO;
                _notificationService.ShowSuccess("Cuenta bancaria creada correctamente.");
                return;
            }
            bankDTO.BankAccounts.Add(bankAccountDTO);
            SelectedItem = bankAccountDTO;
            _notificationService.ShowSuccess("Cuenta bancaria creada correctamente.");
            return;
        }

        public Task HandleAsync(BankAccountDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                BankDummyDTO bankDummyDTO = DummyItems.FirstOrDefault(x => x is BankDummyDTO) as BankDummyDTO ?? throw new Exception("");
                if (bankDummyDTO is null) return;
                TreasuryBankMasterTreeDTO bankAccountDTO = bankDummyDTO.Banks.FirstOrDefault(x => x.Id == message.DeletedBankAccount.Bank.Id) ?? throw new Exception("");
                if (bankAccountDTO is null) return;
                bankAccountDTO.BankAccounts.Remove(bankAccountDTO.BankAccounts.Where(x => x.Id == message.DeletedBankAccount.Id).First());
            });
            _notificationService.ShowSuccess("Cuenta bancaria eliminada correctamente.");
            return Task.CompletedTask;
        }

        public async Task HandleAsync(BankAccountUpdateMessage message, CancellationToken cancellationToken)
        {
            TreasuryBankAccountMasterTreeDTO bankAccountDTO = Context.AutoMapper.Map<TreasuryBankAccountMasterTreeDTO>(message.UpdatedBankAccount);
            BankDummyDTO bankDummyDTO = DummyItems.FirstOrDefault(x => x is BankDummyDTO) as BankDummyDTO ?? throw new Exception("");
            if (bankDummyDTO is null) return;
            TreasuryBankMasterTreeDTO bankDTO = bankDummyDTO.Banks.FirstOrDefault(x => x.Id == message.UpdatedBankAccount.Bank.Id) ?? throw new Exception("");
            if (bankDTO is null) return;
            TreasuryBankAccountMasterTreeDTO bankAccountToUpdate = bankDTO.BankAccounts.FirstOrDefault(x => x.Id == message.UpdatedBankAccount.Id) ?? throw new Exception("");
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
            _notificationService.ShowSuccess("Cuenta bancaria actualizada correctamente.");
            return;
        }

        public async Task HandleAsync(FranchiseCreateMessage message, CancellationToken cancellationToken)
        {
            IsNewRecord = false;
            TreasuryFranchiseMasterTreeDTO franchiseDTO = Context.AutoMapper.Map<TreasuryFranchiseMasterTreeDTO>(message.CreatedFranchise);
            FranchiseDummyDTO franchiseDummyDTO = DummyItems.FirstOrDefault(x => x is FranchiseDummyDTO) as FranchiseDummyDTO ?? throw new Exception("");
            if (franchiseDummyDTO is null) return;
            if (!franchiseDummyDTO.IsExpanded && franchiseDummyDTO.Franchises[0].IsDummyChild)
            {
                await LoadFranchises(franchiseDummyDTO);
                franchiseDummyDTO.IsExpanded = true;
                TreasuryFranchiseMasterTreeDTO? franchise = franchiseDummyDTO.Franchises.FirstOrDefault(x => x.Id == franchiseDTO.Id);
                if (franchise is null) return;
                SelectedItem = franchise;
                _notificationService.ShowSuccess("Franquicia creada correctamente.");
                return;
            }
            if (!franchiseDummyDTO.IsExpanded)
            {
                franchiseDummyDTO.IsExpanded = true;
                franchiseDummyDTO.Franchises.Add(franchiseDTO);
                SelectedItem = franchiseDTO;
                _notificationService.ShowSuccess("Franquicia creada correctamente.");
                return;
            }
            franchiseDummyDTO.Franchises.Add(franchiseDTO);
            SelectedItem = franchiseDTO;
            _notificationService.ShowSuccess("Franquicia creada correctamente.");
            return;
        }

        public Task HandleAsync(FranchiseDeleteMessage message, CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                FranchiseDummyDTO franchiseDummyDTO = DummyItems.FirstOrDefault(x => x is FranchiseDummyDTO) as FranchiseDummyDTO ?? throw new Exception("");
                if (franchiseDummyDTO is null) return;
                franchiseDummyDTO.Franchises.Remove(franchiseDummyDTO.Franchises.Where(x => x.Id == message.DeletedFranchise.Id).First());
            });
            _notificationService.ShowSuccess("Franquicia eliminada correctamente.");
            return Task.CompletedTask;
        }

        public async Task HandleAsync(FranchiseUpdateMessage message, CancellationToken cancellationToken)
        {
            TreasuryFranchiseMasterTreeDTO franchiseDTO = Context.AutoMapper.Map<TreasuryFranchiseMasterTreeDTO>(message.UpdatedFranchise);
            FranchiseDummyDTO franchiseDummyDTO = DummyItems.FirstOrDefault(x => x is FranchiseDummyDTO) as FranchiseDummyDTO ?? throw new Exception("");
            if (franchiseDummyDTO is null) return;
            TreasuryFranchiseMasterTreeDTO franchiseToUpdate = franchiseDummyDTO.Franchises.FirstOrDefault(x => x.Id == message.UpdatedFranchise.Id) ?? throw new Exception("");
            if (franchiseToUpdate is null) return;
            franchiseToUpdate.Id = franchiseDTO.Id;
            franchiseToUpdate.Name = franchiseDTO.Name;
            franchiseToUpdate.FormulaCommission = franchiseDTO.FormulaCommission;
            franchiseToUpdate.FormulaReteiva = franchiseDTO.FormulaReteiva;
            franchiseToUpdate.FormulaReteica = franchiseDTO.FormulaReteica;
            franchiseToUpdate.FormulaRetefte = franchiseDTO.FormulaRetefte;
            franchiseToUpdate.CommissionMargin = franchiseDTO.CommissionMargin;
            franchiseToUpdate.ReteivaMargin = franchiseDTO.ReteivaMargin;
            franchiseToUpdate.ReteicaMargin = franchiseDTO.ReteicaMargin;
            franchiseToUpdate.RetefteMargin = franchiseDTO.RetefteMargin;
            franchiseToUpdate.IvaMargin = franchiseDTO.IvaMargin;
            franchiseToUpdate.BankAccount = franchiseDTO.BankAccount;
            franchiseToUpdate.AccountingAccountCommission = franchiseDTO.AccountingAccountCommission;
            franchiseToUpdate.FranchiseSettingsByCostCenter = franchiseDTO.FranchiseSettingsByCostCenter;
            await Task.Run(() => FranchiseEditor.SetForEdit(franchiseToUpdate));
            _notificationService.ShowSuccess("Franquicia actualizada correctamente.");
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

    public class CashDrawerComboBoxesDataContext
    {
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts { get; set; } = [];
        public ObservableCollection<CashDrawerGraphQLModel> CashDrawers { get; set; } = [];
        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; set; } = [];
        public ObservableCollection<BankAccountGraphQLModel> BankAccounts { get; set; } = [];
    }
}
