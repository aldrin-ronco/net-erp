﻿using Caliburn.Micro;
using Common.Extensions;
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
using NetErp.Helpers;
using NetErp.Treasury.Masters.DTO;
using Services.Billing.DAL.PostgreSQL;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Treasury.Masters.ViewModels
{
    public class TreasuryRootMasterViewModel : Screen, INotifyDataErrorInfo,
        IHandle<TreasuryCashDrawerCreateMessage>,
        IHandle<TreasuryCashDrawerDeleteMessage>,
        IHandle<TreasuryCashDrawerUpdateMessage>
    {
        public TreasuryRootViewModel Context { get; set; }

        Dictionary<string, List<string>> _errors;

        public readonly IGenericDataAccess<CompanyLocationGraphQLModel> CompanyLocationService = IoC.Get<IGenericDataAccess<CompanyLocationGraphQLModel>>();

        public readonly IGenericDataAccess<CostCenterGraphQLModel> CostCenterService = IoC.Get<IGenericDataAccess<CostCenterGraphQLModel>>();

        public readonly IGenericDataAccess<CashDrawerGraphQLModel> CashDrawerService = IoC.Get<IGenericDataAccess<CashDrawerGraphQLModel>>();

        public readonly IGenericDataAccess<BankGraphQLModel> BankService = IoC.Get<IGenericDataAccess<BankGraphQLModel>>();

        public readonly IGenericDataAccess<BankAccountGraphQLModel> BankAccountService = IoC.Get<IGenericDataAccess<BankAccountGraphQLModel>>();

        public readonly IGenericDataAccess<FranchiseGraphQLModel> FranchiseService = IoC.Get<IGenericDataAccess<FranchiseGraphQLModel>>();

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
                    _ = HandleSelectedItemChangedAsync();
                }
            }
        }

        public async Task HandleSelectedItemChangedAsync()
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
                        await SetMajorCashDrawerForEdit(majorCashDrawerMasterTreeDTO);
                        ClearAllErrors();
                        ValidateProperty(nameof(MajorCashDrawerName), MajorCashDrawerName);
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
                        await SetMajorCashDrawerForNew();
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
                if (_editCommand is null) _editCommand = new AsyncCommand(Edit, CanEdit);
                return _editCommand;
            }
        }

        public async Task Edit()
        {
            IsEditing = true;
            CanUndo = true;
            CanEdit = false;

            if (SelectedItem is MajorCashDrawerMasterTreeDTO) this.SetFocus(nameof(MajorCashDrawerName));
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
                if (_undoCommand is null) _undoCommand = new AsyncCommand(Undo, CanUndo);
                return _undoCommand;
            }
        }

        public async Task Undo()
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
            if (SelectedItem is MajorCashDrawerMasterTreeDTO majorCashDrawerMasterTreeDTO) await SetMajorCashDrawerForEdit(majorCashDrawerMasterTreeDTO);
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

        public TreasuryMajorCashDrawerCostCenterMasterTreeDTO MajorCostCenterBeforeNewCashDrawer { get; set; } = new();

        public async Task SetMajorCashDrawerForEdit(MajorCashDrawerMasterTreeDTO majorCashDrawerMasterTreeDTO)
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

        public async Task SetMajorCashDrawerForNew()
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
                    //if (_selectedItem is CompanyDTO companyDTO) CompanyIdBeforeNewCompanyLocation = companyDTO.Id;
                    return true;
                }
                if (_selectedItem is TreasuryMajorCashDrawerCostCenterMasterTreeDTO treasuryMajorCashDrawerCostCenterMasterTreeDTO) MajorCostCenterBeforeNewCashDrawer = treasuryMajorCashDrawerCostCenterMasterTreeDTO;
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
                if (_saveCommand is null) _saveCommand = new AsyncCommand(Save, CanSave);
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
                    await LoadMajorCashDrawerComboBoxes();
                    if (IsNewRecord)
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryCashDrawerCreateMessage() { CreatedCashDrawer = result });
                    }
                    else
                    {
                        await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryCashDrawerUpdateMessage() { UpdatedCashDrawer = result });

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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "Save" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
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
                        if (MajorCashDrawerAutoTransfer == true && SelectedCashDrawerAutoTransfer.Id == 0) return false;
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
                if(IsNewRecord) variables.Data.IsPettyCash = false;
                variables.Data.CashDrawerIdAutoTransfer = MajorCashDrawerAutoTransfer ? SelectedCashDrawerAutoTransfer.Id : 0;
                variables.Data.CostCenterId = IsNewRecord ? MajorCostCenterBeforeNewCashDrawer.Id : MajorCashDrawerCostCenterId;
                if (!IsNewRecord) variables.Data.AccountingAccountIdCash = MajorCashDrawerSelectedAccountingAccountCash.Id;
                if (!IsNewRecord) variables.Data.AccountingAccountIdCheck = MajorCashDrawerSelectedAccountingAccountCheck.Id;
                if (!IsNewRecord) variables.Data.AccountingAccountIdCard = MajorCashDrawerSelectedAccountingAccountCard.Id;
                if (IsNewRecord) variables.Data.ParentId = 0;
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


        #endregion

        #endregion

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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCompanyLocations" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
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
                            accountingEntity{
                                id
                                searchName
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
                    accountingAccount{
                      id
                      name
                    }
                    bank{
                      id
                      accountingEntity{
                        searchName
                      }
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

        public async Task LoadAuxiliaryCashDrawers(CostCenterGraphQLModel costCenter, MajorCashDrawerMasterTreeDTO majorCashDrawer)
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
                    }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.costCenterId = costCenter.Id;
                variables.filter.parentId = majorCashDrawer.Id;

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
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.costCenterId = costCenterDTO.Id;
                variables.filter.isPettyCash = true;

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

        public async Task LoadMajorCashDrawerComboBoxes()
        {
            try
            {
                string query = @"
                query($accountingAccountFilter: AccountingAccountFilterInput!, $cashDrawerFilter: CashDrawerFilterInput!){
                  accountingAccounts(filter: $accountingAccountFilter){
                    id
                    code
                    name
                  }
                  cashDrawers(filter: $cashDrawerFilter){
                    id
                    name
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.accountingAccountFilter = new ExpandoObject();
                variables.cashDrawerFilter = new ExpandoObject();
                variables.cashDrawerFilter.isPettyCash = false;
                variables.accountingAccountFilter.IncludeOnlyAuxiliaryAccounts = true;
                var result = await CashDrawerService.GetDataContext<CashDrawerComboBoxesDataContext>(query, variables);
                CashDrawerAccountingAccounts = new ObservableCollection<AccountingAccountGraphQLModel>(result.AccountingAccounts);
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
            _errors = new Dictionary<string, List<string>>();
            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        public async Task Initialize()
        {
            await LoadMajorCashDrawerComboBoxes();
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);
            await Initialize();
        }

        public async Task HandleAsync(TreasuryCashDrawerCreateMessage message, CancellationToken cancellationToken)
        {
            IsNewRecord = false;
            MajorCashDrawerMasterTreeDTO majorCashDrawerMasterTreeDTO = Context.AutoMapper.Map<MajorCashDrawerMasterTreeDTO>(message.CreatedCashDrawer);
            MajorCashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.FirstOrDefault(x => x is MajorCashDrawerDummyDTO) as MajorCashDrawerDummyDTO  ?? throw new Exception("");
            if (majorCashDrawerDummyDTO is null) return;
            TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO companyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.CreatedCashDrawer.CostCenter.Location.Id) ?? throw new Exception("");
            if (companyLocation is null) return;
            TreasuryMajorCashDrawerCostCenterMasterTreeDTO costCenter = companyLocation.CostCenters.FirstOrDefault(x => x.Id == message.CreatedCashDrawer.CostCenter.Id) ?? throw new Exception("");
            if (costCenter is null) return;
            if (!costCenter.IsExpanded && costCenter.CashDrawers[0].IsDummyChild)
            {
                await LoadMajorCashDrawers(costCenter);
                costCenter.IsExpanded = true;
                MajorCashDrawerMasterTreeDTO? majorCasDrawer = costCenter.CashDrawers.FirstOrDefault(x => x.Id == majorCashDrawerMasterTreeDTO.Id);
                if (majorCasDrawer is null) return;
                SelectedItem = majorCasDrawer;
                return;
            }
            if (!costCenter.IsExpanded)
            {
                costCenter.IsExpanded = true;
                costCenter.CashDrawers.Add(majorCashDrawerMasterTreeDTO);
                SelectedItem = majorCashDrawerMasterTreeDTO;
                return;
            }
            costCenter.CashDrawers.Add(majorCashDrawerMasterTreeDTO);
            SelectedItem = majorCashDrawerMasterTreeDTO;
            return;


        }

        public Task HandleAsync(TreasuryCashDrawerDeleteMessage message, CancellationToken cancellationToken)
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

        public async Task HandleAsync(TreasuryCashDrawerUpdateMessage message, CancellationToken cancellationToken)
        {
            MajorCashDrawerMasterTreeDTO cashDrawerDTO = Context.AutoMapper.Map<MajorCashDrawerMasterTreeDTO>(message.UpdatedCashDrawer);
            MajorCashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.FirstOrDefault(x => x is MajorCashDrawerDummyDTO) as MajorCashDrawerDummyDTO ?? throw new Exception("");
            if (majorCashDrawerDummyDTO is null) return;
            TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO companyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.CostCenter.Location.Id) ?? throw new Exception("");
            if (companyLocation is null) return;
            TreasuryMajorCashDrawerCostCenterMasterTreeDTO costCenter = companyLocation.CostCenters.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.CostCenter.Id) ?? throw new Exception("");
            if (costCenter is null) return;
            MajorCashDrawerMasterTreeDTO cashDrawerToUpdate = costCenter.CashDrawers.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.Id) ?? throw new Exception("");
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
            await SetMajorCashDrawerForEdit(cashDrawerToUpdate);
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
    }

    public class CashDrawerComboBoxesDataContext
    {
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts { get; set; }
        public ObservableCollection<CashDrawerGraphQLModel> CashDrawers { get; set; }
    }
}