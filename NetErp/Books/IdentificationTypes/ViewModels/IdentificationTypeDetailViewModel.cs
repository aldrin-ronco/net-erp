using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using GraphQL.Client.Http;
using Models.Books;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.IdentificationTypes.ViewModels
{
    public class IdentificationTypeDetailViewModel : Screen
    {
        #region Dependencies

        private readonly IRepository<IdentificationTypeGraphQLModel> _identificationTypeService;
        private readonly IEventAggregator _eventAggregator;

        #endregion

        #region Properties

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
                }
            }
        }

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                    NotifyOfPropertyChange(nameof(IsReadOnlyCode));
                }
            }
        }

        private string _code = string.Empty;
        public string Code
        {
            get => _code;
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

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
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
            get => _hasVerificationDigit;
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

        private bool _allowsLetters;
        public bool AllowsLetters
        {
            get => _allowsLetters;
            set
            {
                if (_allowsLetters != value)
                {
                    _allowsLetters = value;
                    NotifyOfPropertyChange(nameof(AllowsLetters));
                    this.TrackChange(nameof(AllowsLetters));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _minimumDocumentLength = 7;
        public int MinimumDocumentLength
        {
            get => _minimumDocumentLength;
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

        public bool IsNewRecord => Id == 0;
        public bool IsReadOnlyCode => !IsNewRecord;

        public bool CanSave
        {
            get
            {
                if (string.IsNullOrEmpty(Code) || Code.Length != 2) return false;
                if (string.IsNullOrEmpty(Name)) return false;
                if (MinimumDocumentLength < 5) return false;
                if (!this.HasChanges()) return false;
                return true;
            }
        }

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync, () => CanSave);
                return _saveCommand;
            }
        }

        private ICommand? _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand ??= new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        #endregion

        #region Constructor

        public IdentificationTypeDetailViewModel(
            IRepository<IdentificationTypeGraphQLModel> identificationTypeService,
            IEventAggregator eventAggregator)
        {
            _identificationTypeService = identificationTypeService;
            _eventAggregator = eventAggregator;
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            if (IsNewRecord)
            {
                this.SeedValue(nameof(HasVerificationDigit), HasVerificationDigit);
                this.SeedValue(nameof(AllowsLetters), AllowsLetters);
                this.SeedValue(nameof(MinimumDocumentLength), MinimumDocumentLength);
            }
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Load

        public void LoadForEdit(IdentificationTypeGraphQLModel entity)
        {
            Id = entity.Id;
            Code = entity.Code;
            Name = entity.Name;
            HasVerificationDigit = entity.HasVerificationDigit;
            AllowsLetters = entity.AllowsLetters;
            MinimumDocumentLength = entity.MinimumDocumentLength;
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();

                string[] excludes = IsNewRecord ? [] : [nameof(Code)];

                if (IsNewRecord)
                {
                    string query = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput", excludeProperties: excludes);
                    UpsertResponseType<IdentificationTypeGraphQLModel> result = await _identificationTypeService.CreateAsync<UpsertResponseType<IdentificationTypeGraphQLModel>>(query, variables);

                    if (!result.Success)
                    {
                        ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo",
                            title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                        return;
                    }

                    await _eventAggregator.PublishOnCurrentThreadAsync(new IdentificationTypeCreateMessage { CreatedIdentificationType = result });
                }
                else
                {
                    string query = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData", excludeProperties: excludes);
                    variables.updateResponseId = Id;
                    UpsertResponseType<IdentificationTypeGraphQLModel> result = await _identificationTypeService.UpdateAsync<UpsertResponseType<IdentificationTypeGraphQLModel>>(query, variables);

                    if (!result.Success)
                    {
                        ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo",
                            title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                        return;
                    }

                    await _eventAggregator.PublishOnCurrentThreadAsync(new IdentificationTypeUpdateMessage { UpdatedIdentificationType = result });
                }

                await TryCloseAsync(true);
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content!.ToString()!);
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"\r\n{graphQLError.Errors[0].Message}\r\n{graphQLError.Errors[0].Extensions.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !",
                    $"{GetType().Name}.{currentMethod!.Name.Between("<", ">")} \r\n{ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<string> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<IdentificationTypeGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "identificationType", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Code)
                    .Field(f => f.HasVerificationDigit)
                    .Field(f => f.AllowsLetters)
                    .Field(f => f.MinimumDocumentLength))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateIdentificationTypeInput!");
            var fragment = new GraphQLQueryFragment("createIdentificationType", [parameter], fields, "CreateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        private static readonly Lazy<string> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<IdentificationTypeGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "identificationType", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Code)
                    .Field(f => f.HasVerificationDigit)
                    .Field(f => f.AllowsLetters)
                    .Field(f => f.MinimumDocumentLength))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateIdentificationTypeInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateIdentificationType", parameters, fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        #endregion
    }
}
