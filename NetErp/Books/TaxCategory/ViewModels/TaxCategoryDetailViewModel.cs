using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Global.AuthorizationSequence.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using Ninject.Activation;
using Services.Billing.DAL.PostgreSQL;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Amazon.S3.Util.S3EventNotification;
using static Chilkat.Http;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Extensions.Global;

namespace NetErp.Books.TaxCategory.ViewModels
{
    public class TaxCategoryDetailViewModel :  Screen, INotifyDataErrorInfo
    {
        #region DependecyProperties
        private readonly IRepository<TaxCategoryGraphQLModel> _TaxCategoryService;

        #endregion

        #region InitializationMethods
        public TaxCategoryDetailViewModel(TaxCategoryViewModel context, TaxCategoryGraphQLModel? entity, IRepository<TaxCategoryGraphQLModel> TaxCategoryService)
        {


            Context = context;
            _TaxCategoryService = TaxCategoryService ?? throw new ArgumentNullException(nameof(TaxCategoryService));
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
            CleanUpControls();
            if (Entity != null)
            {
                SetUpdateProperties(Entity);
            }
        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
            this.SetFocus(() => Name);
            ValidateProperties();
        }
        public void SetUpdateProperties(TaxCategoryGraphQLModel entity)
        {
            Name = entity.Name;
            Prefix = entity.Prefix;
            GeneratedTaxAccountIsRequired = entity.GeneratedTaxAccountIsRequired;
            GeneratedTaxRefundAccountIsRequired = entity.GeneratedTaxRefundAccountIsRequired;
            DeductibleTaxAccountIsRequired = entity.DeductibleTaxAccountIsRequired;
            DeductibleTaxRefundAccountIsRequired = entity.DeductibleTaxRefundAccountIsRequired;
        }
        #endregion

        #region Properties
        private TaxCategoryViewModel _context;
        public TaxCategoryViewModel Context
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
        private TaxCategoryGraphQLModel? _entity;
        public TaxCategoryGraphQLModel Entity
        {
            get { return _entity; }
            set
            {
                if (_entity != value)
                {
                    _entity = value;
                    NotifyOfPropertyChange(nameof(Entity));

                }
            }
        }
     
        private bool _isNewRecord => Entity?.Id > 0 ? false : true;
        public bool IsReadOnlyGeneratedTaxRefundAccountIsRequired => GeneratedTaxAccountIsRequired.Equals(false);
        public bool IsReadOnlyDeductibleTaxRefundAccountIsRequired => DeductibleTaxAccountIsRequired.Equals(false);
        public bool IsNewRecord
        {
            get { return _isNewRecord; }

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
                    ValidateProperty(nameof(Name), Name);
                    NotifyOfPropertyChange(nameof(Name));
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }
        private string _prefix;
        public string Prefix
        {
            get { return _prefix; }
            set
            {
                if (_prefix != value)
                {
                    _prefix = value;
                    ValidateProperty(nameof(Prefix), Prefix);
                    NotifyOfPropertyChange(nameof(Prefix));
                    this.TrackChange(nameof(Prefix));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        private bool _generatedTaxAccountIsRequired;
        public bool GeneratedTaxAccountIsRequired
        {
            get { return _generatedTaxAccountIsRequired; }
            set
            {
                if (_generatedTaxAccountIsRequired != value)
                {
                    _generatedTaxAccountIsRequired = value;
                    NotifyOfPropertyChange(nameof(GeneratedTaxAccountIsRequired));
                    this.TrackChange(nameof(GeneratedTaxAccountIsRequired));
                    NotifyOfPropertyChange(nameof(CanSave));
                    NotifyOfPropertyChange(nameof(IsReadOnlyGeneratedTaxRefundAccountIsRequired));
                    if (value == false) GeneratedTaxRefundAccountIsRequired = false;
                }
            }
        }
        private bool _generatedTaxRefundAccountIsRequired;
        public bool GeneratedTaxRefundAccountIsRequired
        {
            get { return _generatedTaxRefundAccountIsRequired; }
            set
            {
                if (_generatedTaxRefundAccountIsRequired != value)
                {
                    _generatedTaxRefundAccountIsRequired = value;
                    NotifyOfPropertyChange(nameof(GeneratedTaxRefundAccountIsRequired));
                    this.TrackChange(nameof(GeneratedTaxRefundAccountIsRequired));
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }
        private bool _deductibleTaxAccountIsRequired;
        public bool DeductibleTaxAccountIsRequired
        {
            get { return _deductibleTaxAccountIsRequired; }
            set
            {
                if (_deductibleTaxAccountIsRequired != value)
                {
                    _deductibleTaxAccountIsRequired = value;
                    NotifyOfPropertyChange(nameof(DeductibleTaxAccountIsRequired));
                    NotifyOfPropertyChange(nameof(IsReadOnlyDeductibleTaxRefundAccountIsRequired));
                    this.TrackChange(nameof(DeductibleTaxAccountIsRequired));
                    NotifyOfPropertyChange(nameof(CanSave));
                    if (value == false) DeductibleTaxRefundAccountIsRequired = false;


                }
               
            }
        }
        private bool _deductibleTaxRefundAccountIsRequired;
        public bool DeductibleTaxRefundAccountIsRequired
        {
            get { return _deductibleTaxRefundAccountIsRequired; }
            set
            {
                if (_deductibleTaxRefundAccountIsRequired != value)
                {
                    _deductibleTaxRefundAccountIsRequired = value;
                    NotifyOfPropertyChange(nameof(DeductibleTaxRefundAccountIsRequired));
                    this.TrackChange(nameof(DeductibleTaxRefundAccountIsRequired));
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }
        #endregion

        #region Validation
        public bool HasErrors => _errors.Count > 0;

        Dictionary<string, List<string>> _errors;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
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
        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
        }

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
        private void ValidateProperties()
        {
            ValidateProperty(nameof(Name), Name);
            ValidateProperty(nameof(Prefix), Prefix);

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
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "El nombre no puede estar vacío");
                        break;
                    case nameof(Prefix):
                        if (string.IsNullOrEmpty(value)) AddError(propertyName, "El Prefijo no puede estar vacío");
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
        public void CleanUpControls()
        {
            Name = "";
            Prefix = "";
            GeneratedTaxAccountIsRequired = false;
            GeneratedTaxRefundAccountIsRequired = false;
            DeductibleTaxAccountIsRequired = false;
            DeductibleTaxRefundAccountIsRequired = false;


        }
        public void GoBack(object p)
        {
            CleanUpControls();
            _ = Task.Run(() => Context.ActivateMasterViewModelAsync());

        }
        #endregion
       
        public bool CanSave
        {
            get
            {
                if (_errors.Count > 0 || (GeneratedTaxAccountIsRequired == false && DeductibleTaxAccountIsRequired == false) || !this.HasChanges()) { return false; }
                return true;
            }
        }
        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<TaxCategoryGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new TaxCategoryCreateMessage() { CreatedTaxCategory = result }
                        : new TaxCategoryUpdateMessage() { UpdatedTaxCategory = result }
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
        public async Task<UpsertResponseType<TaxCategoryGraphQLModel>> ExecuteSaveAsync()
        {
           
            
            if (IsNewRecord)
            {
                object variables = new
                {
                    createResponseInput = new
                    {
                        Name = Name,
                        Prefix = Prefix,
                        GeneratedTaxAccountIsRequired = GeneratedTaxAccountIsRequired,
                        GeneratedTaxRefundAccountIsRequired = GeneratedTaxRefundAccountIsRequired,
                        DeductibleTaxAccountIsRequired = DeductibleTaxAccountIsRequired,
                        DeductibleTaxRefundAccountIsRequired = DeductibleTaxRefundAccountIsRequired
                    }
                };
                string query = GetCreateQuery();

                
                UpsertResponseType<TaxCategoryGraphQLModel> taxCategoryCreated = await _TaxCategoryService.CreateAsync<UpsertResponseType<TaxCategoryGraphQLModel>>(query, variables);
                return taxCategoryCreated;
            }
            else
            {
                string query = GetUpdateQuery();

                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Entity.Id;

                UpsertResponseType<TaxCategoryGraphQLModel> updatedTaxCategory = await _TaxCategoryService.UpdateAsync<UpsertResponseType<TaxCategoryGraphQLModel>>(query, variables);
                return updatedTaxCategory;
                
            }

        }
        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<TaxCategoryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "taxCategory", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Prefix)
                     .Field(e => e.GeneratedTaxRefundAccountIsRequired)
                    .Field(e => e.GeneratedTaxAccountIsRequired)
                    .Field(e => e.DeductibleTaxRefundAccountIsRequired)
                    .Field(e => e.DeductibleTaxAccountIsRequired)

                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Field)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateTaxCategoryInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateTaxCategory", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
   
        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<TaxCategoryGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "taxCategory", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Prefix)
                     .Field(e => e.GeneratedTaxRefundAccountIsRequired)
                    .Field(e => e.GeneratedTaxAccountIsRequired)
                    .Field(e => e.DeductibleTaxRefundAccountIsRequired)
                    .Field(e => e.DeductibleTaxAccountIsRequired)
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Field)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateTaxCategoryInput!");

            var fragment = new GraphQLQueryFragment("createTaxCategory", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
      
        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {

            // Desconectar eventos para evitar memory leaks
            Context.EventAggregator.Unsubscribe(this);
            return base.OnDeactivateAsync(close, cancellationToken);
        }

    }
}
