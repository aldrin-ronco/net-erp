using Caliburn.Micro;
using Common.Helpers;
using Dictionaries;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.DTO.Global;
using Models.Global;
using NetErp.Helpers;
using System;
using Common.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Core;


namespace NetErp.Billing.Sellers.ViewModels
{
    public class SellerDetailViewModel : Screen, INotifyDataErrorInfo
    {

        #region Commands

        private ICommand _deleteMailCommand;
        public ICommand DeleteMailCommand
        {
            get
            {
                if (_deleteMailCommand == null) _deleteMailCommand = new RelayCommand(CanRemoveEmail, RemoveEmail);
                return _deleteMailCommand;
            }
        }

        #endregion

        #region Properties

        Dictionary<string, List<string>> _errors;

        public SellerViewModel Context { get; private set; }

        private string _firstName = string.Empty;
        public string FirstName
        {
            get => _firstName;
            set
            {
                if (_firstName != value)
                {
                    _firstName = value;
                    ValidateProperty(nameof(FirstName), value);
                    NotifyOfPropertyChange(nameof(FirstName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _middleName = string.Empty;
        public string MiddleName
        {
            get => _middleName;
            set
            {
                if (_middleName != value)
                {
                    _middleName = value;
                    NotifyOfPropertyChange(nameof(MiddleName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _firstLastName = string.Empty;
        public string FirstLastName
        {
            get => _firstLastName;
            set
            {
                if (_firstLastName != value)
                {
                    _firstLastName = value;
                    ValidateProperty(nameof(FirstLastName), value);
                    NotifyOfPropertyChange(nameof(FirstLastName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _middleLastName = string.Empty;
        public string MiddleLastName
        {
            get => _middleLastName;
            set
            {
                if (_middleLastName != value)
                {
                    _middleLastName = value;
                    NotifyOfPropertyChange(nameof(MiddleLastName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _phone1 = string.Empty;
        public string Phone1
        {
            get => _phone1;
            set
            {
                if (_phone1 != value)
                {
                    _phone1 = value;
                    ValidateProperty(nameof(Phone1), value);
                    NotifyOfPropertyChange(nameof(Phone1));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _phone2 = string.Empty;
        public string Phone2
        {
            get => _phone2;
            set
            {
                if (_phone2 != value)
                {
                    _phone2 = value;
                    ValidateProperty(nameof(Phone2), value);
                    NotifyOfPropertyChange(nameof(Phone2));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _cellPhone1 = string.Empty;
        public string CellPhone1
        {
            get => _cellPhone1;
            set
            {
                _cellPhone1 = value;
                ValidateProperty(nameof(CellPhone1), value);
                NotifyOfPropertyChange(nameof(CellPhone1));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private string _cellPhone2 = string.Empty;
        public string CellPhone2
        {
            get => _cellPhone2;
            set
            {
                if (_cellPhone2 != value)
                {
                    _cellPhone2 = value;
                    ValidateProperty(nameof(CellPhone2), value);
                    NotifyOfPropertyChange(nameof(CellPhone2));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set
            {
                if (_address != value)
                {
                    _address = value;
                    NotifyOfPropertyChange(nameof(Address));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                NotifyOfPropertyChange(nameof(Id));
                NotifyOfPropertyChange(nameof(IsNewRecord));
            }
        }

        public bool IsNewRecord => Id == 0;

        public bool CanRemoveEmail(object p) => true;
        public bool CanAddEmail => !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(EmailDescription) && Email.IsValidEmail();

        private string _emailDescription;
        public string EmailDescription
        {
            get => _emailDescription;
            set
            {
                if (_emailDescription != value)
                {
                    _emailDescription = value;
                    NotifyOfPropertyChange(nameof(EmailDescription));
                    NotifyOfPropertyChange(nameof(CanAddEmail));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    NotifyOfPropertyChange(nameof(Email));
                    NotifyOfPropertyChange(nameof(CanAddEmail));
                }
            }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        private ObservableCollection<CostCenterDTO> _costCenters;
        public ObservableCollection<CostCenterDTO> CostCenters
        {
            get => _costCenters;
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        private IdentificationTypeGraphQLModel _selectedIdentificationType;
        public IdentificationTypeGraphQLModel SelectedIdentificationType
        {
            get => _selectedIdentificationType;
            set
            {
                if (_selectedIdentificationType != value)
                {
                    _selectedIdentificationType = value;
                    NotifyOfPropertyChange(nameof(SelectedIdentificationType));
                    NotifyOfPropertyChange(nameof(CanSave));
                    if (IsNewRecord)
                    {
                        _ = this.SetFocus(nameof(IdentificationNumber));
                    }
                }
            }
        }

        private ObservableCollection<CountryGraphQLModel> _countries;
        public ObservableCollection<CountryGraphQLModel> Countries
        {
            get => _countries;
            set
            {
                if (_countries != value)
                {
                    _countries = value;
                    NotifyOfPropertyChange(nameof(Countries));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private CountryGraphQLModel _selectedCountry;
        public CountryGraphQLModel SelectedCountry
        {
            get => _selectedCountry;
            set
            {
                if (_selectedCountry != value)
                {
                    _selectedCountry = value;
                    NotifyOfPropertyChange(nameof(SelectedCountry));
                    if (_selectedCountry != null)
                    {
                        SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.CountryId == _selectedCountry.Id);
                        NotifyOfPropertyChange(nameof(SelectedDepartment));
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private DepartmentGraphQLModel _selectedDepartment;
        public DepartmentGraphQLModel SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (_selectedDepartment != value)
                {
                    _selectedDepartment = value;
                    NotifyOfPropertyChange(nameof(SelectedDepartment));
                    if (_selectedDepartment != null)
                    {
                        SelectedCityId = SelectedDepartment.Cities.FirstOrDefault().Id;
                        NotifyOfPropertyChange(nameof(SelectedCityId));
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<EmailDTO> _emails;
        public ObservableCollection<EmailDTO> Emails
        {
            get => _emails;
            set
            {
                if (_emails != value)
                {
                    _emails = value;
                    NotifyOfPropertyChange(nameof(Emails));
                    NotifyOfPropertyChange(nameof(FilteredEmails));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<EmailDTO> _filteredEmails;
        public ObservableCollection<EmailDTO> FilteredEmails
        {
            get
            {
                if (_filteredEmails == null) _filteredEmails = new ObservableCollection<EmailDTO>();
                _filteredEmails.Clear();
                if (Emails == null) return _filteredEmails;
                foreach (EmailDTO email in Emails)
                    if (!email.Deleted) _filteredEmails.Add(email);
                return _filteredEmails;
            }
        }

        private EmailDTO _selectedEmail;
        public EmailDTO SelectedEmail
        {
            get => _selectedEmail;
            set
            {
                if (_selectedEmail != value)
                {
                    _selectedEmail = value;
                    NotifyOfPropertyChange(nameof(SelectedEmail));
                }
            }
        }

        private BooksDictionaries.CaptureTypeEnum _selectedCaptureType;
        public BooksDictionaries.CaptureTypeEnum SelectedCaptureType
        {
            get => _selectedCaptureType;
            set
            {
                if (_selectedCaptureType != value)
                {
                    _selectedCaptureType = value;
                    NotifyOfPropertyChange(() => SelectedCaptureType);
                    NotifyOfPropertyChange(() => CaptureInfoAsPN);
                    NotifyOfPropertyChange(() => CaptureInfoAsRS);
                    if (CaptureInfoAsPN)
                    {
                        ValidateProperty(nameof(FirstName), FirstName);
                        ValidateProperty(nameof(FirstLastName), FirstLastName);
                    }
                    if (CaptureInfoAsRS)
                    {
                        ClearErrors(nameof(FirstName));
                        ClearErrors(nameof(FirstLastName));
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperties();
                    if (string.IsNullOrEmpty(IdentificationNumber))
                    {
                        this.SetFocus(nameof(IdentificationNumber));
                    }
                    else
                    {
                        this.SetFocus(nameof(FirstName));
                    }
                }
            }
        }

        public bool CaptureInfoAsPN => SelectedCaptureType.Equals(BooksDictionaries.CaptureTypeEnum.PN);
        public bool CaptureInfoAsRS => SelectedCaptureType.Equals(BooksDictionaries.CaptureTypeEnum.RS);

        private int _selectedCityId;
        public int SelectedCityId
        {
            get => _selectedCityId;
            set
            {
                if (_selectedCityId != value)
                {
                    _selectedCityId = value;
                    NotifyOfPropertyChange(nameof(SelectedCityId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _identificationNumber = string.Empty;
        public string IdentificationNumber
        {
            get => _identificationNumber;
            set
            {
                if (_identificationNumber != value)
                {
                    _identificationNumber = value;
                    ValidateProperty(nameof(IdentificationNumber), value);
                    NotifyOfPropertyChange(nameof(IdentificationNumber));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool CanSave
        {
            get
            {
                // Debe haber definido el respectivo tipo de identificacion
                //if (SelectedIdentificationType == null) return false;
                // Si el documento de identidad esta vacion o su longitud es inferior a la longitud minima definida para ese tipo de documento
                if (string.IsNullOrEmpty(IdentificationNumber.Trim()) || IdentificationNumber.Length < SelectedIdentificationType.MinimumDocumentLength) return false;
                // Si la captura de informacion es del tipo persona natural, los datos obligados son primer nombre y primer apellido
                if (CaptureInfoAsPN && (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(FirstLastName))) return false;
                // Si el control de errores por propiedades tiene algun error
                return _errors.Count <= 0;
            }
        }

        #endregion

        #region Methods

        public void EndRowEditing()
        {
            try
            {
                foreach (EmailDTO email in Emails)
                    if (email.UUID == SelectedEmail.UUID) email.Edited = true;
                NotifyOfPropertyChange(nameof(Emails));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public void CleanUpControls()
        {
            try
            {
                List<CostCenterDTO> costCenters = new List<CostCenterDTO>();
                Id = 0; // Por medio del Id se establece si es un nuevo registro o una actualizacion
                SelectedIdentificationType = Context.IdentificationTypes.First(); // Traigo solo el primero debido a que solo traigo el tipo de documento 13 que es la cedula
                IdentificationNumber = string.Empty;
                SelectedCaptureType = BooksDictionaries.CaptureTypeEnum.PN;
                FirstName = string.Empty;
                MiddleName = string.Empty;
                FirstLastName = string.Empty;
                MiddleLastName = string.Empty;
                Phone1 = string.Empty;
                Phone2 = string.Empty;
                CellPhone1 = string.Empty;
                CellPhone2 = string.Empty;
                Address = string.Empty;
                Emails = new ObservableCollection<EmailDTO>();
                SelectedCountry = Context.Countries.FirstOrDefault(x => x.Code == "169"); // 169 es el cóodigo de colombia
                SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.Code == "05"); // 08 es el código del atlántico
                SelectedCityId = SelectedDepartment.Cities.FirstOrDefault(x => x.Code == "001").Id; // 001 es el Codigo de Barranquilla
                foreach (CostCenterDTO costCenter in Context.CostCenters)
                {
                    costCenters.Add(new CostCenterDTO()
                    {
                        Id = costCenter.Id,
                        Name = costCenter.Name,
                        IsSelected = false
                    });
                }
                CostCenters = new ObservableCollection<CostCenterDTO>(costCenters);
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public void PhoneInputLostFocus(FrameworkElement element)
        {
            switch (element.Name.ToLower())
            {
                case "phone1":
                    Phone1 = Phone1.ToPhoneFormat("### ## ##");
                    break;
                case "phone2":
                    Phone2 = Phone2.ToPhoneFormat("### ## ##");
                    break;
                case "cellphone1":
                    CellPhone1 = CellPhone1.ToPhoneFormat("### ### ## ##");
                    break;
                case "cellphone2":
                    CellPhone2 = CellPhone2.ToPhoneFormat("### ### ## ##");
                    break;
                default:
                    break;
            }
        }

        public async Task Save()
        {
            try
            {
                IsBusy = true;
                Refresh();
                SellerGraphQLModel result = await ExecuteSave();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new SellerCreateMessage() { CreatedSeller = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new SellerUpdateMessage() { UpdatedSeller = result });
                }
                await Context.ActivateMasterView();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<SellerGraphQLModel> ExecuteSave()
        {
            string action = string.Empty;
            string query;
            try
            {
                List<ExpandoObject> costCenterSelection = new List<ExpandoObject>();
                List<object> emailList = new List<object>();
                List<string> phones = new List<string>();

                if (!string.IsNullOrEmpty(Phone1)) phones.Add(Phone1);
                if (!string.IsNullOrEmpty(Phone2)) phones.Add(Phone2);
                if (!string.IsNullOrEmpty(CellPhone1)) phones.Add(CellPhone1);
                if (!string.IsNullOrEmpty(CellPhone2)) phones.Add(CellPhone2);

                if (Emails != null)
                {
                    foreach (EmailDTO email in Emails)
                    {
                        if (!email.Saved && !email.Deleted)
                            action = "I";
                        else if (email.Saved && email.Edited && !email.Deleted)
                            action = "U";
                        else if (email.Saved && email.Deleted) action = "D";
                        if (!string.IsNullOrEmpty(action)) emailList.Add(new { email.Id, email.Name, email.Email, email.SendElectronicInvoice, Action = action });
                    }
                }

                foreach (CostCenterDTO costCenter in CostCenters)
                {
                    if (IsNewRecord)
                    {
                        if (costCenter.IsSelected)
                        {
                            dynamic costCenterItem = new ExpandoObject();
                            costCenterItem.Id = costCenter.Id;
                            costCenterSelection.Add(costCenterItem);
                        }
                    }
                    else
                    {
                        dynamic costCenterItem = new ExpandoObject();
                        costCenterItem.Id = costCenter.Id;
                        costCenterItem.IsSelected = costCenter.IsSelected;
                        costCenterSelection.Add(costCenterItem);
                    }
                }

                dynamic variables = new ExpandoObject();
                // Root
                if (!IsNewRecord) variables.Id = Id; // Condicional
                // Structure
                variables.Data = new ExpandoObject();
                variables.Data.Entity = new ExpandoObject();
                variables.Data.Seller = new ExpandoObject();
                // Entity
                variables.Data.Entity.IdentificationNumber = IdentificationNumber;
                variables.Data.Entity.VerificationDigit = "";
                variables.Data.Entity.BusinessName = "";
                variables.Data.Entity.CaptureType = SelectedCaptureType;
                variables.Data.Entity.FirstName = FirstName;
                variables.Data.Entity.MiddleName = MiddleName;
                variables.Data.Entity.FirstLastName = FirstLastName;
                variables.Data.Entity.MiddleLastName = MiddleLastName;
                variables.Data.Entity.FullName = $"{FirstName} {MiddleName} {FirstLastName} {MiddleLastName}".Trim().RemoveExtraSpaces();
                variables.Data.Entity.Phone1 = Phone1;
                variables.Data.Entity.Phone2 = Phone2;
                variables.Data.Entity.CellPhone1 = CellPhone1;
                variables.Data.Entity.CellPhone2 = CellPhone2;
                variables.Data.Entity.Address = Address;
                variables.Data.Entity.Regime = "N";
                variables.Data.Entity.TradeName = "";
                variables.Data.Entity.CommercialCode = "";
                variables.Data.Entity.SearchName = $"{FirstName} {MiddleName} {FirstLastName} {MiddleLastName}".Trim().RemoveExtraSpaces();
                variables.Data.Entity.TelephonicInformation = string.Join(" - ", phones);
                variables.Data.Entity.IdentificationTypeId = SelectedIdentificationType.Id;
                variables.Data.Entity.CountryId = SelectedCountry.Id;
                variables.Data.Entity.DepartmentId = SelectedDepartment.Id;
                variables.Data.Entity.CityId = SelectedCityId;
                // Seller
                variables.Data.Seller.IsActive = true;
                variables.Data.Seller.CostCenters = costCenterSelection;
                // Emails
                if (emailList.Count > 0) variables.Data.Emails = emailList;

                query = IsNewRecord ?
                    @"mutation($data:CreateSellerInput!) {
                      createResponse: createSeller(data:$data) {
                        id
                        isActive
                        entity {
                          id
                          verificationDigit
                          identificationNumber
                          firstName
                          middleName
                          firstLastName
                          middleLastName
                          searchName
                          phone1
                          phone2
                          cellPhone1
                          cellPhone2
                          address
                          telephonicInformation
                          country {
                            id
                          }
                          department {
                            id
                          }
                          city {
                            id
                          }
                          emails {
                            id
                            name
                            email
                            sendElectronicInvoice
                          }
                        }
                        costCenters {
                          id
                          name
                        }
                      }
                    }" :
                    @"mutation ($data: UpdateSellerInput!, $id: Int!) {
                        updateResponse: updateSeller(data: $data, id: $id) {
                        id
                        isActive
                        entity {
                            id
                            verificationDigit
                            identificationNumber
                            firstName
                            middleName
                            firstLastName
                            middleLastName
                            searchName
                            phone1
                            phone2
                            cellPhone1
                            cellPhone2
                            address
                            telephonicInformation
                            country {
                             id
                            }
                            department {
                             id
                            }
                            city {
                             id
                            }
                            emails {
                              id
                              name
                              email
                              sendElectronicInvoice
                            }
                        }
                        costCenters {
                            id
                            name
                          }    
                        }
                    }";

                dynamic result = IsNewRecord ? await Context.BillingSeller.Create(query, variables) : await Context.BillingSeller.Update(query, variables);
                return (SellerGraphQLModel)result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void AddEmail()
        {
            try
            {
                EmailDTO email = new EmailDTO() { Name = EmailDescription, Email = Email, Saved = false, Deleted = false, Edited = true };
                Email = string.Empty;
                EmailDescription = string.Empty;
                Emails.Add(email);
                NotifyOfPropertyChange(nameof(FilteredEmails));
                _ = this.SetFocus(nameof(EmailDescription));
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public void RemoveEmail(object p)
        {
            try
            {
                if (Xceed.Wpf.Toolkit.MessageBox.Show($"¿ Confirma que desea eliminar el email : {SelectedEmail.Email} ?", "Confirme ...", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                foreach (EmailDTO item in Emails)
                    if (item.UUID == SelectedEmail.UUID) item.Deleted = true;
                NotifyOfPropertyChange(nameof(FilteredEmails));
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public SellerDetailViewModel(SellerViewModel context)
        {
            _errors = new Dictionary<string, List<string>>();
            Context = context;
        }

        public async Task GoBack()
        {
            await Context.ActivateMasterView();
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            _ = IsNewRecord ? this.SetFocus(nameof(IdentificationNumber)) : this.SetFocus(nameof(FirstName));
        }

        #endregion

        #region Validaciones

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
            try
            {
                ClearErrors(propertyName);
                if (propertyName.Contains("Phone"))
                {
                    // Remover espacios a la cadena
                    value = value.Replace(" ", "").Replace(Convert.ToChar(9).ToString(), "");
                    // Remover , = 44 y ; = 59
                    value = value.Replace(Convert.ToChar(44).ToString(), "").Replace(Convert.ToChar(59).ToString(), "");
                    // Remover - = 45 y _ = 95
                    value = value.Replace(Convert.ToChar(45).ToString(), "").Replace(Convert.ToChar(95).ToString(), "");
                }
                switch (propertyName)
                {
                    case nameof(IdentificationNumber):
                        if (string.IsNullOrEmpty(IdentificationNumber) || IdentificationNumber.Trim().Length < SelectedIdentificationType.MinimumDocumentLength) AddError(propertyName, "El número de identificación no puede estar vacío");
                        break;
                    case nameof(FirstName):
                        if (string.IsNullOrEmpty(FirstName.Trim()) && CaptureInfoAsPN) AddError(propertyName, "El primer nombre no puede estar vacío");
                        break;
                    case nameof(FirstLastName):
                        if (string.IsNullOrEmpty(FirstLastName.Trim()) && CaptureInfoAsPN) AddError(propertyName, "El primer apellido no puede estar vacío");
                        break;
                    case nameof(Phone1):
                        if (value.Length != 7 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                        break;
                    case nameof(Phone2):
                        if (value.Length != 7 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                        break;
                    case nameof(CellPhone1):
                        if (value.Length != 10 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono celular debe contener 10 digitos");
                        break;
                    case nameof(CellPhone2):
                        if (value.Length != 10 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono celular debe contener 10 digitos");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => Xceed.Wpf.Toolkit.MessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void ValidateProperties()
        {
            if (CaptureInfoAsPN)
            {
                ValidateProperty(nameof(FirstName), FirstName);
                ValidateProperty(nameof(FirstLastName), FirstLastName);
                ValidateProperty(nameof(IdentificationNumber), IdentificationNumber);
            }
        }

        #endregion
    }
}