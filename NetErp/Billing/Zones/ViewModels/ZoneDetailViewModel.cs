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
        private bool _CanSave;
        public bool CanSave
        {
            get { return _CanSave; }
            set
            {
                if (_CanSave != value)
                {
                    _CanSave = value;
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }
        public bool IsNewZone => ZoneId == 0;

        private int _ZoneId;
        public int ZoneId
        {
            get { return _ZoneId; }
            set 
            { 
                if(_ZoneId != value)
                {
                    _ZoneId = value;
                    NotifyOfPropertyChange(nameof(ZoneId));
                } 
            }
        }
        private string _ZoneName;
        public string ZoneName
        {
            get { return _ZoneName; }
            set
            {
                if (_ZoneName != value)
                {
                    _ZoneName = value;
                    NotifyOfPropertyChange(nameof(ZoneName));
                    CheckSave();
                }
            }
        }
        private bool _ZoneIsActive = true;
        public bool ZoneIsActive
        {
            get { return _ZoneIsActive; }
            set
            {
                if (_ZoneIsActive != value)
                {
                    _ZoneIsActive = value;
                    NotifyOfPropertyChange(nameof(ZoneIsActive));
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
                    IsNewZone
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

        public void CheckSave()
        {
            CanSave = !string.IsNullOrEmpty(ZoneName);
        }
        public async Task<UpsertResponseType<ZoneGraphQLModel>> ExecuteSaveAsync()
        {
          

            try
            {
                if (IsNewZone)
                {
                    string query = GetCreateQuery();

                    object variables = new
                    {
                        createResponseInput = new
                        {
                            Name = ZoneName,
                            IsActive = ZoneIsActive
                           
                        }

                    };
                   
                    UpsertResponseType<ZoneGraphQLModel> zoneCreated = await _zoneService.CreateAsync<UpsertResponseType<ZoneGraphQLModel>>(query, variables);
                    return zoneCreated;
                }
                else
                {
                    string query = GetUpdateQuery();

                    object variables = new
                    {
                        updateResponseData = new
                        {
                            Name = ZoneName,
                            IsActive = ZoneIsActive
                        },
                        UpdateResponseId = ZoneId
                    };

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
                .Select(selector: f => f.Entity, overrideName: "billingZone", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.IsActive)
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Field)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateBillingZoneInput!");

            var fragment = new GraphQLQueryFragment("createBillingZone", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<ZoneGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, overrideName: "billingZone", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.IsActive)
                    )
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Field)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateBillingZoneInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateBillingZone", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public async Task GoBackAsync()
        {
            this.ZoneName = string.Empty;
            await Context.ActivateMasterViewAsync();
        }
    }
}
