using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Billing;
using NetErp.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp.Billing.DocumentSequence.ViewModels
{
    public class DocumentSequenceDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Properties

        Dictionary<string, List<string>> _errors;

        public readonly IGenericDataAccess<DocumentSequenceMasterGraphQLModel> DocumentSequenceMasterService = IoC.Get<IGenericDataAccess<DocumentSequenceMasterGraphQLModel>>();

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    ValidateProperty(nameof(Id));
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _number;
        public string Number
        {
            get => _number;
            set
            {
                if (_number != value)
                {
                    _number = value;
                    ValidateProperty(nameof(Number));
                    NotifyOfPropertyChange(nameof(Number));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private DateTime? _initialDate;
        public DateTime? InitialDate
        {
            get => _initialDate;
            set
            {
                if (_initialDate != value)
                {
                    _initialDate = value;
                    ValidateProperty(nameof(InitialDate));
                    NotifyOfPropertyChange(nameof(InitialDate));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private DateTime? _finalDate;
        public DateTime? FinalDate
        {
            get => _finalDate;
            set
            {
                if (_finalDate != value)
                {
                    _finalDate = value;
                    ValidateProperty(nameof(FinalDate));
                    NotifyOfPropertyChange(nameof(FinalDate));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _prefix;
        public string Prefix
        {
            get => _prefix;
            set
            {
                if (_prefix != value)
                {
                    _prefix = value;
                    ValidateProperty(nameof(Prefix));
                    NotifyOfPropertyChange(nameof(Prefix));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _initialNumber;
        public int InitialNumber
        {
            get => _initialNumber;
            set
            {
                if (_initialNumber != value)
                {
                    _initialNumber = value;
                    ValidateProperty(nameof(InitialNumber));
                    ValidateProperty(nameof(FinalNumber));
                    ValidateProperty(nameof(ActualNumber));
                    NotifyOfPropertyChange(nameof(InitialNumber));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _finalNumber;
        public int FinalNumber
        {
            get => _finalNumber;
            set
            {
                if (_finalNumber != value)
                {
                    _finalNumber = value;
                    ValidateProperty(nameof(InitialNumber));
                    ValidateProperty(nameof(FinalNumber));
                    ValidateProperty(nameof(ActualNumber));
                    NotifyOfPropertyChange(nameof(FinalNumber));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _actualNumber;
        public int ActualNumber
        {
            get => _actualNumber;
            set
            {
                if (_actualNumber != value)
                {
                    _actualNumber = value;
                    ValidateProperty(nameof(InitialNumber));
                    ValidateProperty(nameof(FinalNumber));
                    ValidateProperty(nameof(ActualNumber));
                    NotifyOfPropertyChange(nameof(ActualNumber));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _reference;
        public string Reference
        {
            get => _reference;
            set
            {
                if (_reference != value)
                {
                    _reference = value;
                    ValidateProperty(nameof(Reference));
                    NotifyOfPropertyChange(nameof(Reference));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _isActiveAuthorization;
        public bool IsActiveAuthorization
        {
            get => _isActiveAuthorization;
            set
            {
                if (_isActiveAuthorization != value)
                {
                    _isActiveAuthorization = value;
                    ValidateProperty(nameof(IsActive));
                    NotifyOfPropertyChange(nameof(IsActive));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _technicalKey;
        public string TechnicalKey
        {
            get => _technicalKey;
            set
            {
                if (_technicalKey != value)
                {
                    _technicalKey = value;
                    ValidateProperty(nameof(TechnicalKey));
                    NotifyOfPropertyChange(nameof(TechnicalKey));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _selectedCostCenterId;
        public int SelectedCostCenterId
        {
            get => _selectedCostCenterId;
            set
            {
                if (_selectedCostCenterId != value)
                {
                    _selectedCostCenterId = value;
                    ValidateProperty(nameof(SelectedCostCenterId));
                    NotifyOfPropertyChange(nameof(SelectedCostCenterId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool CanSave => _errors.Count <= 0 && !IsBusy;

        public bool IsNewRecord => Id == 0;

        public bool IsElectronic => SelectedAuthorizationType == "ELECTRONICA";

        public DocumentSequenceViewModel Context { get; private set; }
        public Dictionary<string, string> AuthorizationKindDictionary => Dictionaries.BillingDictionaries.BillingDocumentSequenceAuthorizationKindDictionary;

        private string _selectedAuthorizationKind = "AUTORIZA";
        public string SelectedAuthorizationKind
        {
            get => _selectedAuthorizationKind;
            set
            {
                if (_selectedAuthorizationKind != value)
                {
                    _selectedAuthorizationKind = value;
                    ValidateProperty(nameof(SelectedAuthorizationKind));
                    NotifyOfPropertyChange(nameof(SelectedAuthorizationKind));
                    ValidateProperty(nameof(CanSave));
                }
            }
        }

        public Dictionary<string, string> SequenceLabelDictionary => Dictionaries.BillingDictionaries.BillingDocumentSequenceLabelDictionary;

        private string _selectedSequenceLabel = "FACTURA DE VENTA";
        public string SelectedSequenceLabel
        {
            get => _selectedSequenceLabel;
            set
            {
                if (_selectedSequenceLabel != value)
                {
                    _selectedSequenceLabel = value;
                    ValidateProperty(nameof(SelectedSequenceLabel));
                    NotifyOfPropertyChange(nameof(SelectedSequenceLabel));
                    ValidateProperty(nameof(CanSave));
                }
            }
        }

        public Dictionary<string, string> TitleLabelDictionary => Dictionaries.BillingDictionaries.BillingDocumentSequenceTitleLabelDictionary;
        private string _selectedTitleLabel = "AUT. DE NUMERACIÓN DE FACTURACIÓN";
        public string SelectedTitleLabel
        {
            get => _selectedTitleLabel;
            set
            {
                if (_selectedTitleLabel != value)
                {
                    _selectedTitleLabel = value;
                    ValidateProperty(nameof(SelectedTitleLabel));
                    NotifyOfPropertyChange(nameof(SelectedTitleLabel));
                    ValidateProperty(nameof(CanSave));
                }
            }
        }

        public Dictionary<string, string> AuthorizationTypeDictionary => Dictionaries.BillingDictionaries.BillingDocumentSequenceAuthorizationTypeDictionary;
        private string _selectedAuthorizationType = "PAPEL";
        public string SelectedAuthorizationType
        {
            get => _selectedAuthorizationType;
            set
            {
                if (_selectedAuthorizationType != value)
                {
                    _selectedAuthorizationType = value;
                    ValidateProperty(nameof(SelectedAuthorizationType));
                    NotifyOfPropertyChange(nameof(SelectedAuthorizationType));
                    NotifyOfPropertyChange(nameof(IsElectronic));
                    ValidateProperty(nameof(TechnicalKey));
                    ValidateProperty(nameof(CanSave));
                }
            }
        }

        #endregion

        #region Methods

        public async Task GoBack()
        {
            await Context.ActivateMasterView();
        }

        public async Task Save()
        {
            try
            {
                IsBusy = true;
                Refresh();
                DocumentSequenceMasterGraphQLModel result = await ExecuteSave();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new DocumentSequenceCreateMessage() { CreatedDocumentSequence = result});
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new DocumentSequenceUpdateMessage() { UpdatedDocumentSequence = result });
                }
                await Context.ActivateMasterView();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<DocumentSequenceMasterGraphQLModel> ExecuteSave()
        {
            string query;
            try
            {
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Data.DocumentSequenceDetail = new ExpandoObject();
                if (!IsNewRecord) variables.Id = Id;
                variables.Data.CostCenterId = SelectedCostCenterId;
                variables.Data.Number = Number;
                variables.Data.InitialDate = InitialDate;
                variables.Data.FinalDate = FinalDate;
                variables.Data.Prefix = Prefix;
                variables.Data.InitialNumber = InitialNumber;
                variables.Data.FinalNumber = FinalNumber;
                variables.Data.TitleLabel = SelectedTitleLabel;
                variables.Data.SequenceLabel = SelectedSequenceLabel;
                variables.Data.AuthorizationType = SelectedAuthorizationType;
                variables.Data.AuthorizationKind = SelectedAuthorizationKind;
                variables.Data.Reference = Reference;
                variables.Data.IsActive = IsActiveAuthorization;
                variables.Data.TechnicalKey = TechnicalKey;
                variables.Data.DocumentSequenceDetail.Number = ActualNumber;
                query = IsNewRecord ? @"mutation ($data: CreateDocumentSequenceMasterInput!) {
                  createResponse: createDocumentSequence(data: $data) {
                    id
                    costCenter {
                      id
                      name
                    }
                    number
                    initialDate
                    finalDate
                    prefix
                    isActive
                    initialNumber
                    finalNumber
                    titleLabel
                    sequenceLabel    
                    authorizationType
                    authorizationKind
                    reference
                    documentSequenceDetail {
                      id
                      number
                    }
                  }
                }" : @"mutation ($data: UpdateDocumentSequenceMasterInput!, $id: Int!) {
                  updateResponse: updateDocumentSequence(data: $data, id: $id) {
                    id
                    costCenter {
                      id
                      name
                    }
                    number
                    initialDate
                    finalDate
                    prefix
                    isActive
                    initialNumber
                    finalNumber
                    titleLabel
                    sequenceLabel    
                    authorizationType
                    authorizationKind
                    reference
                    documentSequenceDetail {
                      id
                      number
                    }
                  }
                }";

                DocumentSequenceMasterGraphQLModel result = IsNewRecord ? await DocumentSequenceMasterService.Create(query, variables) :
                                                                           await DocumentSequenceMasterService.Update(query, variables);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DocumentSequenceDetailViewModel(DocumentSequenceViewModel context)
        {
            Context = context;
            _errors = new Dictionary<string, List<string>>();
        }

        public void CleanControls()
        {
            Id = 0;
            SelectedCostCenterId = Context.CostCenters.First().Id;
            SelectedAuthorizationKind = "AUTORIZA";
            SelectedSequenceLabel = "FACTURA DE VENTA";
            SelectedTitleLabel = "AUT. DE NUMERACIÓN DE FACTURACIÓN";
            SelectedAuthorizationType = "PAPEL";
            Number = string.Empty;
            InitialDate = null;
            FinalDate = null;
            Prefix = string.Empty;
            InitialNumber = 0;
            FinalNumber = 0;
            ActualNumber = 0;
            Reference = string.Empty;
            IsActiveAuthorization = true;
            TechnicalKey = string.Empty;
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = this.SetFocus(nameof(Number));
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
                _ = _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ValidateProperty(string propertyName)
        {
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(Number):
                        if (string.IsNullOrEmpty(Number.Trim())) AddError(propertyName, "El número de la autorización no puede estar vacío");
                        break;
                    case nameof(InitialDate):
                        if (InitialDate is null) AddError(propertyName, "Debe seleccionar la fecha inicial para la autorización");
                        break;
                    case nameof(FinalDate):
                        if (FinalDate is null) AddError(propertyName, "Debe seleccionar la fecha final para la autorización");
                        break;
                    case nameof(InitialNumber):
                        if (InitialNumber <= 0) AddError(propertyName, "El rango inicial para la autorización no es válido");
                        break;
                    case nameof(FinalNumber):
                        if (FinalNumber <= 0 || FinalNumber < InitialNumber) AddError(propertyName, "El rango final para la autorización no es válido");
                        break;
                    case nameof(ActualNumber):
                        if (ActualNumber <= 0 || ActualNumber < InitialNumber || ActualNumber > FinalNumber) AddError(propertyName, "La factura actual para esta numeración no es válida");
                        break;
                    case nameof(SelectedTitleLabel):
                        if (string.IsNullOrEmpty(SelectedTitleLabel)) AddError(propertyName, "El valor para la etiqueta : 'Titulo' no puede estar vacío");
                        break;
                    case nameof(SelectedSequenceLabel):
                        if (string.IsNullOrEmpty(SelectedSequenceLabel)) AddError(propertyName, "El valor para la etiqueta : 'Identificar como' no puede estar vacío");
                        break;
                    case nameof(SelectedAuthorizationType):
                        if (string.IsNullOrEmpty(SelectedAuthorizationType)) AddError(propertyName, "El valor para la etiqueta : 'Tipo de autorización' no puede estar vacío ");
                        break;
                    case nameof(SelectedAuthorizationKind):
                        if (string.IsNullOrEmpty(SelectedAuthorizationKind)) AddError(propertyName, "El valor para la etiqueta : 'Tipo' no puede estar vacío");
                        break;
                    case nameof(TechnicalKey):
                        if (string.IsNullOrEmpty(TechnicalKey) && SelectedAuthorizationType.Equals("ELECTRONICA")) AddError(propertyName, "El valor de la clave técnica no puede estar vacío si la autorización es electrónica");
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
            ValidateProperty(nameof(Number));
            ValidateProperty(nameof(InitialDate));
            ValidateProperty(nameof(FinalDate));
            ValidateProperty(nameof(InitialNumber));
            ValidateProperty(nameof(FinalNumber));
            ValidateProperty(nameof(SelectedTitleLabel));
            ValidateProperty(nameof(SelectedSequenceLabel));
            ValidateProperty(nameof(SelectedAuthorizationType));
            ValidateProperty(nameof(SelectedAuthorizationKind));
            ValidateProperty(nameof(TechnicalKey));
        }

        #endregion
    }
}
