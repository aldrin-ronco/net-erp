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

        public readonly IGenericDataAccess<CompanyLocationGraphQLModel> CompanyLocationService = IoC.Get<IGenericDataAccess<CompanyLocationGraphQLModel>>();

        public readonly IGenericDataAccess<CostCenterGraphQLModel> CostCenterService = IoC.Get<IGenericDataAccess<CostCenterGraphQLModel>>();

        public readonly IGenericDataAccess<CashDrawerGraphQLModel> CashDrawerService = IoC.Get<IGenericDataAccess<CashDrawerGraphQLModel>>();

        public readonly IGenericDataAccess<BankGraphQLModel> BankService = IoC.Get<IGenericDataAccess<BankGraphQLModel>>();

        public readonly IGenericDataAccess<BankAccountGraphQLModel> BankAccountService = IoC.Get<IGenericDataAccess<BankAccountGraphQLModel>>();

        public readonly IGenericDataAccess<FranchiseGraphQLModel> FranchiseService = IoC.Get<IGenericDataAccess<FranchiseGraphQLModel>>();

        Helpers.IDialogService _dialogService = IoC.Get<Helpers.IDialogService>();

        public SearchWithTwoColumnsGridViewModel<AccountingEntityGraphQLModel> SearchWithTwoColumnsGridViewModel { get; set; }

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
                if (!IsNewRecord)
                {
                    IsEditing = false;
                    CanEdit = true;
                    CanUndo = false;
                    SelectedIndex = 0;
                    if (_selectedItem is MajorCashDrawerMasterTreeDTO majorCashDrawerMasterTreeDTO)
                    {
                        SetMajorCashDrawerForEdit(majorCashDrawerMasterTreeDTO);
                        ClearAllErrors();
                        ValidateProperty(nameof(MajorCashDrawerName), MajorCashDrawerName);
                        NotifyOfPropertyChange(nameof(CanSave));
                        return;
                    }
                    if (_selectedItem is MinorCashDrawerMasterTreeDTO minorCashDrawerMasterTreeDTO)
                    {
                        SetMinorCashDrawerForEdit(minorCashDrawerMasterTreeDTO);
                        ClearAllErrors();
                        ValidateProperty(nameof(MinorCashDrawerName), MinorCashDrawerName);
                        NotifyOfPropertyChange(nameof(CanSave));
                        return;
                    }
                    if (_selectedItem is TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawer)
                    {
                        SetAuxiliaryCashDrawerForEdit(auxiliaryCashDrawer);
                        ClearAllErrors();
                        ValidateAuxiliaryCashDrawerProperties();
                        NotifyOfPropertyChange(nameof(CanSave));
                        return;
                    }
                    if(_selectedItem is TreasuryBankMasterTreeDTO bank)
                    {
                        SetBankForEdit(bank);
                        ClearAllErrors();
                        ValidateProperty(nameof(BankAccountingEntityName), BankAccountingEntityName);
                        NotifyOfPropertyChange(nameof(CanSave));
                        return;
                    }
                    if(_selectedItem is TreasuryBankAccountMasterTreeDTO bankAccount)
                    {
                        BankAccountNumber = "";
                        SetBankAccountForEdit(bankAccount);
                        ClearAllErrors();
                        ValidateProperty(nameof(BankAccountNumber), BankAccountNumber);
                        NotifyOfPropertyChange(nameof(CanSave));
                        return;
                    }
                    if(_selectedItem is TreasuryFranchiseMasterTreeDTO franchsie)
                    {
                        SetFranchiseForEdit(franchsie);
                        ClearAllErrors();
                        ValidateProperty(nameof(FranchiseName), FranchiseName);
                        NotifyOfPropertyChange(nameof(CanSave));
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
                        SetMajorCashDrawerForNew();
                        ClearAllErrors();
                        ValidateProperty(nameof(MajorCashDrawerName), MajorCashDrawerName);
                        NotifyOfPropertyChange(nameof(CanSave));
                        return;
                    }
                    if (_selectedItem is MinorCashDrawerMasterTreeDTO)
                    {
                        SetMinorCashDrawerForNew();
                        ClearAllErrors();
                        ValidateProperty(nameof(MinorCashDrawerName), MinorCashDrawerName);
                        NotifyOfPropertyChange(nameof(CanSave));
                        return;
                    }
                    if (_selectedItem is TreasuryAuxiliaryCashDrawerMasterTreeDTO)
                    {
                        SetAuxiliaryCashDrawerForNew();
                        ClearAllErrors();
                        ValidateAuxiliaryCashDrawerProperties();
                        NotifyOfPropertyChange(nameof(CanSave));
                        return;
                    }
                    if (_selectedItem is TreasuryBankMasterTreeDTO)
                    {
                        SetBankForNew();
                        ClearAllErrors();
                        ValidateProperty(nameof(BankAccountingEntityName), BankAccountingEntityName);
                        NotifyOfPropertyChange(nameof(CanSave));
                        return;
                    }
                    if (_selectedItem is TreasuryBankAccountMasterTreeDTO)
                    {
                        SetBankAccountForNew();
                        ClearAllErrors();
                        ValidateProperty(nameof(BankAccountNumber), BankAccountNumber);
                        NotifyOfPropertyChange(nameof(CanSave));
                        return;
                    }
                    if(_selectedItem is TreasuryFranchiseMasterTreeDTO)
                    {
                        SetFranchiseForNew();
                        ClearAllErrors();
                        ValidateProperty(nameof(FranchiseName), FranchiseName);
                        NotifyOfPropertyChange(nameof(CanSave));
                        return;
                    }
                }
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

            if (SelectedItem is MajorCashDrawerMasterTreeDTO) this.SetFocus(nameof(MajorCashDrawerName));
            if (SelectedItem is MinorCashDrawerMasterTreeDTO) this.SetFocus(nameof(MinorCashDrawerName));
            if (SelectedItem is TreasuryAuxiliaryCashDrawerMasterTreeDTO) this.SetFocus(nameof(AuxiliaryCashDrawerName));
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
            if (IsNewRecord)
            {
                SelectedItem = null;
            }
            IsEditing = false;
            CanUndo = false;
            CanEdit = true;
            IsNewRecord = false;
            SelectedIndex = 0;
            if (SelectedItem is MajorCashDrawerMasterTreeDTO majorCashDrawer) SetMajorCashDrawerForEdit(majorCashDrawer);
            if (SelectedItem is MinorCashDrawerMasterTreeDTO minorCashDrawer) SetMinorCashDrawerForEdit(minorCashDrawer);
            if (SelectedItem is TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawer) SetAuxiliaryCashDrawerForEdit(auxiliaryCashDrawer);
            if (SelectedItem is TreasuryBankMasterTreeDTO bank) SetBankForEdit(bank);
            if (SelectedItem is TreasuryBankAccountMasterTreeDTO bankAccount) SetBankAccountForEdit(bankAccount);
            if (SelectedItem is TreasuryFranchiseMasterTreeDTO franchise) SetFranchiseForEdit(franchise);
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
                this.SetFocus(nameof(MajorCashDrawerName));
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
                this.SetFocus(nameof(MinorCashDrawerName));
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
                this.SetFocus(nameof(AuxiliaryCashDrawerName));
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
                this.SetFocus(nameof(BankAccountNumber));
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
                this.SetFocus(nameof(FranchiseName));
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }


        public void SetMajorCashDrawerForEdit(MajorCashDrawerMasterTreeDTO majorCashDrawerMasterTreeDTO)
        {
            MajorCashDrawerId = majorCashDrawerMasterTreeDTO.Id;
            MajorCashDrawerName = majorCashDrawerMasterTreeDTO.Name;
            MajorCashDrawerCostCenterId = majorCashDrawerMasterTreeDTO.CostCenter.Id;
            MajorCashDrawerCostCenterName = majorCashDrawerMasterTreeDTO.CostCenter.Name;
            MajorCashDrawerAutoTransferCashDrawers = new ObservableCollection<CashDrawerGraphQLModel>(CashDrawers.Where(x => x.Id != majorCashDrawerMasterTreeDTO.Id));
            MajorCashDrawerSelectedAccountingAccountCash = CashDrawerAccountingAccounts.FirstOrDefault(x => x.Id == majorCashDrawerMasterTreeDTO.AccountingAccountCash.Id) ?? throw new Exception("");
            MajorCashDrawerSelectedAccountingAccountCheck = CashDrawerAccountingAccounts.FirstOrDefault(x => x.Id == majorCashDrawerMasterTreeDTO.AccountingAccountCheck.Id) ?? throw new Exception("");
            MajorCashDrawerSelectedAccountingAccountCard = CashDrawerAccountingAccounts.FirstOrDefault(x => x.Id == majorCashDrawerMasterTreeDTO.AccountingAccountCard.Id) ?? throw new Exception("");
            MajorCashDrawerCashReviewRequired = majorCashDrawerMasterTreeDTO.CashReviewRequired;
            MajorCashDrawerAutoAdjustBalance = majorCashDrawerMasterTreeDTO.AutoAdjustBalance;
            MajorCashDrawerAutoTransfer = majorCashDrawerMasterTreeDTO.AutoTransfer;
            SelectedCashDrawerAutoTransfer = MajorCashDrawerAutoTransfer ? MajorCashDrawerAutoTransferCashDrawers.FirstOrDefault(x => x.Id == majorCashDrawerMasterTreeDTO.CashDrawerAutoTransfer.Id) ?? throw new Exception("") : MajorCashDrawerAutoTransferCashDrawers.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("");

        }

        public void SetMajorCashDrawerForNew()
        {
            MajorCashDrawerId = 0;
            MajorCashDrawerName = $"CAJA GENERAL EN {MajorCostCenterBeforeNewCashDrawer.Name}";
            MajorCashDrawerCostCenterName = MajorCostCenterBeforeNewCashDrawer.Name;
            MajorCashDrawerAutoTransferCashDrawers = new ObservableCollection<CashDrawerGraphQLModel>(CashDrawers);
            MajorCashDrawerSelectedAccountingAccountCash = new();
            MajorCashDrawerSelectedAccountingAccountCheck = new();
            MajorCashDrawerSelectedAccountingAccountCard = new();
            MajorCashDrawerCashReviewRequired = false;
            MajorCashDrawerAutoAdjustBalance = false;
            MajorCashDrawerAutoTransfer = false;
            SelectedCashDrawerAutoTransfer = CashDrawers.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("");

        }

        public void SetMinorCashDrawerForEdit(MinorCashDrawerMasterTreeDTO minorCashDrawerMasterTreeDTO)
        {
            MinorCashDrawerId = minorCashDrawerMasterTreeDTO.Id;
            MinorCashDrawerName = minorCashDrawerMasterTreeDTO.Name;
            MinorCashDrawerCostCenterId = minorCashDrawerMasterTreeDTO.CostCenter.Id;
            MinorCashDrawerCostCenterName = minorCashDrawerMasterTreeDTO.CostCenter.Name;
            MinorCashDrawerSelectedAccountingAccountCash = CashDrawerAccountingAccounts.FirstOrDefault(x => x.Id == minorCashDrawerMasterTreeDTO.AccountingAccountCash.Id) ?? throw new Exception("");
            MinorCashDrawerCashReviewRequired = minorCashDrawerMasterTreeDTO.CashReviewRequired;
            MinorCashDrawerAutoAdjustBalance = minorCashDrawerMasterTreeDTO.AutoAdjustBalance;

        }

        public void SetMinorCashDrawerForNew()
        {
            MinorCashDrawerId = 0;
            MinorCashDrawerName = $"CAJA MENOR EN {MinorCostCenterBeforeNewCashDrawer.Name}";
            MinorCashDrawerCostCenterName = MinorCostCenterBeforeNewCashDrawer.Name;
            MinorCashDrawerSelectedAccountingAccountCash = new();
            MinorCashDrawerCashReviewRequired = false;
            MinorCashDrawerAutoAdjustBalance = false;
        }

        public void SetAuxiliaryCashDrawerForEdit(TreasuryAuxiliaryCashDrawerMasterTreeDTO minorCashDrawerMasterTreeDTO)
        {
            AuxiliaryCashDrawerId = minorCashDrawerMasterTreeDTO.Id;
            AuxiliaryCashDrawerName = minorCashDrawerMasterTreeDTO.Name;
            AuxiliaryCashDrawerAutoTransferCashDrawers = new ObservableCollection<CashDrawerGraphQLModel>(CashDrawers.Where(x => x.Id != minorCashDrawerMasterTreeDTO.Id));
            AuxiliaryCashDrawerSelectedAccountingAccountCash = CashDrawerAccountingAccounts.FirstOrDefault(x => x.Id == minorCashDrawerMasterTreeDTO.AccountingAccountCash.Id) ?? throw new Exception("");
            AuxiliaryCashDrawerSelectedAccountingAccountCheck = CashDrawerAccountingAccounts.FirstOrDefault(x => x.Id == minorCashDrawerMasterTreeDTO.AccountingAccountCheck.Id) ?? throw new Exception("");
            AuxiliaryCashDrawerSelectedAccountingAccountCard = CashDrawerAccountingAccounts.FirstOrDefault(x => x.Id == minorCashDrawerMasterTreeDTO.AccountingAccountCard.Id) ?? throw new Exception("");
            AuxiliaryCashDrawerCashReviewRequired = minorCashDrawerMasterTreeDTO.CashReviewRequired;
            AuxiliaryCashDrawerAutoAdjustBalance = minorCashDrawerMasterTreeDTO.AutoAdjustBalance;
            AuxiliaryCashDrawerAutoTransfer = minorCashDrawerMasterTreeDTO.AutoTransfer;
            SelectedCashDrawerAutoTransfer = AuxiliaryCashDrawerAutoTransfer ? AuxiliaryCashDrawerAutoTransferCashDrawers.FirstOrDefault(x => x.Id == minorCashDrawerMasterTreeDTO.CashDrawerAutoTransfer.Id) ?? throw new Exception("") : AuxiliaryCashDrawerAutoTransferCashDrawers.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("");
            AuxiliaryCashDrawerComputerName = minorCashDrawerMasterTreeDTO.ComputerName;
        }

        public void SetAuxiliaryCashDrawerForNew()
        {
            AuxiliaryCashDrawerId = 0;
            AuxiliaryCashDrawerName = $"CAJA AUXILIAR";
            AuxiliaryCashDrawerAutoTransferCashDrawers = new ObservableCollection<CashDrawerGraphQLModel>(CashDrawers);
            AuxiliaryCashDrawerSelectedAccountingAccountCash = new();
            AuxiliaryCashDrawerSelectedAccountingAccountCheck = new();
            AuxiliaryCashDrawerSelectedAccountingAccountCard = new();
            AuxiliaryCashDrawerCashReviewRequired = false;
            AuxiliaryCashDrawerAutoAdjustBalance = false;
            AuxiliaryCashDrawerAutoTransfer = false;
            SelectedCashDrawerAutoTransfer = CashDrawers.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("");
            AuxiliaryCashDrawerComputerName = "";
        }

        public void SetBankForEdit(TreasuryBankMasterTreeDTO bank)
        {
            BankId = bank.Id;
            BankAccountingEntityId = bank.AccountingEntity.Id;
            BankAccountingEntityName = bank.AccountingEntity.SearchName;
            BankPaymentMethodPrefix = bank.PaymentMethodPrefix;
        }

        public void SetBankForNew()
        {
            BankId = 0;
            BankAccountingEntityId = 0;
            BankAccountingEntityName = "";
            BankPaymentMethodPrefix = "";
        }

        public void SetBankAccountForEdit(TreasuryBankAccountMasterTreeDTO bankAccount)
        {
            foreach(var costCenter in BankAccountCostCenters)
            {
                costCenter.IsChecked = false;
            }
            BankAccountId = bankAccount.Id;
            BankAccountType = bankAccount.Type;
            BankAccountBankCaptureType = (CaptureTypeEnum)Enum.Parse(typeof(CaptureTypeEnum), bankAccount.Bank.AccountingEntity.CaptureType);
            BankAccountProvider = bankAccount.Provider;
            BankAccountNumber = bankAccount.Number;
            BankAccountIsActive = bankAccount.IsActive;
            BankAccountReference = bankAccount.Reference;
            BankAccountDisplayOrder = bankAccount.DisplayOrder;
            BankAccountBankId = bankAccount.Bank.Id;
            BankAccountBankName = bankAccount.Bank.AccountingEntity.SearchName;
            BankAccountAccountingAccountAutoCreate = false;
            BankAccountAccountingAccountSelectExisting = true;
            BankAccountSelectedAccountingAccount = BankAccountAccountingAccounts.FirstOrDefault(x => x.Id == bankAccount.AccountingAccount.Id) ?? throw new Exception("");
            BankAccountPaymentMethodAbbreviation = bankAccount.PaymentMethod.Abbreviation;
            foreach(var costCenter in BankAccountCostCenters)
            {
                costCenter.IsChecked = bankAccount.AllowedCostCenters.Any(x => x.Id == costCenter.Id);
            }
        }

        public void SetBankAccountForNew()
        {
            BankAccountId = 0;
            BankAccountBankCaptureType = (CaptureTypeEnum)Enum.Parse(typeof(CaptureTypeEnum), BankBeforeNewBankAccount.AccountingEntity.CaptureType);
            BankAccountType = BankAccountBankCaptureInfoAsRS ? "A" : "M";
            BankAccountProvider = BankAccountBankCaptureInfoAsPN ? "N" : "";
            BankAccountNumber = "";
            BankAccountIsActive = true;
            BankAccountReference = "";
            BankAccountDisplayOrder = 0;
            BankAccountBankId = BankBeforeNewBankAccount.Id;
            BankAccountBankName = BankBeforeNewBankAccount.AccountingEntity.SearchName;
            BankAccountAccountingAccountAutoCreate = true;
            BankAccountAccountingAccountSelectExisting = false;
            BankAccountSelectedAccountingAccount = BankAccountAccountingAccounts.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("");
        }

        public void SetFranchiseForEdit(TreasuryFranchiseMasterTreeDTO franchise)
        {
            FranchiseId = franchise.Id;
            FranchiseName = franchise.Name;
            FranchiseType = franchise.Type;
            FranchiseCommissionMargin = franchise.CommissionMargin;
            FranchiseReteivaMargin = franchise.ReteivaMargin;
            FranchiseReteicaMargin = franchise.ReteicaMargin;
            FranchiseRetefteMargin = franchise.RetefteMargin;
            FranchiseFormulaCommission = franchise.FormulaCommission;
            FranchiseFormulaReteica = franchise.FormulaReteica;
            FranchiseFormulaReteiva = franchise.FormulaReteiva;
            FranchiseFormulaRetefte = franchise.FormulaRetefte;
            FranchiseIvaMargin = franchise.IvaMargin;
            FranchiseSelectedAccountingAccountCommission = FranchiseAccountingAccountsCommission.FirstOrDefault(x => x.Id == franchise.AccountingAccountCommission.Id) ?? throw new Exception("");
            FranchiseSelectedBankAccount = FranchiseBankAccounts.FirstOrDefault(x => x.Id == franchise.BankAccount.Id) ?? throw new Exception("");
            FranchiseSettingsByCostCenter = new List<FranchiseByCostCenterGraphQLModel>(franchise.FranchiseSettingsByCostCenter);
            FranchiseSelectedCostCenter = FranchiseCostCenters.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("");
            FranchiseCardValue = 0;
            FranchiseSimulatedCommission = 0;
            FranchiseSimulatedReteiva = 0;
            FranchiseSimulatedReteica = 0;
            FranchiseSimulatedRetefte = 0;
            FranchiseSimulatedIvaValue = 0;
        }

        public void SetFranchiseForNew()
        {
            FranchiseId = 0;
            FranchiseName = "";
            FranchiseType = "TC";
            FranchiseCommissionMargin = 0;
            FranchiseReteivaMargin = 0;
            FranchiseReteicaMargin = 0;
            FranchiseRetefteMargin = 0;
            FranchiseFormulaCommission = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_COMISION]/100)";
            FranchiseFormulaReteica = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_ICA]/1000)";
            FranchiseFormulaReteiva = "[VALOR_IVA]*([MARGEN_RETE_IVA]/100)";
            FranchiseFormulaRetefte = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_FUENTE]/100)";
            FranchiseIvaMargin = 0;
            FranchiseSelectedAccountingAccountCommission = FranchiseAccountingAccountsCommission.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("");
            FranchiseSelectedBankAccount = FranchiseBankAccounts.FirstOrDefault(x => x.Id == 0) ?? throw new Exception(""); ;
            FranchiseSettingsByCostCenter = [];
            FranchiseSelectedCostCenter = FranchiseCostCenters.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("");
            FranchiseCardValue = 0;
            FranchiseSimulatedCommission = 0;
            FranchiseSimulatedReteiva = 0;
            FranchiseSimulatedReteica = 0;
            FranchiseSimulatedRetefte = 0;
            FranchiseSimulatedIvaValue = 0;
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
            AuxiliaryCashDrawerComputerName = SessionInfo.GetComputerName();
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
            try
            {
                IsBusy = true;
                Refresh();
                if (SelectedItem is MajorCashDrawerMasterTreeDTO majorCashDrawerMasterTreeDTO)
                {
                    CashDrawerGraphQLModel result = await ExecuteSaveMajorCashDrawer();
                    await LoadComboBoxesAsync();
                    if (IsNewRecord)
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryCashDrawerCreateMessage() { CreatedCashDrawer = result });
                    }
                    else
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryCashDrawerUpdateMessage() { UpdatedCashDrawer = result });

                    }
                }
                if (SelectedItem is MinorCashDrawerMasterTreeDTO minorCashDrawerMasterTreeDTO)
                {
                    CashDrawerGraphQLModel result = await ExecuteSaveMinorCashDrawer();
                    await LoadComboBoxesAsync();
                    if (IsNewRecord)
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryCashDrawerCreateMessage() { CreatedCashDrawer = result });
                    }
                    else
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryCashDrawerUpdateMessage() { UpdatedCashDrawer = result });

                    }
                }
                if (SelectedItem is TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawerMasterTreeDTO)
                {
                    CashDrawerGraphQLModel result = await ExecuteSaveAuxiliaryCashDrawer();
                    await LoadComboBoxesAsync();
                    if (IsNewRecord)
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryCashDrawerCreateMessage() { CreatedCashDrawer = result });
                    }
                    else
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryCashDrawerUpdateMessage() { UpdatedCashDrawer = result });

                    }
                }
                if (SelectedItem is TreasuryBankMasterTreeDTO bank)
                {
                    BankGraphQLModel result = await ExecuteSaveBank();
                    if (IsNewRecord)
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new BankCreateMessage() { CreatedBank = result });
                    }
                    else
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new BankUpdateMessage() { UpdatedBank = result });
                    }
                }
                if (SelectedItem is TreasuryBankAccountMasterTreeDTO bankAccount)
                {
                    BankAccountGraphQLModel result = await ExecuteSaveBankAccount();
                    await LoadComboBoxesAsync();
                    if (IsNewRecord)
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new BankAccountCreateMessage() { CreatedBankAccount = result });
                    }
                    else
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new BankAccountUpdateMessage() { UpdatedBankAccount = result });
                    }
                }
                if (SelectedItem is TreasuryFranchiseMasterTreeDTO franchise)
                {
                    FranchiseGraphQLModel result = await ExecuteSaveFranchise();
                    if (IsNewRecord)
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new FranchiseCreateMessage() { CreatedFranchise = result });
                    }
                    else
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new FranchiseUpdateMessage() { UpdatedFranchise = result });
                    }
                }
                IsEditing = false;
                CanUndo = false;
                CanEdit = true;
                SelectedIndex = 0;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                if (graphQLError != null)
                {
                    App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.Save \r\n{graphQLError.Errors[0].Message} \r\n {graphQLError.Errors[0].Extensions.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.Save \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool CanSave
        {
            get
            {
                if (_selectedItem is MajorCashDrawerMasterTreeDTO)
                {
                    if (IsEditing == true && _errors.Count <= 0)
                    {
                        if (MajorCashDrawerAutoTransfer == true && (SelectedCashDrawerAutoTransfer is null || SelectedCashDrawerAutoTransfer.Id == 0)) return false;
                        return true;
                    }
                    return false;
                }
                if (_selectedItem is MinorCashDrawerMasterTreeDTO)
                {
                    if (IsEditing == true && _errors.Count <= 0) return true;
                    return false;
                }
                if (_selectedItem is TreasuryAuxiliaryCashDrawerMasterTreeDTO)
                {
                    if (IsEditing == true && _errors.Count <= 0)
                    {
                        if (AuxiliaryCashDrawerAutoTransfer == true && (SelectedCashDrawerAutoTransfer is null || SelectedCashDrawerAutoTransfer.Id == 0)) return false;
                        return true;
                    }
                    return false;
                }
                if(_selectedItem is TreasuryBankMasterTreeDTO)
                {
                    if (IsEditing == true && _errors.Count <= 0) return true;
                    return false;
                }
                if(_selectedItem is TreasuryBankAccountMasterTreeDTO)
                {
                    if(IsEditing == true && _errors.Count <= 0)
                    {
                        if (BankAccountAccountingAccountSelectExisting == true && (BankAccountSelectedAccountingAccount is null || BankAccountSelectedAccountingAccount.Id == 0)) return false;
                        return true;
                    }
                    return false;
                }
                if(_selectedItem is TreasuryFranchiseMasterTreeDTO)
                {
                    if (IsEditing == true && _errors.Count <= 0)
                    {
                        if ((FranchiseSelectedAccountingAccountCommission is null || FranchiseSelectedAccountingAccountCommission.Id == 0) || (FranchiseSelectedBankAccount is null || FranchiseSelectedBankAccount.Id == 0)) return false;
                        return true;
                    }
                    return false;
                }
                return false;
            }
        }

        public async Task<CashDrawerGraphQLModel> ExecuteSaveMajorCashDrawer()
        {
            try
            {
                string query;
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                if (!IsNewRecord) variables.Id = MajorCashDrawerId;
                variables.Data.Name = MajorCashDrawerName.Trim().RemoveExtraSpaces();
                variables.Data.CashReviewRequired = MajorCashDrawerCashReviewRequired;
                variables.Data.AutoAdjustBalance = MajorCashDrawerAutoAdjustBalance;
                variables.Data.AutoTransfer = MajorCashDrawerAutoTransfer;
                if (IsNewRecord) variables.Data.IsPettyCash = false;
                variables.Data.CashDrawerIdAutoTransfer = MajorCashDrawerAutoTransfer ? SelectedCashDrawerAutoTransfer.Id : 0;
                variables.Data.CostCenterId = IsNewRecord ? MajorCostCenterBeforeNewCashDrawer.Id : MajorCashDrawerCostCenterId;
                if (!IsNewRecord) variables.Data.AccountingAccountIdCash = MajorCashDrawerSelectedAccountingAccountCash.Id;
                if (!IsNewRecord) variables.Data.AccountingAccountIdCheck = MajorCashDrawerSelectedAccountingAccountCheck.Id;
                if (!IsNewRecord) variables.Data.AccountingAccountIdCard = MajorCashDrawerSelectedAccountingAccountCard.Id;
                if (IsNewRecord) variables.Data.ParentId = 0;
                variables.Data.ComputerName = "";
                if (IsNewRecord)
                {
                    query = @"
                        mutation($data: CreateCashDrawerInput!){
                            CreateResponse: createCashDrawer(data: $data){
                                id
                                name
                                cashReviewRequired
                                autoAdjustBalance
                                autoTransfer
                                isPettyCash
                                cashDrawerAutoTransfer{
                                    id
                                    name
                                }
                                costCenter{
                                    id
                                    name
                                    location{
                                        id
                                    }
                                }
                                accountingAccountCash{
                                    id
                                    name
                                }
                                accountingAccountCheck{
                                    id
                                    name
                                }
                                accountingAccountCard{
                                    id
                                    name
                                }
                            }
                        }";
                }
                else
                {
                    query = @"
                        mutation($id: Int!, $data: UpdateCashDrawerInput!){
                            UpdateResponse: updateCashDrawer(id: $id, data: $data){
                                id
                                name
                                cashReviewRequired
                                autoAdjustBalance
                                autoTransfer
                                isPettyCash
                                cashDrawerAutoTransfer{
                                    id
                                    name
                                }
                                costCenter{
                                    id
                                    name
                                    location{
                                        id
                                    }
                                }
                                accountingAccountCash{
                                    id
                                    name
                                }
                                accountingAccountCheck{
                                    id
                                    name
                                }
                                accountingAccountCard{
                                    id
                                    name
                                }
                            }
                        }";
                }
                var result = IsNewRecord ? await CashDrawerService.Create(query, variables) : await CashDrawerService.Update(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<CashDrawerGraphQLModel> ExecuteSaveMinorCashDrawer()
        {
            try
            {
                string query;
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                if (!IsNewRecord) variables.Id = MinorCashDrawerId;
                variables.Data.Name = MinorCashDrawerName.Trim().RemoveExtraSpaces();
                variables.Data.CashReviewRequired = MinorCashDrawerCashReviewRequired;
                variables.Data.AutoAdjustBalance = MinorCashDrawerAutoAdjustBalance;
                if (IsNewRecord) variables.Data.IsPettyCash = true;
                variables.Data.AutoTransfer = false;
                variables.Data.CashDrawerIdAutoTransfer = 0;
                variables.Data.CostCenterId = IsNewRecord ? MinorCostCenterBeforeNewCashDrawer.Id : MinorCashDrawerCostCenterId;
                if (!IsNewRecord) variables.Data.AccountingAccountIdCash = MinorCashDrawerSelectedAccountingAccountCash.Id;
                if (IsNewRecord) variables.Data.ParentId = 0;
                variables.Data.ComputerName = "";
                if (IsNewRecord)
                {
                    query = @"
                        mutation($data: CreateCashDrawerInput!){
                            CreateResponse: createCashDrawer(data: $data){
                                id
                                name
                                cashReviewRequired
                                autoAdjustBalance
                                isPettyCash
                                costCenter{
                                    id
                                    name
                                    location{
                                        id
                                    }
                                }
                                accountingAccountCash{
                                    id
                                    name
                                }
                            }
                        }";
                }
                else
                {
                    query = @"
                        mutation($id: Int!, $data: UpdateCashDrawerInput!){
                            UpdateResponse: updateCashDrawer(id: $id, data: $data){
                                id
                                name
                                cashReviewRequired
                                autoAdjustBalance
                                isPettyCash
                                costCenter{
                                    id
                                    name
                                    location{
                                        id
                                    }
                                }
                                accountingAccountCash{
                                    id
                                    name
                                }
                            }
                        }";
                }
                var result = IsNewRecord ? await CashDrawerService.Create(query, variables) : await CashDrawerService.Update(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<CashDrawerGraphQLModel> ExecuteSaveAuxiliaryCashDrawer()
        {
            try
            {
                string query;
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                if (!IsNewRecord) variables.Id = AuxiliaryCashDrawerId;
                variables.Data.Name = AuxiliaryCashDrawerName.Trim().RemoveExtraSpaces();
                variables.Data.CashReviewRequired = AuxiliaryCashDrawerCashReviewRequired;
                variables.Data.AutoAdjustBalance = AuxiliaryCashDrawerAutoAdjustBalance;
                variables.Data.AutoTransfer = AuxiliaryCashDrawerAutoTransfer;
                if (IsNewRecord) variables.Data.IsPettyCash = false;
                variables.Data.CashDrawerIdAutoTransfer = AuxiliaryCashDrawerAutoTransfer ? SelectedCashDrawerAutoTransfer.Id : 0;
                variables.Data.CostCenterId = 0;
                if (!IsNewRecord) variables.Data.AccountingAccountIdCash = AuxiliaryCashDrawerSelectedAccountingAccountCash.Id;
                if (!IsNewRecord) variables.Data.AccountingAccountIdCheck = AuxiliaryCashDrawerSelectedAccountingAccountCheck.Id;
                if (!IsNewRecord) variables.Data.AccountingAccountIdCard = AuxiliaryCashDrawerSelectedAccountingAccountCard.Id;
                if (IsNewRecord) variables.Data.ParentId = MajorCashDrawerIdBeforeNewAuxiliaryCashDrawer;
                variables.Data.ComputerName = AuxiliaryCashDrawerComputerName.Trim().RemoveExtraSpaces();
                if (IsNewRecord)
                {
                    query = @"
                        mutation ($data: CreateCashDrawerInput!) {
                          CreateResponse: createCashDrawer(data: $data) {
                            id
                            name
                            cashReviewRequired
                            autoAdjustBalance
                            autoTransfer
                            isPettyCash
                            computerName
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
                            computerName
                            parent {
                              id
                              costCenter {
                                id
                                location {
                                  id
                                }
                              }
                            }
                          }
                        }
                        ";
                }
                else
                {
                    query = @"
                        mutation($id: Int!, $data: UpdateCashDrawerInput!){
                            UpdateResponse: updateCashDrawer(id: $id, data: $data){
                            id
                            name
                            cashReviewRequired
                            autoAdjustBalance
                            autoTransfer
                            isPettyCash
                            computerName
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
                            computerName
                            parent {
                              id
                              costCenter {
                                id
                                location {
                                  id
                                }
                              }
                            }
                          }
                        }";
                }
                var result = IsNewRecord ? await CashDrawerService.Create(query, variables) : await CashDrawerService.Update(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<BankGraphQLModel> ExecuteSaveBank()
        {
            try
            {
                string query;
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                if (!IsNewRecord) variables.Id = BankId;
                variables.Data.AccountingEntityId = BankAccountingEntityId;
                variables.Data.PaymentMethodPrefix = "Z";
                if (IsNewRecord)
                {
                    query = @"
                        mutation($data: CreateBankInput!){
                            CreateResponse: createBank(data: $data){
                                id
                                accountingEntity{
                                    id
                                    searchName
                                }
                                paymentMethodPrefix
                            }
                        }";
                }
                else
                {
                    query = @"
                        mutation($id: Int!, $data: UpdateBankInput!){
                            UpdateResponse: updateBank(id: $id, data: $data){
                                id
                                accountingEntity{
                                    id
                                    searchName
                                }
                                paymentMethodPrefix
                            }
                        }";
                }
                var result = IsNewRecord ? await BankService.Create(query, variables) : await BankService.Update(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<BankAccountGraphQLModel> ExecuteSaveBankAccount()
        {
            try
            {
                string query;
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                if (!IsNewRecord) variables.Id = BankAccountId;
                variables.Data.Type = BankAccountType;
                variables.Data.Number = BankAccountNumber;
                variables.Data.IsActive = BankAccountIsActive;
                variables.Data.Description = BankAccountDescription;
                variables.Data.Reference = BankAccountReference;
                variables.Data.DisplayOrder = BankAccountDisplayOrder;
                variables.Data.AccountingAccountId = BankAccountAccountingAccountSelectExisting ? BankAccountSelectedAccountingAccount.Id : 0;
                variables.Data.Provider = BankAccountBankCaptureInfoAsPN ? BankAccountProvider : "";
                variables.Data.BankId = BankAccountBankId;
                variables.Data.PaymentMethodName = BankAccountPaymentMethodName;
                variables.Data.AllowedCostCenters = BankAccountCostCenters.Where(x => x.IsChecked).Select(x => x.Id).ToList();
                if (IsNewRecord)
                {
                    query = @"
                    mutation($data: CreateBankAccountInput!){
                        CreateResponse: createBankAccount(data: $data){
                            id
                            type
                            number
                            isActive
                            description
                            reference
                            displayOrder
                            provider
                            allowedCostCenters{
                                id
                                name
                                bankAccountId
                            }
                            paymentMethod{
                                id
                                abbreviation
                                name
                            }
                            accountingAccount{
                                id
                                code
                                name
                            }
                            bank{
                                id
                                accountingEntity{
                                    id
                                    searchName
                                    captureType
                                }
                            }
                        }
                    }";
                }
                else
                {
                    query = @"
                        mutation($id: Int!, $data: UpdateBankAccountInput!){
                            UpdateResponse: updateBankAccount(data: $data, id: $id){
                                id
                                type
                                number
                                isActive
                                description
                                reference
                                displayOrder
                                provider
                                allowedCostCenters{
                                    id
                                    name
                                    bankAccountId
                                }
                                paymentMethod{
                                    id
                                    abbreviation
                                    name
                                }
                                accountingAccount{
                                    id
                                    code
                                    name
                                }
                                bank{
                                    id
                                    accountingEntity{
                                        id
                                        searchName
                                        captureType
                                    }
                                }
                            }
                        }";
                }
                var result = IsNewRecord ? await BankAccountService.Create(query, variables) : await BankAccountService.Update(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<FranchiseGraphQLModel> ExecuteSaveFranchise()
        {
            try
            {
                string query;
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                if (!IsNewRecord) variables.Id = FranchiseId;
                variables.Data.Name = FranchiseName.Trim().RemoveExtraSpaces();
                variables.Data.Type = FranchiseType;
                variables.Data.CommissionMargin = FranchiseCommissionMargin;
                variables.Data.ReteivaMargin = FranchiseReteivaMargin;
                variables.Data.ReteicaMargin = FranchiseReteicaMargin;
                variables.Data.RetefteMargin = FranchiseRetefteMargin;
                variables.Data.FormulaCommission = FranchiseFormulaCommission;
                variables.Data.FormulaReteica = FranchiseFormulaReteica;
                variables.Data.FormulaReteiva = FranchiseFormulaReteiva;
                variables.Data.FormulaRetefte = FranchiseFormulaRetefte;
                variables.Data.IvaMargin = FranchiseIvaMargin;
                variables.Data.AccountingAccountIdCommission = FranchiseSelectedAccountingAccountCommission.Id;
                variables.Data.BankAccountId = FranchiseSelectedBankAccount.Id;
                variables.Data.CompanyId = 1; //TODO: Cambiar por el valor correcto
                variables.Data.CostCenterId = FranchiseSelectedCostCenter.Id;
                if (IsNewRecord)
                {
                    query = @"
                    mutation ($data: CreateFranchiseInput!) {
                      CreateResponse: createFranchise(data: $data) {
                        id
                        name
                        type
                        commissionMargin
                        reteivaMargin
                        reteicaMargin
                        retefteMargin
                        ivaMargin
                        accountingAccountCommission {
                          id
                          code
                          name
                        }
                        bankAccount {
                          id
                          description
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
                          accountingAccountIdCommmission
                          formulaCommission
                          formulaReteiva
                          formulaReteica
                          formulaRetefte
                          franchiseId
                        }
                      }
                    }";
                }
                else
                {
                    query = @"
                        mutation($id: Int!, $data: UpdateFranchiseInput!){
                          UpdateResponse: updateFranchise(id: $id, data: $data){
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
                            franchiseSettingsByCostCenter{
                              id
                              costCenterId
                              commissionMargin
                              reteivaMargin
                              reteicaMargin
                              retefteMargin
                              ivaMargin
                              bankAccountId
                              accountingAccountIdCommmission
                              formulaCommission
                              formulaReteiva
                              formulaReteica
                              formulaRetefte
                              franchiseId
                            }
                          }
                        }";
                }
                var result = IsNewRecord ? await FranchiseService.Create(query, variables) : await FranchiseService.Update(query, variables);
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

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

                var validation = await this.CashDrawerService.CanDelete(query, variables);

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
                CashDrawerGraphQLModel deletedCashDrawer = await CashDrawerService.Delete(query, variables);
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

                var validation = await this.CashDrawerService.CanDelete(query, variables);

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
                CashDrawerGraphQLModel deletedCashDrawer = await CashDrawerService.Delete(query, variables);
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

                var validation = await this.CashDrawerService.CanDelete(query, variables);

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
                CashDrawerGraphQLModel deletedCashDrawer = await CashDrawerService.Delete(query, variables);
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

                var validation = await this.BankService.CanDelete(query, variables);

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
                BankGraphQLModel deletedBank = await BankService.Delete(query, variables);
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

                var validation = await this.BankService.CanDelete(query, variables);

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
                BankAccountGraphQLModel deletedBankAccount = await BankAccountService.Delete(query, variables);
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

                var validation = await this.FranchiseService.CanDelete(query, variables);

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
                FranchiseGraphQLModel deletedFranchise = await FranchiseService.Delete(query, variables);
                this.SelectedItem = null;
                return deletedFranchise;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CanDeleteFranchise => true;

        #region "MajorCashDrawer"

        #region "Properties"

        public int MajorCashDrawerId { get; set; }

        private string _majorCashDrawerName;

        public string MajorCashDrawerName
        {
            get { return _majorCashDrawerName; }
            set
            {
                if (_majorCashDrawerName != value)
                {
                    _majorCashDrawerName = value;
                    NotifyOfPropertyChange(nameof(MajorCashDrawerName));
                    ValidateProperty(nameof(MajorCashDrawerName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _majorCashDrawerCostCenterId;

        public int MajorCashDrawerCostCenterId
        {
            get { return _majorCashDrawerCostCenterId; }
            set
            {
                if (_majorCashDrawerCostCenterId != value)
                {
                    _majorCashDrawerCostCenterId = value;
                    NotifyOfPropertyChange(nameof(MajorCashDrawerCostCenterId));
                }
            }
        }

        private string _majorCashDrawerCostCenterName;

        public string MajorCashDrawerCostCenterName
        {
            get { return _majorCashDrawerCostCenterName; }
            set
            {
                if (_majorCashDrawerCostCenterName != value)
                {
                    _majorCashDrawerCostCenterName = value;
                    NotifyOfPropertyChange(nameof(MajorCashDrawerCostCenterName));
                }
            }
        }

        private AccountingAccountGraphQLModel _majorCashDrawerSelectedAccountingAccountCash;

        public AccountingAccountGraphQLModel MajorCashDrawerSelectedAccountingAccountCash
        {
            get { return _majorCashDrawerSelectedAccountingAccountCash; }
            set
            {
                if (_majorCashDrawerSelectedAccountingAccountCash != value)
                {
                    _majorCashDrawerSelectedAccountingAccountCash = value;
                    NotifyOfPropertyChange(nameof(MajorCashDrawerSelectedAccountingAccountCash));
                }
            }
        }

        private AccountingAccountGraphQLModel _majorCashDrawerSelectedAccountingAccountCheck;

        public AccountingAccountGraphQLModel MajorCashDrawerSelectedAccountingAccountCheck
        {
            get { return _majorCashDrawerSelectedAccountingAccountCheck; }
            set
            {
                if (_majorCashDrawerSelectedAccountingAccountCheck != value)
                {
                    _majorCashDrawerSelectedAccountingAccountCheck = value;
                    NotifyOfPropertyChange(nameof(MajorCashDrawerSelectedAccountingAccountCheck));
                }
            }
        }

        private AccountingAccountGraphQLModel _majorCashDrawerSelectedAccountingAccountCard;

        public AccountingAccountGraphQLModel MajorCashDrawerSelectedAccountingAccountCard
        {
            get { return _majorCashDrawerSelectedAccountingAccountCard; }
            set
            {
                if (_majorCashDrawerSelectedAccountingAccountCard != value)
                {
                    _majorCashDrawerSelectedAccountingAccountCard = value;
                    NotifyOfPropertyChange(nameof(MajorCashDrawerSelectedAccountingAccountCard));
                }
            }
        }

        private bool _majorCashDrawerCashReviewRequired;

        public bool MajorCashDrawerCashReviewRequired
        {
            get { return _majorCashDrawerCashReviewRequired; }
            set
            {
                if (_majorCashDrawerCashReviewRequired != value)
                {
                    _majorCashDrawerCashReviewRequired = value;
                    NotifyOfPropertyChange(nameof(MajorCashDrawerCashReviewRequired));
                }
            }
        }

        private bool _majorCashDrawerAutoAdjustBalance;

        public bool MajorCashDrawerAutoAdjustBalance
        {
            get { return _majorCashDrawerAutoAdjustBalance; }
            set
            {
                if (_majorCashDrawerAutoAdjustBalance != value)
                {
                    _majorCashDrawerAutoAdjustBalance = value;
                    NotifyOfPropertyChange(nameof(MajorCashDrawerAutoAdjustBalance));
                }
            }
        }

        private bool _majorCashDrawerAutoTransfer;

        public bool MajorCashDrawerAutoTransfer
        {
            get { return _majorCashDrawerAutoTransfer; }
            set
            {
                if (_majorCashDrawerAutoTransfer != value)
                {
                    _majorCashDrawerAutoTransfer = value;
                    NotifyOfPropertyChange(nameof(MajorCashDrawerAutoTransfer));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

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

        private CashDrawerGraphQLModel _selectedCashDrawerAutoTransfer;

        public CashDrawerGraphQLModel SelectedCashDrawerAutoTransfer
        {
            get { return _selectedCashDrawerAutoTransfer; }
            set
            {
                if (_selectedCashDrawerAutoTransfer != value)
                {
                    _selectedCashDrawerAutoTransfer = value;
                    NotifyOfPropertyChange(nameof(SelectedCashDrawerAutoTransfer));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public TreasuryMajorCashDrawerCostCenterMasterTreeDTO MajorCostCenterBeforeNewCashDrawer { get; set; } = new();

        #endregion

        #endregion

        #region "MinorCashDrawer"

        #region "Properties"

        public int MinorCashDrawerId { get; set; }

        private string _minorCashDrawerCostCenterName;

        public string MinorCashDrawerCostCenterName
        {
            get { return _minorCashDrawerCostCenterName; }
            set
            {
                if (_minorCashDrawerCostCenterName != value)
                {
                    _minorCashDrawerCostCenterName = value;
                    NotifyOfPropertyChange(nameof(MinorCashDrawerCostCenterName));
                }
            }
        }

        private string _minorCashDrawerName;

        public string MinorCashDrawerName
        {
            get { return _minorCashDrawerName; }
            set
            {
                if (_minorCashDrawerName != value)
                {
                    _minorCashDrawerName = value;
                    NotifyOfPropertyChange(nameof(MinorCashDrawerName));
                    ValidateProperty(nameof(MinorCashDrawerName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _minorCashDrawerCostCenterId;

        public int MinorCashDrawerCostCenterId
        {
            get { return _minorCashDrawerCostCenterId; }
            set
            {
                if (_minorCashDrawerCostCenterId != value)
                {
                    _minorCashDrawerCostCenterId = value;
                    NotifyOfPropertyChange(nameof(MinorCashDrawerCostCenterId));
                }
            }
        }

        private AccountingAccountGraphQLModel _minorCashDrawerSelectedAccountingAccountCash;

        public AccountingAccountGraphQLModel MinorCashDrawerSelectedAccountingAccountCash
        {
            get { return _minorCashDrawerSelectedAccountingAccountCash; }
            set
            {
                if (_minorCashDrawerSelectedAccountingAccountCash != value)
                {
                    _minorCashDrawerSelectedAccountingAccountCash = value;
                    NotifyOfPropertyChange(nameof(MinorCashDrawerSelectedAccountingAccountCash));
                }
            }
        }

        public TreasuryMinorCashDrawerCostCenterMasterTreeDTO MinorCostCenterBeforeNewCashDrawer { get; set; } = new();

        private bool _minorCashDrawerCashReviewRequired;

        public bool MinorCashDrawerCashReviewRequired
        {
            get { return _minorCashDrawerCashReviewRequired; }
            set
            {
                if (_minorCashDrawerCashReviewRequired != value)
                {
                    _minorCashDrawerCashReviewRequired = value;
                    NotifyOfPropertyChange(nameof(MinorCashDrawerCashReviewRequired));
                }
            }
        }

        private bool _minorCashDrawerAutoAdjustBalance;

        public bool MinorCashDrawerAutoAdjustBalance
        {
            get { return _minorCashDrawerAutoAdjustBalance; }
            set
            {
                if (_minorCashDrawerAutoAdjustBalance != value)
                {
                    _minorCashDrawerAutoAdjustBalance = value;
                    NotifyOfPropertyChange(nameof(MinorCashDrawerAutoAdjustBalance));
                }
            }
        }

        #endregion

        #endregion

        #region "AuxiliaryCashDrawer"

        #region "Properties"

        public int AuxiliaryCashDrawerId { get; set; }

        public int MajorCashDrawerIdBeforeNewAuxiliaryCashDrawer { get; set; }

        private string _auxiliaryCashDrawerName;

        public string AuxiliaryCashDrawerName
        {
            get { return _auxiliaryCashDrawerName; }
            set
            {
                if (_auxiliaryCashDrawerName != value)
                {
                    _auxiliaryCashDrawerName = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryCashDrawerName));
                    ValidateProperty(nameof(AuxiliaryCashDrawerName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _auxiliaryCashDrawerCashReviewRequired;

        public bool AuxiliaryCashDrawerCashReviewRequired
        {
            get { return _auxiliaryCashDrawerCashReviewRequired; }
            set
            {
                if (_auxiliaryCashDrawerCashReviewRequired != value)
                {
                    _auxiliaryCashDrawerCashReviewRequired = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryCashDrawerCashReviewRequired));
                }
            }
        }

        private bool _auxiliaryCashDrawerAutoAdjustBalance;

        public bool AuxiliaryCashDrawerAutoAdjustBalance
        {
            get { return _auxiliaryCashDrawerAutoAdjustBalance; }
            set
            {
                if (_auxiliaryCashDrawerAutoAdjustBalance != value)
                {
                    _auxiliaryCashDrawerAutoAdjustBalance = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryCashDrawerAutoAdjustBalance));
                }
            }
        }

        private bool _auxiliaryCashDrawerAutoTransfer;

        public bool AuxiliaryCashDrawerAutoTransfer
        {
            get { return _auxiliaryCashDrawerAutoTransfer; }
            set
            {
                if (_auxiliaryCashDrawerAutoTransfer != value)
                {
                    _auxiliaryCashDrawerAutoTransfer = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryCashDrawerAutoTransfer));
                    NotifyOfPropertyChange(nameof(CanSave));
                    if (value is false) SelectedCashDrawerAutoTransfer = CashDrawers.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("");
                }
            }
        }

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

        private CashDrawerGraphQLModel _selectedAuxiliaryCashDrawerAutoTransfer;

        public CashDrawerGraphQLModel SelectedAuxiliaryCashDrawerAutoTransfer
        {
            get { return _selectedAuxiliaryCashDrawerAutoTransfer; }
            set
            {
                if (_selectedAuxiliaryCashDrawerAutoTransfer != value)
                {
                    _selectedAuxiliaryCashDrawerAutoTransfer = value;
                    NotifyOfPropertyChange(nameof(SelectedAuxiliaryCashDrawerAutoTransfer));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private AccountingAccountGraphQLModel _auxiliaryCashDrawerSelectedAccountingAccountCash;

        public AccountingAccountGraphQLModel AuxiliaryCashDrawerSelectedAccountingAccountCash
        {
            get { return _auxiliaryCashDrawerSelectedAccountingAccountCash; }
            set
            {
                if (_auxiliaryCashDrawerSelectedAccountingAccountCash != value)
                {
                    _auxiliaryCashDrawerSelectedAccountingAccountCash = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryCashDrawerSelectedAccountingAccountCash));
                }
            }
        }

        private AccountingAccountGraphQLModel _auxiliaryCashDrawerSelectedAccountingAccountCheck;

        public AccountingAccountGraphQLModel AuxiliaryCashDrawerSelectedAccountingAccountCheck
        {
            get { return _auxiliaryCashDrawerSelectedAccountingAccountCheck; }
            set
            {
                if (_auxiliaryCashDrawerSelectedAccountingAccountCheck != value)
                {
                    _auxiliaryCashDrawerSelectedAccountingAccountCheck = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryCashDrawerSelectedAccountingAccountCheck));
                }
            }
        }

        private AccountingAccountGraphQLModel _auxiliaryCashDrawerSelectedAccountingAccountCard;

        public AccountingAccountGraphQLModel AuxiliaryCashDrawerSelectedAccountingAccountCard
        {
            get { return _auxiliaryCashDrawerSelectedAccountingAccountCard; }
            set
            {
                if (_auxiliaryCashDrawerSelectedAccountingAccountCard != value)
                {
                    _auxiliaryCashDrawerSelectedAccountingAccountCard = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryCashDrawerSelectedAccountingAccountCard));
                }
            }
        }

        private string _auxiliaryCashDrawerComputerName;

        public string AuxiliaryCashDrawerComputerName
        {
            get { return _auxiliaryCashDrawerComputerName; }
            set
            {
                if (_auxiliaryCashDrawerComputerName != value)
                {
                    _auxiliaryCashDrawerComputerName = value;
                    NotifyOfPropertyChange(nameof(AuxiliaryCashDrawerComputerName));
                    ValidateProperty(nameof(AuxiliaryCashDrawerComputerName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }



        #endregion

        #endregion

        #region "Bank"

        #region "Properties"

        public int BankId { get; set; }

        private int _bankAccountingEntityId;

        public int BankAccountingEntityId
        {
            get { return _bankAccountingEntityId; }
            set
            {
                if (_bankAccountingEntityId != value)
                {
                    _bankAccountingEntityId = value;
                    NotifyOfPropertyChange(nameof(BankAccountingEntityId));
                }
            }
        }

        private string _bankAccountingEntityName;

        public string BankAccountingEntityName
        {
            get { return _bankAccountingEntityName; }
            set
            {
                if (_bankAccountingEntityName != value)
                {
                    _bankAccountingEntityName = value;
                    NotifyOfPropertyChange(nameof(BankAccountingEntityName));
                    ValidateProperty(nameof(BankAccountingEntityName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _bankPaymentMethodPrefix;

        public string BankPaymentMethodPrefix
        {
            get { return _bankPaymentMethodPrefix; }
            set 
            {
                if (_bankPaymentMethodPrefix != value)
                {
                    _bankPaymentMethodPrefix = value;
                    NotifyOfPropertyChange(nameof(BankPaymentMethodPrefix));
                }
            }
        }



        #endregion

        #endregion

        #region "BankAccount"

        #region "Properties"

        public int BankAccountId { get; set; }

        private string _bankAccountNumber;

        public string BankAccountNumber
        {
            get { return _bankAccountNumber; }
            set
            {
                if (_bankAccountNumber != value)
                {
                    _bankAccountNumber = value;
                    NotifyOfPropertyChange(nameof(BankAccountNumber));
                    NotifyOfPropertyChange(nameof(BankAccountDescription));
                    NotifyOfPropertyChange(nameof(BankAccountPaymentMethodName));
                    ValidateProperty(nameof(BankAccountNumber), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _bankAccountType;

        public string BankAccountType
        {
            get { return _bankAccountType; }
            set 
            {
                if (_bankAccountType != value)
                {
                    _bankAccountType = value;
                    NotifyOfPropertyChange(nameof(BankAccountType));
                    NotifyOfPropertyChange(nameof(BankAccountDescription));
                    NotifyOfPropertyChange(nameof(BankAccountPaymentMethodName));
                }
            }
        }

        private string _bankAccountProvider;

        public string BankAccountProvider
        {
            get { return _bankAccountProvider; }
            set 
            {
                if (_bankAccountProvider != value)
                {
                    _bankAccountProvider = value;
                    NotifyOfPropertyChange(nameof(BankAccountProvider));
                    NotifyOfPropertyChange(nameof(BankAccountDescription));
                    NotifyOfPropertyChange(nameof(BankAccountPaymentMethodName));
                }
            }
        }


        private bool _bankAccountIsActive;

        public bool BankAccountIsActive
        {
            get { return _bankAccountIsActive; }
            set 
            {
                if (_bankAccountIsActive != value)
                {
                    _bankAccountIsActive = value;
                    NotifyOfPropertyChange(nameof(BankAccountIsActive));
                }
            }
        }

        private string _bankAccountReference;

        public string BankAccountReference
        {
            get { return _bankAccountReference; }
            set 
            {
                if (_bankAccountReference != value)
                {
                    _bankAccountReference = value;
                    NotifyOfPropertyChange(nameof(BankAccountReference));
                    NotifyOfPropertyChange(nameof(BankAccountDescription));
                }
            }
        }

        private int _bankAccountDisplayOrder;

        public int BankAccountDisplayOrder
        {
            get { return _bankAccountDisplayOrder; }
            set 
            {
                if (_bankAccountDisplayOrder != value)
                {
                    _bankAccountDisplayOrder = value;
                    NotifyOfPropertyChange(nameof(BankAccountDisplayOrder));
                }
            }
        }

        private int _bankAccountAccountingAccountId;

        public int BankAccountAccountingAccountId
        {
            get { return _bankAccountAccountingAccountId; }
            set 
            {
                if (_bankAccountAccountingAccountId != value)
                {
                    _bankAccountAccountingAccountId = value;
                    NotifyOfPropertyChange(nameof(BankAccountAccountingAccountId));
                }
            }
        }

        private int _bankAccountBankId;

        public int BankAccountBankId
        {
            get { return _bankAccountBankId; }
            set 
            {
                if (_bankAccountBankId != value)
                {
                    _bankAccountBankId = value;
                    NotifyOfPropertyChange(nameof(BankAccountBankId));
                }
            }
        }

        private string _bankAccountBankName;

        public string BankAccountBankName
        {
            get { return _bankAccountBankName; }
            set 
            {
                if (_bankAccountBankName != value)
                {
                    _bankAccountBankName = value;
                    NotifyOfPropertyChange(nameof(BankAccountBankName));
                }
            }
        }

        private string _bankAccountAccountinAccountNameForNew;

        public string BankAccountDescription
        {
            get 
            {
                if (BankAccountBankCaptureInfoAsRS)
                {
                    return $"{BankAccountBankName} [{(BankAccountType == "A" ? "CTA. DE AHORROS" : "CTA. CORRIENTE")} No. {BankAccountNumber}] {(string.IsNullOrEmpty(BankAccountReference) ? "" : $"- RF. {BankAccountReference}")}".Trim();
                }
                return $"{(BankAccountProvider == "N" ? "NEQUI" : "DAVIPLATA")} - {BankAccountNumber} {(string.IsNullOrEmpty(BankAccountReference) ? "" : $"- RF. {BankAccountReference}")}";
            }
        }

        public bool BankAccountBankCaptureInfoAsPN => BankAccountBankCaptureType.Equals(CaptureTypeEnum.PN);
        public bool BankAccountBankCaptureInfoAsRS => BankAccountBankCaptureType.Equals(CaptureTypeEnum.RS);

        private CaptureTypeEnum _bankAccountBankCaptureType;

        public CaptureTypeEnum BankAccountBankCaptureType
        {
            get { return _bankAccountBankCaptureType; }
            set 
            {
                if (_bankAccountBankCaptureType != value)
                {
                    _bankAccountBankCaptureType = value;
                    NotifyOfPropertyChange(nameof(BankAccountBankCaptureType));
                    NotifyOfPropertyChange(nameof(BankAccountBankCaptureInfoAsPN));
                    NotifyOfPropertyChange(nameof(BankAccountBankCaptureInfoAsRS));
                }
            }
        }


        private string _bankAccountCurrentBalance = "0";

        public string BankAccountCurrentBalance
        {
            get { return _bankAccountCurrentBalance; }
            set 
            {
                if (_bankAccountCurrentBalance != value)
                {
                    _bankAccountCurrentBalance = value;
                    NotifyOfPropertyChange(nameof(BankAccountCurrentBalance));
                }
            }
        }

        private bool _bankAccountAccountingAccountAutoCreate = true;

        public bool BankAccountAccountingAccountAutoCreate
        {
            get { return _bankAccountAccountingAccountAutoCreate; }
            set 
            {
                if (_bankAccountAccountingAccountAutoCreate != value)
                {
                    _bankAccountAccountingAccountAutoCreate = value;
                    NotifyOfPropertyChange(nameof(BankAccountAccountingAccountAutoCreate));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _bankAccountAccountingAccountSelectExisting = false;

        public bool BankAccountAccountingAccountSelectExisting
        {
            get { return _bankAccountAccountingAccountSelectExisting; }
            set 
            {
                if (_bankAccountAccountingAccountSelectExisting != value)
                {
                    _bankAccountAccountingAccountSelectExisting = value;
                    NotifyOfPropertyChange(nameof(BankAccountAccountingAccountSelectExisting));
                    NotifyOfPropertyChange(nameof(CanSave));
                    if(BankAccountAccountingAccountSelectExisting is false)
                    {
                        BankAccountSelectedAccountingAccount = BankAccountAccountingAccounts.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("");
                    }
                }
            }
        }

        public TreasuryBankMasterTreeDTO BankBeforeNewBankAccount { get; set; } = new();

        private bool _bankAccountBankIsRS;

        public bool BankAccountBankIsRS
        {
            get { return _bankAccountBankIsRS; }
            set 
            {
                if (_bankAccountBankIsRS != value)
                {
                    _bankAccountBankIsRS = value;
                    NotifyOfPropertyChange(nameof(BankAccountBankIsRS));
                }
            }
        }

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

        private AccountingAccountGraphQLModel _bankAccountSelectedAccountingAccount;

        public AccountingAccountGraphQLModel BankAccountSelectedAccountingAccount
        {
            get { return _bankAccountSelectedAccountingAccount; }
            set 
            {
                if (_bankAccountSelectedAccountingAccount != value)
                {
                    _bankAccountSelectedAccountingAccount = value;
                    NotifyOfPropertyChange(nameof(BankAccountSelectedAccountingAccount));
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }

        private string _bankAccountPaymentMethodName;

        public string BankAccountPaymentMethodName
        {
            get
            {
                if (BankAccountBankCaptureInfoAsRS)
                {
                    return $"TRANSF/CONSIG EN {BankAccountBankName.Trim()} EN {(BankAccountType == "A" ? "CTA. DE AHORROS" : "CUENTA CORRIENTE")} TERMINADA EN {(BankAccountNumber.Length > 5 ? $"* {BankAccountNumber[^5..]}" : "")}";
                }
                return $"TRANSF/CONSGI EN {(BankAccountProvider == "N" ? "NEQUI" : "DAVIPLATA")} {BankAccountNumber}";
            }
        }

        private string _bankAccountPaymentMethodAbbrevation;

        public string BankAccountPaymentMethodAbbreviation
        {
            get { return _bankAccountPaymentMethodAbbrevation; }
            set 
            {
                if (_bankAccountPaymentMethodAbbrevation != value)
                {
                    _bankAccountPaymentMethodAbbrevation = value;
                    NotifyOfPropertyChange(nameof(BankAccountPaymentMethodAbbreviation));
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

        private TreasuryBankAccountCostCenterDTO _bankAccountSelectedCostCenter;

        public TreasuryBankAccountCostCenterDTO BankAccountSelectedCostCenter
        {
            get { return _bankAccountSelectedCostCenter; }
            set
            {
                if (_bankAccountSelectedCostCenter != value)
                {
                    _bankAccountSelectedCostCenter = value;
                    NotifyOfPropertyChange(nameof(BankAccountSelectedCostCenter));
                }
            }
        }

        #endregion

        #endregion

        #region "Franchise"

        #region "Properties"

        public int FranchiseId { get; set; }

        private string _franchiseName;

        public string FranchiseName
        {
            get { return _franchiseName; }
            set 
            {
                if (_franchiseName != value)
                {
                    _franchiseName = value;
                    NotifyOfPropertyChange(nameof(FranchiseName));
                    ValidateProperty(nameof(FranchiseName), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _franchiseType;

        public string FranchiseType
        {
            get { return _franchiseType; }
            set 
            {
                if (_franchiseType != value)
                {
                    _franchiseType = value;
                    NotifyOfPropertyChange(nameof(FranchiseType));
                }
            }
        }

        private decimal _franchiseCommissionMargin;

        public decimal FranchiseCommissionMargin
        {
            get { return _franchiseCommissionMargin; }
            set 
            {
                if (_franchiseCommissionMargin != value)
                {
                    _franchiseCommissionMargin = value;
                    NotifyOfPropertyChange(nameof(FranchiseCommissionMargin));
                }
            }
        }

        private decimal _franchiseReteivaMargin;

        public decimal FranchiseReteivaMargin
        {
            get { return _franchiseReteivaMargin; }
            set 
            {
                if (_franchiseReteivaMargin != value)
                {
                    _franchiseReteivaMargin = value;
                    NotifyOfPropertyChange(nameof(FranchiseReteivaMargin));
                }
            }
        }

        private decimal _franchiseReteicaMargin;

        public decimal FranchiseReteicaMargin
        {
            get { return _franchiseReteicaMargin; }
            set 
            {
                if (_franchiseReteicaMargin != value)
                {
                    _franchiseReteicaMargin = value;
                    NotifyOfPropertyChange(nameof(FranchiseReteicaMargin));
                }
            }
        }

        private decimal _franchiseRetefteMargin;

        public decimal FranchiseRetefteMargin
        {
            get { return _franchiseRetefteMargin; }
            set 
            {
                if (_franchiseRetefteMargin != value)
                {
                    _franchiseRetefteMargin = value;
                    NotifyOfPropertyChange(nameof(FranchiseRetefteMargin));
                }
            }
        }

        private decimal _franchiseIvaMargin;

        public decimal FranchiseIvaMargin
        {
            get { return _franchiseIvaMargin; }
            set 
            {
                if (_franchiseIvaMargin != value)
                {
                    _franchiseIvaMargin = value;
                    NotifyOfPropertyChange(nameof(FranchiseIvaMargin));
                }
            }
        }

        private string _franchiseFormulaCommission;

        public string FranchiseFormulaCommission
        {
            get { return _franchiseFormulaCommission; }
            set 
            {
                if (_franchiseFormulaCommission != value)
                {
                    _franchiseFormulaCommission = value;
                    NotifyOfPropertyChange(nameof(FranchiseFormulaCommission));
                    CanFranchiseSimulator(new object { });
                    ValidateProperty(nameof(FranchiseFormulaCommission), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _franchiseFormulaReteiva;

        public string FranchiseFormulaReteiva
        {
            get { return _franchiseFormulaReteiva; }
            set 
            {
                if (_franchiseFormulaReteiva != value)
                {
                    _franchiseFormulaReteiva = value;
                    NotifyOfPropertyChange(nameof(FranchiseFormulaReteiva));
                    CanFranchiseSimulator(new object { });
                    ValidateProperty(nameof(FranchiseFormulaReteiva), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _franchiseFormulaReteica;

        public string FranchiseFormulaReteica
        {
            get { return _franchiseFormulaReteica; }
            set 
            {
                if (_franchiseFormulaReteica != value)
                {
                    _franchiseFormulaReteica = value;
                    NotifyOfPropertyChange(nameof(FranchiseFormulaReteica));
                    CanFranchiseSimulator(new object { });
                    ValidateProperty(nameof(FranchiseFormulaReteica), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                } 
            }
        }

        private string _franchiseFormulaRetefte;

        public string FranchiseFormulaRetefte
        {
            get { return _franchiseFormulaRetefte; }
            set 
            {
                if (_franchiseFormulaRetefte != value)
                {
                    _franchiseFormulaRetefte = value;
                    NotifyOfPropertyChange(nameof(FranchiseFormulaRetefte));
                    CanFranchiseSimulator(new object { });
                    ValidateProperty(nameof(FranchiseFormulaRetefte), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

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

        private BankAccountGraphQLModel _franchiseSelectedBankAccount;

        public BankAccountGraphQLModel FranchiseSelectedBankAccount
        {
            get { return _franchiseSelectedBankAccount; }
            set 
            {
                if (_franchiseSelectedBankAccount != value)
                {
                    _franchiseSelectedBankAccount = value;
                    NotifyOfPropertyChange(nameof(FranchiseSelectedBankAccount));
                    NotifyOfPropertyChange(nameof(FranchiseSelectedBankAccount));
                    NotifyOfPropertyChange(nameof(CanSave));
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

        private TreasuryFranchiseCostCenterDTO _franchiceSelectedCostCenter;

        public TreasuryFranchiseCostCenterDTO FranchiseSelectedCostCenter
        {
            get { return _franchiceSelectedCostCenter; }
            set 
            {
                if (_franchiceSelectedCostCenter != value)
                {
                    _franchiceSelectedCostCenter = value;
                    NotifyOfPropertyChange(nameof(FranchiseSelectedCostCenter));
                    EditFranchiseByCostCenter(FranchiseSelectedCostCenter.Id);
                }
            }
        }


        private AccountingAccountGraphQLModel _franchiseSelectedAccountingAccountCommission;

        public AccountingAccountGraphQLModel FranchiseSelectedAccountingAccountCommission
        {
            get { return _franchiseSelectedAccountingAccountCommission; }
            set 
            {
                if (_franchiseSelectedAccountingAccountCommission != value)
                {
                    _franchiseSelectedAccountingAccountCommission = value;
                    NotifyOfPropertyChange(nameof(FranchiseSelectedAccountingAccountCommission));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public List<FranchiseByCostCenterGraphQLModel> FranchiseSettingsByCostCenter { get; set; }

        private decimal _franchiseCardValue;

        public decimal FranchiseCardValue
        {
            get { return _franchiseCardValue; }
            set 
            {
                if (_franchiseCardValue != value)
                {
                    _franchiseCardValue = value;
                    NotifyOfPropertyChange(nameof(FranchiseCardValue));
                    CanFranchiseSimulator(new object { });
                }
            }
        }
        public string FranchiseDecimalsCount { get; set; } = "n4"; //n concatenado con el número que se quiere mostrar como decimales

        public string FranchiseReplacedFormulaCommission { get; set; } = string.Empty;
        public string FranchiseReplacedFormulaReteiva { get; set; } = string.Empty;
        public string FranchiseReplacedFormulaReteica { get; set; } = string.Empty;
        public string FranchiseReplacedFormulaRetefte { get; set; } = string.Empty;

        private decimal _franchiseSimulatedCommission;

        public decimal FranchiseSimulatedCommission
        {
            get { return _franchiseSimulatedCommission; }
            set 
            {
                if (_franchiseSimulatedCommission != value)
                {
                    _franchiseSimulatedCommission = value;
                    NotifyOfPropertyChange(nameof(FranchiseSimulatedCommission));
                }
            }
        }

        private decimal _franchiseSimulatedReteiva;

        public decimal FranchiseSimulatedReteiva
        {
            get { return _franchiseSimulatedReteiva; }
            set 
            {
                if (_franchiseSimulatedReteiva != value)
                {
                    _franchiseSimulatedReteiva = value;
                    NotifyOfPropertyChange(nameof(FranchiseSimulatedReteiva));
                }
            }
        }

        private decimal _franchiseSimulatedReteica;

        public decimal FranchiseSimulatedReteica
        {
            get { return _franchiseSimulatedReteica; }
            set 
            {
                if (_franchiseSimulatedReteica != value)
                {
                    _franchiseSimulatedReteica = value;
                    NotifyOfPropertyChange(nameof(FranchiseSimulatedReteica));
                }
            }
        }

        private decimal _franchiseSimulatedRetefte;

        public decimal FranchiseSimulatedRetefte
        {
            get { return _franchiseSimulatedRetefte; }
            set 
            {
                if (_franchiseSimulatedRetefte != value)
                {
                    _franchiseSimulatedRetefte = value;
                    NotifyOfPropertyChange(nameof(FranchiseSimulatedRetefte));
                }
            }
        }

        private decimal _franchiseSimulatedIvaValue;

        public decimal FranchiseSimulatedIvaValue
        {
            get { return _franchiseSimulatedIvaValue; }
            set 
            {
                if (_franchiseSimulatedIvaValue != value)
                {
                    _franchiseSimulatedIvaValue = value;
                    NotifyOfPropertyChange(nameof(FranchiseSimulatedIvaValue));
                }
            }
        }


        #endregion

        #endregion

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
            FranchiseFormulaReteiva = "[VALOR_IVA]*([MARGEN_RETE_IVA]/100)";
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
            FranchiseFormulaCommission = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_COMISION]/100)";
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
            FranchiseFormulaReteica = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_ICA]/1000)";
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
            FranchiseFormulaRetefte = "([VALOR_TARJETA]-[VALOR_IVA])*([MARGEN_RETE_FUENTE]/100)";
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
            try
            {
                //Igualación para no modificar las formulas en la vista
                FranchiseReplacedFormulaCommission = FranchiseFormulaCommission;
                FranchiseReplacedFormulaReteiva = FranchiseFormulaReteiva;
                FranchiseReplacedFormulaReteica = FranchiseFormulaReteica;
                FranchiseReplacedFormulaRetefte = FranchiseFormulaRetefte;

                //Obtener el valor del IVA
                FranchiseSimulatedIvaValue = FranchiseCardValue - (FranchiseCardValue / (1 + (FranchiseIvaMargin / 100)));

                //Settear el diccionario
                Dictionary<string, decimal> formulaVariables = new()
                {
                    { "VALOR_TARJETA", FranchiseCardValue },
                    { "MARGEN_COMISION", FranchiseCommissionMargin },
                    { "MARGEN_RETE_IVA", FranchiseReteivaMargin },
                    { "MARGEN_RETE_ICA", FranchiseReteicaMargin },
                    { "MARGEN_RETE_FUENTE", FranchiseRetefteMargin },
                    { "VALOR_IVA", FranchiseSimulatedIvaValue }
                };

                //Reemplazar las variables en las formulas
                foreach (var item in formulaVariables)
                {
                    FranchiseReplacedFormulaCommission = FranchiseReplacedFormulaCommission.Replace($"[{item.Key}]", item.Value.ToString(CultureInfo.InvariantCulture));
                    FranchiseReplacedFormulaReteiva = FranchiseReplacedFormulaReteiva.Replace($"[{item.Key}]", item.Value.ToString(CultureInfo.InvariantCulture));
                    FranchiseReplacedFormulaReteica = FranchiseReplacedFormulaReteica.Replace($"[{item.Key}]", item.Value.ToString(CultureInfo.InvariantCulture));
                    FranchiseReplacedFormulaRetefte = FranchiseReplacedFormulaRetefte.Replace($"[{item.Key}]", item.Value.ToString(CultureInfo.InvariantCulture));
                }

                //Calcular las formulas
                FranchiseSimulatedCommission = Convert.ToDecimal(new DataTable().Compute(FranchiseReplacedFormulaCommission, null), CultureInfo.InvariantCulture);
                FranchiseSimulatedReteiva = Convert.ToDecimal(new DataTable().Compute(FranchiseReplacedFormulaReteiva, null), CultureInfo.InvariantCulture);
                FranchiseSimulatedReteica = Convert.ToDecimal(new DataTable().Compute(FranchiseReplacedFormulaReteica, null), CultureInfo.InvariantCulture);
                FranchiseSimulatedRetefte = Convert.ToDecimal(new DataTable().Compute(FranchiseReplacedFormulaRetefte, null), CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"Error al simular la franquicia. \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public bool CanFranchiseSimulator(object p)
        {
            if (FranchiseCardValue != 0 && (!string.IsNullOrEmpty(FranchiseFormulaCommission) && !string.IsNullOrEmpty(FranchiseFormulaReteiva) && !string.IsNullOrEmpty(FranchiseFormulaReteica) && !string.IsNullOrEmpty(FranchiseFormulaRetefte))) return true; 
            return false;
        }

        public void EditFranchiseByCostCenter(int costCenterId)
        {
            if (IsNewRecord) return;

            if (costCenterId == 0)
            {
                if (SelectedItem is TreasuryFranchiseMasterTreeDTO selectedFranchise)
                {
                    FranchiseCommissionMargin = selectedFranchise.CommissionMargin;
                    FranchiseReteicaMargin = selectedFranchise.ReteivaMargin;
                    FranchiseReteicaMargin = selectedFranchise.ReteicaMargin;
                    FranchiseRetefteMargin = selectedFranchise.RetefteMargin;
                    FranchiseIvaMargin = selectedFranchise.IvaMargin;
                    FranchiseSelectedBankAccount = FranchiseBankAccounts.FirstOrDefault(x => x.Id == selectedFranchise.BankAccount.Id) ?? new BankAccountGraphQLModel();
                    FranchiseFormulaCommission = selectedFranchise.FormulaCommission;
                    FranchiseFormulaReteiva = selectedFranchise.FormulaReteiva;
                    FranchiseFormulaReteica = selectedFranchise.FormulaReteica;
                    FranchiseFormulaRetefte = selectedFranchise.FormulaRetefte;
                    FranchiseCardValue = 0;
                    FranchiseSimulatedCommission = 0;
                    FranchiseSimulatedReteiva = 0;
                    FranchiseSimulatedReteica = 0;
                    FranchiseSimulatedRetefte = 0;
                    FranchiseSimulatedIvaValue = 0;
                }
                return;
            }
            var selectedFranchiseSetting = FranchiseSettingsByCostCenter.FirstOrDefault(x => x.CostCenterId == costCenterId);
            if(selectedFranchiseSetting != null)
            {
                FranchiseCommissionMargin = selectedFranchiseSetting.CommissionMargin;
                FranchiseReteicaMargin = selectedFranchiseSetting.ReteivaMargin;
                FranchiseReteicaMargin = selectedFranchiseSetting.ReteicaMargin;
                FranchiseRetefteMargin = selectedFranchiseSetting.RetefteMargin;
                FranchiseIvaMargin = selectedFranchiseSetting.IvaMargin;
                FranchiseSelectedBankAccount = FranchiseBankAccounts.FirstOrDefault(x => x.Id == selectedFranchiseSetting.BankAccountId) ?? new BankAccountGraphQLModel();
                FranchiseFormulaCommission = selectedFranchiseSetting.FormulaCommission;
                FranchiseFormulaReteiva = selectedFranchiseSetting.FormulaReteiva;
                FranchiseFormulaReteica = selectedFranchiseSetting.FormulaReteica;
                FranchiseFormulaRetefte = selectedFranchiseSetting.FormulaRetefte;
                FranchiseCardValue = 0;
                FranchiseSimulatedCommission = 0;
                FranchiseSimulatedReteiva = 0;
                FranchiseSimulatedReteica = 0;
                FranchiseSimulatedRetefte = 0;
                FranchiseSimulatedIvaValue = 0;
            }
        }


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
            SearchWithTwoColumnsGridViewModel = new(query, fieldHeader1, fieldHeader2, fieldData1, fieldData2, null, SearchWithTwoColumnsGridMessageToken.BankAccountingEntity);

            _dialogService.ShowDialog(SearchWithTwoColumnsGridViewModel, "Búsqueda de terceros");
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

                IEnumerable<CompanyLocationGraphQLModel> source = await CompanyLocationService.GetList(query, new { });
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

                IEnumerable<CompanyLocationGraphQLModel> source = await CompanyLocationService.GetList(query, new { });
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

                var source = await BankService.GetList(query, new { });
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
                variables.filter.BankId = bank.Id;

                var source = await BankAccountService.GetList(query, variables);
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

                var source = await CostCenterService.GetList(query, variables);
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

                var source = await CostCenterService.GetList(query, variables);
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
                variables.filter.costCenterId = costCenterDTO.Id;
                variables.filter.parentId = 0;
                variables.filter.isPettyCash = false;

                var source = await CashDrawerService.GetList(query, variables);
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
                variables.filter.parentId = majorCashDrawer.Id;
                variables.filter.isPettyCash = false;

                var source = await CashDrawerService.GetList(query, variables);
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
                variables.filter.costCenterId = costCenterDTO.Id;
                variables.filter.isPettyCash = true;
                variables.filter.parentId = 0;

                var source = await CashDrawerService.GetList(query, variables);
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
                variables.filter.CompanyId = 1;
                var source = await FranchiseService.GetList(query, variables);
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
                variables.cashDrawerFilter = new ExpandoObject();
                variables.bankAccountFilter = new ExpandoObject();
                variables.cashDrawerFilter.isPettyCash = false;
                variables.accountingAccountFilter.IncludeOnlyAuxiliaryAccounts = true;
                variables.bankAccountFilter.AllowedTypes = "AC";
                var result = await CashDrawerService.GetDataContext<CashDrawerComboBoxesDataContext>(query, variables);
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
        public TreasuryRootMasterViewModel(TreasuryRootViewModel context)
        {
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
            BankAccountingEntityId = message.ReturnedData.Id;
            BankAccountingEntityName = message.ReturnedData.SearchName;
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
                TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO majorCashDrawerCompanyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.CreatedCashDrawer.CostCenter.Location.Id) ?? throw new Exception("");
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
                    return;
                }
                if (!majorCashDrawerCostCenter.IsExpanded)
                {
                    majorCashDrawerCostCenter.IsExpanded = true;
                    majorCashDrawerCostCenter.CashDrawers.Add(majorCashDrawerMasterTreeDTO);
                    SelectedItem = majorCashDrawerMasterTreeDTO;
                    return;
                }
                majorCashDrawerCostCenter.CashDrawers.Add(majorCashDrawerMasterTreeDTO);
                SelectedItem = majorCashDrawerMasterTreeDTO;
                return;
            }
            //caja auxiliar
            if (message.CreatedCashDrawer.IsPettyCash == false && message.CreatedCashDrawer.Parent != null)
            {
                TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawerMasterTreeDTO = Context.AutoMapper.Map<TreasuryAuxiliaryCashDrawerMasterTreeDTO>(message.CreatedCashDrawer);
                MajorCashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.FirstOrDefault(x => x is MajorCashDrawerDummyDTO) as MajorCashDrawerDummyDTO ?? throw new Exception("");
                if (majorCashDrawerDummyDTO is null) return;
                TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO majorCashDrawerCompanyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.CreatedCashDrawer.Parent.CostCenter.Location.Id) ?? throw new Exception("");
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
                    return;
                }
                if (!majorCashDrawer.IsExpanded)
                {
                    majorCashDrawer.IsExpanded = true;
                    majorCashDrawer.AuxiliaryCashDrawers.Add(auxiliaryCashDrawerMasterTreeDTO);
                    SelectedItem = auxiliaryCashDrawerMasterTreeDTO;
                    return;
                }
                majorCashDrawer.AuxiliaryCashDrawers.Add(auxiliaryCashDrawerMasterTreeDTO);
                SelectedItem = auxiliaryCashDrawerMasterTreeDTO;
                return;
            }

            //caja menor
            MinorCashDrawerMasterTreeDTO minorCashDrawerMasterTreeDTO = Context.AutoMapper.Map<MinorCashDrawerMasterTreeDTO>(message.CreatedCashDrawer);
            MinorCashDrawerDummyDTO minorCashDrawerDummyDTO = DummyItems.FirstOrDefault(x => x is MinorCashDrawerDummyDTO) as MinorCashDrawerDummyDTO ?? throw new Exception("");
            if (minorCashDrawerDummyDTO is null) return;
            TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO minorCashDrawerCompanyLocation = minorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.CreatedCashDrawer.CostCenter.Location.Id) ?? throw new Exception("");
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
                return;
            }
            if (!minorCashDrawerCostCenter.IsExpanded)
            {
                minorCashDrawerCostCenter.IsExpanded = true;
                minorCashDrawerCostCenter.CashDrawers.Add(minorCashDrawerMasterTreeDTO);
                SelectedItem = minorCashDrawerMasterTreeDTO;
                return;
            }
            minorCashDrawerCostCenter.CashDrawers.Add(minorCashDrawerMasterTreeDTO);
            SelectedItem = minorCashDrawerMasterTreeDTO;
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
                    TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO companyLocation = majorCashDrawerDTO.Locations.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.CostCenter.Location.Id) ?? throw new Exception("");
                    if (companyLocation is null) return;
                    TreasuryMajorCashDrawerCostCenterMasterTreeDTO costCenter = companyLocation.CostCenters.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.CostCenter.Id) ?? throw new Exception("");
                    if (costCenter is null) return;
                    costCenter.CashDrawers.Remove(costCenter.CashDrawers.Where(x => x.Id == message.DeletedCashDrawer.Id).First());
                    SelectedItem = null;
                });
                return Task.CompletedTask;
            }
            if(message.DeletedCashDrawer.IsPettyCash is false && message.DeletedCashDrawer.Parent != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MajorCashDrawerDummyDTO majorCashDrawerDTO = DummyItems.FirstOrDefault(x => x is MajorCashDrawerDummyDTO) as MajorCashDrawerDummyDTO ?? throw new Exception("");
                    if (majorCashDrawerDTO is null) return;
                    TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO companyLocation = majorCashDrawerDTO.Locations.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.Parent.CostCenter.Location.Id) ?? throw new Exception("");
                    if (companyLocation is null) return;
                    TreasuryMajorCashDrawerCostCenterMasterTreeDTO costCenter = companyLocation.CostCenters.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.Parent.CostCenter.Id) ?? throw new Exception("");
                    if (costCenter is null) return;
                    MajorCashDrawerMasterTreeDTO majorCashDrawer = costCenter.CashDrawers.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.Parent.Id) ?? throw new Exception("");
                    if (majorCashDrawer is null) return;
                    majorCashDrawer.AuxiliaryCashDrawers.Remove(majorCashDrawer.AuxiliaryCashDrawers.Where(x => x.Id == message.DeletedCashDrawer.Id).First());
                    SelectedItem = null;
                });
                return Task.CompletedTask;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MinorCashDrawerDummyDTO minorCashDrawerDTO = DummyItems.FirstOrDefault(x => x is MinorCashDrawerDummyDTO) as MinorCashDrawerDummyDTO ?? throw new Exception("");
                if (minorCashDrawerDTO is null) return;
                TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO companyLocation = minorCashDrawerDTO.Locations.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.CostCenter.Location.Id) ?? throw new Exception("");
                if (companyLocation is null) return;
                TreasuryMinorCashDrawerCostCenterMasterTreeDTO costCenter = companyLocation.CostCenters.FirstOrDefault(x => x.Id == message.DeletedCashDrawer.CostCenter.Id) ?? throw new Exception("");
                if (costCenter is null) return;
                costCenter.CashDrawers.Remove(costCenter.CashDrawers.Where(x => x.Id == message.DeletedCashDrawer.Id).First());
                SelectedItem = null;
            });
            return Task.CompletedTask;
        }

        public async Task HandleAsync(TreasuryCashDrawerUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedCashDrawer.IsPettyCash is false && message.UpdatedCashDrawer.Parent is null)
            {
                MajorCashDrawerMasterTreeDTO cashDrawerDTO = Context.AutoMapper.Map<MajorCashDrawerMasterTreeDTO>(message.UpdatedCashDrawer);
                MajorCashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.FirstOrDefault(x => x is MajorCashDrawerDummyDTO) as MajorCashDrawerDummyDTO ?? throw new Exception("");
                if (majorCashDrawerDummyDTO is null) return;
                TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO majorCashDrawerCompanyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.CostCenter.Location.Id) ?? throw new Exception("");
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
                await Task.Run(() => SetMajorCashDrawerForEdit(cashDrawerToUpdate), cancellationToken);
                return;
            }
            if(message.UpdatedCashDrawer.IsPettyCash is false && message.UpdatedCashDrawer.Parent != null)
            {
                TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawer = Context.AutoMapper.Map<TreasuryAuxiliaryCashDrawerMasterTreeDTO>(message.UpdatedCashDrawer);
                MajorCashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.FirstOrDefault(x => x is MajorCashDrawerDummyDTO) as MajorCashDrawerDummyDTO ?? throw new Exception("");
                if (majorCashDrawerDummyDTO is null) return;
                TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO majorCashDrawerCompanyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.Parent.CostCenter.Location.Id) ?? throw new Exception("");
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
                await Task.Run(() => SetAuxiliaryCashDrawerForEdit(auxiliaryCashDrawerToUpdate), cancellationToken);
                return;
            }
            MinorCashDrawerMasterTreeDTO minorCashDrawerMasterTreeDTO = Context.AutoMapper.Map<MinorCashDrawerMasterTreeDTO>(message.UpdatedCashDrawer);
            MinorCashDrawerDummyDTO minorCashDrawerDummyDTO = DummyItems.FirstOrDefault(x => x is MinorCashDrawerDummyDTO) as MinorCashDrawerDummyDTO ?? throw new Exception("");
            if (minorCashDrawerDummyDTO is null) return;
            TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO minorCashDrawerCompanyLocation = minorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.CostCenter.Location.Id) ?? throw new Exception("");
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
            await Task.Run(() => SetMinorCashDrawerForEdit(minorCashDrawerToUpdate), cancellationToken);
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

        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(MajorCashDrawerName):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "El nombre no puede estar vacío");
                        break;
                    case nameof(MinorCashDrawerName):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "El nombre no puede estar vacío");
                        break;
                    case nameof(AuxiliaryCashDrawerName):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "El nombre no puede estar vacío");
                        break;
                    case nameof(AuxiliaryCashDrawerComputerName):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "El nombre del equipo no puede estar vacío");
                        break;
                    case nameof(BankAccountingEntityName):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "El nombre del banco no puede estar vacío");
                        break;
                    case nameof(BankAccountNumber):
                        if (string.IsNullOrEmpty(value.Trim()) && BankAccountBankCaptureInfoAsRS) AddError(propertyName, "El número de cuenta no puede estar vacío");
                        if (string.IsNullOrEmpty(value.Trim()) && BankAccountBankCaptureInfoAsPN) AddError(propertyName, "El número celular no puede estar vacío");
                        break;
                    case nameof(FranchiseName):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "El nombre de la franquicia no puede estar vacío");
                        break;
                    case nameof(FranchiseFormulaCommission):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "La fórmula de comisión no puede estar vacía");
                        break;
                    case nameof(FranchiseFormulaReteiva):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "La fórmula de reteiva no puede estar vacía");
                        break;
                    case nameof(FranchiseFormulaReteica):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "La fórmula de reteica no puede estar vacía");
                        break;
                    case nameof(FranchiseFormulaRetefte):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "La fórmula de retefte no puede estar vacía");
                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void ClearAllErrors()
        {
            _errors.Clear();
        }

        private void ValidateAuxiliaryCashDrawerProperties()
        {
            ValidateProperty(nameof(AuxiliaryCashDrawerName), AuxiliaryCashDrawerName);
            ValidateProperty(nameof(AuxiliaryCashDrawerComputerName), AuxiliaryCashDrawerComputerName);
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
                return;
            }
            if (!bankDummyDTO.IsExpanded)
            {
                bankDummyDTO.IsExpanded = true;
                bankDummyDTO.Banks.Add(bankDTO);
                SelectedItem = bankDTO;
                return;
            }
            bankDummyDTO.Banks.Add(bankDTO);
            SelectedItem = bankDTO;
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
                return;
            }
            if (!bankDTO.IsExpanded)
            {
                bankDTO.IsExpanded = true;
                bankDTO.BankAccounts.Add(bankAccountDTO);
                SelectedItem = bankAccountDTO;
                return;
            }
            bankDTO.BankAccounts.Add(bankAccountDTO);
            SelectedItem = bankAccountDTO;
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
            await Task.Run(() => SetBankAccountForEdit(bankAccountToUpdate));
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
                return;
            }
            if (!franchiseDummyDTO.IsExpanded)
            {
                franchiseDummyDTO.IsExpanded = true;
                franchiseDummyDTO.Franchises.Add(franchiseDTO);
                SelectedItem = franchiseDTO;
                return;
            }
            franchiseDummyDTO.Franchises.Add(franchiseDTO);
            SelectedItem = franchiseDTO;
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
            await Task.Run(() => SetFranchiseForEdit(franchiseToUpdate));
            return;
        }
    }

    public class CashDrawerComboBoxesDataContext
    {
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts { get; set; }
        public ObservableCollection<CashDrawerGraphQLModel> CashDrawers { get; set; }
        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; set; }
        public ObservableCollection<BankAccountGraphQLModel> BankAccounts { get; set; }
    }
}
