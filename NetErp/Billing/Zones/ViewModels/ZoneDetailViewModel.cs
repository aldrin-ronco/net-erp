using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.XtraEditors.Filtering;
using Models.Billing;
using Models.Books;
using Models.Global;
using Ninject.Activation;
using Services.Billing.DAL.PostgreSQL;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;
using System.Xml.Linq;
using NetErp.Helpers.GraphQLQueryBuilder;
using Common.Extensions;
using GraphQL.Client.Http;
using Extensions.Global;

namespace NetErp.Billing.Zones.ViewModels
{
    public class ZoneDetailViewModel : Screen
    {
        private readonly IRepository<ZoneGraphQLModel> _zoneService;
        public ZoneViewModel Context { get; set; }

        public ZoneDetailViewModel(
            ZoneViewModel context,
            IRepository<ZoneGraphQLModel> zoneService)
        {
            Context = context;
            _zoneService = zoneService;
            NotifyOfPropertyChange(nameof(CanSave));
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
        private bool _canSave;
        public bool CanSave
        {
            get 
            { 
                return !string.IsNullOrEmpty(Name) && this.HasChanges(); 
            }
        }
        public bool IsNewRecord => Id == 0;

        private int _id;

        public int Id
        {
            get { return _id; }
            set 
            { 
                if(_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
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

        private bool _isActive = true;
        public new bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.TrackChange(nameof(IsActive));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ICommand? _goBackCommand;

        public ICommand? GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBackAsync);
                return _goBackCommand;
            }
        }

        private ICommand? _saveCommand;

        public ICommand? SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }
        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();

                UpsertResponseType<ZoneGraphQLModel> result = await ExecuteSaveAsync();
                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }

                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    IsNewRecord
                        ? new ZoneCreateMessage() { CreatedZone = result }
                        : new ZoneUpdateMessage() { UpdatedZone = result }
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

        public async Task<UpsertResponseType<ZoneGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    string query = GetCreateQuery();

                    dynamic variables = new ExpandoObject();
                    variables.createResponseData = new ExpandoObject();
                    variables.createResponseData.name = Name.RemoveExtraSpaces().Trim();
                    variables.createResponseData.isActive = IsActive;

                    UpsertResponseType<ZoneGraphQLModel> zoneCreated = await _zoneService.CreateAsync<UpsertResponseType<ZoneGraphQLModel>>(query, variables);
                    return zoneCreated;
                }
                else
                {
                    string query = GetUpdateQuery();
                    
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;

                    UpsertResponseType<ZoneGraphQLModel> updatedZone = await _zoneService.UpdateAsync<UpsertResponseType<ZoneGraphQLModel>>(query, variables);
                    return updatedZone;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
       
        public string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<ZoneGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "zone", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.IsActive)
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateZoneInput!");

            var fragment = new GraphQLQueryFragment("createZone", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<ZoneGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "zone", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
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
                new("data", "UpdateZoneInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateZone", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public async Task GoBackAsync()
        {
            this.Name = string.Empty;
            await Context.ActivateMasterViewAsync();
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }
    }
}
