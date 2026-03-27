using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class UpdatePromotionModalViewModel : Screen, INotifyDataErrorInfo
    {
        private readonly Helpers.IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Dictionary<string, List<string>> _errors;
        private readonly IRepository<PriceListGraphQLModel> _priceListService;

        #region Properties

        public int Id { get; set; }

        public DateTime? MinimumDate { get; set; }

        private string _name = string.Empty;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    ValidateProperty(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(Name));
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private DateTime? _startDate = DateTime.Now;

        public DateTime? StartDate
        {
            get { return _startDate; }
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    NotifyOfPropertyChange(nameof(StartDate));
                    this.TrackChange(nameof(StartDate));
                    NotifyOfPropertyChange(nameof(CanSave));
                    EndDate = StartDate;
                }
            }
        }

        private bool _isActiveFlag;

        [ExpandoPath("isActive")]
        public bool IsActiveFlag
        {
            get { return _isActiveFlag; }
            set
            {
                if (_isActiveFlag != value)
                {
                    _isActiveFlag = value;
                    NotifyOfPropertyChange(nameof(IsActiveFlag));
                    this.TrackChange(nameof(IsActiveFlag));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private DateTime? _endDate = DateTime.Now;

        public DateTime? EndDate
        {
            get { return _endDate; }
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    NotifyOfPropertyChange(nameof(EndDate));
                    this.TrackChange(nameof(EndDate));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region CanSave

        public bool CanSave
        {
            get
            {
                if (_errors.Count > 0) return false;
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
                _saveCommand ??= new AsyncCommand(SaveAsync);
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

        #region Focus

        private bool _nameFocus;

        public bool NameFocus
        {
            get { return _nameFocus; }
            set
            {
                _nameFocus = value;
                NotifyOfPropertyChange(nameof(NameFocus));
            }
        }

        #endregion

        #region Constructor

        public UpdatePromotionModalViewModel(
            Helpers.IDialogService dialogService,
            IEventAggregator eventAggregator,
            IRepository<PriceListGraphQLModel> priceListService)
        {
            _errors = [];
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _priceListService = priceListService;
        }

        #endregion

        #region SetForEdit / Seeding

        public void SetForEdit(PriceListGraphQLModel promotion)
        {
            Id = promotion.Id;
            Name = promotion.Name;
            IsActiveFlag = promotion.IsActive;
            MinimumDate = promotion.StartDate;
            StartDate = promotion.StartDate;
            EndDate = promotion.EndDate;
            SeedCurrentValues();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(IsActiveFlag), IsActiveFlag);
            this.SeedValue(nameof(StartDate), StartDate);
            this.SeedValue(nameof(EndDate), EndDate);
            this.AcceptChanges();
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                string query = _updateQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;

                // Override dates with UTC conversion if they were changed
                var dict = (IDictionary<string, object>)variables;
                if (dict.ContainsKey("updateResponseDataStartDate"))
                    dict["updateResponseDataStartDate"] = DateTimeHelper.DateTimeKindUTC(StartDate);
                if (dict.ContainsKey("updateResponseDataEndDate"))
                    dict["updateResponseDataEndDate"] = DateTimeHelper.DateTimeKindUTC(EndDate);

                var result = await _priceListService.UpdateAsync<UpsertResponseType<PriceListGraphQLModel>>(query, variables);

                if (!result.Success)
                {
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(new PriceListUpdateMessage { UpdatedPriceList = result });
                await _dialogService.CloseDialogAsync(this, true);
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(
                        title: "Atenci\u00f3n!",
                        text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        public async Task CancelAsync()
        {
            await _dialogService.CloseDialogAsync(this, true);
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperty(nameof(Name), Name);
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
            _ = Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(() => Name)), DispatcherPriority.Render);
        }

        #endregion

        #region Validation

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable? GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = [];

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
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(Name):
                        if (string.IsNullOrEmpty(value.Trim())) AddError(propertyName, "El nombre no puede estar vac\u00edo");
                        break;
                }
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() =>
                {
                    ThemedMessageBox.Show(
                        title: "Atenci\u00f3n!",
                        text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                });
            }
        }

        #endregion

        #region GraphQL Query

        private static readonly Lazy<string> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<PriceListGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "priceList", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.IsActive)
                    .Field(f => f.StartDate)
                    .Field(f => f.EndDate))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var dataParam = new GraphQLQueryParameter("data", "UpdatePriceListInput!");
            var idParam = new GraphQLQueryParameter("id", "ID!");
            var fragment = new GraphQLQueryFragment("updatePriceList", [dataParam, idParam], fields, "UpdateResponse");
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        #endregion
    }
}
