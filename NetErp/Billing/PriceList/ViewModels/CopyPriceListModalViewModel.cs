using Caliburn.Micro;
using Common.Extensions;
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
    public class CopyPriceListModalViewModel : Screen, INotifyDataErrorInfo
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public double DialogWidth { get; set; }
        public double DialogHeight { get; set; }

        public int SourceId { get; set; }

        public string SourceName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SourceName));
                }
            }
        } = string.Empty;

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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        #endregion

        #region CanSave

        public bool CanSave
        {
            get
            {
                if (IsBusy) return false;
                if (_errors.Count > 0) return false;
                if (string.IsNullOrWhiteSpace(Name)) return false;
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

        void SetFocus(Expression<Func<object>> propertyExpression)
        {
            string controlName = propertyExpression.GetMemberInfo().Name;
            NameFocus = false;
            NameFocus = controlName == nameof(Name);
        }

        #endregion

        #region Constructor

        public CopyPriceListModalViewModel(
            Helpers.IDialogService dialogService,
            IEventAggregator eventAggregator,
            IRepository<PriceListGraphQLModel> priceListService,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _errors = [];
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _priceListService = priceListService ?? throw new ArgumentNullException(nameof(priceListService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
        }

        #endregion

        #region SetForCopy

        public void SetForCopy(PriceListGraphQLModel source)
        {
            SourceId = source.Id;
            SourceName = source.Name;
            Name = string.Empty;
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                var (fragment, query) = _copyQuery.Value;

                dynamic variables = new GraphQLVariables()
                    .For(fragment, "input", new
                    {
                        sourcePriceListId = SourceId,
                        name = Name.Trim().RemoveExtraSpaces(),
                        copyDetails = true
                    })
                    .Build();

                UpsertResponseType<PriceListGraphQLModel> result =
                    await _priceListService.CreateAsync<UpsertResponseType<PriceListGraphQLModel>>(query, variables);

                if (!result.Success)
                {
                    ThemedMessageBox.Show(
                        text: $"La copia no ha sido exitosa \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(new PriceListCreateMessage { CreatedPriceList = result });
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
            NotifyOfPropertyChange(nameof(CanSave));
            _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(
                new System.Action(() => SetFocus(() => Name)),
                DispatcherPriority.Render);
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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _copyQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<PriceListGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "priceList", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.EditablePrice)
                    .Field(f => f.IsActive)
                    .Field(f => f.AutoApplyDiscount)
                    .Field(f => f.IsPublic)
                    .Field(f => f.AllowNewUsersAccess)
                    .Field(f => f.ListUpdateBehaviorOnCostChange)
                    .Field(f => f.IsTaxable)
                    .Field(f => f.PriceListIncludeTax)
                    .Field(f => f.UseAlternativeFormula)
                    .Field(f => f.CostMode)
                    .Select(f => f.Parent, p => p.Field(x => x!.Id).Field(x => x!.Name))
                    .Select(f => f.Storage, s => s.Field(x => x!.Id).Field(x => x!.Name)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("copyPriceList",
                [new("input", "CopyPriceListInput!")], fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
