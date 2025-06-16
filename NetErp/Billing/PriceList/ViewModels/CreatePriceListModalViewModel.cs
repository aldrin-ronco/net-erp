using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Global;
using NetErp.Billing.PriceList.DTO;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Inventory.CatalogItems.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;
using Xceed.Wpf.Toolkit.Primitives;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class CreatePriceListModalViewModel<TModel>: Screen
    {

        private readonly Helpers.IDialogService _dialogService;

        private IGenericDataAccess<PriceListGraphQLModel> PriceListService { get; set; } = IoC.Get<IGenericDataAccess<PriceListGraphQLModel>>();
        private IGenericDataAccess<StorageGraphQLModel> StorageService { get; set; } = IoC.Get<IGenericDataAccess<StorageGraphQLModel>>();

        private string _name = string.Empty;

        public string Name
        {
            get { return _name; }
            set 
            {
                if (_name != value) 
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                }
            }
        }

        private bool _isTaxable;

        public bool IsTaxable
        {
            get { return _isTaxable; }
            set 
            {
                if (_isTaxable != value) 
                {
                    _isTaxable = value;
                    NotifyOfPropertyChange(nameof(IsTaxable));
                    if (value is false) PriceListIncludeTax = false;
                    RefreshCostCenters();
                }
            }
        }

        private bool _priceListIncludeTax;

        public bool PriceListIncludeTax
        {
            get { return _priceListIncludeTax; }
            set 
            {
                if (_priceListIncludeTax != value)
                {
                    _priceListIncludeTax = value;
                    NotifyOfPropertyChange(nameof(PriceListIncludeTax));
                    RefreshCostCenters();
                }
            }
        }

        private bool _useAlternativeFormula;

        public bool UseAlternativeFormula
        {
            get { return _useAlternativeFormula; }
            set 
            {
                if (_useAlternativeFormula != value)
                {
                    _useAlternativeFormula = value;
                    NotifyOfPropertyChange(nameof(UseAlternativeFormula));
                }
            }
        }

        private ObservableCollection<StorageGraphQLModel> _storages;

        public ObservableCollection<StorageGraphQLModel> Storages
        {
            get { return _storages; }
            set 
            {
                if (_storages != value)
                {
                    _storages = value;
                    NotifyOfPropertyChange(nameof(Storages));
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

        private ObservableCollection<CostCenterGraphQLModel> _shadowCostCenters;

        public ObservableCollection<CostCenterGraphQLModel> ShadowCostCenters
        {
            get { return _shadowCostCenters; }
            set 
            {
                if (_shadowCostCenters != value)
                {
                    _shadowCostCenters = value;
                    NotifyOfPropertyChange(nameof(ShadowCostCenters));
                }
            }
        }



        private StorageGraphQLModel _selectedStorage;

        public StorageGraphQLModel SelectedStorage
        {
            get { return _selectedStorage; }
            set 
            {
                if (_selectedStorage != value)
                {
                    _selectedStorage = value;
                    NotifyOfPropertyChange(nameof(SelectedStorage));
                }
            }
        }

        private string _selectedFormula = "D";

        public string SelectedFormula
        {
            get { return _selectedFormula; }
            set
            {
                if (_selectedFormula != value)
                {
                    _selectedFormula = value;
                    NotifyOfPropertyChange(nameof(SelectedFormula));
                    NotifyOfPropertyChange(nameof(Formula));
                }
            }
        }

        public string Formula
        {
            get
            {
                if (SelectedFormula == "D") return "COSTO / (1 - (MARGEN_UTILIDAD/100))";
                return "(COSTO * (1 + (MARGEN_IMPUESTO / 100))) / (1 - (MARGEN_UTILIDAD / 100))";
            }
        }

        public void RefreshCostCenters()
        {
            ShadowCostCenters = [.. CostCenters.Where(x => x.IsTaxable == IsTaxable && x.PriceListIncludeTax == PriceListIncludeTax)];
        }


        private ICommand _saveCommand;

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                string query = @"
                    mutation ($data: CreatePriceListInput!) {
                      CreateResponse: createPriceList(data: $data) {
                        id
                        name
                        editablePrice
                        isActive
                        autoApplyDiscount
                        isPublic
                        allowNewUsersAccess
                        listUpdateBehaviorOnCostChange
                        parent{
                            id
                            name
                        }
                        isTaxable
                        priceListIncludeTax
                        useAlternativeFormula
                        storage {
                          id
                          name
                        }
                        paymentMethods{
                          id
                          name
                          abbreviation
                        }
                      }
                    }
                    ";
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.Name = Name.Trim().RemoveExtraSpaces(); //capture the name from the UI
                variables.Data.EditablePrice = true; //static value
                variables.Data.IsActive = true; //static value
                variables.Data.AutoApplyDiscount = true; //static value
                variables.Data.IsPublic = true; //static value
                variables.Data.AllowNewUsersAccess = true; //static value
                variables.Data.ListUpdateBehaviorOnCostChange = "UPDATE_PROFIT_MARGIN"; //static value
                variables.Data.ParentId = 0; //static value
                variables.Data.StartDate = null; //static value
                variables.Data.EndDate = null; //static value
                variables.Data.IsTaxable = IsTaxable; //capture the value from the UI
                variables.Data.PriceListIncludeTax = IsTaxable && PriceListIncludeTax; //capture the value from the UI
                variables.Data.UseAlternativeFormula = UseAlternativeFormula; //capture the value from the UI
                variables.Data.StorageId = SelectedStorage.Id; //capture the value from the UI
                var result = await PriceListService.Create(query, variables);

                Messenger.Default.Send(message: new ReturnedDataFromCreatePriceListModalViewMessage<TModel>() { ReturnedData = result }, token: "CreatePriceList");
                await _dialogService.CloseDialogAsync(this, true);
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        //TODO:  refactorizar mensaje de error
        public async Task InitializeAsync()
        {
            try
            {
                string query = @"
                    query {
                      storages {
                        id
                        name
                      }
                      costCenters {
                        id
                        name
                        isTaxable
                        priceListIncludeTax
                      }
                    }
                ";
                var result = await StorageService.GetDataContext<InitializeDataContext>(query, new { });
                Storages = [.. result.Storages];
                CostCenters = [.. result.CostCenters];
                RefreshCostCenters();
                Storages.Insert(0, new StorageGraphQLModel { Id = 0, Name = "COSTO PROMEDIO" });
                SelectedStorage = Storages.FirstOrDefault(x => x.Id == 0) ?? throw new Exception("Invalid null reference");
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        private bool _nameFocus;
        public bool NameFocus
        {
            get { return _nameFocus; }
            set
            {
                _nameFocus = value;
                NotifyOfPropertyChange(nameof(NameFocus));
            }
        }

        void SetFocus(Expression<Func<object>> propertyExpression)
        {
            string controlName = propertyExpression.GetMemberInfo().Name;
            NameFocus = false;

            NameFocus = controlName == nameof(Name);
        }



        private ICommand _cancelCommand;

        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null) _cancelCommand = new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        public async Task CancelAsync()
        {
            await _dialogService.CloseDialogAsync(this, true);
        }


        public CreatePriceListModalViewModel(Helpers.IDialogService dialogService) 
        {
            _dialogService = dialogService;
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(() => Name)), DispatcherPriority.Render);
        }
    }

    public class InitializeDataContext
    {
        public IEnumerable<StorageGraphQLModel> Storages { get; set; } = [];
        public IEnumerable<CostCenterGraphQLModel> CostCenters { get; set; } = [];
        public IEnumerable<PaymentMethodGraphQLModel> PaymentMethods { get; set; } = [];
    }

    public class ReturnedDataFromCreatePriceListModalViewMessage<TModel>
    {
        public TModel? ReturnedData { get; set; }
    }
}
