using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Extensions.Global;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using NetErp.Books.AccountingAccountGroups.DTO;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.WithholdingCertificateConfig.ViewModels
{
    public class WithholdingCertificateConfigDetailViewModel : Screen, INotifyDataErrorInfo
    {

        private readonly IRepository<WithholdingCertificateConfigGraphQLModel> _withholdingCertificateConfigService;
        private readonly CtasRestVtasAccountingAccountGroupCache _ctasRestVtasAccountingAccountGroupCache;
        private readonly CostCenterCache _costCenterCache;

        public WithholdingCertificateConfigDetailViewModel(WithholdingCertificateConfigViewModel context,  IRepository<WithholdingCertificateConfigGraphQLModel> withholdingCertificateConfigService, CtasRestVtasAccountingAccountGroupCache ctasRestVtasAccountingAccountGroupCache, CostCenterCache costCenterCache)
        {
            _errors = new Dictionary<string, List<string>>();
            Context = context;
            _ctasRestVtasAccountingAccountGroupCache = ctasRestVtasAccountingAccountGroupCache ?? throw new ArgumentNullException(nameof(ctasRestVtasAccountingAccountGroupCache));
            _costCenterCache = costCenterCache ?? throw new ArgumentNullException(nameof(costCenterCache));
            _withholdingCertificateConfigService = withholdingCertificateConfigService ?? throw new ArgumentNullException(nameof(withholdingCertificateConfigService));
       

            Context.EventAggregator.SubscribeOnUIThread(this);
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
           // joinable.Run(async () => await InitializeAsync());

        }
        #region Propiedades
        // Context
        private WithholdingCertificateConfigViewModel _context;
        public WithholdingCertificateConfigViewModel Context
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

        // Identity
        private WithholdingCertificateConfigGraphQLModel? _entity;
        public WithholdingCertificateConfigGraphQLModel? Entity
        {
            get { return _entity; }
            set {
                if (_entity != value)
                {
                    _entity = value;
                    NotifyOfPropertyChange(nameof(_entity));
                }
            }
        }
        [ExpandoPath("AccountingAccountIds")]
        public List<int> AccountingAccountIds => AccountingAccounts.Where(f => f.IsChecked == true).Select(x => x.Id).ToList();

        
        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    NotifyOfPropertyChange(nameof(Description));
                    this.TrackChange(nameof(Description));

                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                    ValidateProperty(nameof(Description), value);
                }
            }
        }
        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(Name), value);
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
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => Name);
            ValidateProperties();
            this.AcceptChanges();
        }
        
        public bool CanSave
        {
            get
            {
              
                if (string.IsNullOrEmpty(Name)) return false;

                // Debe haber ingresado una descripcion
                if (string.IsNullOrEmpty(Description)) return false;

                if (CostCenterId == null || CostCenterId == 0) return false;

                if (!AccountingAccounts.Any(x => x.IsChecked == true)) return false;
                if (_errors.Count > 0 || !this.HasChanges()) { return false; }
               
                // Debe haber ingresado un nombre
                return true;
            }
        }
        public bool HasErrors => _errors.Count > 0;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
        }
        private CaptureTypeEnum _selectedCaptureType;


        private int _costCenterId;
        public int CostCenterId
        {
            get { return _costCenterId; }
            set
            {
                if (_costCenterId != value)
                {
                    _costCenterId = value;
                    NotifyOfPropertyChange(nameof(CostCenterId));
                    this.TrackChange(nameof(CostCenterId));
                    this.NotifyOfPropertyChange(nameof(this.CanSave));
                }
            }
        }
        private ObservableCollection<CostCenterDTO> _costCenters;


        private ObservableCollection<AccountingAccountGroupDetailDTO> _accountingAccounts;

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
        public bool WithholdingCertificateConfigComboBoxIsEnabled
        {
            get
            {
                return true;
            }
        }
        public ObservableCollection<AccountingAccountGroupDetailDTO> AccountingAccounts
        {
            get { return _accountingAccounts; }
            set
            {
                if (_accountingAccounts != value)
                {
                    _accountingAccounts = value;
                   
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                    ListenAccountingAccountChek();

                }
            }
        }
        private void ListenAccountingAccountChek()
        {
            foreach (var account in AccountingAccounts)
            {
                account.PropertyChanged += AccountingAccount_PropertyChanged;
            }

            // Escuchar cuando se agregan nuevos elementos
            AccountingAccounts.CollectionChanged += AccountingAccount_CollectionChanged;
        }
        private void AccountingAccount_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AccountingAccountGroupDetailDTO.IsChecked))
            {
                // Aquí puedes actualizar otra propiedad del ViewModel si necesitas
                NotifyOfPropertyChange(() => AccountingAccountIds);
                this.TrackChange(nameof(AccountingAccountIds));

                NotifyOfPropertyChange(() => CanSave);
               
            }
        }
        private void AccountingAccount_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Escuchar cambios de los nuevos elementos
            if (e.NewItems != null)
            {
                foreach (AccountingAccountGroupDetailDTO p in e.NewItems)
                    p.PropertyChanged += AccountingAccount_PropertyChanged;
            }

            // Opcional: dejar de escuchar eliminados
            if (e.OldItems != null)
            {
                foreach (AccountingAccountGroupDetailDTO p in e.OldItems)
                    p.PropertyChanged -= AccountingAccount_PropertyChanged;
            }

            NotifyOfPropertyChange(() => CanSave);
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }
        public bool CaptureInfoAsPN => true;
        #endregion
        #region command
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
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        public void GoBack(object p)
        {
            _ = Task.Run(() => Context.ActivateMasterViewModelAsync());
            CleanUpControls();
        }
        public void CleanUpControls()
        {
            Name = "";
            Description = "";
            CostCenterId = 0;
        }
        public bool CanGoBack(object p)
        {
            return !IsBusy;
        }
        #endregion

        public async Task InitializeAsync()
        {
            await Task.WhenAll(
                  _ctasRestVtasAccountingAccountGroupCache.EnsureLoadedAsync(),
                  _costCenterCache.EnsureLoadedAsync()
                  );
            CostCenters = Context.AutoMapper.Map<ObservableCollection<CostCenterDTO>>(_costCenterCache.Items);
            CostCenters.Insert(0, new CostCenterDTO() { Id = 0, Name = "SELECCIONE CENTRO DE COSTO" });
            CostCenterId = Entity?.CostCenter?.Id ?? 0;
           
            //Cuentas
            IEnumerable<AccountingAccountGroupGraphQLModel> source = _ctasRestVtasAccountingAccountGroupCache.Items; //result.AccountingAccountGroups.Entries;
            ObservableCollection<AccountingAccountGroupDetailDTO> acgd = Context.AutoMapper.Map<ObservableCollection<AccountingAccountGroupDetailDTO>>(source.First().Accounts);
            foreach (var accountingAccount in acgd)
            {
                accountingAccount.Context = this;
                accountingAccount.IsChecked = Entity?.AccountingAccounts?.FirstOrDefault(x => x.Id == accountingAccount.Id) != null ? true : false;
            }

            AccountingAccounts = [.. acgd];
            this.AcceptChanges();

        }

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<WithholdingCertificateConfigGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new WithholdingCertificateConfigCreateMessage() { CreatedWithholdingCertificateConfig = result }
                        : new WithholdingCertificateConfigUpdateMessage() { UpdatedWithholdingCertificateConfig = result }
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
        public async Task<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>> ExecuteSaveAsync()
        {
            dynamic variables = new ExpandoObject();
          

            if (IsNewRecord)
            {

                variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                string query = GetCreateQuery();
                UpsertResponseType<WithholdingCertificateConfigGraphQLModel> WithholdingCertificateCreated = await _withholdingCertificateConfigService.CreateAsync<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>>(query, variables);
                return WithholdingCertificateCreated;
            }
            else
            {
                string query = GetUpdateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Entity.Id;
                UpsertResponseType<WithholdingCertificateConfigGraphQLModel> updatedWithholdingCertificate = await _withholdingCertificateConfigService.UpdateAsync<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>>(query, variables);
                return updatedWithholdingCertificate;
            }
           
        }
        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "withholdingCertificate", nested: sq => sq
                   .Field(e => e.Id)
                   .Field(e => e.Description)
                   .Field(e => e.Name)
                    

                    .Select(e => e.CostCenter, cos => cos
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                     )
                    

                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateWithholdingCertificateInput!");

            var fragment = new GraphQLQueryFragment("createWithholdingCertificate", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<WithholdingCertificateConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "withholdingCertificate", nested: sq => sq
                    .Field(e => e.Id)
                   .Field(e => e.Description)
                   .Field(e => e.Name)
                   

                    .Select(e => e.CostCenter, cos => cos
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                     )
                   

                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();


            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateWithholdingCertificateInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateWithholdingCertificate", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
      
       
       
        public async Task<WithholdingCertificateConfigGraphQLModel> LoadDataForEditAsync(int id)
        {
            try
            {
                string query = GetLoadSByIdQuery();

                dynamic variables = new ExpandoObject();


                variables.singleItemResponseId = id;

                var certificate = await _withholdingCertificateConfigService.FindByIdAsync(query, variables);

                // Poblar el ViewModel con los datos del seller (sin bloquear UI thread)
                PopulateFromTax(certificate);

                return certificate;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public void PopulateFromTax(WithholdingCertificateConfigGraphQLModel entity)
        {
            // Propiedades básicas del tax
            Name = entity.Name;
            Description = entity.Description;
            CostCenterId = entity.CostCenter.Id;
            Entity = entity;
            this.AcceptChanges();



        }
        public string GetLoadSByIdQuery()
        {
            var singleIdFields = FieldSpec<WithholdingCertificateConfigGraphQLModel>
             .Create()

                 .Field(e => e.Id)
                     
                  .Field(e => e.Description)
                  .Field(e => e.Name)
                  .SelectList(e => e.AccountingAccounts, cat => cat
                      .Field(c => c.Id)
                      .Field(c => c.Name)

                  )
                  .Select(e => e.CostCenter, cat => cat
                      .Field(c => c.Id)
                      .Field(c => c.Name)
                      .Field(c => c.Address)
                      .Select(e => e.City, cit => cit
                              .Field(d => d.Id)
                              .Field(d => d.Name)
                              .Select(d => d.Department, dep => dep
                              .Field(d => d.Id)
                              .Field(d => d.Name)
                              )
                      )


                  )
              

             .Build();
            var singleIdParameter = new GraphQLQueryParameter("id", "ID!");

            var singleIdFragment = new GraphQLQueryFragment("withholdingCertificate", [singleIdParameter], singleIdFields, "SingleItemResponse");

            var builder = new GraphQLQueryBuilder([singleIdFragment]);

            return builder.GetQuery();

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
        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(Name):
                        if (string.IsNullOrEmpty(value)  ) AddError(propertyName, "El nombre no puede estar vacío");
                        break;
                    case nameof(Description):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "La descripción no puede estar vacío");
                        break;
                   
                        
                }
             }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
       

        private void ValidateProperties()
        {
            ValidateProperty(nameof(Name), Name);
            ValidateProperty(nameof(Description), Description);
           
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
        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {

            // Desconectar eventos para evitar memory leaks
            Context.EventAggregator.Unsubscribe(this);
            return base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
