using Caliburn.Micro;
using Common.Config;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;

using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Amazon.S3.Util.S3EventNotification;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;
using Extensions.Global;
namespace NetErp.Global.AuthorizationSequence.ViewModels
{
    public class AuthorizationSequenceDetailViewModel : Screen, INotifyDataErrorInfo
    {

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AuthorizationSequenceGraphQLModel> _authorizationSequenceService;


        public AuthorizationSequenceDetailViewModel(
            AuthorizationSequenceViewModel context,
            AuthorizationSequenceGraphQLModel? entity,
            Helpers.Services.INotificationService notificationService,
            IRepository<AuthorizationSequenceGraphQLModel> authorizationSequenceService,
            ObservableCollection<CostCenterDTO> costCenters
            )
        {

            _notificationService = notificationService;
            _authorizationSequenceService = authorizationSequenceService;
            CostCenters = costCenters;
            CostCenters.Insert(0, new CostCenterDTO() { Id = 0, Name = "SELECCIONE CENTRO DE COSTO" });
            Context = context;
            _errors = new Dictionary<string, List<string>>();


            if (entity != null)
            {
                _entity = entity;

            }

            Context.EventAggregator.SubscribeOnUIThread(this);

            var joinable = new JoinableTaskFactory(new JoinableTaskContext());

            joinable.Run(async () => await InitializeAsync());

        }
        public async Task InitializeAsync()
        {
            if (Entity != null)
            {
                SetUpdateProperties(Entity);
            }
            await LoadListAsync();
                
           
        }
        public  void SearchAuthorizationSequences()
        {
            try
            {
                IsBusy = true;
               
                Refresh();
                AuthorizationSequences = GetAuthorizationSequences.GetNumberingRange(RequestMethods.GetNumberingRange);
                AuthorizationSequences.Insert(0, new AuthorizationSequenceGraphQLModel() { Id = 9999999, Description = "SELECCIONE UNA AUTORIZACION" });
                SelectedAuthorizationSequence = AuthorizationSequences.First(f => f.Id == 9999999);
            }
            catch (Exception e)
            {

            }
            finally
            {
                IsBusy = false;
            }
        }
        public  void SetUpdateProperties(AuthorizationSequenceGraphQLModel entity)
        {
           
            Id = entity.Id;
            Number = entity.Number;
            Reference = entity.Reference;
            Prefix = entity.Prefix;
            TechnicalKey = entity.TechnicalKey;
            SelectedMode = entity.Mode;
            StartDate = entity.StartDate;
            EndDate = entity.EndDate;
            StartRange = entity.StartRange;
            EndRange = entity.EndRange;
            CurrentInvoiceNumber = entity.CurrentInvoiceNumber;
            IsActive = entity.IsActive;
        }
        #region DBProperties
        private AuthorizationSequenceGraphQLModel? _entity;
        public AuthorizationSequenceGraphQLModel Entity
        {
            get { return _entity; }
            set
            {
                if (_entity != value)
                {
                    _entity = value;
                    NotifyOfPropertyChange(nameof(OriginVisibility));

                }
            }
        }
        private int _id ;
        private int? _startRange;
        private int? _endRange;

        private string _number;
        private string _prefix;
        private string _technicalKey;
        private string _reference;
        private int? _currentInvoiceNumber;
        

        private DateTime? _startDate = DateTime.Now;
        private DateTime? _endDate = DateTime.Now;
        private bool _isActive = true;
        


        public int Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                }
            }
        }
        public int? StartRange
        {
            get { return _startRange; }
            set
            {
                if (_startRange != value)
                {
                    _startRange = value;
                   NotifyOfPropertyChange(nameof(StartRange));
                    ValidateProperty(nameof(StartRange), value);
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                }
            }
        }
        public int? EndRange
        {
            get { return _endRange; }
            set
            {
                if (_endRange != value)
                {
                    _endRange = value;
                    NotifyOfPropertyChange(nameof(EndRange));
                    ValidateProperty(nameof(EndRange), value);
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                }
            }
        }

        
        
        public string Number
        {
            get { return _number; }
            set
            {
                if (_number != value)
                {
                    _number = value;
                    NotifyOfPropertyChange(nameof(Number));
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                    ValidateProperty(nameof(Number), value);
                }
            }
        }


        public string Prefix
        {
            get { return _prefix; }
            set
            {
                if (_prefix != value)
                {
                    _prefix = value;
                    NotifyOfPropertyChange(nameof(Prefix));
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                    ValidateProperty(nameof(Prefix), value);
                    
                }
            }
        }

       

        public string TechnicalKey
        {
            get { return _technicalKey; }
            set
            {
                if (_technicalKey != value)
                {
                    _technicalKey = value;
                    NotifyOfPropertyChange(nameof(TechnicalKey));
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                    ValidateProperty(nameof(TechnicalKey), value);
                }
            }
        }

        public string Reference
        {
            get { return _reference; }
            set
            {
                if (_reference != value)
                {
                    _reference = value;
                    NotifyOfPropertyChange(nameof(Reference));
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                    ValidateProperty(nameof(Reference), value);
                }
            }
        }
        public int? CurrentInvoiceNumber
        {
            get { return _currentInvoiceNumber; }
            set
            {
                if (_currentInvoiceNumber != value)
                {
                    _currentInvoiceNumber = value;
                    NotifyOfPropertyChange(nameof(CurrentInvoiceNumber));
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                    ValidateProperty(nameof(CurrentInvoiceNumber), value);
                }
            }
        }


        public DateTime? StartDate
        {
            get { return _startDate; }
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    NotifyOfPropertyChange(nameof(StartDate));
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                }
            }
        }

        public DateTime? EndDate
        {
            get { return _endDate; }
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    NotifyOfPropertyChange(nameof(EndDate));
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                }
            }
        }

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                }
            }
        }

        private CostCenterDTO _selectedCostCenter;
        public CostCenterDTO SelectedCostCenter
        {
            get { return _selectedCostCenter; }
            set
            {
                if (_selectedCostCenter != value)
                {
                    _selectedCostCenter = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenter));
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                    ValidateProperty(nameof(SelectedCostCenter), value?.Id);
                }
            }
        }
        private AuthorizationSequenceGraphQLModel _selectedAuthorizationSequence;
        public AuthorizationSequenceGraphQLModel SelectedAuthorizationSequence
        {
            get { return _selectedAuthorizationSequence; }
            set
            {
                if (_selectedAuthorizationSequence != value)
                {
                    _selectedAuthorizationSequence = value;
                    if (value != null) { SetSelectedAuthorizationSequence(value);  }
                    if (value == null) { ClearValues(); }
                    NotifyOfPropertyChange(nameof(SelectedAuthorizationSequence));
                    if (value == null) { ClearValues(); }
                    NotifyOfPropertyChange(nameof(FieldsVisibility));
                    
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                }
            }
        }
        private AuthorizationSequenceGraphQLModel _selectedReliefAuthorizationSequence;
        public AuthorizationSequenceGraphQLModel SelectedReliefAuthorizationSequence
        {
            get { return _selectedReliefAuthorizationSequence; }
            set
            {
                if (_selectedReliefAuthorizationSequence != value)
                {
                    _selectedReliefAuthorizationSequence = value;
                    
                    NotifyOfPropertyChange(nameof(SelectedReliefAuthorizationSequence));
                    
                }
            }
        }
        private AuthorizationSequenceTypeGraphQLModel _selectedAuthorizationSequenceType;
        public AuthorizationSequenceTypeGraphQLModel SelectedAuthorizationSequenceType
        {
            get { return _selectedAuthorizationSequenceType; }
            set
            {
                if (_selectedAuthorizationSequenceType != value)
                {
                    _selectedAuthorizationSequenceType = value;
                    NotifyOfPropertyChange(nameof(SelectedAuthorizationSequenceType));
                    NotifyOfPropertyChange(nameof(TechnicalKey));
                    ValidateProperty(nameof(TechnicalKey), TechnicalKey);
                    ValidateProperty(nameof(SelectedAuthorizationSequenceType), value?.Id);
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                }
            }
        }
        // Regimen Seleccionado
        private char _selectedMode = 'A';
        public char SelectedMode
        {
            get { return _selectedMode; }
            set
            {
                if (_selectedMode != value)
                {
                    _selectedMode = value;
                    NotifyOfPropertyChange(nameof(SelectedMode));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        #endregion

        private Visibility _fieldsVisibility;
        public Visibility FieldsVisibility
        {
            get { return (SelectedSequenceOrigin.Equals(SequenceOriginEnum.M) || (SelectedSequenceOrigin.Equals(SequenceOriginEnum.D) && SelectedAuthorizationSequence != null && SelectedAuthorizationSequence?.Id != 9999999) ) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _fieldsVisibility = value;
            }
        }
        private Visibility _authorizationsVisibility;
        public Visibility AuthorizationsVisibility
        {
            get { return  SelectedSequenceOrigin.Equals(SequenceOriginEnum.D) && AuthorizationSequences?.Count > 0 ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _authorizationsVisibility = value;
            }
        }


        #region Properties
        private Visibility _lv1Visibility;
        public Visibility Lv1Visibility
        {
            get { return SelectedSequenceOrigin.Equals(SequenceOriginEnum.D) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _lv1Visibility = value;
            }
        }

        private Visibility _reliefVisibility;
        public Visibility ReliefVisibility
        {
            get { return LoadOrphan  ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _reliefVisibility = value;
            }
        }

        private Visibility _originVisibility;
        public Visibility OriginVisibility
        {
            get { return (Entity == null || Entity?.Id < 1) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _originVisibility = value;
            }
        }
        public bool LoadOrphan => (_entity != null && (_entity.CostCenter?.FeCashDefaultAuthorizationSequence?.Id == Entity.Id || _entity.CostCenter?.FeCreditDefaultAuthorizationSequence?.Id == Entity.Id) && (_entity.EndRange - _entity.CurrentInvoiceNumber) <= 50);
        public bool SequenceD => SelectedSequenceOrigin.Equals(SequenceOriginEnum.D);
        public bool EnabledAST => (Entity == null && (SelectedSequenceOrigin.Equals(SequenceOriginEnum.M) || (SelectedSequenceOrigin.Equals(SequenceOriginEnum.D) && string.IsNullOrEmpty(TechnicalKey)) ));
        private SequenceOriginEnum _selectedSequenceOrigin;
        public SequenceOriginEnum SelectedSequenceOrigin
        {
            get { return _selectedSequenceOrigin; }
            set
            {
                if (_selectedSequenceOrigin != value)
                {
                    _selectedSequenceOrigin = value;
                    NotifyOfPropertyChange(nameof(SelectedSequenceOrigin));
                    NotifyOfPropertyChange(nameof(Lv1Visibility));
                    NotifyOfPropertyChange(nameof(SequenceD));
                    NotifyOfPropertyChange(nameof(EnabledAST));
                    NotifyOfPropertyChange(nameof(CanSave));
                    NotifyOfPropertyChange(nameof(FieldsVisibility));
                    NotifyOfPropertyChange(nameof(AuthorizationsVisibility));
                    
                    ClearValues();
                }
            }
        }

        private AuthorizationSequenceViewModel _context;
        public AuthorizationSequenceViewModel Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }
        Dictionary<string, List<string>> _errors;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        private bool _isNewRecord => Entity?.Id > 0 ? false : true;

        public bool IsNewRecord
        {
            get { return _isNewRecord; }
            
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
        private ObservableCollection<AuthorizationSequenceGraphQLModel> _authorizationSequences;
        public ObservableCollection<AuthorizationSequenceGraphQLModel> AuthorizationSequences
        {
            get { return _authorizationSequences; }
            set
            {
                if (_authorizationSequences != value)
                {
                    _authorizationSequences = value;
                    NotifyOfPropertyChange(nameof(FieldsVisibility));
                    NotifyOfPropertyChange(nameof(AuthorizationSequences));
                    NotifyOfPropertyChange(nameof(AuthorizationsVisibility));

                }
            }
        }
        private ObservableCollection<CostCenterDTO> _costCenters;
        private ObservableCollection<AuthorizationSequenceGraphQLModel> _orphanAuthorizationSequences;
        public ObservableCollection<AuthorizationSequenceGraphQLModel> OrphanAuthorizationSequences
        {
            get { return _orphanAuthorizationSequences; }
            set
            {
                if (_orphanAuthorizationSequences != value)
                {
                    _orphanAuthorizationSequences = value;
                    NotifyOfPropertyChange(nameof(OrphanAuthorizationSequences));
                }
            }
        }
        public ObservableCollection<CostCenterDTO> CostCenters
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
        private ObservableCollection<AuthorizationSequenceTypeGraphQLModel> _avaliableauthorizationSequenceTypes;
        private ObservableCollection<AuthorizationSequenceTypeGraphQLModel> _authorizationSequenceTypes;

        public ObservableCollection<AuthorizationSequenceTypeGraphQLModel> AuthorizationSequenceTypes
        {
            get { return _authorizationSequenceTypes; }
            set
            {
                if (_authorizationSequenceTypes != value)
                {
                    _authorizationSequenceTypes = value;
                    NotifyOfPropertyChange(nameof(AuthorizationSequenceTypes));
                }
            }
        }
        public ObservableCollection<AuthorizationSequenceTypeGraphQLModel> AvaliableAuthorizationSequenceTypes
        {
            get { return _avaliableauthorizationSequenceTypes; }
            set
            {
                if (_avaliableauthorizationSequenceTypes != value)
                {
                    _avaliableauthorizationSequenceTypes = value;
                    NotifyOfPropertyChange(nameof(AvaliableAuthorizationSequenceTypes));
                }
            }
        }

        public Dictionary<char, string> ModeDictionary { get { return BooksDictionaries.ModeDictionary; } }
       
        #endregion

        #region Commands
        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync, CanSave);
                return _saveCommand;
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
        public bool CanGoBack(object p)
        {
            return !IsBusy;
        }
        public  void SetSelectedAuthorizationSequence(AuthorizationSequenceGraphQLModel authoritationSequence)
        {
            Number = authoritationSequence.Number;
            Prefix = authoritationSequence.Prefix;
            TechnicalKey = authoritationSequence.TechnicalKey;
            StartDate = authoritationSequence.StartDate;
            EndDate = authoritationSequence.EndDate;
            StartRange = authoritationSequence.StartRange;
            EndRange = authoritationSequence.EndRange;
            CurrentInvoiceNumber = authoritationSequence.StartRange;

            if (string.IsNullOrEmpty(authoritationSequence.TechnicalKey))
            {
                AvaliableAuthorizationSequenceTypes = Context.AutoMapper.Map<ObservableCollection<AuthorizationSequenceTypeGraphQLModel>>(AuthorizationSequenceTypes.Where(f => f.Prefix != "FE"));
                AvaliableAuthorizationSequenceTypes.Insert(0, new AuthorizationSequenceTypeGraphQLModel() { Id = 0, Name = "SELECCIONE TIPO" });
                SelectedAuthorizationSequenceType = AvaliableAuthorizationSequenceTypes.First(f => f.Id == 0);

            }
            else
            {
                SelectedAuthorizationSequenceType = AvaliableAuthorizationSequenceTypes.First(f => f.Prefix != "FE");
            }
            NotifyOfPropertyChange(nameof(EnabledAST));
        }
        public void ClearValues()
        {
            Number = "";
            Prefix = "";
            TechnicalKey = "";
            StartDate = null;
            EndDate = null;
            StartRange = null;
            EndRange = null;
            AvaliableAuthorizationSequenceTypes = [.. AuthorizationSequenceTypes];
            AvaliableAuthorizationSequenceTypes.Insert(0, new AuthorizationSequenceTypeGraphQLModel() { Id = 0, Name = "SELECCIONE TIPO" });

        }
        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<AuthorizationSequenceGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new AuthorizationSequenceCreateMessage() { CreatedAuthorizationSequence = result }
                        : new AuthorizationSequenceUpdateMessage() { UpdatedAuthorizationSequence = result }
                );

                // Context.EnableOnViewReady = false;
                await Context.ActivateMasterViewModelAsync();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{graphQLError.Errors[0].Extensions.Message} {graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "Save" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
                

        }

        public async Task<UpsertResponseType<AuthorizationSequenceGraphQLModel>> ExecuteSaveAsync()
        {
            //Campos comunes para creacion y edicion
            dynamic responseInput = new ExpandoObject();
            responseInput.number = Number;
            responseInput.description = $"AUTORIZACION DIAN No. {Number} de {StartDate}, prefijo: {Prefix} del {StartRange} al {EndRange}";  //Consultar
          
            responseInput.mode = SelectedMode;
            responseInput.technicalKey = TechnicalKey;
            responseInput.reference = Reference;
            responseInput.isActive = IsActive;
            responseInput.startDate = StartDate.Value.ToString("yyyy-MM-dd");
            responseInput.endDate = EndDate.Value.ToString("yyyy-MM-dd");
            responseInput.prefix = Prefix;
            responseInput.startRange = StartRange;
            responseInput.endRange = EndRange;
            responseInput.currentInvoiceNumber = CurrentInvoiceNumber;

            dynamic variables = new ExpandoObject();
           

            if (IsNewRecord)
            {
                //Campos solo para creacion
                responseInput.authorizationSequenceTypeId = SelectedAuthorizationSequenceType.Id;
                responseInput.costCenterId = SelectedCostCenter.Id;

                variables.createResponseInput = responseInput;
                string query = GetCreateQuery();
                UpsertResponseType<AuthorizationSequenceGraphQLModel> AuthorizationCreated = await _authorizationSequenceService.CreateAsync<UpsertResponseType<AuthorizationSequenceGraphQLModel>>(query, variables);
                return AuthorizationCreated;
            }
            else
            {
                if (SelectedReliefAuthorizationSequence != null) { responseInput.nextAuthorizationSequenceId = SelectedReliefAuthorizationSequence.Id; }
               
                string query = GetUpdateQuery();
                variables.updateResponseInput = responseInput;
                variables.UpdateResponseId = Entity.Id;
                UpsertResponseType<AuthorizationSequenceGraphQLModel> updatedAuthorization = await _authorizationSequenceService.UpdateAsync<UpsertResponseType<AuthorizationSequenceGraphQLModel>>(query, variables);
                return updatedAuthorization;
            }
            
        }

        #endregion

                #region Validaciones
        public bool CanSave
        {
            get
            {
                if (SelectedCostCenter == null || SelectedCostCenter.Id == 0) return false;
                if (string.IsNullOrEmpty(SelectedMode.ToString())) return false;
                if (SelectedAuthorizationSequenceType == null || SelectedAuthorizationSequenceType.Id == 0) return false;
                if (_errors.Count > 0) { return false;  }

                return true;
            }
        }
        public bool EnabledToCreated
        {
            get
            {
                if (Entity == null ) return true;
               

                return false;
            }
        }
        public bool HasErrors => _errors.Count > 0;

        protected override void OnViewReady(object view)
        {
            SelectedSequenceOrigin = SequenceOriginEnum.M;
            base.OnViewReady(view);
            this.SetFocus(() => Number);
           
            ValidateProperties();
        }
        public void GoBack(object p)
        {
            CleanUpControls();
            _ = Task.Run(() => Context.ActivateMasterViewModelAsync());
           
        }
        public void CleanUpControls()
        {
            Number = "";
            Reference = "";
            SelectedCostCenter = CostCenters.First(f => f.Id == 0);
            SelectedSequenceOrigin = SequenceOriginEnum.M;
        }
        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
        }
        private void ValidateProperty(string propertyName, int? value)
        {
            try
            {

                ClearErrors(propertyName);
                switch (propertyName)
                {

                    case nameof(SelectedCostCenter):
                        if (!value.HasValue || value == 0) AddError(propertyName, "Debe seleccionar un centro de costo");
                        break;
                    case nameof(SelectedAuthorizationSequenceType):
                        if (!value.HasValue || value == 0) AddError(propertyName, "Debe seleccionar un tipo de autorización");
                        break;
                    case nameof(StartRange):
                        if (!value.HasValue) AddError(propertyName, "El rango inicial no puede estar vacío");
                        break;
                    case nameof(EndRange):
                        if (!value.HasValue) AddError(propertyName, "El rango inicial no puede estar vacío");
                        break;
                    case nameof(CurrentInvoiceNumber):
                        if (!value.HasValue) AddError(propertyName, "El número de factura no puede estar vacío");
                        if (value.HasValue && (value < StartRange || value > EndRange)) AddError(propertyName, "El número de factura debe estar dentro del rango");
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            try
            {

                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(Number):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "El número de Autorizacion no puede estar vacío");
                        break;
                    case nameof(Prefix):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "El prefijo no puede estar vacío");
                        if(!string.IsNullOrEmpty(value) && int.TryParse(value.Substring(value.Length - 1, 1)[0].ToString(), out int numericValue)) AddError(propertyName, "El ultimo carácter no debe ser numérico ");
                        break;
                    case nameof(Reference):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "La Referencia no puede estar vacío");
                        break;
                    case nameof(TechnicalKey):
                        if (string.IsNullOrEmpty(value) && SelectedAuthorizationSequenceType?.Prefix == "FE") AddError(propertyName, "La Clave técnica no puede estar vacío");
                        break;
                   
                  


                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
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
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
        private void ValidateProperties()
        {
            ValidateProperty(nameof(SelectedCostCenter), SelectedCostCenter?.Id);
            ValidateProperty(nameof(SelectedAuthorizationSequenceType), SelectedAuthorizationSequenceType?.Id);

            ValidateProperty(nameof(Number), Number);
            ValidateProperty(nameof(Prefix), Prefix);
            ValidateProperty(nameof(Reference), Reference);
            ValidateProperty(nameof(TechnicalKey), TechnicalKey);
            ValidateProperty(nameof(CurrentInvoiceNumber), CurrentInvoiceNumber);
            ValidateProperty(nameof(StartRange), StartRange);
            ValidateProperty(nameof(EndRange), EndRange);


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
        #endregion

        #region ApiMethods
      
        
        private async Task LoadListAsync()
        {
            string query = GetLoadQueries();
            dynamic variables = new ExpandoObject();
            try
            {
                IsBusy = true;
                variables.filter = new ExpandoObject();
                if (LoadOrphan)
                {
                    variables.authorizationSequencesFilters = new ExpandoObject();
                    variables.authorizationSequencesFilters.isActive = true;
                    variables.authorizationSequencesFilters.CostCenterId = _entity.CostCenter.Id;
                    //variables.authorizationSequencesFilters.nextAuthorizationSequenceId = null;
                    variables.authorizationSequencesFilters.endDateFrom = DateTime.Today.ToString("yyyy-MM-dd");
                }

                AuthorizationSequenceDetailDataContext source = await _authorizationSequenceService.GetDataContextAsync<AuthorizationSequenceDetailDataContext>(query, variables);

                AuthorizationSequenceTypes = Context.AutoMapper.Map<ObservableCollection<AuthorizationSequenceTypeGraphQLModel>>(source.AuthorizationSequenceTypes.Entries);
                AvaliableAuthorizationSequenceTypes = Context.AutoMapper.Map<ObservableCollection<AuthorizationSequenceTypeGraphQLModel>>(source.AuthorizationSequenceTypes.Entries);
                AvaliableAuthorizationSequenceTypes.Insert(0, new AuthorizationSequenceTypeGraphQLModel() { Id = 0, Name = "SELECCIONE TIPO" });
                if (LoadOrphan)
                {
                        var Orphans = source.AuthorizationSequences?.Entries.Where(f => f.NextAuthorizationSequence == null).Where(f => f.Id != Entity.Id);
                        var selected = Orphans.FirstOrDefault(f => f.Id == Entity.NextAuthorizationSequence?.Id);
                        if (Entity.NextAuthorizationSequence != null && Orphans.FirstOrDefault(f => f.Id == Entity.NextAuthorizationSequence?.Id) == null)
                        {
                            Orphans.Append(Entity.NextAuthorizationSequence);
                        }
                    OrphanAuthorizationSequences = Context.AutoMapper.Map<ObservableCollection<AuthorizationSequenceGraphQLModel>>(Orphans);

                }

                if (!IsNewRecord)
                {
                    SelectedCostCenter = CostCenters.First(f => f.Id == _entity?.CostCenter.Id);
                    SelectedAuthorizationSequenceType = AvaliableAuthorizationSequenceTypes.First(f => f.Id == _entity?.AuthorizationSequenceType.Id);

                }
                else
                {
                    SelectedCostCenter = CostCenters.First(f => f.Id == 0);
                    SelectedAuthorizationSequenceType = AvaliableAuthorizationSequenceTypes.First(f => f.Id == 0);

                }
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                IsBusy = false;
            }

        }
        
        public string GetLoadQueries()
        {
        
            var authorizationSequenceTypeFields = FieldSpec<PageType<AuthorizationSequenceTypeGraphQLModel>>
               .Create()
               .SelectList(it => it.Entries, entries => entries
                   .Field(e => e.Id)

                   .Field(e => e.Name)
                   .Field(e => e.IsActive)
                   .Field(e => e.Prefix)
                   
                  
               )
               .Field(o => o.PageNumber)
               .Field(o => o.PageSize)
               .Field(o => o.TotalPages)
               .Field(o => o.TotalEntries)
               .Build();
            
            var authorizationSequenceTypefilterParameters = new GraphQLQueryParameter("filters", "AuthorizationSequenceTypeFilters");
            var authorizationSequenceTypeFragment = new GraphQLQueryFragment("authorizationSequenceTypesPage", [authorizationSequenceTypefilterParameters], authorizationSequenceTypeFields, "AuthorizationSequenceTypes");
            var authorizationFields = FieldSpec<PageType<AuthorizationSequenceGraphQLModel>>
              .Create()
              .SelectList(it => it.Entries, entries => entries
                  .Field(e => e.Id)

                  .Field(e => e.Description)
                  .Field(e => e.Number)
                  .Field(e => e.IsActive)
                  .Field(e => e.Prefix)
                  .Field(e => e.CurrentInvoiceNumber)
                  .Field(e => e.Mode)
                  .Field(e => e.TechnicalKey)
                  .Field(e => e.Reference)
                  .Field(e => e.StartDate)
                  .Field(e => e.EndDate)
                  .Field(e => e.StartRange)
                  .Field(e => e.EndRange)
                  .Select(e => e.NextAuthorizationSequence, cat => cat
                      .Field(c => c.Id)
                      .Field(c => c.Description)

                  )
                  .Select(e => e.CostCenter, cat => cat
                      .Field(c => c.Id)
                      .Field(c => c.Name)
                      .Select(e => e.FeCreditDefaultAuthorizationSequence, dep => dep
                              .Field(d => d.Id)
                              .Field(d => d.Description)
                          )
                      .Select(e => e.FeCashDefaultAuthorizationSequence, dep => dep
                              .Field(d => d.Id)
                              .Field(d => d.Description)
                          )
                  )
                  .Select(e => e.AuthorizationSequenceType, cat => cat
                      .Field(c => c.Id)
                      .Field(c => c.Name)
                  )
              )
              .Field(o => o.PageNumber)
              .Field(o => o.PageSize)
              .Field(o => o.TotalPages)
              .Field(o => o.TotalEntries)
              .Build();

            var authorizationPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var authorizationfilterParameters = new GraphQLQueryParameter("filters", "AuthorizationSequenceFilters");
            var authorizationFragment = new GraphQLQueryFragment("authorizationSequencesPage", [authorizationPagParameters, authorizationfilterParameters], authorizationFields,  "AuthorizationSequences");
            var builder = LoadOrphan ? new GraphQLQueryBuilder([authorizationFragment, authorizationSequenceTypeFragment]) :  new GraphQLQueryBuilder([authorizationSequenceTypeFragment]);
            return builder.GetQuery();
        }
        public string GetCreateQuery()
        {           
            var fields = FieldSpec<UpsertResponseType<AuthorizationSequenceGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "authorizationSequence", nested: sq => sq
                   .Field(e => e.Id)
                   .Field(e => e.Description)
                   .Field(e => e.Prefix)
                    .Field(e => e.CurrentInvoiceNumber)
                    .Field(e => e.StartRange)
                    .Field(e => e.EndRange)
                    .Field(e => e.StartDate)
                    .Field(e => e.EndDate)
                    .Field(e => e.IsActive)

                    .Select(e => e.CostCenter, cos => cos
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                     )
                    .Select(e => e.AuthorizationSequenceType, type => type
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                    )
                    
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Field)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateAuthorizationSequenceInput!");

            var fragment = new GraphQLQueryFragment("createAuthorizationSequence", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AuthorizationSequenceGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "authorizationSequence", nested: sq => sq
                    .Field(e => e.Id)
                   .Field(e => e.Description)
                   .Field(e => e.Prefix)
                    .Field(e => e.CurrentInvoiceNumber)
                    .Field(e => e.StartRange)
                    .Field(e => e.EndRange)
                    .Field(e => e.StartDate)
                    .Field(e => e.EndDate)
                    .Field(e => e.IsActive)

                    .Select(e => e.CostCenter, cos => cos
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                     )
                    .Select(e => e.AuthorizationSequenceType, type => type
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                    )

                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Field)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("input", "UpdateAuthorizationSequenceInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateAuthorizationSequence", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        #endregion
        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {

            // Desconectar eventos para evitar memory leaks
            Context.EventAggregator.Unsubscribe(this);
            return base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
