using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Global;
using NetErp.Billing.PriceList.DTO;
using NetErp.Helpers;
using Ninject.Activation;
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

namespace NetErp.Billing.PriceList.ViewModels
{
    public class UpdatePriceListModalViewModel<TModel>: Screen
    {
        private readonly Helpers.IDialogService _dialogService;
        private readonly IMapper _autoMapper;
        private IGenericDataAccess<PriceListGraphQLModel> PriceListService { get; set; } = IoC.Get<IGenericDataAccess<PriceListGraphQLModel>>();
        private IGenericDataAccess<StorageGraphQLModel> StorageService { get; set; } = IoC.Get<IGenericDataAccess<StorageGraphQLModel>>();

        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                    _ = this.SetFocus(nameof(Name));
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

        private bool _editablePrice;

        public bool EditablePrice
        {
            get { return _editablePrice; }
            set 
            {
                if (_editablePrice != value)
                {
                    _editablePrice = value;
                    NotifyOfPropertyChange(nameof(EditablePrice));
                }
            }
        }

        private bool _autoApplyDiscount;

        public bool AutoApplyDiscount
        {
            get { return _autoApplyDiscount; }
            set 
            {
                if (_autoApplyDiscount != value)
                {
                    _autoApplyDiscount = value;
                    NotifyOfPropertyChange(nameof(AutoApplyDiscount));
                }
            }
        }

        private bool _isPublic;

        public bool IsPublic
        {
            get { return _isPublic; }
            set 
            {
                if (_isPublic != value)
                {
                    _isPublic = value;
                    NotifyOfPropertyChange(nameof(IsPublic));
                }
            }
        }

        private bool _isActive;

        public new bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));
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

        private ObservableCollection<PaymentMethodPriceListDTO> _paymentMethods;

        public ObservableCollection<PaymentMethodPriceListDTO> PaymentMethods
        {
            get { return _paymentMethods; }
            set
            {
                if (_paymentMethods != value)
                {
                    _paymentMethods = value;
                    NotifyOfPropertyChange(nameof(PaymentMethods));
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


        public Dictionary<string, string> ListUpdateBehaviorOnCostChange { get; set; } = new()
        {
            { "UPDATE_PROFIT_MARGIN", "Actualizar margen de utilidad" },
            { "UPDATE_PRICE", "Actualizar precio de venta" },
        };

        public string SelectedListUpdateBehaviorOnCostChange { get; set; } 

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

        public int SelectedPriceListId { get; set; }

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
                List<int> paymentMethodsIds = [];
                foreach (var item in PaymentMethods)
                {
                    if (!item.IsChecked) paymentMethodsIds.Add(item.Id);
                }

                string query = @"
                    mutation ($data: UpdatePriceListInput!, $id: Int!) {
                      UpdateResponse: updatePriceList(data: $data, id: $id) {
                        id
                        name
                        editablePrice
                        isActive
                        autoApplyDiscount
                        isPublic
                        allowNewUsersAccess
                        listUpdateBehaviorOnCostChange
                        isTaxable
                        priceListIncludeTax
                        useAlternativeFormula
                        parent{
                            id
                            name
                        }
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
                variables.Id = SelectedPriceListId;
                variables.Data = new ExpandoObject();
                variables.Data.Name = Name.Trim().RemoveExtraSpaces();
                variables.Data.EditablePrice = EditablePrice;
                variables.Data.IsActive = IsActive;
                variables.Data.AutoApplyDiscount = AutoApplyDiscount;
                variables.Data.IsPublic = IsPublic;
                variables.Data.AllowNewUsersAccess = true; //TODO
                variables.Data.ListUpdateBehaviorOnCostChange = SelectedListUpdateBehaviorOnCostChange;
                variables.Data.ParentId = 0; //static value
                variables.Data.StartDate = null; //static value
                variables.Data.EndDate = null; //static value
                variables.Data.IsTaxable = IsTaxable;
                variables.Data.PriceListIncludeTax = IsTaxable && PriceListIncludeTax;
                variables.Data.UseAlternativeFormula = UseAlternativeFormula;
                variables.Data.StorageId = SelectedStorage.Id;
                variables.Data.PaymentMethodsIds = paymentMethodsIds;
                var result = await PriceListService.Update(query, variables);

                Messenger.Default.Send(message: new ReturnedDataFromUpdatePriceListModalViewMessage<TModel>() { ReturnedData = result }, token: "UpdatePriceList");
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

        public void RefreshCostCenters()
        {
            ShadowCostCenters = [.. CostCenters.Where(x => x.IsTaxable == IsTaxable && x.PriceListIncludeTax == PriceListIncludeTax)];
        }

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
                      paymentMethods {
                        id
                        abbreviation
                        name
                      }
                    }
                ";
                var result = await StorageService.GetDataContext<InitializeDataContext>(query, new { });
                Storages = [.. result.Storages];
                CostCenters = [.. result.CostCenters];
                RefreshCostCenters();
                Storages.Insert(0, new StorageGraphQLModel { Id = 0, Name = "COSTO PROMEDIO" });
                PaymentMethods = [.. _autoMapper.Map<ObservableCollection<PaymentMethodPriceListDTO>>(result.PaymentMethods)];
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


        public UpdatePriceListModalViewModel(Helpers.IDialogService dialogService, IMapper autoMapper)
        {
            _dialogService = dialogService;
            _autoMapper = autoMapper;
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(() => Name)), DispatcherPriority.Render);
        }
    }

    public class ReturnedDataFromUpdatePriceListModalViewMessage<TModel>
    {
        public TModel? ReturnedData { get; set; }
    }
}
