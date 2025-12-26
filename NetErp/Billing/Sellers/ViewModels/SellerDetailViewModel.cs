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
using NetErp.Billing.Zones.DTO;
using System.Diagnostics;
using System.Collections.Specialized;
using DevExpress.XtraEditors.Filtering;
using NetErp.Helpers.GraphQLQueryBuilder;
using static Models.Global.GraphQLResponseTypes;
using Services.Books.DAL.PostgreSQL;
using Extensions.Global;


namespace NetErp.Billing.Sellers.ViewModels
{
    public class SellerDetailViewModel : Screen, INotifyDataErrorInfo
    {
        private readonly IRepository<ZoneGraphQLModel> _zoneService;
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
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync, CanSave);
                return _saveCommand;
            }
        }

        #endregion

        #region Properties

        private readonly IRepository<SellerGraphQLModel> _sellerService;

        Dictionary<string, List<string>> _errors;

        public SellerViewModel Context { get; private set; }

        private string _firstName = string.Empty;
        [ExpandoPath("accountingEntity.firstName")]

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
                    this.TrackChange(nameof(FirstName));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _middleName = string.Empty;
        [ExpandoPath("accountingEntity.middleName")]

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
                    this.TrackChange(nameof(MiddleName));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _firstLastName = string.Empty;
        [ExpandoPath("accountingEntity.firstLastName")]

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
                    this.TrackChange(nameof(FirstLastName));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _middleLastName = string.Empty;
        [ExpandoPath("accountingEntity.middleLastName")]
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
                    this.TrackChange(nameof(MiddleLastName));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        

        private string _primaryPhone = string.Empty;
        [ExpandoPath("accountingEntity.primaryPhone")]
        public string PrimaryPhone
        {
            get
            {
                if (_primaryPhone is null) return string.Empty;
                return _primaryPhone;
            }
            set
            {
                if (_primaryPhone != value)
                {
                    _primaryPhone = value;
                    ValidateProperty(nameof(PrimaryPhone), value);
                    NotifyOfPropertyChange(nameof(PrimaryPhone));
                    this.TrackChange(nameof(PrimaryPhone));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _secondaryPhone = string.Empty;
        [ExpandoPath("accountingEntity.secondaryPhone")]
        public string SecondaryPhone
        {
            get
            {
                if (_secondaryPhone is null) return string.Empty;
                return _secondaryPhone;
            }
            set
            {
                if (_secondaryPhone != value)
                {
                    _secondaryPhone = value;
                    ValidateProperty(nameof(SecondaryPhone), value);
                    NotifyOfPropertyChange(nameof(SecondaryPhone));
                    this.TrackChange(nameof(SecondaryPhone));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _primaryCellPhone = string.Empty;
        [ExpandoPath("accountingEntity.primaryCellPhone")]
        public string PrimaryCellPhone
        {
            get
            {
                if (_primaryCellPhone is null) return string.Empty;
                return _primaryCellPhone;
            }
            set
            {
                _primaryCellPhone = value;
                ValidateProperty(nameof(PrimaryCellPhone), value);
                NotifyOfPropertyChange(nameof(PrimaryCellPhone));
                this.TrackChange(nameof(PrimaryCellPhone));

                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private string _secondaryCellPhone = string.Empty;
        [ExpandoPath("accountingEntity.secondaryCellPhone")]

        public string SecondaryCellPhone
        {
            get
            {
                if (_secondaryCellPhone is null) return string.Empty;
                return _secondaryCellPhone;
            }
            set
            {
                if (_secondaryCellPhone != value)
                {
                    _secondaryCellPhone = value;
                    ValidateProperty(nameof(SecondaryCellPhone), value);
                    NotifyOfPropertyChange(nameof(SecondaryCellPhone));
                    this.TrackChange(nameof(SecondaryCellPhone));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _address = string.Empty;
        [ExpandoPath("accountingEntity.address")]
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
                    this.TrackChange(nameof(Address));

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
                    this.TrackChange(nameof(EmailDescription));

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
                    this.TrackChange(nameof(Email));

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
        [ExpandoPath("accountingEntity.identificationTypeId", SerializeAsId = true)]

        public ObservableCollection<CostCenterDTO> CostCenters
        {
            get => _costCenters;
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                    NotifyOfPropertyChange(nameof(SelectedCostCenterIds));
                    this.TrackChange(nameof(SelectedCostCenterIds));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ListenCostCenterChek();
                }
            }
        }
        private ObservableCollection<ZoneDTO> _zones;
        public ObservableCollection<ZoneDTO> Zones
        {
            get => _zones;
            set
            {
                if (_zones != value)
                {
                    _zones = value;
                    NotifyOfPropertyChange(nameof(Zones));
                }
            }
        }
        [ExpandoPath("CostCenterIds")]
        public List<int> SelectedCostCenterIds => CostCenters.Where(f => f.IsSelected).Select(s => s.Id).ToList();
        
        private IdentificationTypeGraphQLModel _selectedIdentificationType;
        [ExpandoPath("accountingEntity.identificationTypeId", SerializeAsId = true)]

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
                        this.TrackChange(nameof(SelectedIdentificationType));
                    }
                    

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
        [ExpandoPath("accountingEntity.countryId", SerializeAsId = true)]

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
        [ExpandoPath("accountingEntity.departmentId", SerializeAsId = true)]

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

        //[ExpandoPath("AccountingEntity.emails")]
        public ObservableCollection<EmailDTO> Emails
        {
            get => _emails;
            set
            {
                if (_emails != value)
                {
                    _emails = value;
                    NotifyOfPropertyChange(nameof(Emails));
                    //this.TrackChange(nameof(Emails));
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

        private BooksDictionaries.CaptureTypeEnum _captureType;
        [ExpandoPath("accountingEntity.captureType")]
        public BooksDictionaries.CaptureTypeEnum CaptureType
        {
            get => _captureType;
            set
            {
                if (_captureType != value)
                {
                    _captureType = value;
                    NotifyOfPropertyChange(() => CaptureType);
                    NotifyOfPropertyChange(() => CaptureInfoAsPN);
                    NotifyOfPropertyChange(() => CaptureInfoAsRS);
                    this.TrackChange(nameof(CaptureType));
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

        public bool CaptureInfoAsPN => CaptureType.Equals(BooksDictionaries.CaptureTypeEnum.PN);
        public bool CaptureInfoAsRS => CaptureType.Equals(BooksDictionaries.CaptureTypeEnum.RS);

        private int _cityId;
        [ExpandoPath("accountingEntity.cityId")]

        public int SelectedCityId
        {
            get => _cityId;
            set
            {
                if (_cityId != value)
                {
                    _cityId = value;
                    NotifyOfPropertyChange(nameof(SelectedCityId));
                    this.TrackChange(nameof(SelectedCityId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private int? _zoneId;

        public int? ZoneId
        {
            get => _zoneId;
            set
            {
                if (_zoneId != value)
                {
                    _zoneId = value;
                    NotifyOfPropertyChange(nameof(ZoneId));
                    this.TrackChange(nameof(ZoneId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private string _identificationNumber = string.Empty;
        [ExpandoPath("accountingEntity.identificationNumber")]
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
                    this.TrackChange(nameof(IdentificationNumber));

                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private char _regime = 'N';
        [ExpandoPath("accountingEntity.regime")]
        public char Regime
        {
            get => _regime;
            set
            {
                if (_regime != value)
                {
                    _regime = value;
                    NotifyOfPropertyChange(nameof(Regime));
                    this.TrackChange(nameof(Regime));
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
                if (string.IsNullOrEmpty(IdentificationNumber.Trim()) || IdentificationNumber.Length < SelectedIdentificationType?.MinimumDocumentLength) return false;
                if (CostCenters.Where(f => f.IsSelected == true).Count() == 0) return false;
                // Si la captura de informacion es del tipo persona natural, los datos obligados son primer nombre y primer apellido
                if (CaptureInfoAsPN && (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(FirstLastName))) return false;
                // Si no hay cambios
                if (!this.HasChanges()) return false;
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
                _ = Context.ActivateMasterViewAsync();
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

                CaptureType = BooksDictionaries.CaptureTypeEnum.PN;
                FirstName = string.Empty;
                MiddleName = string.Empty;
                FirstLastName = string.Empty;
                MiddleLastName = string.Empty;
                PrimaryPhone = string.Empty;
                SecondaryPhone = string.Empty;
                PrimaryCellPhone = string.Empty;
                SecondaryCellPhone = string.Empty;
                Address = string.Empty;
                Emails = new ObservableCollection<EmailDTO>();

                
                SelectedCountry = Context.Countries.FirstOrDefault(x => x.Code == "169"); // 169 es el cóodigo de colombia
                SelectedDepartment = SelectedCountry.Departments.FirstOrDefault(x => x.Code == "01"); // 08 es el código del atlántico
                SelectedCityId = SelectedDepartment.Cities.FirstOrDefault(x => x.Code == "001").Id; // 001 es el Codigo de Barranquilla
                this.AcceptChanges();
                this.SeedValue(nameof(CaptureType), CaptureType);
                this.SeedValue(nameof(SelectedCountry), SelectedCountry);
                this.SeedValue(nameof(SelectedDepartment), SelectedDepartment);
                this.SeedValue(nameof(SelectedIdentificationType), SelectedIdentificationType);
                
                this.SeedValue(nameof(SelectedCityId), SelectedCityId);
                this.SeedValue(nameof(Regime), Regime);

                foreach (CostCenterDTO costCenter in Context.CostCenters)
                {
                    costCenters.Add(new CostCenterDTO()
                    {
                        Id = costCenter.Id,
                        Name = costCenter.Name,
                        IsSelected = false
                    });
                }
                
                Zones = [.. Context.Zones];
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
                case "primaryphone":
                    PrimaryPhone = PrimaryPhone.ToPhoneFormat("### ## ##");
                    break;
                case "secondaryphone":
                    SecondaryPhone = SecondaryPhone.ToPhoneFormat("### ## ##");
                    break;
                case "primarycellphone":
                    PrimaryCellPhone = PrimaryCellPhone.ToPhoneFormat("### ### ## ##");
                    break;
                case "secondarycellphone":
                    SecondaryCellPhone = SecondaryCellPhone.ToPhoneFormat("### ### ## ##");
                    break;
                default:
                    break;
            }
        }
        private void ListenCostCenterChek()
        {
            foreach (var costCenter in CostCenters)
            {
                costCenter.PropertyChanged += CostCenter_PropertyChanged;
            }

            // Escuchar cuando se agregan nuevos elementos
            CostCenters.CollectionChanged += CostCenter_CollectionChanged;
        }
        private void CostCenter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CostCenterDTO.IsSelected))
            {
                // Aquí puedes actualizar otra propiedad del ViewModel si necesitas
                NotifyOfPropertyChange(() => CanSave);
            }
        }
        private void CostCenter_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Escuchar cambios de los nuevos elementos
            if (e.NewItems != null)
            {
                foreach (CostCenterDTO p in e.NewItems)
                    p.PropertyChanged += CostCenter_PropertyChanged;
            }

            // Opcional: dejar de escuchar eliminados
            if (e.OldItems != null)
            {
                foreach (CostCenterDTO p in e.OldItems)
                    p.PropertyChanged -= CostCenter_PropertyChanged;
            }

            NotifyOfPropertyChange(() => CanSave);
        }
       
        public async Task<UpsertResponseType<SellerGraphQLModel>> ExecuteSaveAsync()
        {

            try
            {
                List<object> emailList = new List<object>();

                if (Emails != null)
                {
                    foreach (EmailDTO email in Emails)
                    {
                        emailList.Add(new { email.Description, email.Email, email.isElectronicInvoiceRecipient });
                    }
                }
                if (IsNewRecord)
                {
                    string query = GetCreateQuery();
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    variables.createResponseInput.accountingEntity.emails =  emailList;
                      
                    UpsertResponseType<SellerGraphQLModel> sellerCreated = await _sellerService.CreateAsync<UpsertResponseType<SellerGraphQLModel>>(query, variables);
                    return sellerCreated;
                }
                else
                {
                    string query = GetUpdateQuery();

                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;

                    UpsertResponseType<SellerGraphQLModel> updatedSeller = await _sellerService.UpdateAsync<UpsertResponseType<SellerGraphQLModel>>(query, variables);
                    return updatedSeller;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        /*
        public async Task<SellerGraphQLModel> ExecuteSave()
        {
            string action = string.Empty;
            string query;
            try
            {
                
               
                List<object> emailList = new List<object>();
               
                if (Emails != null)
                {
                    foreach (EmailDTO email in Emails)
                    {
                        emailList.Add(new { email.Description, email.Email, email.isElectronicInvoiceRecipient });
                    }
                }

               
                

               

                variables.Data.Entity.VerificationDigit = "";
                variables.Data.Entity.BusinessName = "";
                variables.Data.Entity.Regime = "N";
                variables.Data.Entity.TradeName = "";
                variables.Data.Entity.CommercialCode = "";


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

               

        }*/
        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<SellerGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new SellerCreateMessage() { CreatedSeller = result }
                        : new SellerUpdateMessage() { UpdatedSeller = result }
                );
                await Context.ActivateMasterViewAsync();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }

        }

        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<SellerGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "seller", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.IsActive)
                   )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateSellerInput!");

            var fragment = new GraphQLQueryFragment("createSeller", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<SellerGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "seller", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.IsActive)
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateSellerInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateSeller", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
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

        public SellerDetailViewModel(
            SellerViewModel context,
            IRepository<SellerGraphQLModel> sellerService,
            IRepository<ZoneGraphQLModel> zoneService)
        {
            _errors = new Dictionary<string, List<string>>();
            Context = context;
            _sellerService = sellerService;
            _zoneService = zoneService;
        }

        public async Task Initialize()
        {
            Countries = Context.Countries;
            SelectedIdentificationType = Context.IdentificationTypes.FirstOrDefault(x => x.Code == "40"); // 13 es CC
           
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
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
          
           
            NotifyOfPropertyChange(nameof(CanSave));
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
                        if (string.IsNullOrEmpty(IdentificationNumber) || IdentificationNumber.Trim().Length < SelectedIdentificationType?.MinimumDocumentLength) AddError(propertyName, "El número de identificación no puede estar vacío");
                        break;
                    case nameof(FirstName):
                        if (string.IsNullOrEmpty(FirstName.Trim()) && CaptureInfoAsPN) AddError(propertyName, "El primer nombre no puede estar vacío");
                        break;
                    case nameof(FirstLastName):
                        if (string.IsNullOrEmpty(FirstLastName.Trim()) && CaptureInfoAsPN) AddError(propertyName, "El primer apellido no puede estar vacío");
                        break;
                    case nameof(PrimaryPhone):
                        if (value.Length != 7 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                        break;
                    case nameof(SecondaryPhone):
                        if (value.Length != 7 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono debe contener 7 digitos");
                        break;
                    case nameof(PrimaryCellPhone):
                        if (value.Length != 10 && !string.IsNullOrEmpty(value)) AddError(propertyName, "El número de teléfono celular debe contener 10 digitos");
                        break;
                    case nameof(SecondaryCellPhone):
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