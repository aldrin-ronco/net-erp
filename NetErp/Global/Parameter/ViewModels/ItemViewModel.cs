using AutoMapper;
using Caliburn.Micro;
using DevExpress.Mvvm;
using Dictionaries;
using Models.Billing;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Billing.PriceList.ViewModels;
using NetErp.Global.Modals.ViewModels;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.ViewModels;
using Ninject.Activation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Global.Parameter.ViewModels
{
    public class ItemViewModel : Screen
    {

        Helpers.IDialogService _dialogService = IoC.Get<Helpers.IDialogService>();
        public ItemViewModel()
        {
            Messenger.Default.Register<ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel>>(this, SearchWithTwoColumnsGridMessageToken.CompanyAccountingEntity, false, OnFindCompanyAccountingEntityMessage);
        }
        public void OnFindCompanyAccountingEntityMessage(ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel> message)
        {
            if (message.ReturnedData is null) return;
            if (Code == "SearchCompany")
            {
                Value = message.ReturnedData.Id.ToString();
                SelectedOption = new Qualifier { Id = message.ReturnedData.Id, Name = message.ReturnedData.SearchName };

            }


        }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int ModuleId { get; set; }
        public int DatatypeId { get; set; }

        private ObservableCollection<Qualifier> _qualifiers { get; set; } = [];
        public ObservableCollection<Qualifier> Qualifiers
        {
            get { return _qualifiers; }
            set
            {
                if (_qualifiers != value)
                {
                    _qualifiers = value;
                    if (Qualifiers.Count > 0) 
                    { Qualifiers[0].IsChecked = true;
                        SelectedOption = Qualifiers[0];
                    }
                    NotifyOfPropertyChange(nameof(Qualifiers));
                   
                }
            }
        } 
        
       
        private Qualifier _selectedOption;
        public Qualifier SelectedOption
        {
            get => _selectedOption;
            set
            {
                _selectedOption = value;
                Value = value.Id.ToString();
                NotifyOfPropertyChange(() => SelectedOption);
            }
        }

        private Datatype _datatype;
        public Datatype Datatype
        {
            get { return _datatype; }
            set
            {
                if (_datatype != value)
                {
                    _datatype = value;
                    NotifyOfPropertyChange(nameof(AlphanumericVisibility));
                    NotifyOfPropertyChange(nameof(IntegerVisibility));
                    NotifyOfPropertyChange(nameof(DecimalVisibility));
                    NotifyOfPropertyChange(nameof(DateVisibility));
                    NotifyOfPropertyChange(nameof(ListVisibility));
                    NotifyOfPropertyChange(nameof(BoolVisibility));

                }
            }
        }

        public async void Execute()
        {
            switch (Code)
            {
                case "CostCenter":
                    var viewModel = new CreatePriceListModalViewModel<PriceListGraphQLModel>(_dialogService);
                    await viewModel.InitializeAsync();
                    await _dialogService.ShowDialogAsync(viewModel, "Creación de lista de precios");
                    break;

                case "SearchCompany":
                SearchCompanyAccountingEntityCompany(null);
                    break;
                default:
                    break;
            }
           
        }
        private Visibility _modalVisibility;
        public Visibility ModalVisibility
        {
            get { return (Datatype.Name == DatatypeEnum.MODAL) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _modalVisibility = value;
            }
        }
        private Visibility _alphanumericVisibility;
        public Visibility AlphanumericVisibility
        {
            get { return (Datatype.Name == DatatypeEnum.ALFANUMERICO ) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _alphanumericVisibility = value;
            }
        }
        private Visibility _radioVisibility;
        public Visibility RadioVisibility
        {
            get { return (Datatype.Name == DatatypeEnum.RADIO) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _radioVisibility = value;
            }
        }
        private Visibility _integerVisibility;
        public Visibility IntegerVisibility
        {
            get { return (Datatype.Name == DatatypeEnum.ENTERO) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _integerVisibility = value;
            }
        }

        private Visibility _decimalVisibility;
        public Visibility DecimalVisibility
        {
            get { return (Datatype.Name == DatatypeEnum.DECIMAL) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _decimalVisibility = value;
            }
        }

        private Visibility _dateVisibility;
        public Visibility DateVisibility
        {
            get { return (Datatype.Name == DatatypeEnum.FECHA) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _dateVisibility = value;
            }
        }

        private Visibility _listVisibility;
        public Visibility ListVisibility
        {
            get { return (Datatype.Name == DatatypeEnum.LISTA) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _listVisibility = value;
            }
        }

        private Visibility _boolVisibility;
        public Visibility BoolVisibility
        {
            get { return (Datatype.Name == DatatypeEnum.BOOLEAN) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _boolVisibility = value;
            }
        }

        //Prueba con modales

        public async void SearchProducts(object p)
        {
           /* string query = @"query($filter: ItemFilterInput){
                            PageResponse: itemPage(filter: $filter){
                            count
                            rows{
                                id
                                name
                                code
                                reference
                                allowFraction
                                measurementUnit{
                                id
                                name
                                }
                                subCategory{
                                    id
                                    itemCategory{
                                        id
                                        itemType{
                                            id
                                        }
                                    }
                                }
                            }
                            }
                        }";

            string fieldHeader1 = "Código";
            string fieldHeader2 = "Nombre";
            string fieldHeader3 = "Referencia";
            string fieldData1 = "Code";
            string fieldData2 = "Name";
            string fieldData3 = "Reference";
            dynamic variables = new ExpandoObject();
            variables.filter = new ExpandoObject();
            variables.filter.and = new ExpandoObject[]
            {
                new(),
                new()
            };
            variables.filter.and[0].catalogId = new ExpandoObject();
            variables.filter.and[0].catalogId.@operator = "=";
            variables.filter.and[0].catalogId.value = 1;
            var viewModel = new SearchItemModalViewModel<ItemDTO, ItemGraphQLModel>(query, fieldHeader1, fieldHeader2, fieldHeader3, fieldData1, fieldData2, fieldData3, variables, MessageToken.SearchProduct, null, _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Búsqueda de productos");*/

         

        }
        public async void SearchCompanyAccountingEntityCompany(object p)
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
            var viewModel = new SearchWithTwoColumnsGridViewModel<AccountingEntityGraphQLModel>(query, fieldHeader1, fieldHeader2, fieldData1, fieldData2, null, SearchWithTwoColumnsGridMessageToken.CompanyAccountingEntity, _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Búsqueda de terceros");
        }
        public bool CanOpenSearchProducts(object p) => true;
    }


    public class QualifierScreen : Screen
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int QualifierTypeId { get; set; }
    }


    

    }
