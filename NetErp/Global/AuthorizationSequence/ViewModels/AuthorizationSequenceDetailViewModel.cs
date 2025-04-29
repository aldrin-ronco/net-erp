using Caliburn.Micro;
using Common.Config;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.Xpo.DB.Helpers;
using Dictionaries;
using DTOLibrary.Books;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Books.WithholdingCertificateConfig.ViewModels;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers;
using Ninject.Activation;
using Services.Books.DAL.PostgreSQL;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using static Amazon.S3.Util.S3EventNotification;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;
using static Dictionaries.BooksDictionaries;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NetErp.Global.AuthorizationSequence.ViewModels
{
    public class AuthorizationSequenceDetailViewModel : Screen, INotifyDataErrorInfo
    {
        public readonly IGenericDataAccess<CostCenterGraphQLModel> CostCenterService = IoC.Get<IGenericDataAccess<CostCenterGraphQLModel>>();
        public readonly IGenericDataAccess<AuthorizationSequenceTypeGraphQLModel> AuthorizationSequenceTypeService = IoC.Get<IGenericDataAccess<AuthorizationSequenceTypeGraphQLModel>>();
        public readonly IGenericDataAccess<AuthorizationSequenceGraphQLModel> AuthorizationSequenceService = IoC.Get<IGenericDataAccess<AuthorizationSequenceGraphQLModel>>();

        public AuthorizationSequenceDetailViewModel(AuthorizationSequenceViewModel context, AuthorizationSequenceGraphQLModel? entity)
        {
            Context = context;
            _errors = new Dictionary<string, List<string>>();
           

            if(entity != null)
            {
                _entity = entity;
                setUpdateProperties(entity);
            }

          
              Context.EventAggregator.SubscribeOnUIThread(this);
              var joinable = new JoinableTaskFactory(new JoinableTaskContext());
              joinable.Run(async () => await Initialize());
        }
        public async Task Initialize()
        {

            await LoadListAsync();
        }
        public async Task SearchSequences()
        {
            try
            {
                IsBusy = true;
                Refresh();
                Sequences = GetSequences.GetNumberingRange(RequestMethods.GetNumberingRange);
            }
            catch (Exception e)
            {

            }
            finally
            {
                IsBusy = false;
            }
        }
        public  void setUpdateProperties(AuthorizationSequenceGraphQLModel entity)
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
            CurrentInvoiceNumber = entity.CurrentInvoceNumber;
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
        private string _currentInvoiceNumber;
        

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
        public string CurrentInvoiceNumber
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
                }
            }
        }
        private SequenceDto _selectedSecuence;
        public SequenceDto SelectedSecuence
        {
            get { return _selectedSecuence; }
            set
            {
                if (_selectedSecuence != value)
                {
                    _selectedSecuence = value;
                    if (value != null) { setSelectedSecuence(value);  }
                    if (value == null) { ClearValues(); }
                    NotifyOfPropertyChange(nameof(SelectedSecuence));
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
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




        #region Properties
        private Visibility _lv1Visibility;
        public Visibility Lv1Visibility
        {
            get { return SelectedSecuenceOrigin.Equals(SecuenceOriginEnum.D) ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _lv1Visibility = value;
            }
        }

        private Visibility _originVisibility;
        public Visibility OriginVisibility
        {
            get { return Entity?.Id < 1 ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                _originVisibility = value;
            }
        }

        public bool SequenceD => SelectedSecuenceOrigin.Equals(SecuenceOriginEnum.D);
        public bool EnabledAST => (SelectedSecuenceOrigin.Equals(SecuenceOriginEnum.M) || (SelectedSecuenceOrigin.Equals(SecuenceOriginEnum.D) && string.IsNullOrEmpty(TechnicalKey) ));
        private SecuenceOriginEnum _selectedSecuenceOrigin = SecuenceOriginEnum.M;
       public SecuenceOriginEnum SelectedSecuenceOrigin
        {
            get { return _selectedSecuenceOrigin; }
            set
            {
                if (_selectedSecuenceOrigin != value)
                {
                    _selectedSecuenceOrigin = value;
                    NotifyOfPropertyChange(nameof(SelectedSecuenceOrigin));
                    NotifyOfPropertyChange(nameof(Lv1Visibility));
                    NotifyOfPropertyChange(nameof(SequenceD));
                    NotifyOfPropertyChange(nameof(EnabledAST));
                    NotifyOfPropertyChange(nameof(CanSave));
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
        private ObservableCollection<SequenceDto> _sequences;
        public ObservableCollection<SequenceDto> Sequences
        {
            get { return _sequences; }
            set
            {
                if (_sequences != value)
                {
                    _sequences = value;
                    NotifyOfPropertyChange(nameof(Sequences));
                }
            }
        }
        private ObservableCollection<CostCenterDTO> _costCenters;

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
                if (_saveCommand is null) _saveCommand = new AsyncCommand(Save, CanSave);
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
        public  void setSelectedSecuence(SequenceDto sequence)
        {
            Number = sequence.ResolutionNumber;
            Prefix = sequence.Prefix;
            TechnicalKey = sequence.TechnicalKey;
            StartDate = sequence.ValidDateFrom;
            EndDate = sequence.ValidDateTo;
            StartRange = sequence.FromNumber;
            EndRange = sequence.ToNumber;
            if (string.IsNullOrEmpty(sequence.TechnicalKey))
            {
                AvaliableAuthorizationSequenceTypes = Context.AutoMapper.Map<ObservableCollection<AuthorizationSequenceTypeGraphQLModel>>(AuthorizationSequenceTypes.Where(f => f.Prefix != "FE"));
                AvaliableAuthorizationSequenceTypes.Insert(0, new AuthorizationSequenceTypeGraphQLModel() { Id = 0, Name = "SELECCIONE TIPO" });

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
        public async Task Save()
        {
            try
            {
                IsBusy = true;
                Refresh();
                AuthorizationSequenceGraphQLModel result = await ExecuteSave();
                var pageResult = await LoadPage();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new AuthorizationSequenceCreateMessage() { CreatedAuthorizationSequence =result, AuthorizationSequences = pageResult.PageResponse.Rows });
                }
                else
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new AuthorizationSequenceUpdateMessage() { UpdatedAuthorizationSequence = result, AuthorizationSequences = pageResult.PageResponse.Rows });
                }
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

        public async Task<AuthorizationSequenceGraphQLModel> ExecuteSave()
        {
            dynamic variables = new ExpandoObject();
            variables.Data = new ExpandoObject();
            variables.Data.number = Number;
            variables.Data.description = $"AUTORIZACION DIAN No. {Number} de {StartDate}, prefijo: {Prefix} del {StartRange} al {EndRange}";  //Consultar
            variables.Data.costCenterId = SelectedCostCenter.Id;
            variables.Data.mode = SelectedMode;
            variables.Data.technicalKey = TechnicalKey;
            variables.Data.reference = Reference;
            variables.Data.isActive = IsActive;
            variables.Data.startDate = StartDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");

            variables.Data.endDate = EndDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
            variables.Data.authorizationSequenceTypeId = SelectedAuthorizationSequenceType.Id;
            variables.Data.prefix = Prefix;
            variables.Data.startRange = StartRange;
            variables.Data.endRange = EndRange;
            variables.Data.currentInvoiceNumber = CurrentInvoiceNumber;
            if (IsNewRecord)
            {
                return await CreateAsync(variables);
            }
            else
            {
                return await UpdateAsync(variables);
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
        public bool HasErrors => _errors.Count > 0;

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => Number);
            ValidateProperties();
        }
        public void GoBack(object p)
        {
            _ = Task.Run(() => Context.ActivateMasterViewModelAsync());
            CleanUpControls();
        }
        public void CleanUpControls()
        {
            Number = "";
            Reference = "";
            SelectedCostCenter = CostCenters.First(f => f.Id == 0);
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
                    case nameof(StartRange):
                        if (!value.HasValue) AddError(propertyName, "El rango inicial no puede estar vacío");
                        break;
                    case nameof(EndRange):
                        if (!value.HasValue) AddError(propertyName, "El rango inicial no puede estar vacío");
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
                        break;
                    case nameof(Reference):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "La Referencia no puede estar vacío");
                        break;
                    case nameof(TechnicalKey):
                        if (string.IsNullOrEmpty(value) && SelectedAuthorizationSequenceType?.Prefix == "FE") AddError(propertyName, "La Clave técnica no puede estar vacío");
                        break;
                    case nameof(CurrentInvoiceNumber):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "El número de factura no puede estar vacío");
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
        private async Task<AuthorizationSequenceGraphQLModel> CreateAsync(dynamic variables)
        {
            try
            {
                IsBusy = true;
                var query = @"
                    mutation($data: CreateAuthorizationSequenceInput!){
                      CreateResponse: createAuthorizationSequence(data: $data){
                        id
                        description
                        costCenter  {
                          id
                          name
                        }
                        authorizationSequenceType {
                          id
                          name
                        }
                        startRange
                        endDate
                        startDate
                        endDate
                        prefix
                        currentInvoiceNumber
                      }
                    }";

                AuthorizationSequenceGraphQLModel record = await AuthorizationSequenceService.Create(query, variables);
                return record;
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
        public async Task<AuthorizationSequenceGraphQLModel> UpdateAsync(dynamic variables)
        {
            try
            {

                var query = @"
                    mutation($data: UpdateAuthorizationSequenceInput!, $id : Int!){
                     UpdateResponse: updateAuthorizationSequence(data: $data, id: $id){
                        id
                        description
                        costCenter  {
                          id
                          name
                        }
                        authorizationSequenceType {
                          id
                        }
                        startRange
                        endDate
                        startDate
                        endDate
                        prefix
                        currentInvoiceNumber
                      }
                    }";
                variables.id = Id;
                return await AuthorizationSequenceService.Update(query, variables);
               
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private async Task LoadListAsync()
        {
            string query = @"
                            query(){
                               authorizationSequenceTypes(){
                                   id
                                   name
                                   prefix
                                  },
                                  costCenters(){
                                    id
                                    name
                                    address
                                    city  {
                                      id
                                      name
                                      department {
                                       id
                                       name
                                      } 
                                    }
  
                                  }
                            }";
            dynamic variables = new ExpandoObject();
            try
            {
                IsBusy = true;
           
            AuthorizationSequenceDataContext source = await CostCenterService.GetDataContext<AuthorizationSequenceDataContext>(query, variables);

            CostCenters = Context.AutoMapper.Map<ObservableCollection<CostCenterDTO>>(source.CostCenters);
            AuthorizationSequenceTypes = Context.AutoMapper.Map<ObservableCollection<AuthorizationSequenceTypeGraphQLModel>>(source.AuthorizationSequenceTypes);
            AvaliableAuthorizationSequenceTypes = Context.AutoMapper.Map<ObservableCollection<AuthorizationSequenceTypeGraphQLModel>>(source.AuthorizationSequenceTypes);
            AvaliableAuthorizationSequenceTypes.Insert(0, new AuthorizationSequenceTypeGraphQLModel() { Id = 0, Name = "SELECCIONE TIPO" });

            CostCenters.Insert(0, new CostCenterDTO() { Id = 0, Name = "SELECCIONE CENTRO DE COSTO" });

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
            catch (Exception e)
            {

            }
            finally
            {
                IsBusy = false;
            }

        }
        

        public async Task<IGenericDataAccess<AuthorizationSequenceGraphQLModel>.PageResponseType> LoadPage()
        {
            string query = @"
               query( $filter: AuthorizationSequenceFilterInput!){
                      PageResponse: authorizationSequencePage(filter: $filter){
                        count
                        rows {
                          id
                          description
                          costCenter  {
                              id
                              name
                            }
                            authorizationSequenceType {
                              id
                            }
                            startRange
                            endDate
                            startDate
                            endDate
                            prefix
                            currentInvoiceNumber
                        }
                      }
                    }
                ";

            dynamic variables = new ExpandoObject();
            variables.filter = new ExpandoObject();
            variables.filter.Pagination = new ExpandoObject();
            variables.filter.Pagination.Page = 1;
            variables.filter.Pagination.PageSize = 50;

            return await AuthorizationSequenceService.GetPage(query,variables);
        }
            #endregion
        }
}
