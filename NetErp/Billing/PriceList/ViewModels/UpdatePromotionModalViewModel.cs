using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #region Properties

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public int Id { get; set; }

        public DateTime? MinimumDate { get; set; }

        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(Name));
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public DateTime? StartDate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(StartDate));
                    this.TrackChange(nameof(StartDate));
                    NotifyOfPropertyChange(nameof(CanSave));
                    EndDate = StartDate;
                }
            }
        } = DateTime.Now;

        [ExpandoPath("isActive")]
        public bool IsActiveFlag
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsActiveFlag));
                    this.TrackChange(nameof(IsActiveFlag));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public DateTime? EndDate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(EndDate));
                    this.TrackChange(nameof(EndDate));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = DateTime.Now;

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

        public ICommand SaveCommand
        {
            get
            {
                field ??= new AsyncCommand(SaveAsync);
                return field;
            }
        }

        public ICommand CancelCommand
        {
            get
            {
                field ??= new AsyncCommand(CancelAsync);
                return field;
            }
        }

        #endregion

        #region Focus

        public bool NameFocus
        {
            get;
            set
            {
                field = value;
                NotifyOfPropertyChange(nameof(NameFocus));
            }
        }

        #endregion

        #region Constructor

        public UpdatePromotionModalViewModel(
            Helpers.IDialogService dialogService,
            IEventAggregator eventAggregator,
            IRepository<PriceListGraphQLModel> priceListService,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _errors = [];
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _priceListService = priceListService;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
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
                IsBusy = true;
                var (_, query) = _updateQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;

                // Override dates with UTC conversion if they were changed
                IDictionary<string, object> dict = (IDictionary<string, object>)variables;
                if (dict.ContainsKey("updateResponseDataStartDate"))
                    dict["updateResponseDataStartDate"] = DateTimeHelper.DateTimeKindUTC(StartDate);
                if (dict.ContainsKey("updateResponseDataEndDate"))
                    dict["updateResponseDataEndDate"] = DateTimeHelper.DateTimeKindUTC(EndDate);

                UpsertResponseType<PriceListGraphQLModel> result = await _priceListService.UpdateAsync<UpsertResponseType<PriceListGraphQLModel>>(query, variables);

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
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
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
            Dispatcher.CurrentDispatcher.BeginInvoke(() => this.SetFocus(() => Name));
        }

        #endregion

        #region Validation

        public int NameMaxLength => _stringLengthCache.GetMaxLength<PriceListGraphQLModel>(nameof(PriceListGraphQLModel.Name));

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return Enumerable.Empty<string>();
            return _errors[propertyName];
        }

        private void SetPropertyErrors(string propertyName, IReadOnlyList<string> errors)
        {
            bool hadErrors = _errors.ContainsKey(propertyName);

            if (errors.Count > 0)
                _errors[propertyName] = [.. errors];
            else if (hadErrors)
                _errors.Remove(propertyName);

            if (hadErrors || errors.Count > 0)
                RaiseErrorsChanged(propertyName);
        }

        private void ValidateProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty;
            List<string> errors = [];
            switch (propertyName)
            {
                case nameof(Name):
                    if (string.IsNullOrEmpty(value.Trim())) errors.Add("El nombre no puede estar vacío");
                    break;
            }
            SetPropertyErrors(propertyName, errors);
        }

        #endregion

        #region GraphQL Query

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
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

            var fragment = new GraphQLQueryFragment("updatePriceList",
                [new("data", "UpdatePriceListInput!"), new("id", "ID!")], fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
