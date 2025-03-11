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
using Services.Billing.DAL.PostgreSQL;
using Common.Interfaces;
using Microsoft.VisualStudio.Threading;
using System.Windows.Threading;
using DevExpress.Mvvm;
using NetErp.Global.CostCenters.DTO;


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
        private ICommand _goBackCommand;
        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new RelayCommand(CanGoBack, GoBack);
                return _goBackCommand;
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

        #endregion

        #region Properties

        public readonly IGenericDataAccess<SellerGraphQLModel> SellerService = IoC.Get<IGenericDataAccess<SellerGraphQLModel>>();

        Dictionary<string, List<string>> _errors;

        public SellerViewModel Context { get; private set; }

        private string _firstName = string.Empty;
        public string FirstName
        {
            get
            {
                if (_firstName is null) return string.Empty;
                return _firstName;
            }
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
            get
            {
                if (_middleName is null) return string.Empty;
                return _middleName;
            }
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
            get
            {
                if (_firstLastName is null) return string.Empty;
                return _firstLastName;
            }
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
            get
            {
                if (_middleLastName is null) return string.Empty;
                return _middleLastName;
            }
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
            get
            {
                if (_phone1 is null) return string.Empty;
                return _phone1;
            }
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
            get
            {
                if (_phone2 is null) return string.Empty;
                return _phone2;
            }
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
            get
            {
                if (_cellPhone1 is null) return string.Empty;
                return _cellPhone1;
            }
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
            get
            {
                if (_cellPhone2 is null) return string.Empty;
                return _cellPhone2;
            }
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
            get
            {
                if (_address is null) return string.Empty;
                return _address;
            }
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
            get
            {
                if (_emailDescription is null) return string.Empty;
                return _emailDescription;
            }
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
            get
            {
                if (_email is null) return string.Empty;
                return _email;
            }
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
                    ValidateProperty(nameof(IdentificationNumber), _identificationNumber);
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
        private int _selectedIndexPage = 0;
        public int SelectedIndexPage
        {
            get => _selectedIndexPage;
            set
            {
                if (_selectedIndexPage != value)
                {
                    _selectedIndexPage = value;
                    NotifyOfPropertyChange(nameof(SelectedIndexPage));
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
            get
            {
                if (_identificationNumber is null) return string.Empty;
                return _identificationNumber;
            }
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

        public void GoBack(object p)
        {
            try
            {
                _ = Task.Run(() => Context.ActivateMasterViewAsync());
            }
            catch (AsyncException ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }

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
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }

        public void CleanUpControls()
        {
            try
            {
                List<CostCenterDTO> costCenters = new List<CostCenterDTO>();
                Id = 0; // Por medio del Id se establece si es un nuevo registro o una actualizacion
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
                throw new AsyncException(innerException: ex);
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

        public async Task<IGenericDataAccess<SellerGraphQLModel>.PageResponseType> LoadPage()
        {
            try
            {
                string queryForPage;
                queryForPage = @"
                    query ($filter: SellerFilterInput){
                      PageResponse: sellerPage(filter: $filter) {
                        count
                        rows {
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
                              description
                              email
                              sendElectronicInvoice
                            }
                          }
                          costCenters {
                            id
                            name
                          }
                        }
                      }
                      identificationTypes {
                        id
                        code
                        name
                        hasVerificationDigit
                        minimumDocumentLength
                      }
                      countries{
                        id
                        code
                        name
                        departments {
                          id
                          code
                          name
                          cities {
                            id
                            code
                            name
                          }
                        }
                      }
                      costCenters{
                        id
                        name
                      }
                    }";
                return await SellerService.GetPage(queryForPage, new object { });
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task Save()
        {
            try
            {
                IsBusy = true;
                Refresh();
                SellerGraphQLModel result = await ExecuteSave();
                var pageResult = await LoadPage();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new SellerCreateMessage() { CreatedSeller = Context.AutoMapper.Map<SellerDTO>(result), Sellers = pageResult.PageResponse.Rows});
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new SellerUpdateMessage() { UpdatedSeller = Context.AutoMapper.Map<SellerDTO>(result), Sellers = pageResult.PageResponse.Rows });
                }
                Context.EnableOnViewReady = false;
                await Context.ActivateMasterViewAsync();
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
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
                List<int> costCenterSelection = new List<int>();
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
                        emailList.Add(new { email.Description, email.Email, email.SendElectronicInvoice });
                    }
                }

                foreach (CostCenterDTO costCenter in CostCenters)
                {
                    if (IsNewRecord)
                    {
                        if (costCenter.IsSelected)
                        {
                            costCenterSelection.Add(costCenter.Id);
                        }
                    }
                }

                dynamic variables = new ExpandoObject();
                // Root
                if (!IsNewRecord) variables.Id = Id; // Condicional
                // Structure
                variables.Data = new ExpandoObject();
                variables.Data.Entity = new ExpandoObject();

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
                variables.Data.IsActive = true;
                variables.Data.CostCenters = costCenterSelection;
                // Emails
                if (emailList.Count == 0) variables.Data.Entity.Emails = new List<object>();
                if (emailList.Count > 0)
                {
                    variables.Data.Entity.Emails = new List<object>();
                    variables.Data.Entity.Emails = emailList;
                }

                if(costCenterSelection.Count == 0) variables.Data.CostCenters = new List<int>();
                if(costCenterSelection.Count > 0)
                {
                    variables.Data.CostCenters = new List<int>();
                    variables.Data.CostCenters = costCenterSelection;
                }

                query = IsNewRecord ?
                    @"mutation($data:CreateSellerDataInput!) {
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
                            description
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
                    @"mutation ($data: UpdateSellerDataInput!, $id: Int!) {
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
                              description
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

                dynamic result = IsNewRecord ? await SellerService.Create(query, variables) : await SellerService.Update(query, variables);
                return (SellerGraphQLModel)result;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public void AddEmail()
        {
            try
            {
                EmailDTO email = new EmailDTO() { Description = EmailDescription, Email = Email, Saved = false, Deleted = false, Edited = true };
                Email = string.Empty;
                EmailDescription = string.Empty;
                Emails.Add(email);
                NotifyOfPropertyChange(nameof(FilteredEmails));
                _ = this.SetFocus(nameof(EmailDescription));
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }

        public void RemoveEmail(object p)
        {
            try
            {
                if (ThemedMessageBox.Show("Confirme ...", $"¿ Confirma que desea eliminar el email : {SelectedEmail.Email} ?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                if (SelectedEmail != null)
                {
                    EmailDTO? emailToDelete = Emails.FirstOrDefault(email => email.Id == SelectedEmail.Id);
                    if (emailToDelete is null) return;
                    Emails.Remove(emailToDelete);
                }
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
            }
        }

        public SellerDetailViewModel(SellerViewModel context)
        {
            _errors = new Dictionary<string, List<string>>();
            Context = context;
        }

        public async Task Initialize()
        {
            Countries = Context.Countries;
            SelectedIdentificationType = Context.IdentificationTypes.FirstOrDefault(x => x.Code == "13"); // 13 es CC
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            ValidateProperties();
            _ = Application.Current.Dispatcher.BeginInvoke(() =>
            {
                SelectedIndexPage = 0; // Selecciona el primer TAB page
                _ = IsNewRecord
                      ? Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(nameof(IdentificationNumber))), DispatcherPriority.Render)
                      : Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(nameof(FirstName))), DispatcherPriority.Render);
            });
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
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
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
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                });
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
        public bool CanGoBack(object p)
        {
            return !IsBusy;
        }

        #endregion
    }
}