using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Global;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using Common.Extensions;
using GraphQL.Client.Http;
using Extensions.Global;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.Zones.ViewModels
{
    public class ZoneDetailViewModel : Screen, INotifyDataErrorInfo
    {
        private readonly IRepository<ZoneGraphQLModel> _zoneService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly Dictionary<string, List<string>> _errors = new();
        public ZoneViewModel Context { get; set; }

        public int NameMaxLength => _stringLengthCache.GetMaxLength<ZoneGraphQLModel>(nameof(ZoneGraphQLModel.Name));

        public ZoneDetailViewModel(
            ZoneViewModel context,
            IRepository<ZoneGraphQLModel> zoneService,
            StringLengthCache stringLengthCache)
        {
            Context = context;
            _zoneService = zoneService;
            _stringLengthCache = stringLengthCache;
        }

        #region Properties

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

        public bool CanSave
        {
            get
            {
                if (string.IsNullOrEmpty(Name?.Trim())) return false;
                if (!this.HasChanges()) return false;
                return _errors.Count <= 0;
            }
        }

        public bool IsNewRecord => Id == 0;

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

        private string _name = string.Empty;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                    ValidateProperty(nameof(Name), value);
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

        #endregion

        #region Commands

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

        #endregion

        #region Methods

        public void SetForNew()
        {
            Id = 0;
            Name = string.Empty;
            IsActive = true;
            SeedDefaultValues();
        }

        public void SetForEdit(ZoneGraphQLModel zone)
        {
            Id = zone.Id;
            Name = zone.Name;
            IsActive = zone.IsActive;
            SeedCurrentValues();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(IsActive), IsActive);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(IsActive), IsActive);
            this.AcceptChanges();
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
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    variables.createResponseData = new ExpandoObject();
                    return await _zoneService.CreateAsync<UpsertResponseType<ZoneGraphQLModel>>(query, variables);
                }
                else
                {
                    string query = GetUpdateQuery();
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _zoneService.UpdateAsync<UpsertResponseType<ZoneGraphQLModel>>(query, variables);
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
            await Context.ActivateMasterViewAsync();
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            NotifyOfPropertyChange(nameof(CanSave));
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                this.AcceptChanges();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
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
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ValidateProperty(string propertyName, string value)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(Name):
                    if (string.IsNullOrEmpty(value?.Trim()))
                        AddError(propertyName, "El nombre de la zona no puede estar vacío");
                    break;
            }
        }

        #endregion
    }
}
