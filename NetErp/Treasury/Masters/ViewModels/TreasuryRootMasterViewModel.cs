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

        private readonly IRepository<CompanyLocationGraphQLModel> _companyLocationService;
        private readonly IRepository<CostCenterGraphQLModel> _costCenterService;
        private readonly IRepository<CashDrawerGraphQLModel> _cashDrawerService;
        private readonly IRepository<BankGraphQLModel> _bankService;
        private readonly IRepository<BankAccountGraphQLModel> _bankAccountService;
        private readonly IRepository<FranchiseGraphQLModel> _franchiseService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly BankAccountCache _bankAccountCache;
        private readonly MajorCashDrawerCache _majorCashDrawerCache;

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

        private string BuildCanDeleteQuery(string fragmentName)
        {
            var fields = FieldSpec<CanDeleteType>.Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment(fragmentName, [parameter], fields, "CanDeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery();
        }

        private string BuildDeleteMutationQuery(string fragmentName)
        {
            var fields = FieldSpec<DeleteResponseType>.Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment(fragmentName, [parameter], fields, "DeleteResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
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

        #region Load Query Builders

        private string GetLoadCompanyLocationsQuery()
        {
            var fields = FieldSpec<PageType<CompanyLocationGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.Company, company => company
                        .Field(c => c.Id)))
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("companyLocationsPage", [parameter], fields, "PageResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        private string GetLoadBanksQuery()
        {
            var fields = FieldSpec<PageType<BankGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Code)
                    .Field(e => e.PaymentMethodPrefix)
                    .Select(e => e.AccountingEntity, ae => ae
                        .Field(a => a.Id)
                        .Field(a => a.SearchName)
                        .Field(a => a.CaptureType)))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("pagination", "Pagination")
            };
            var fragment = new GraphQLQueryFragment("banksPage", parameters, fields, "PageResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        private string GetLoadBankAccountsQuery()
        {
            var fields = FieldSpec<PageType<BankAccountGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Type)
                    .Field(e => e.Number)
                    .Field(e => e.IsActive)
                    .Field(e => e.Description)
                    .Field(e => e.Reference)
                    .Field(e => e.DisplayOrder)
                    .Field(e => e.Provider)
                    .Select(e => e.PaymentMethod, pm => pm
                        .Field(p => p.Id)
                        .Field(p => p.Abbreviation)
                        .Field(p => p.Name))
                    .Select(e => e.AccountingAccount, aa => aa
                        .Field(a => a.Id)
                        .Field(a => a.Name)
                        .Field(a => a.Code))
                    .Select(e => e.Bank, b => b
                        .Field(bk => bk.Id)
                        .Select(bk => bk.AccountingEntity, ae => ae
                            .Field(a => a.SearchName)
                            .Field(a => a.CaptureType))))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("pagination", "Pagination"),
                new("filters", "BankAccountFilters")
            };
            var fragment = new GraphQLQueryFragment("bankAccountsPage", parameters, fields, "PageResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        private string GetLoadCostCentersByLocationQuery()
        {
            var fields = FieldSpec<PageType<CostCenterGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.CompanyLocation, loc => loc
                        .Field(l => l.Id)))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("pagination", "Pagination"),
                new("filters", "CostCenterFilters")
            };
            var fragment = new GraphQLQueryFragment("costCentersPage", parameters, fields, "PageResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        private string GetLoadMajorCashDrawersQuery()
        {
            var fields = FieldSpec<PageType<CashDrawerGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.CashReviewRequired)
                    .Field(e => e.AutoAdjustBalance)
                    .Field(e => e.AutoTransfer)
                    .Field(e => e.IsPettyCash)
                    .Select(e => e.CostCenter, cc => cc
                        .Field(c => c.Id)
                        .Field(c => c.Name))
                    .Select(e => e.CashAccountingAccount, aa => aa
                        .Field(a => a.Id)
                        .Field(a => a.Code)
                        .Field(a => a.Name))
                    .Select(e => e.CheckAccountingAccount, aa => aa
                        .Field(a => a.Id)
                        .Field(a => a.Code)
                        .Field(a => a.Name))
                    .Select(e => e.CardAccountingAccount, aa => aa
                        .Field(a => a.Id)
                        .Field(a => a.Code)
                        .Field(a => a.Name))
                    .Select(e => e.AutoTransferCashDrawer, cd => cd
                        .Field(c => c.Id)
                        .Field(c => c.Name)))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("pagination", "Pagination"),
                new("filters", "CashDrawerFilters")
            };
            var fragment = new GraphQLQueryFragment("cashDrawersPage", parameters, fields, "PageResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        private string GetLoadAuxiliaryCashDrawersQuery()
        {
            var fields = FieldSpec<PageType<CashDrawerGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.CashReviewRequired)
                    .Field(e => e.AutoAdjustBalance)
                    .Field(e => e.AutoTransfer)
                    .Field(e => e.IsPettyCash)
                    .Field(e => e.ComputerName)
                    .Select(e => e.CashAccountingAccount, aa => aa
                        .Field(a => a.Id)
                        .Field(a => a.Code)
                        .Field(a => a.Name))
                    .Select(e => e.CheckAccountingAccount, aa => aa
                        .Field(a => a.Id)
                        .Field(a => a.Code)
                        .Field(a => a.Name))
                    .Select(e => e.CardAccountingAccount, aa => aa
                        .Field(a => a.Id)
                        .Field(a => a.Code)
                        .Field(a => a.Name))
                    .Select(e => e.AutoTransferCashDrawer, cd => cd
                        .Field(c => c.Id)
                        .Field(c => c.Name)))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("pagination", "Pagination"),
                new("filters", "CashDrawerFilters")
            };
            var fragment = new GraphQLQueryFragment("cashDrawersPage", parameters, fields, "PageResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        private string GetLoadMinorCashDrawersQuery()
        {
            var fields = FieldSpec<PageType<CashDrawerGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.CashReviewRequired)
                    .Field(e => e.AutoAdjustBalance)
                    .Field(e => e.IsPettyCash)
                    .Select(e => e.CostCenter, cc => cc
                        .Field(c => c.Id)
                        .Field(c => c.Name))
                    .Select(e => e.CashAccountingAccount, aa => aa
                        .Field(a => a.Id)
                        .Field(a => a.Code)
                        .Field(a => a.Name)))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("pagination", "Pagination"),
                new("filters", "CashDrawerFilters")
            };
            var fragment = new GraphQLQueryFragment("cashDrawersPage", parameters, fields, "PageResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        private string GetLoadFranchisesQuery()
        {
            var fields = FieldSpec<PageType<FranchiseGraphQLModel>>
                .Create()
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Type)
                    .Field(e => e.CommissionRate)
                    .Field(e => e.ReteivaRate)
                    .Field(e => e.ReteicaRate)
                    .Field(e => e.RetefteRate)
                    .Field(e => e.TaxRate)
                    .Field(e => e.FormulaCommission)
                    .Field(e => e.FormulaReteiva)
                    .Field(e => e.FormulaReteica)
                    .Field(e => e.FormulaRetefte)
                    .Select(e => e.BankAccount, ba => ba
                        .Field(b => b.Id)
                        .Field(b => b.Description))
                    .Select(e => e.CommissionAccountingAccount, aa => aa
                        .Field(a => a.Id)
                        .Field(a => a.Name))
                    .SelectList(e => e.FranchisesByCostCenter, fs => fs
                        .Field(s => s.Id)
                        .Select(s => s.CostCenter, cc => cc.Field(c => c.Id))
                        .Field(s => s.CommissionRate)
                        .Field(s => s.ReteivaRate)
                        .Field(s => s.ReteicaRate)
                        .Field(s => s.RetefteRate)
                        .Field(s => s.TaxRate)
                        .Select(s => s.BankAccount, ba => ba.Field(b => b.Id))
                        .Field(s => s.FormulaCommission)
                        .Field(s => s.FormulaReteiva)
                        .Field(s => s.FormulaReteica)
                        .Field(s => s.FormulaRetefte)
                        .Select(s => s.CommissionAccountingAccount, caa => caa.Field(x => x.Id))
                        .Select(s => s.Franchise, fr => fr.Field(x => x.Id))))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("pagination", "Pagination")
            };
            var fragment = new GraphQLQueryFragment("franchisesPage", parameters, fields, "PageResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
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
        /// Método unificado para cargar CompanyLocations tanto para Cajas Generales como Cajas Menores.
        /// El Type del dummyDTO determina qué tipo de DTO hijo se crea.
        /// </summary>
        public async Task LoadCashDrawerCompanyLocations(CashDrawerDummyDTO dummyDTO)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    dummyDTO.Locations.Remove(dummyDTO.Locations[0]);
                });
                Refresh();
                string query = GetLoadCompanyLocationsQuery();

                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.pageSize = -1;

                PageType<CompanyLocationGraphQLModel> result = await _companyLocationService.GetPageAsync(query, variables);
                var locations = Context.AutoMapper.Map<ObservableCollection<CashDrawerCompanyLocationDTO>>(result.Entries);
                if (locations.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (CashDrawerCompanyLocationDTO location in locations)
                        {
                            location.Context = this;
                            location.DummyParent = dummyDTO;
                            location.Type = dummyDTO.Type;
                            location.CostCenters.Add(new CashDrawerCostCenterDTO() { IsDummyChild = true, Name = "Dummy", Type = dummyDTO.Type });
                            dummyDTO.Locations.Add(location);
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCashDrawerCompanyLocations" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        /// <summary>
        /// Método unificado para cargar CostCenters tanto para Cajas Generales como Cajas Menores.
        /// El Type del locationDTO determina qué tipo de DTO hijo se crea.
        /// </summary>
        public async Task LoadCashDrawerCostCenters(CashDrawerCompanyLocationDTO locationDTO)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    locationDTO.CostCenters.Remove(locationDTO.CostCenters[0]);
                });

                string query = GetLoadCostCentersByLocationQuery();
                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.companyLocationId = locationDTO.Id;
                variables.pageResponsePagination.pageSize = -1;

                PageType<CostCenterGraphQLModel> result = await _costCenterService.GetPageAsync(query, variables);
                var costCenters = Context.AutoMapper.Map<ObservableCollection<CashDrawerCostCenterDTO>>(result.Entries);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (CashDrawerCostCenterDTO costCenter in costCenters)
                    {
                        costCenter.Context = this;
                        costCenter.Location = locationDTO;
                        costCenter.Type = locationDTO.Type;
                        // El dummy child para CashDrawers - usamos la clase base
                        costCenter.CashDrawers.Add(new CashDrawerMasterTreeDTO() { IsDummyChild = true, Name = "Dummy" });
                        locationDTO.CostCenters.Add(costCenter);
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCashDrawerCostCenters" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        /// <summary>
        /// Método unificado para cargar CashDrawers tanto Generales como Menores.
        /// El Type del costCenterDTO determina:
        /// - Query a usar (Major vs Minor)
        /// - Filtro isPettyCash (false para Major, true para Minor)
        /// - Tipo de DTO a mapear (MajorCashDrawerMasterTreeDTO vs MinorCashDrawerMasterTreeDTO)
        /// - Si agregar AuxiliaryCashDrawers dummy (solo para Major)
        /// </summary>
        public async Task LoadCashDrawers(CashDrawerCostCenterDTO costCenterDTO)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    costCenterDTO.CashDrawers.Remove(costCenterDTO.CashDrawers[0]);
                });

                bool isMajor = costCenterDTO.Type == CashDrawerType.Major;
                string query = isMajor ? GetLoadMajorCashDrawersQuery() : GetLoadMinorCashDrawersQuery();

                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.pageSize = -1;
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.costCenterId = costCenterDTO.Id;
                variables.pageResponseFilters.isPettyCash = !isMajor; // false para Major, true para Minor
                variables.pageResponseFilters.parentId = null;

                PageType<CashDrawerGraphQLModel> result = await _cashDrawerService.GetPageAsync(query, variables);

                if (isMajor)
                {
                    var cashDrawers = Context.AutoMapper.Map<ObservableCollection<MajorCashDrawerMasterTreeDTO>>(result.Entries);
                    if (cashDrawers.Count > 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (MajorCashDrawerMasterTreeDTO cashDrawerDTO in cashDrawers)
                            {
                                cashDrawerDTO.Context = this;
                                cashDrawerDTO.AuxiliaryCashDrawers.Add(new TreasuryAuxiliaryCashDrawerMasterTreeDTO() { IsDummyChild = true, Name = "Dummy" });
                                costCenterDTO.CashDrawers.Add(cashDrawerDTO);
                            }
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            costCenterDTO.IsExpanded = false;
                        });
                        _notificationService.ShowInfo("Este centro de costo no tiene cajas generales registradas");
                    }
                }
                else
                {
                    var cashDrawers = Context.AutoMapper.Map<ObservableCollection<MinorCashDrawerMasterTreeDTO>>(result.Entries);
                    if (cashDrawers.Count > 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (MinorCashDrawerMasterTreeDTO cashDrawerDTO in cashDrawers)
                            {
                                costCenterDTO.CashDrawers.Add(cashDrawerDTO);
                            }
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            costCenterDTO.IsExpanded = false;
                        });
                        _notificationService.ShowInfo("Este centro de costo no tiene cajas menores registradas");
                    }
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCashDrawers" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
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
                string query = GetLoadBanksQuery();

                dynamic variables = new ExpandoObject();
                variables.banksPagePagination = new ExpandoObject();
                variables.banksPagePagination.pageSize = -1;

                PageType<BankGraphQLModel> result = await _bankService.GetPageAsync(query, variables);
                var banks = Context.AutoMapper.Map<ObservableCollection<TreasuryBankMasterTreeDTO>>(result.Entries);
                if (banks.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (TreasuryBankMasterTreeDTO bank in banks)
                        {
                            bank.Context = this;
                            bank.DummyParent = bankDummyDTO;
                            bank.BankAccounts.Add(new TreasuryBankAccountMasterTreeDTO() { IsDummyChild = true, Description = "Dummy" });
                            bankDummyDTO?.Banks.Add(bank);
                        }
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        bankDummyDTO.IsExpanded = false;
                    });
                    _notificationService.ShowInfo("No hay bancos registrados");
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
                string query = GetLoadBankAccountsQuery();
                dynamic variables = new ExpandoObject();
                variables.bankAccountsPagePagination = new ExpandoObject();
                variables.bankAccountsPagePagination.pageSize = -1;
                variables.bankAccountsPageFilters = new ExpandoObject();
                variables.bankAccountsPageFilters.bankId = bank.Id;

                PageType<BankAccountGraphQLModel> result = await _bankAccountService.GetPageAsync(query, variables);
                var bankAccounts = Context.AutoMapper.Map<ObservableCollection<TreasuryBankAccountMasterTreeDTO>>(result.Entries);

                if (bankAccounts.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (TreasuryBankAccountMasterTreeDTO bankAccount in bankAccounts)
                        {
                            bankAccount.Context = this;
                            bank.BankAccounts.Add(bankAccount);
                        }
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        bank.IsExpanded = false;
                    });
                    _notificationService.ShowInfo("Este banco no tiene cuentas bancarias registradas");
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadCostCenters" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
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

                string query = GetLoadAuxiliaryCashDrawersQuery();

                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.pageSize = -1;
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.parentId = majorCashDrawer.Id;
                variables.pageResponseFilters.isPettyCash = false;

                PageType<CashDrawerGraphQLModel> result = await _cashDrawerService.GetPageAsync(query, variables);
                var cashDrawers = Context.AutoMapper.Map<ObservableCollection<TreasuryAuxiliaryCashDrawerMasterTreeDTO>>(result.Entries);

                if (cashDrawers.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawer in cashDrawers)
                        {
                            majorCashDrawer.AuxiliaryCashDrawers.Add(auxiliaryCashDrawer);
                        }
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        majorCashDrawer.IsExpanded = false;
                    });
                    _notificationService.ShowInfo("Esta caja general no tiene cajas auxiliares registradas");
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
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadAuxiliaryCashDrawers" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
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
                string query = GetLoadFranchisesQuery();
                dynamic variables = new ExpandoObject();
                variables.franchisesPagePagination = new ExpandoObject();
                variables.franchisesPagePagination.pageSize = -1;

                PageType<FranchiseGraphQLModel> result = await _franchiseService.GetPageAsync(query, variables);
                var franchises = Context.AutoMapper.Map<ObservableCollection<TreasuryFranchiseMasterTreeDTO>>(result.Entries);
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
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        franchiseDummyDTO.IsExpanded = false;
                    });
                    _notificationService.ShowInfo("No hay franquicias registradas");
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
                // Ensure all caches are loaded
                await Task.WhenAll(
                    _auxiliaryAccountingAccountCache.EnsureLoadedAsync(),
                    _costCenterCache.EnsureLoadedAsync(),
                    _bankAccountCache.EnsureLoadedAsync(),
                    _majorCashDrawerCache.EnsureLoadedAsync()
                );

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
            IRepository<CompanyLocationGraphQLModel> companyLocationService,
            IRepository<CostCenterGraphQLModel> costCenterService,
            IRepository<CashDrawerGraphQLModel> cashDrawerService,
            IRepository<BankGraphQLModel> bankService,
            IRepository<BankAccountGraphQLModel> bankAccountService,
            IRepository<FranchiseGraphQLModel> franchiseService,
            Helpers.IDialogService dialogService,
            Helpers.Services.INotificationService notificationService,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            CostCenterCache costCenterCache,
            BankAccountCache bankAccountCache,
            MajorCashDrawerCache majorCashDrawerCache)
        {
            _companyLocationService = companyLocationService;
            _costCenterService = costCenterService;
            _cashDrawerService = cashDrawerService;
            _bankService = bankService;
            _bankAccountService = bankAccountService;
            _franchiseService = franchiseService;
            _dialogService = dialogService;
            _notificationService = notificationService;
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache;
            _costCenterCache = costCenterCache;
            _bankAccountCache = bankAccountCache;
            _majorCashDrawerCache = majorCashDrawerCache;

            DummyItems = [
            new CashDrawerDummyDTO() {
                Id = 1, Name = "CAJA GENERAL", Type = CashDrawerType.Major,
                Locations = [new CashDrawerCompanyLocationDTO() { IsDummyChild = true, Name = "Dummy", Type = CashDrawerType.Major }],
                Context = this
            },
            new CashDrawerDummyDTO() {
                Id = 2, Name = "CAJA MENOR", Type = CashDrawerType.Minor,
                Locations = [new CashDrawerCompanyLocationDTO() { IsDummyChild = true, Name = "Dummy", Type = CashDrawerType.Minor }],
                Context = this
            },
            new BankDummyDTO(){
                Id = 3, Name = "BANCOS", Banks = [new TreasuryBankMasterTreeDTO() { IsDummyChild = true }], Context = this
            },
            new FranchiseDummyDTO(){
                Id = 4, Name = "FRANQUICIAS", Franchises = [new TreasuryFranchiseMasterTreeDTO() { IsDummyChild = true, Name = "Dummy"}], Context = this
            }
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

            //caja general
            if (createdCashDrawer.IsPettyCash == false && createdCashDrawer.Parent is null)
            {
                MajorCashDrawerMasterTreeDTO majorCashDrawerMasterTreeDTO = Context.AutoMapper.Map<MajorCashDrawerMasterTreeDTO>(createdCashDrawer);
                CashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Major) ?? throw new Exception("");
                if (majorCashDrawerDummyDTO is null) return;
                CashDrawerCompanyLocationDTO majorCashDrawerCompanyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == createdCashDrawer.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
                if (majorCashDrawerCompanyLocation is null) return;
                CashDrawerCostCenterDTO majorCashDrawerCostCenter = majorCashDrawerCompanyLocation.CostCenters.FirstOrDefault(x => x.Id == createdCashDrawer.CostCenter.Id) ?? throw new Exception("");
                if (majorCashDrawerCostCenter is null) return;
                if (!majorCashDrawerCostCenter.IsExpanded && majorCashDrawerCostCenter.CashDrawers.Count > 0 && majorCashDrawerCostCenter.CashDrawers[0].IsDummyChild)
                {
                    await LoadCashDrawers(majorCashDrawerCostCenter);
                    majorCashDrawerCostCenter.IsExpanded = true;
                    MajorCashDrawerMasterTreeDTO? majorCashDrawer = majorCashDrawerCostCenter.CashDrawers.OfType<MajorCashDrawerMasterTreeDTO>().FirstOrDefault(x => x.Id == majorCashDrawerMasterTreeDTO.Id);
                    if (majorCashDrawer is null) return;
                    SelectedItem = majorCashDrawer;
                    _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
                    return;
                }
                if (!majorCashDrawerCostCenter.IsExpanded)
                {
                    majorCashDrawerCostCenter.IsExpanded = true;
                    majorCashDrawerCostCenter.CashDrawers.Add(majorCashDrawerMasterTreeDTO);
                    SelectedItem = majorCashDrawerMasterTreeDTO;
                    _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
                    return;
                }
                majorCashDrawerCostCenter.CashDrawers.Add(majorCashDrawerMasterTreeDTO);
                SelectedItem = majorCashDrawerMasterTreeDTO;
                _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
                return;
            }
            //caja auxiliar
            if (createdCashDrawer.IsPettyCash == false && createdCashDrawer.Parent != null)
            {
                TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawerMasterTreeDTO = Context.AutoMapper.Map<TreasuryAuxiliaryCashDrawerMasterTreeDTO>(createdCashDrawer);
                CashDrawerDummyDTO majorCashDrawerDummyDTO = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Major) ?? throw new Exception("");
                if (majorCashDrawerDummyDTO is null) return;
                CashDrawerCompanyLocationDTO majorCashDrawerCompanyLocation = majorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == createdCashDrawer.Parent.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
                if (majorCashDrawerCompanyLocation is null) return;
                CashDrawerCostCenterDTO majorCashDrawerCostCenter = majorCashDrawerCompanyLocation.CostCenters.FirstOrDefault(x => x.Id == createdCashDrawer.Parent.CostCenter.Id) ?? throw new Exception("");
                if (majorCashDrawerCostCenter is null) return;
                MajorCashDrawerMasterTreeDTO majorCashDrawer = majorCashDrawerCostCenter.CashDrawers.OfType<MajorCashDrawerMasterTreeDTO>().FirstOrDefault(x => x.Id == createdCashDrawer.Parent.Id) ?? throw new Exception("");
                if (majorCashDrawer == null) return;
                if (!majorCashDrawer.IsExpanded && majorCashDrawer.AuxiliaryCashDrawers.Count > 0 && majorCashDrawer.AuxiliaryCashDrawers[0].IsDummyChild)
                {
                    await LoadAuxiliaryCashDrawers(majorCashDrawer);
                    majorCashDrawer.IsExpanded = true;
                    TreasuryAuxiliaryCashDrawerMasterTreeDTO? auxiliaryCashDrawer = majorCashDrawer.AuxiliaryCashDrawers.FirstOrDefault(x => x.Id == auxiliaryCashDrawerMasterTreeDTO.Id);
                    if (auxiliaryCashDrawerMasterTreeDTO is null) return;
                    SelectedItem = auxiliaryCashDrawer;
                    _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
                    return;
                }
                if (!majorCashDrawer.IsExpanded)
                {
                    majorCashDrawer.IsExpanded = true;
                    majorCashDrawer.AuxiliaryCashDrawers.Add(auxiliaryCashDrawerMasterTreeDTO);
                    SelectedItem = auxiliaryCashDrawerMasterTreeDTO;
                    _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
                    return;
                }
                majorCashDrawer.AuxiliaryCashDrawers.Add(auxiliaryCashDrawerMasterTreeDTO);
                SelectedItem = auxiliaryCashDrawerMasterTreeDTO;
                _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
                return;
            }

            //caja menor
            MinorCashDrawerMasterTreeDTO minorCashDrawerMasterTreeDTO = Context.AutoMapper.Map<MinorCashDrawerMasterTreeDTO>(createdCashDrawer);
            CashDrawerDummyDTO minorCashDrawerDummyDTO = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Minor) ?? throw new Exception("");
            if (minorCashDrawerDummyDTO is null) return;
            CashDrawerCompanyLocationDTO minorCashDrawerCompanyLocation = minorCashDrawerDummyDTO.Locations.FirstOrDefault(x => x.Id == createdCashDrawer.CostCenter.CompanyLocation.Id) ?? throw new Exception("");
            if (minorCashDrawerCompanyLocation is null) return;
            CashDrawerCostCenterDTO minorCashDrawerCostCenter = minorCashDrawerCompanyLocation.CostCenters.FirstOrDefault(x => x.Id == createdCashDrawer.CostCenter.Id) ?? throw new Exception("");
            if (minorCashDrawerCostCenter is null) return;
            if (!minorCashDrawerCostCenter.IsExpanded && minorCashDrawerCostCenter.CashDrawers.Count > 0 && minorCashDrawerCostCenter.CashDrawers[0].IsDummyChild)
            {
                await LoadCashDrawers(minorCashDrawerCostCenter);
                minorCashDrawerCostCenter.IsExpanded = true;
                MinorCashDrawerMasterTreeDTO? minorCasDrawer = minorCashDrawerCostCenter.CashDrawers.OfType<MinorCashDrawerMasterTreeDTO>().FirstOrDefault(x => x.Id == minorCashDrawerMasterTreeDTO.Id);
                if (minorCasDrawer is null) return;
                SelectedItem = minorCasDrawer;
                _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
                return;
            }
            if (!minorCashDrawerCostCenter.IsExpanded)
            {
                minorCashDrawerCostCenter.IsExpanded = true;
                minorCashDrawerCostCenter.CashDrawers.Add(minorCashDrawerMasterTreeDTO);
                SelectedItem = minorCashDrawerMasterTreeDTO;
                _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
                return;
            }
            minorCashDrawerCostCenter.CashDrawers.Add(minorCashDrawerMasterTreeDTO);
            SelectedItem = minorCashDrawerMasterTreeDTO;
            _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
            return;
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

        public async Task HandleAsync(BankCreateMessage message, CancellationToken cancellationToken)
        {
            var createdBank = message.CreatedBank.Entity;
            IsNewRecord = false;

            TreasuryBankMasterTreeDTO bankDTO = Context.AutoMapper.Map<TreasuryBankMasterTreeDTO>(createdBank);
            BankDummyDTO bankDummyDTO = DummyItems.FirstOrDefault(x => x is BankDummyDTO) as BankDummyDTO ?? throw new Exception("");
            if (bankDummyDTO is null) return;
            if (!bankDummyDTO.IsExpanded && bankDummyDTO.Banks.Count > 0 && bankDummyDTO.Banks[0].IsDummyChild)
            {
                await LoadBanks();
                bankDummyDTO.IsExpanded = true;
                TreasuryBankMasterTreeDTO? bank = bankDummyDTO.Banks.FirstOrDefault(x => x.Id == bankDTO.Id);
                if (bank is null) return;
                SelectedItem = bank;
                _notificationService.ShowSuccess(message.CreatedBank.Message);
                return;
            }
            if (!bankDummyDTO.IsExpanded)
            {
                bankDummyDTO.IsExpanded = true;
                bankDummyDTO.Banks.Add(bankDTO);
                SelectedItem = bankDTO;
                _notificationService.ShowSuccess(message.CreatedBank.Message);
                return;
            }
            bankDummyDTO.Banks.Add(bankDTO);
            SelectedItem = bankDTO;
            _notificationService.ShowSuccess(message.CreatedBank.Message);
            return;

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
            BankDummyDTO bankDummyDTO = DummyItems.FirstOrDefault(x => x is BankDummyDTO) as BankDummyDTO ?? throw new Exception("");
            if (bankDummyDTO is null) return;
            TreasuryBankMasterTreeDTO bankDTO = bankDummyDTO.Banks.FirstOrDefault(x => x.Id == createdBankAccount.Bank.Id) ?? throw new Exception("");
            if (bankDTO is null) return;
            if (!bankDTO.IsExpanded && bankDTO.BankAccounts.Count > 0 && bankDTO.BankAccounts[0].IsDummyChild)
            {
                await LoadBankAccounts(bankDTO);
                bankDTO.IsExpanded = true;
                TreasuryBankAccountMasterTreeDTO? bankAccount = bankDTO.BankAccounts.FirstOrDefault(x => x.Id == bankAccountDTO.Id);
                if (bankAccount is null) return;
                SelectedItem = bankAccount;
                _notificationService.ShowSuccess(message.CreatedBankAccount.Message);
                return;
            }
            if (!bankDTO.IsExpanded)
            {
                bankDTO.IsExpanded = true;
                bankDTO.BankAccounts.Add(bankAccountDTO);
                SelectedItem = bankAccountDTO;
                _notificationService.ShowSuccess(message.CreatedBankAccount.Message);
                return;
            }
            bankDTO.BankAccounts.Add(bankAccountDTO);
            SelectedItem = bankAccountDTO;
            _notificationService.ShowSuccess(message.CreatedBankAccount.Message);
            return;
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

        public async Task HandleAsync(FranchiseCreateMessage message, CancellationToken cancellationToken)
        {
            var createdFranchise = message.CreatedFranchise.Entity;
            IsNewRecord = false;
            TreasuryFranchiseMasterTreeDTO franchiseDTO = Context.AutoMapper.Map<TreasuryFranchiseMasterTreeDTO>(createdFranchise);
            FranchiseDummyDTO franchiseDummyDTO = DummyItems.FirstOrDefault(x => x is FranchiseDummyDTO) as FranchiseDummyDTO ?? throw new Exception("");
            if (franchiseDummyDTO is null) return;
            if (!franchiseDummyDTO.IsExpanded && franchiseDummyDTO.Franchises.Count > 0 && franchiseDummyDTO.Franchises[0].IsDummyChild)
            {
                await LoadFranchises(franchiseDummyDTO);
                franchiseDummyDTO.IsExpanded = true;
                TreasuryFranchiseMasterTreeDTO? franchise = franchiseDummyDTO.Franchises.FirstOrDefault(x => x.Id == franchiseDTO.Id);
                if (franchise is null) return;
                SelectedItem = franchise;
                _notificationService.ShowSuccess(message.CreatedFranchise.Message);
                return;
            }
            if (!franchiseDummyDTO.IsExpanded)
            {
                franchiseDummyDTO.IsExpanded = true;
                franchiseDummyDTO.Franchises.Add(franchiseDTO);
                SelectedItem = franchiseDTO;
                _notificationService.ShowSuccess(message.CreatedFranchise.Message);
                return;
            }
            franchiseDummyDTO.Franchises.Add(franchiseDTO);
            SelectedItem = franchiseDTO;
            _notificationService.ShowSuccess(message.CreatedFranchise.Message);
            return;
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
