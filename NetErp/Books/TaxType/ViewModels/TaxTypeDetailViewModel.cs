﻿using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Global.AuthorizationSequence.ViewModels;
using NetErp.Helpers;
using Ninject.Activation;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Amazon.S3.Util.S3EventNotification;
using static Chilkat.Http;
using static Dictionaries.BooksDictionaries;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NetErp.Books.TaxType.ViewModels
{
    public class TaxTypeDetailViewModel :  Screen, INotifyDataErrorInfo
    {
        private readonly IRepository<TaxTypeGraphQLModel> _taxTypeService;

        public TaxTypeDetailViewModel(TaxTypeViewModel context, TaxTypeGraphQLModel? entity, IRepository<TaxTypeGraphQLModel> taxTypeService)
        {


            Context = context;
            _taxTypeService = taxTypeService ?? throw new ArgumentNullException(nameof(taxTypeService));

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
            this.SetFocus(() => Name);

            ValidateProperties();
        }
        private TaxTypeViewModel _context;
        public TaxTypeViewModel Context
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
        private TaxTypeGraphQLModel? _entity;
        public TaxTypeGraphQLModel Entity
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
        public void SetUpdateProperties(TaxTypeGraphQLModel entity)
        {
            Name = entity.Name;
            Prefix = entity.Prefix;
            GeneratedTaxAccountIsRequired = entity.GeneratedTaxAccountIsRequired;
            GeneratedTaxRefundAccountIsRequired = entity.GeneratedTaxRefundAccountIsRequired;
            DeductibleTaxAccountIsRequired = entity.DeductibleTaxAccountIsRequired;
            DeductibleTaxRefundAccountIsRequired = entity.DeductibleTaxRefundAccountIsRequired;
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
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(Prefix), Prefix);
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

                }
            }
        }
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
        public void GoBack(object p)
        {
            CleanUpControls();
            _ = Task.Run(() => Context.ActivateMasterViewModelAsync());

        }
        #endregion
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
        public bool CanSave
        {
            get
            {
                 if (_errors.Count > 0) { return false; }
                 if (GeneratedTaxAccountIsRequired == false && DeductibleTaxAccountIsRequired == false) { return false; }
                return true;
            }
        }
        public async Task Save()
        {
            try
            {
                IsBusy = true;
                Refresh();
                TaxTypeGraphQLModel result = await ExecuteSave();
               
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new TaxTypeCreateMessage() { CreatedTaxType = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnCurrentThreadAsync(new TaxTypeUpdateMessage() { UpdatedTaxType = result });
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
        public async Task<TaxTypeGraphQLModel> ExecuteSave()
        {
            dynamic variables = new ExpandoObject();
            variables.Data = new ExpandoObject();
            variables.Data.name = Name;
            variables.Data.prefix = Prefix;
            variables.Data.generatedTaxAccountIsRequired = GeneratedTaxAccountIsRequired;
            variables.Data.generatedTaxRefundAccountIsRequired = GeneratedTaxRefundAccountIsRequired;
            variables.Data.deductibleTaxAccountIsRequired = DeductibleTaxAccountIsRequired;
            variables.Data.deductibleTaxRefundAccountIsRequired = DeductibleTaxRefundAccountIsRequired;
;
            if (IsNewRecord)
            {
                return await CreateAsync(variables);
            }
            else
            {
                return await UpdateAsync(variables);
            }

        }
       
        private async Task<TaxTypeGraphQLModel> CreateAsync(dynamic variables)
        {
            try
            {
                IsBusy = true;
                var query = @"
                   mutation($data: CreateTaxTypeInput!){
                     CreateResponse: createTaxType(data: $data){
                        id
                        name
                        prefix  
                        generatedTaxAccountIsRequired
                        generatedTaxRefundAccountIsRequired
                        deductibleTaxAccountIsRequired
                        deductibleTaxRefundAccountIsRequired
                      }
                    }";

                TaxTypeGraphQLModel record = await _taxTypeService.CreateAsync(query, variables);
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
        public async Task<TaxTypeGraphQLModel> UpdateAsync(dynamic variables)
        {
            try
            {

                var query = @"
                    mutation($data: UpdateTaxTypeInput!, $id : Int!){
                     UpdateResponse: updateTaxType(data: $data, id: $id){
                        id
                        name
                        prefix  
                        generatedTaxAccountIsRequired
                        generatedTaxRefundAccountIsRequired
                        deductibleTaxAccountIsRequired
                        deductibleTaxRefundAccountIsRequired
                      }
                    }";
                variables.id = Entity.Id;
                

                return await _taxTypeService.UpdateAsync(query, variables);

            }
            catch (Exception ex)
            {
                throw;
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
