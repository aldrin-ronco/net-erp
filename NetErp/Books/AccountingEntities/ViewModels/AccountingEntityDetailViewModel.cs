using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Core;
using Dictionaries;
using DTOLibrary.Books;
using Extensions.Books;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.DTO.Global;
using Models.Global;
using NetErp.Helpers;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static Dictionaries.BooksDictionaries;

namespace NetErp.Books.AccountingEntities.ViewModels
{
    public class AccountingEntityDetailViewModel : ViewModelBase
    {
        public readonly IGenericDataAccess<IdentificationTypeGraphQLModel> IdentificationTypeService = IoC.Get<IGenericDataAccess<IdentificationTypeGraphQLModel>>();

        public readonly IGenericDataAccess<CountryGraphQLModel> CountryService = IoC.Get<IGenericDataAccess<CountryGraphQLModel>>();

        public readonly IGenericDataAccess<AccountingEntityGraphQLModel> AccountingEntityService = IoC.Get<IGenericDataAccess<AccountingEntityGraphQLModel>>();

        Dictionary<string, List<string>> _errors;

        #region Commands
        private ICommand _deleteMailCommand;
        public ICommand DeleteMailCommand
        {
            get
            {
                if (_deleteMailCommand == null) this._deleteMailCommand = new RelayCommand(CanRemoveEmail, RemoveEmail);
                return _deleteMailCommand;
            }
        }

        #endregion

        #region Propiedades
        // Context
        private AccountingEntityViewModel _context;
        public AccountingEntityViewModel Context
        {
            get { return _context; }
            set
            {
                SetValue(ref _context, value);  
            }
        }

        // Identity
        private int _id;
        public int Id
        {
            get { return _id; }
            set
            {
                SetValue(ref _id, value, changedCallback: OnIdChanged);
            }
        }

        private bool _identificationNumberIsFocused;
        public bool IdentificationNumberIsFocused
        {
            get { return _identificationNumberIsFocused; }
            set { SetValue(ref _identificationNumberIsFocused, value); }
        }

        private bool _firstNameIsFocused;

        public bool FirstNameIsFocused
        {
            get { return _firstNameIsFocused; }
            set 
            {
                SetValue(ref _firstNameIsFocused, value);
            }
        }

        private bool _businessNameIsFocused;

        public bool BusinessNameIsFocused
        {
            get { return _businessNameIsFocused; }
            set
            {
                SetValue(ref _businessNameIsFocused, value);
            }
        }

        private bool _emailDescriptionIsFocused;

        public bool EmailDescriptionIsFocused
        {
            get { return _emailDescriptionIsFocused; }
            set 
            { 
                SetValue(ref _emailDescriptionIsFocused, value);
            }
        }


        public void OnIdChanged()
        {
            RaisePropertyChanged(nameof(IsNewRecord));
        }

        // Control de visibilidad de panels de captura de datos
        public bool CaptureInfoAsPN => SelectedCaptureType.Equals(CaptureTypeEnum.PN);
        public bool CaptureInfoAsRS => SelectedCaptureType.Equals(CaptureTypeEnum.RS);

        /// <summary>
        /// Tipo de Captura, Razon Social = RS o Persona Natural = PN
        /// </summary>
        private CaptureTypeEnum _selectedCaptureType;
        public CaptureTypeEnum SelectedCaptureType
        {
            get { return _selectedCaptureType; }
            set
            {
                SetValue(ref _selectedCaptureType, value, changedCallback:OnSelectedCaptureTypeChanged);
            }
        }
        public void OnSelectedCaptureTypeChanged()
        {
            RaisePropertyChanged(nameof(CaptureInfoAsPN));
            RaisePropertyChanged(nameof(CaptureInfoAsRS));
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
            RaisePropertyChanged(nameof(CanSave));
            ValidateProperties();
            if (string.IsNullOrEmpty(IdentificationNumber))
            {
                this.SetFocus(() => IdentificationNumber);
            }
            else
            {
                if (CaptureInfoAsPN)
                {
                    this.SetFocus(() => FirstName);
                }
                else
                {
                    this.SetFocus(() => BusinessName);
                }

            }
        }


        void SetFocus(Expression<Func<object>> expression)
        {
            string controlName = expression.GetMemberInfo().Name;

            IdentificationNumberIsFocused = false;
            FirstNameIsFocused = false;
            BusinessNameIsFocused = false;
            EmailDescriptionIsFocused = false;

            IdentificationNumberIsFocused = controlName == nameof(IdentificationNumber);
            FirstNameIsFocused = controlName == nameof(FirstName);
            BusinessNameIsFocused = controlName == nameof(BusinessName);
            EmailDescriptionIsFocused = controlName == nameof(EmailDescription);
        }

        /// <summary>
        /// Si es un nuevo registro
        /// </summary>
        public bool IsNewRecord
        {
            get { return (this.Id == 0); }
        }

        /// <summary>
        /// Regimen
        /// </summary>
        public Dictionary<char, string> RegimeDictionary { get { return BooksDictionaries.RegimeDictionary; } }
        // Regimen Seleccionado
        private char _selectedRegime = 'R';
        public char SelectedRegime
        {
            get { return _selectedRegime; }
            set
            {
                SetValue(ref _selectedRegime, value, changedCallback: OnSelectedRegimeChange);               
            }
        }

        public void OnSelectedRegimeChange()
        {
            RaisePropertyChanged(nameof(CanSave));
        }

        /// <summary>
        /// Tipos de Documentos
        /// </summary>
        private ObservableCollection<IdentificationTypeGraphQLModel> _identificationTypes;
        public ObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes
        {
            get { return _identificationTypes; }
            set
            {
                SetValue(ref _identificationTypes, value);
            }
        }

        /// <summary>
        /// Selected Email
        /// </summary>
        private EmailDTO _selectedEmail;
        public EmailDTO SelectedEmail
        {
            get { return _selectedEmail; }
            set
            {
                SetValue(ref _selectedEmail, value);
            }
        }

        // Tipo de documento seleccionado
        private IdentificationTypeGraphQLModel _selectedIdentificationType;
        public IdentificationTypeGraphQLModel SelectedIdentificationType
        {
            get { return _selectedIdentificationType; }
            set
            {
                SetValue(ref _selectedIdentificationType, value, changedCallback: OnSelectedIdentificationType);
            }
        }

        public void OnSelectedIdentificationType()
        {
            RaisePropertyChanged(nameof(CanSave));
            if (IsNewRecord)
            {
                this.SetFocus(() => IdentificationNumber);
            }
        }

        // Emails (Lista de emails del tercero)
        private ObservableCollection<EmailDTO> _emails;
        public ObservableCollection<EmailDTO> Emails
        {
            get { return _emails; }
            set
            {
                SetValue(ref _emails, value, changedCallback: OnEmailsChanged);
            }
        }

        public void OnEmailsChanged()
        {
            RaisePropertyChanged(nameof(FilteredEmails));
        }

        // emails filtrados
        private ObservableCollection<EmailDTO> _filteredEmails;
        public ObservableCollection<EmailDTO> FilteredEmails
        {
            get
            {
                if (_filteredEmails == null) _filteredEmails = new ObservableCollection<EmailDTO>();
                _filteredEmails.Clear();
                if (this.Emails != null)
                    foreach (var email in this.Emails)
                        if (!email.Deleted) _filteredEmails.Add(email);
                return _filteredEmails;
            }
        }

        /// Descripcion de Email (Para agregar)
        private string _emailDescription;
        public string EmailDescription
        {
            get { return _emailDescription; }
            set
            {
                SetValue(ref _emailDescription, value, changedCallback: OnEmailDescriptionChanged);
            }
        }

        public void OnEmailDescriptionChanged()
        {
            RaisePropertyChanged(nameof(CanAddEmail));
        }

        /// Email (Para agregar)
        private string _email;
        public string Email
        {
            get { return _email; }
            set
            {
                SetValue(ref _email, value, changedCallback: OnEmailChanged);
            }
        }

        public void OnEmailChanged()
        {
            RaisePropertyChanged(nameof(CanAddEmail));
        }

        /// <summary>
        /// Número de identificación
        /// </summary>
        private string _identificationNumber = string.Empty;
        public string IdentificationNumber
        {
            get => _identificationNumber;
            set
            {
                ValidateProperty(nameof(IdentificationNumber), value);
                SetValue(ref _identificationNumber, value, changedCallback: OnIdentificationNumberChanged);
            }
        }

        public void OnIdentificationNumberChanged()
        {
            RaisePropertyChanged(nameof(VerificationDigit));
            RaisePropertyChanged(nameof(CanSave));
        }

        /// <summary>
        /// Digito de Verificacion
        /// </summary>
        private string _verificationDigit;
        public string VerificationDigit
        {
            set
            {
                SetValue(ref _verificationDigit, value);
            }
            get => !IsNewRecord
                   ? _verificationDigit
                   : SelectedIdentificationType == null
                   ? string.Empty
                   : IdentificationNumber.Trim().Length >= SelectedIdentificationType.MinimumDocumentLength
                   ? IdentificationNumber.GetVerificationDigit()
                   : string.Empty;
        }

        public void OnRequiredFieldsChanged()
        {
            RaisePropertyChanged(nameof(CanSave));
        }
        /// <summary>
        /// Razon Social
        /// </summary>
        private string _businessName = string.Empty;
        public string BusinessName
        {
            get => _businessName;
            set
            {
                    ValidateProperty(nameof(BusinessName), value);
                    SetValue(ref _businessName, value, changedCallback: OnRequiredFieldsChanged);                
            }
        }

        /// <summary>
        /// Primer Nombre
        /// </summary>
        private string _firstName = string.Empty;
        public string FirstName
        {
            get => _firstName; 
            set
            {
                ValidateProperty(nameof(FirstName), value);
                SetValue(ref _firstName, value, changedCallback: OnRequiredFieldsChanged);   
            }
        }



        /// <summary>
        /// Segundo Nombre
        /// </summary>
        private string _middleName = string.Empty;
        public string MiddleName
        {
            get => _middleName;
            set
            {
                SetValue(ref _middleName, value, changedCallback: OnRequiredFieldsChanged);
            }
        }

        /// <summary>
        /// Primer Apellido
        /// </summary>
        private string _firstLastName = string.Empty;
        public string FirstLastName
        {
            get => _firstLastName;
            set
            {
                ValidateProperty(nameof(FirstLastName), value);
                SetValue(ref _firstLastName, value, changedCallback: OnRequiredFieldsChanged);
            }
        }

        /// <summary>
        /// Segundo Apellido
        /// </summary>
        private string _middleLastName = string.Empty;
        public string MiddleLastName
        {
            get => _middleLastName;
            set
            {
                SetValue(ref _middleLastName, value, changedCallback: OnRequiredFieldsChanged);
            }
        }

        /// <summary>
        /// Telefono Fijo 1
        /// </summary>
        private string _phone1 = string.Empty;

        public string Phone1
        {
            get => _phone1;
            set
            {
                ValidateProperty(nameof(Phone1), value);
                SetValue(ref _phone1, value, changedCallback: OnRequiredFieldsChanged);
            }
        }

        /// <summary>
        /// Telefono Fijo 2
        /// </summary>
        private string _phone2 = string.Empty;
        public string Phone2
        {
            get => _phone2; 
            set
            {
                ValidateProperty(nameof(Phone2), value);
                SetValue(ref _phone2, value, changedCallback: OnRequiredFieldsChanged);
            }
        }

        /// <summary>
        /// Telefono Celular 1
        /// </summary>
        private string _cellPhone1 = string.Empty;
        public string CellPhone1
        {
            get => _cellPhone1;
            set
            {
                ValidateProperty(nameof(CellPhone1), value);
                SetValue(ref _cellPhone1, value, changedCallback: OnRequiredFieldsChanged);
            }
        }

        /// <summary>
        /// Telefono Celular 2
        /// </summary>
        private string _cellPhone2 = string.Empty;
        public string CellPhone2
        {
            get => _cellPhone2;
            set
            {
                ValidateProperty(nameof(CellPhone2), value);
                SetValue(ref _cellPhone2, value, changedCallback: OnRequiredFieldsChanged);
            }
        }

        /// <summary>
        /// Direccion
        /// </summary>
        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set
            {
                SetValue(ref _address, value, changedCallback: OnRequiredFieldsChanged);
            }
        }

        /// <summary>
		/// Is Busy
		/// </summary>
        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                SetValue(ref _isBusy, value);
            }
        }

        /// <summary>
        /// Paises
        /// </summary>
        private ObservableCollection<CountryGraphQLModel> _countries;
        public ObservableCollection<CountryGraphQLModel> Countries
        {
            get => _countries;
            set
            {
                SetValue(ref _countries, value);
            }
        }

        // Pais Seleccionado - SelectedItem
        private CountryGraphQLModel _selectedCountry;
        public CountryGraphQLModel SelectedCountry
        {
            get => _selectedCountry;
            set
            {
                SetValue(ref _selectedCountry, value, changedCallback: OnSelectedCountryChanged);
            }
        }

        public void OnSelectedCountryChanged()
        {
            if (_selectedCountry != null)
            {
                SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.CountryId == _selectedCountry.Id);
                RaisePropertyChanged(nameof(SelectedDepartment));
            }
            RaisePropertyChanged(nameof(CanSave));
        }

        /// <summary>
        /// Departamentos
        /// </summary>
        private ObservableCollection<DepartmentGraphQLModel> _departments;
        public ObservableCollection<DepartmentGraphQLModel> Departments
        {
            get => _departments;
            set
            {
                SetValue(ref _departments, value);
            }
        }
        // departamento Seleccionado
        private DepartmentGraphQLModel _selectedDepartment;
        public DepartmentGraphQLModel SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (_selectedDepartment != value)
                {
                    SetValue(ref _selectedDepartment, value, changedCallback: OnSelectedDepartmentChanged);
                }
            }
        }

        public void OnSelectedDepartmentChanged()
        {
            if (_selectedDepartment != null)
            {
                SelectedCityId = SelectedDepartment.Cities.FirstOrDefault().Id;
                RaisePropertyChanged(nameof(SelectedCityId));
            }
            RaisePropertyChanged(nameof(CanSave));
        }

        /// <summary>
        /// Ciudad seleccionada
        /// </summary>
        private int _selectedCityId;
        public int SelectedCityId
        {
            get => _selectedCityId;
            set
            {
                SetValue(ref _selectedCityId, value, changedCallback: OnRequiredFieldsChanged);
            }
        }

        #endregion

        [Command]
        public async void OnViewReady()
        {
            if (IsNewRecord)
            {
                this.SetFocus(() => IdentificationNumber);
                ValidateProperties();
            }
            else
            {
                if (this.SelectedCaptureType.Equals(CaptureTypeEnum.PN))
                {
                    this.SetFocus(() => FirstName);
                }
                else
                {
                    this.SetFocus(() => BusinessName);
                }
            }
        }
        public async Task Initialize()
        {
            // Validaciones
            this._errors = new Dictionary<string, List<string>>();
            string query = @"
			query{
			    ListResponse: identificationTypes{
			    id
			    code
			    name
			    hasVerificationDigit
			    minimumDocumentLength
			    }
			}";

            IEnumerable<IdentificationTypeGraphQLModel> result = await IdentificationTypeService.GetList(query, new object { });
            IdentificationTypes = new ObservableCollection<IdentificationTypeGraphQLModel>(result);
            SelectedIdentificationType = IdentificationTypes.FirstOrDefault(x => x.Code == "31"); // 31 es NIT
            string countriesQuery = @"
                query{
                ListResponse: countries{
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
            }";
            Countries = new ObservableCollection<CountryGraphQLModel>(await CountryService.GetList(countriesQuery, new object { }));

            //this.Detail.GlobalCountryId = 46;
            //var dptId = from city in this.Departments where city.Id == this.Detail.GlobalCityId select city.Id;
            SelectedCountry = Countries.FirstOrDefault(x => x.Code == "169"); // 169 es el cóodigo de colombia
            SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.Code == "05"); // 08 es el código del atlántico
            SelectedCityId = SelectedDepartment.Cities.FirstOrDefault(x => x.Code == "001").Id;// 001 es el Codigo de Barranquilla
        }

        public AccountingEntityDetailViewModel(AccountingEntityViewModel context)
        {
            Context = context;
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await Initialize());
        }

        public void CleanUpControls()
        {
            Id = 0; // Por medio del Id se establece si es un nuevo registro o una actualizacion
            SelectedRegime = 'R';
            VerificationDigit = "";
            SelectedIdentificationType = IdentificationTypes.FirstOrDefault(x => x.Code == "31"); // 31 es NIT
            IdentificationNumber = "";
            SelectedCaptureType = CaptureTypeEnum.PN;
            BusinessName = "";
            FirstName = "";
            MiddleName = "";
            FirstLastName = "";
            MiddleLastName = "";
            Phone1 = "";
            Phone2 = "";
            CellPhone1 = "";
            CellPhone2 = "";
            Address = "";
            Emails = new ObservableCollection<EmailDTO>();
            SelectedCountry = Countries.FirstOrDefault(x => x.Code == "169"); // 169 es el cóodigo de colombia
            SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.Code == "05"); // 08 es el código del atlántico
            SelectedCityId = SelectedDepartment.Cities.FirstOrDefault(x => x.Code == "001").Id; // 001 es el Codigo de Barranquilla
        }


        public async Task Cancel()
        {
            await Context.ActivateMasterView();
        }

        public void EndRowEditing()
        {
            try
            {
                foreach (EmailDTO email in Emails)
                    if (email.UUID == SelectedEmail.UUID) email.Edited = true;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: $"{this.GetType().Name}.{(currentMethod is null ? "EndRowEditing" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
            }
        }

        public async void RemoveEmail(object p)
        {
            try
            {
                if (DXMessageBox.Show(caption: "Confirme ...", messageBoxText: $"¿Confirma que desea eliminar el email: {SelectedEmail.Email}?", button: MessageBoxButton.YesNo, icon: MessageBoxImage.Question) == MessageBoxResult.No) return;
                if (SelectedEmail != null)
                {
                    EmailDTO? emailToDelete = Emails.FirstOrDefault(email => email.Id == SelectedEmail.Id);
                    if (emailToDelete is null) return;
                    Emails.Remove(emailToDelete);
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: $"{this.GetType().Name}.{(currentMethod is null ? "RemoveEmail" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
            }
        }

        public bool CanRemoveEmail(object p) => true;

        public void AddEmail()
        {
            try
            {
                EmailDTO email = new EmailDTO() { Name = EmailDescription, Email = Email };
                Email = "";
                EmailDescription = "";
                Emails.Add(email);
                RaisePropertyChanged(nameof(FilteredEmails));
                this.SetFocus(() => EmailDescription);
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: $"{this.GetType().Name}.{(currentMethod is null ? "AddEmail" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
            }
        }

        public bool CanAddEmail => !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(EmailDescription) && Email.IsValidEmail();

        public async Task Save()
        {
            try
            {
                IsBusy = true;
                //Refresh();
                AccountingEntityGraphQLModel result = await ExecuteSave();
                if (IsNewRecord)
                {
                    Messenger.Default.Send(new AccountingEntityCreateMessage() { CreatedAccountingEntity = Context.AutoMapper.Map<AccountingEntityDTO>(result)});
                }
                else
                {
                    Messenger.Default.Send(new AccountingentityUpdateMessage() { UpdatedAccountingEntity = Context.AutoMapper.Map<AccountingEntityDTO>(result) });
                }
                await Context.ActivateMasterView();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: $"{graphQLError.Errors[0].Extensions.Message} {graphQLError.Errors[0].Message}", button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: $"{this.GetType().Name}.{(currentMethod is null ? "Save" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<AccountingEntityGraphQLModel> ExecuteSave()
        {
            try
            {
                string action = "";
                string query = "";

                List<object> emailList = new List<object>();
                List<string> phones = new List<string>();

                if (!string.IsNullOrEmpty(Phone1)) phones.Add(Phone1);
                if (!string.IsNullOrEmpty(Phone2)) phones.Add(Phone2);
                if (!string.IsNullOrEmpty(CellPhone1)) phones.Add(CellPhone1);
                if (!string.IsNullOrEmpty(CellPhone2)) phones.Add(CellPhone2);

                if (Emails != null)
                    foreach (EmailDTO email in Emails)
                    {
                        emailList.Add(new { email.Name, email.Email, email.SendElectronicInvoice});
                    }

                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.IdentificationNumber = IdentificationNumber;
                variables.Data.VerificationDigit = SelectedIdentificationType.HasVerificationDigit ? VerificationDigit : "";
                variables.Data.CaptureType = SelectedCaptureType.ToString();
                variables.Data.BusinessName = CaptureInfoAsRS ? BusinessName : "";
                variables.Data.TradeName = string.Empty;
                variables.Data.FirstName = CaptureInfoAsPN ? FirstName : "";
                variables.Data.MiddleName = CaptureInfoAsPN ? MiddleName : "";
                variables.Data.FirstLastName = CaptureInfoAsPN ? FirstLastName : "";
                variables.Data.MiddleLastName = CaptureInfoAsPN ? MiddleLastName : "";
                variables.Data.FullName = $"{variables.Data.FirstName} {variables.Data.MiddleName} {variables.Data.FirstLastName} {variables.Data.MiddleLastName}".Trim().RemoveExtraSpaces();
                variables.Data.SearchName = $"{variables.Data.FirstName} {variables.Data.MiddleName} {variables.Data.FirstLastName} {variables.Data.MiddleLastName}".Trim().RemoveExtraSpaces() +
                (CaptureInfoAsRS ? BusinessName.ToString() : "").Trim().RemoveExtraSpaces(); // esto esta pendiente + {(string.IsNullOrEmpty(this.TradeName.Trim()) ? "" : "-")} {this.TradeName}".RemoveExtraSpaces(),
                variables.Data.Phone1 = Phone1;
                variables.Data.Phone2 = Phone2;
                variables.Data.CellPhone1 = CellPhone1;
                variables.Data.CellPhone2 = CellPhone2;
                variables.Data.TelephonicInformation = string.Join(" - ", phones);
                variables.Data.CommercialCode = string.Empty;
                variables.Data.Address = Address;
                variables.Data.Regime = SelectedRegime;
                variables.Data.IdentificationTypeId = SelectedIdentificationType.Id;
                variables.Data.CountryId = SelectedCountry.Id;
                variables.Data.DepartmentId = SelectedDepartment.Id;
                variables.Data.CityId = SelectedCityId;
                if (!IsNewRecord) variables.Id = Id; // Needed for update only
                if (emailList.Count == 0) variables.Data.Emails = new List<object>();
                if (emailList.Count > 0) 
                {
                    variables.Data.Emails = new List<ExpandoObject>();
                    variables.Data.Emails = emailList;
                }; // If no emails registered, don't include the key

                if (IsNewRecord)
                {
                    query = @"
					mutation ($data: CreateAccountingEntityTypeInput!) {
					  CreateResponse: createAccountingEntity(data: $data) 
					  {
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
						}
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
					}";
                    
                    var createdAccountingEntity = await AccountingEntityService.Create(query, variables);
                    return createdAccountingEntity;
                }
                else
                {
                    query = @"
					mutation ($data: UpdateAccountingEntityTypeInput!, $id: Int!) {
					  UpdateResponse: updateAccountingEntity(data: $data, id: $id) {
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
						}
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
					}";
                    var updatedAccountingEntity = await AccountingEntityService.Update(query, variables);
                    return updatedAccountingEntity;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

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
                if (_errors.Count > 0) return false;
                return true;
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
        #endregion

    }
}
