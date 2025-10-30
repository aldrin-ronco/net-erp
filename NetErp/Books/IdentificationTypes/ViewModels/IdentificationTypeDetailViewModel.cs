using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
using NetErp.Helpers;
using Extensions.Books;
using System;
using System.Threading.Tasks;
using Common.Helpers;
using DevExpress.Mvvm;
using System.Windows.Input;
using Dictionaries;
using Models.DTO.Global;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Common.Extensions;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using System.Windows.Threading;
using NetErp.Helpers.GraphQLQueryBuilder;
using static Models.Global.GraphQLResponseTypes;
using Extensions.Global;
using System.Dynamic;

namespace NetErp.Books.IdentificationTypes.ViewModels
{
    public class IdentificationTypeDetailViewModel : Screen
    {
        private readonly IRepository<IdentificationTypeGraphQLModel> _identificationTypeService;
        #region Propiedades
        // Context
        private IdentificationTypeViewModel _context;
        public IdentificationTypeViewModel Context
        {
            get => _context;
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        // Is Busy
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

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(Save, CanSave);
                return _saveCommand;
            }
        }

        // Si es un nuevo registro
        public bool IsNewRecord => Id == 0;

        // Model IdentificationType 
        private IdentificationTypeGraphQLModel _identificationType;
        public IdentificationTypeGraphQLModel IdentificationType
        {
            get => _identificationType;
            set
            {
                if (_identificationType != value)
                {
                    _identificationType = value;
                    NotifyOfPropertyChange(nameof(IdentificationType));
                }
            }
        }

        private int _id;
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


        private string _code;

        public string Code
        {
            get { return _code; }
            set 
            {
                if (_code != value)
                {
                    _code = value;
                    NotifyOfPropertyChange(nameof(Code));
                    this.TrackChange(nameof(Code));
                    NotifyOfPropertyChange(nameof(CanSave));
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
                }
            }
        }

        private bool _hasVerificationDigit;

        public bool HasVerificationDigit
        {
            get { return _hasVerificationDigit; }
            set 
            {
                if (_hasVerificationDigit != value)
                {
                    _hasVerificationDigit = value;
                    NotifyOfPropertyChange(nameof(HasVerificationDigit));
                    this.TrackChange(nameof(HasVerificationDigit));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _minimumDocumentLength;

        public int MinimumDocumentLength
        {
            get { return _minimumDocumentLength; }
            set 
            {
                if (_minimumDocumentLength != value)
                {
                    _minimumDocumentLength = value;
                    NotifyOfPropertyChange(nameof(MinimumDocumentLength));
                    this.TrackChange(nameof(MinimumDocumentLength));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public bool IsReadOnlyCode => !IsNewRecord;

        #endregion

        public IdentificationTypeDetailViewModel(IdentificationTypeViewModel context, IRepository<IdentificationTypeGraphQLModel> identificationTypeService)
        {
            this._identificationTypeService = identificationTypeService;
            Context = context;
            IdentificationType = new IdentificationTypeGraphQLModel();
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            _ = App.Current.Dispatcher.BeginInvoke(() =>
            {
                _ = Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(IsNewRecord ? nameof(Code) : nameof(Name))), DispatcherPriority.Render);
            });
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            if (IsNewRecord)
            {
                this.SeedValue(nameof(HasVerificationDigit), HasVerificationDigit);
                this.SeedValue(nameof(MinimumDocumentLength), MinimumDocumentLength);
            }
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        public void GoBack(object p)
        {
            _ = Task.Run(() => Context.ActivateMasterViewAsync());
        }

        public async Task Save()
        {
            try
            {
                IsBusy = true;
                Refresh();
                UpsertResponseType<IdentificationTypeGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new IdentificationTypeCreateMessage() { CreatedIdentificationType = result }
                        : new IdentificationTypeUpdateMessage() { UpdatedIdentificationType = result }
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
            var fields = FieldSpec<UpsertResponseType<IdentificationTypeGraphQLModel>>
                .Create()
                .Select(selector:f => f.Entity, alias: "entity", overrideName: "identificationType", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Code)
                    .Field(f => f.HasVerificationDigit)
                    .Field(f => f.MinimumDocumentLength)
                    .Field(f => f.InsertedAt)
                    .Field(f => f.UpdatedAt))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Field)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateIdentificationTypeInput!");

            var fragment = new GraphQLQueryFragment("createIdentificationType", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<IdentificationTypeGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "identificationType", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Code)
                    .Field(f => f.HasVerificationDigit)
                    .Field(f => f.MinimumDocumentLength)
                    .Field(f => f.InsertedAt)
                    .Field(f => f.UpdatedAt))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Field)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateIdentificationTypeInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateIdentificationType", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public async Task<UpsertResponseType<IdentificationTypeGraphQLModel>> ExecuteSaveAsync()
        {

            try
            {
                if (IsNewRecord)
                {
                    string query = GetCreateQuery();

                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");

                    UpsertResponseType<IdentificationTypeGraphQLModel> identificationTypeCreated = await _identificationTypeService.CreateAsync<UpsertResponseType<IdentificationTypeGraphQLModel>>(query, variables);
                    return identificationTypeCreated;
                }
                else
                {
                    string query = GetUpdateQuery();

                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;

                    UpsertResponseType<IdentificationTypeGraphQLModel> updatedIdentificationType = await _identificationTypeService.UpdateAsync<UpsertResponseType<IdentificationTypeGraphQLModel>>(query, variables);
                    return updatedIdentificationType;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public void CleanUpControlsForNew()
        {
            Id = 0; // Por medio del Id se establece si es un nuevo registro o una actualizacion
            Code = string.Empty;
            Name = string.Empty;
            HasVerificationDigit = false;
            MinimumDocumentLength = 7;
        }

        public bool CanSave
        {
            get
            {
                if (string.IsNullOrEmpty(Code) || Code.Length != 2) return false;
                if (string.IsNullOrEmpty(Name)) return false;
                if (MinimumDocumentLength == 0) return false;
                if (!this.HasChanges()) return false;
                return true;
            }
        }
    }
}
