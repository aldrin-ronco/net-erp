using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Dictionaries;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.DTO.Global;
using Models.Global;
using NetErp.Helpers;
using System;
using Extensions.Billing;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.VisualStudio.Threading;
using DevExpress.Xpf.Core;
using DevExpress.Mvvm;
using System.Windows.Threading;

namespace NetErp.Billing.Customers.ViewModels
{
    public class CustomerDetailViewModel : Screen,
        INotifyDataErrorInfo
    {
        public readonly IGenericDataAccess<CustomerGraphQLModel> CustomerService = IoC.Get<IGenericDataAccess<CustomerGraphQLModel>>();

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

        Dictionary<string, List<string>> _errors;

        public CustomerViewModel Context { get; set; }

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

        public Dictionary<char, string> RegimeDictionary => BooksDictionaries.RegimeDictionary;

        private char _selectedRegime = 'R';
        public char SelectedRegime
        {
            get => _selectedRegime;
            set
            {
                if (_selectedRegime != value)
                {
                    _selectedRegime = value;
                    NotifyOfPropertyChange(nameof(SelectedRegime));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

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

        private string _businessName = string.Empty;
        public string BusinessName
        {
            get => _businessName;
            set
            {
                if (_businessName != value)
                {
                    _businessName = value;
                    ValidateProperty(nameof(BusinessName), value);
                    NotifyOfPropertyChange(nameof(BusinessName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<RetentionTypeDTO> _retentionTypes;
        public ObservableCollection<RetentionTypeDTO> RetentionTypes
        {
            get => _retentionTypes;
            set
            {
                if (_retentionTypes != value)
                {
                    _retentionTypes = value;
                    NotifyOfPropertyChange(nameof(RetentionTypes));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<IdentificationTypeGraphQLModel> _identificationTypes;
        public ObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes
        {
            get => _identificationTypes;
            set
            {
                if (_identificationTypes != value)
                {
                    _identificationTypes = value;
                    NotifyOfPropertyChange(nameof(IdentificationTypes));
                    NotifyOfPropertyChange(nameof(CanSave));
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
                    NotifyOfPropertyChange(nameof(VerificationDigit));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _verificationDigit;
        public string VerificationDigit
        {
            set
            {
                if (_verificationDigit != value)
                {
                    _verificationDigit = value;
                    NotifyOfPropertyChange(nameof(VerificationDigit));
                }
            }
            get => !IsNewRecord
                    ? _verificationDigit
                    : SelectedIdentificationType == null
                    ? string.Empty
                    : IdentificationNumber.Trim().Length >= SelectedIdentificationType.MinimumDocumentLength
                    ? IdentificationNumber.GetVerificationDigit()
                    : string.Empty;
        }

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
                if (Emails == null)
                    return _filteredEmails;
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
                        ClearErrors(nameof(BusinessName));
                        ValidateProperty(nameof(FirstName), FirstName);
                        ValidateProperty(nameof(FirstLastName), FirstLastName);
                    }
                    if (CaptureInfoAsRS)
                    {
                        ClearErrors(nameof(FirstName));
                        ClearErrors(nameof(FirstLastName));
                        ValidateProperty(nameof(BusinessName), BusinessName);
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperties();
                    if (string.IsNullOrEmpty(IdentificationNumber))
                    {
                        _ = this.SetFocus(nameof(IdentificationNumber));
                    }
                    else
                    {
                        if (CaptureInfoAsPN)
                        {
                            _ = this.SetFocus(nameof(FirstName));
                        }
                        else
                        {
                            _ = this.SetFocus(nameof(BusinessName));
                        }

                    }
                }
            }
        }

        public bool CaptureInfoAsPN => SelectedCaptureType.Equals(BooksDictionaries.CaptureTypeEnum.PN);
        public bool CaptureInfoAsRS => SelectedCaptureType.Equals(BooksDictionaries.CaptureTypeEnum.RS);

        public bool IsNewRecord => Id == 0;

        public bool CanSave
        {
            get
            {
                // Debe haber definido el respectivo tipo de identificacion
                if (SelectedIdentificationType == null) return false;
                // Si el documento de identidad esta vacion o su longitud es inferior a la longitud minima definida para ese tipo de documento
                if (string.IsNullOrEmpty(IdentificationNumber.Trim()) || IdentificationNumber.Length < SelectedIdentificationType.MinimumDocumentLength) return false;
                // El digito de verificacion debe estar presente en caso de ser requerido
                if (SelectedIdentificationType.HasVerificationDigit && string.IsNullOrEmpty(VerificationDigit)) return false;
                // Si la captura de datos es del tipo razon social
                if (CaptureInfoAsRS && string.IsNullOrEmpty(BusinessName)) return false;
                // Si la captura de informacion es del tipo persona natural, los datos obligados son primer nombre y primer apellido
                if (CaptureInfoAsPN && (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(FirstLastName))) return false;
                // Si el control de errores por propiedades tiene algun error
                return _errors.Count <= 0;
            }
        }

        #endregion

        #region Methods

        public async Task Save()
        {
            try
            {
                //IsBusy = true;
                Refresh();
                CustomerGraphQLModel result = await ExecuteSave();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new CustomerCreateMessage() { CreatedCustomer = Context.AutoMapper.Map<CustomerDTO>(result) });
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new CustomerUpdateMessage() { UpdatedCustomer = Context.AutoMapper.Map<CustomerDTO>(result) });
                }
                await Context.ActivateMasterView();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            //finally
            //{
            //    IsBusy = false;
            //}
        }

        public async Task<CustomerGraphQLModel> ExecuteSave()
        {
            string query;

            try
            {
                List<object> retentions = new List<object>();
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
                        emailList.Add(new { email.Name, email.Email, email.SendElectronicInvoice });
                    }
                }

                if (RetentionTypes != null)
                {
                    foreach (RetentionTypeDTO retention in RetentionTypes)
                    {
                        if (retention.IsSelected)
                            retentions.Add(new { RetentionTypeId = retention.Id });
                    }
                }

                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.Entity = new ExpandoObject();

                // Connection
                if (!IsNewRecord) variables.Id = Id;

                // Customer Data
                variables.Data.CreditTerm = 0;
                variables.Data.IsActive = true;
                variables.Data.IsTaxFree = false;
                variables.Data.BlockingReason = string.Empty;
                variables.Data.RetainsAnyBasis = false;
                variables.Data.SellerId = 0;

                // Entity Data
                variables.Data.Entity.IdentificationNumber = IdentificationNumber;
                variables.Data.Entity.VerificationDigit = SelectedIdentificationType.HasVerificationDigit ? VerificationDigit : "";
                variables.Data.Entity.CaptureType = SelectedCaptureType;
                variables.Data.Entity.BusinessName = CaptureInfoAsRS ? BusinessName : "";
                variables.Data.Entity.FirstName = CaptureInfoAsPN ? FirstName : "";
                variables.Data.Entity.MiddleName = CaptureInfoAsPN ? MiddleName : "";
                variables.Data.Entity.FirstLastName = CaptureInfoAsPN ? FirstLastName : "";
                variables.Data.Entity.MiddleLastName = CaptureInfoAsPN ? MiddleLastName : "";
                variables.Data.Entity.Phone1 = Phone1;
                variables.Data.Entity.Phone2 = Phone2;
                variables.Data.Entity.CellPhone1 = CellPhone1;
                variables.Data.Entity.CellPhone2 = CellPhone2;
                variables.Data.Entity.Address = Address;
                variables.Data.Entity.Regime = SelectedRegime;
                variables.Data.Entity.FullName = $"{variables.Data.Entity.FirstName} {variables.Data.Entity.MiddleName} {variables.Data.Entity.FirstLastName} {variables.Data.Entity.MiddleLastName}".Trim().RemoveExtraSpaces();
                variables.Data.Entity.TradeName = string.Empty;
                variables.Data.Entity.SearchName = $"{variables.Data.Entity.FirstName} {variables.Data.Entity.MiddleName} {variables.Data.Entity.FirstLastName} {variables.Data.Entity.MiddleLastName}".Trim().RemoveExtraSpaces() +
                (CaptureInfoAsRS ? BusinessName.ToString() : "").Trim().RemoveExtraSpaces();
                variables.Data.Entity.TelephonicInformation = string.Join(" - ", phones);
                variables.Data.Entity.CommercialCode = string.Empty;
                variables.Data.Entity.IdentificationTypeId = SelectedIdentificationType.Id;
                variables.Data.Entity.CountryId = SelectedCountry.Id;
                variables.Data.Entity.DepartmentId = SelectedDepartment.Id;
                variables.Data.Entity.CityId = SelectedCityId;

                // Emails
                if (emailList.Count == 0) variables.Data.Entity.Emails = new List<object>();
                if (emailList.Count > 0)
                {
                    variables.Data.Entity.Emails = new List<object>();
                    variables.Data.Entity.Emails = emailList;
                }

                // Retentions
                if (retentions.Count == 0) variables.Data.Retentions = new List<object>();
                if (retentions.Count > 0) 
                {
                    variables.Data.Retentions = new List<object>();
                    variables.Data.Retentions = retentions; 
                }

                // Query
                query = IsNewRecord
                    ? @"
                    mutation ($data: CreateCustomerDataInput!) {
                      CreateResponse: createCustomer(data: $data) {
                        id
                        creditTerm
                        isTaxFree
                        isActive
                        blockingReason
                        retainsAnyBasis
                        retentions {
                          id
                          name
                          margin
                        }
                        entity {
                          id
                          identificationNumber
                          verificationDigit
                          captureType
                          businessName
                          firstName
                          middleName
                          firstLastName
                          middleLastName
                          phone1
                          phone2
                          cellPhone1
                          cellPhone2
                          address
                          regime
                          fullName
                          tradeName
                          searchName
                          telephonicInformation
                          commercialCode
                          identificationType {
                            id
                            code
                            name
                          }
                          country {
                            id
                            code
                            name
                          }
                          department {
                            id
                            code
                            name
                          }
                          city {
                            id
                            code
                            name
                          }
                          emails {
                            id
                            name
                            email
                            sendElectronicInvoice
                            isCorporate
                          }
                        }
                      }
                    }"
                    : @"
                    mutation ($data: UpdateCustomerDataInput!, $id: Int!) {
                      UpdateResponse: updateCustomer(data: $data, id: $id) {
                        id
                        creditTerm
                        isTaxFree
                        isActive
                        blockingReason
                        retainsAnyBasis
                        retentions {
                          id
                          name
                          margin
                        }
                        entity {
                          id
                          identificationNumber
                          verificationDigit
                          captureType
                          businessName
                          firstName
                          middleName
                          firstLastName
                          middleLastName
                          phone1
                          phone2
                          cellPhone1
                          cellPhone2
                          address
                          regime
                          fullName
                          tradeName
                          searchName
                          telephonicInformation
                          commercialCode
                          identificationType {
                            id
                            code
                            name
                          }
                          country {
                            id
                            code
                            name
                          }
                          department {
                            id
                            code
                            name
                          }
                          city {
                            id
                            code
                            name
                          }
                          emails {
                            id
                            name
                            email
                            sendElectronicInvoice
                            isCorporate
                          }
                        }
                      }
                    }";

                dynamic result = IsNewRecord ? await CustomerService.Create(query, variables) : await CustomerService.Update(query, variables);
                return (CustomerGraphQLModel)result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        //protected override void OnViewReady(object view)
        //{
        //    base.OnViewReady(view);
        //    ValidateProperties();
        //    _ = IsNewRecord
        //        ? this.SetFocus(nameof(IdentificationNumber))
        //        : CaptureInfoAsPN ? this.SetFocus(nameof(FirstName)) : this.SetFocus(nameof(BusinessName));
        //}

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            ValidateProperties();
            _ = Application.Current.Dispatcher.BeginInvoke(() =>
            {
                SelectedIndexPage = 0; // Selecciona el primer TAB page
                _ = IsNewRecord
                      ? Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(nameof(IdentificationNumber))), DispatcherPriority.Render)
                      : CaptureInfoAsPN
                          ? Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(nameof(FirstName))), DispatcherPriority.Render)
                          : Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(nameof(BusinessName))), DispatcherPriority.Render);
            });
        }

        public CustomerDetailViewModel(CustomerViewModel context)
        {
            _errors = new Dictionary<string, List<string>>();
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await Initialize());
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
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public void RemoveEmail(object p)
        {
            try
            {
                if (DXMessageBox.Show($"¿ Confirma que desea eliminar el email : {SelectedEmail.Email} ?", "Confirme ...", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                if(SelectedEmail != null)
                {
                    EmailDTO? emailToDelete = Emails.FirstOrDefault(email => email.Id == SelectedEmail.Id);
                    if (emailToDelete is null) return;
                    Emails.Remove(emailToDelete);
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Information));
            }
        }

        public async Task Initialize()
        {
            try
            {
                string query = @"
				query {
				  identificationTypes{
					id
                    code
					name
					hasVerificationDigit
					minimumDocumentLength    
				  },
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
				  },
				  sellers{
					id
					isActive
					entity {
					  id
					  searchName
					}
				  },
				  retentionTypes{
					id
					name
				  }  
				}";
                var result = await CustomerService.GetDataContext<CustomersDataContext>(query, new object{ });
                IdentificationTypes = new ObservableCollection<IdentificationTypeGraphQLModel>(result.IdentificationTypes);
                RetentionTypes = new ObservableCollection<RetentionTypeDTO>(Context.AutoMapper.Map<ObservableCollection<RetentionTypeDTO>>(result.RetentionTypes));
                Countries = new ObservableCollection<CountryGraphQLModel>(result.Countries);
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public void GoBack(object p)
        {
            _ = Task.Run(() => Context.ActivateMasterView());
        }

        public void CleanUpControls()
        {
            List<RetentionTypeDTO> retentionList = new List<RetentionTypeDTO>();
            Id = 0; // Por medio del Id se establece si es un nuevo registro o una actualizacion
            SelectedRegime = 'R';
            IdentificationNumber = string.Empty;
            VerificationDigit = string.Empty;
            SelectedIdentificationType = IdentificationTypes.FirstOrDefault(x => x.Code == "31"); // 31 es NIT
            SelectedCaptureType = BooksDictionaries.CaptureTypeEnum.PN;
            BusinessName = string.Empty;
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
            SelectedCountry = Countries.FirstOrDefault(x => x.Code == "169"); // 169 es el cóodigo de colombia
            SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.Code == "05"); // 08 es el código del atlántico
            SelectedCityId = SelectedDepartment.Cities.FirstOrDefault(x => x.Code == "001").Id; // 001 es el Codigo de Barranquilla
            foreach (RetentionTypeDTO retention in RetentionTypes)
            {
                retentionList.Add(new RetentionTypeDTO()
                {
                    Id = retention.Id,
                    Name = retention.Name,
                    Margin = retention.Margin,
                    InitialBase = retention.InitialBase,
                    AccountingAccountSale = retention.AccountingAccountSale,
                    AccountingAccountPurchase = retention.AccountingAccountPurchase,
                    IsSelected = false
                });
            }
            RetentionTypes = new ObservableCollection<RetentionTypeDTO>(retentionList);
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
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Information));
            }
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
            if(string.IsNullOrEmpty(value)) value = string.Empty.Trim();
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
                        if (string.IsNullOrEmpty(value) || value.Trim().Length < SelectedIdentificationType.MinimumDocumentLength) AddError(propertyName, "El número de identificación no puede estar vacío");
                        break;
                    case nameof(FirstName):
                        if (string.IsNullOrEmpty(value.Trim()) && CaptureInfoAsPN) AddError(propertyName, "El primer nombre no puede estar vacío");
                        break;
                    case nameof(FirstLastName):
                        if (string.IsNullOrEmpty(value.Trim()) && CaptureInfoAsPN) AddError(propertyName, "El primer apellido no puede estar vacío");
                        break;
                    case nameof(BusinessName):
                        if (string.IsNullOrEmpty(value.Trim()) && CaptureInfoAsRS) AddError(propertyName, "La razón social no puede estar vacía");
                        break;
                    case nameof(Phone1):
                        if (value.Length != 7 && !string.IsNullOrEmpty(Phone1)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                        break;
                    case nameof(Phone2):
                        if (value.Length != 7 && !string.IsNullOrEmpty(Phone2)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                        break;
                    case nameof(CellPhone1):
                        if (value.Length != 10 && !string.IsNullOrEmpty(CellPhone1)) AddError(propertyName, "El número de teléfono celular debe contener 10 digitos");
                        break;
                    case nameof(CellPhone2):
                        if (value.Length != 10 && !string.IsNullOrEmpty(CellPhone2)) AddError(propertyName, "El número de teléfono celular debe contener 10 digitos");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void ValidateProperties()
        {
            if (CaptureInfoAsRS) ValidateProperty(nameof(BusinessName), BusinessName);
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
