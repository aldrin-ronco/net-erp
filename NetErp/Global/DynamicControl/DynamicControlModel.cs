using Caliburn.Micro;
using DevExpress.Mvvm;
using Dictionaries;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Billing.PriceList.ViewModels;
using NetErp.Global.Modals.ViewModels;
using System.Collections.ObjectModel;
using System.Dynamic;


namespace NetErp.Global.DynamicControl
{
    public class DynamicControlModel : Screen
    {

        Helpers.IDialogService _dialogService = IoC.Get<Helpers.IDialogService>();
        public DynamicControlModel()
        {
            Messenger.Default.Register<ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel>>(this, SearchWithTwoColumnsGridMessageToken.CompanyAccountingEntity, false, OnFindCompanyAccountingEntityMessage);
        }
        public void OnFindCompanyAccountingEntityMessage(ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel> message)
        {
            if (message.ReturnedData is null) return;
            if (Code == "SearchCompany")
            {
                Value = message.ReturnedData.Id;
                SelectedOption = new Qualifier { Id = message.ReturnedData.Id, Name = message.ReturnedData.SearchName };

            }


        }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public dynamic Value { get; set; } 
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
                    {
                        Qualifiers[0].IsChecked = true;
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
                if ((Datatype?.Name == DatatypeEnum.LISTA)) {
                    Value = value.Id.ToString();
                }
               
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
            get { return (Datatype.Name == DatatypeEnum.ALFANUMERICO) ? Visibility.Visible : Visibility.Collapsed; }
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



}

