using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Models.Billing;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualStudio.Threading;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class CreatePromotionModalViewModel : Screen, INotifyDataErrorInfo
    {
        private readonly IRepository<PriceListGraphQLModel> _priceListService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly StringLengthCache _stringLengthCache;
        private readonly Dictionary<string, List<string>> _errors;
        public DateTime MinimumDate { get; set; } = DateTime.Now;

        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ValidateProperty(nameof(Name), field);
                    NotifyOfPropertyChange(nameof(Name));
                    this.TrackChange(nameof(Name));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public DateTime StartDate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = DateTime.SpecifyKind(value, DateTimeKind.Utc);
                    NotifyOfPropertyChange(nameof(StartDate));
                    this.TrackChange(nameof(StartDate));
                    NotifyOfPropertyChange(nameof(CanSave));
                    EndDate = field;
                }
            }
        } = DateTime.Now;

        public DateTime EndDate
        {
            get;
            set
            {
                if (field != value)
                {
                    field = DateTime.SpecifyKind(value, DateTimeKind.Utc);
                    NotifyOfPropertyChange(nameof(EndDate));
                    this.TrackChange(nameof(EndDate));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = DateTime.Now;

        // Propiedades heredadas del padre — se asignan en SetForNew()
        public bool EditablePrice { get; set; }

        [ExpandoPath("isActive")]
        public bool IsActiveFlag { get; set; }

        public bool AutoApplyDiscount { get; set; }
        public bool IsPublic { get; set; }
        public bool AllowNewUsersAccess { get; set; }
        public string ListUpdateBehaviorOnCostChange { get; set; } = string.Empty;
        public bool IsTaxable { get; set; }
        public bool PriceListIncludeTax { get; set; }
        public bool UseAlternativeFormula { get; set; }
        public string CostMode { get; set; } = string.Empty;

        [ExpandoPath("storageId")]
        public int? StorageId { get; set; }

        [ExpandoPath("parentId")]
        public int ParentId { get; set; }

        public PriceListGraphQLModel ParentPriceList { get; set; }

        private ICommand _cancelCommand;

        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null) _cancelCommand = new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        public async Task CancelAsync()
        {
            await _dialogService.CloseDialogAsync(this, true);
        }

        private ICommand _saveCommand;

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var (_, query) = _createQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                UpsertResponseType<PriceListGraphQLModel> result = await _priceListService.CreateAsync<UpsertResponseType<PriceListGraphQLModel>>(query, variables);

                if (!result.Success)
                {
                    ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                    return;
                }

                await _eventAggregator.PublishOnCurrentThreadAsync(new PriceListCreateMessage { CreatedPriceList = result });
                await _dialogService.CloseDialogAsync(this, true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
            }
        }

        public bool CanSave
        {
            get
            {
                return _errors.Count <= 0 && this.HasChanges();
            }
        }

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

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(() => Name)), DispatcherPriority.Render);
            ValidateProperty(nameof(Name), Name);
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        public void SetForNew()
        {
            Name = string.Empty;
            StartDate = DateTime.Now;
            EndDate = DateTime.Now;
            ParentId = ParentPriceList.Id;

            // Heredar configuración de la lista de precios padre
            EditablePrice = ParentPriceList.EditablePrice;
            IsActiveFlag = true;
            AutoApplyDiscount = ParentPriceList.AutoApplyDiscount;
            IsPublic = ParentPriceList.IsPublic;
            AllowNewUsersAccess = ParentPriceList.AllowNewUsersAccess;
            ListUpdateBehaviorOnCostChange = ParentPriceList.ListUpdateBehaviorOnCostChange;
            IsTaxable = ParentPriceList.IsTaxable;
            PriceListIncludeTax = ParentPriceList.PriceListIncludeTax;
            UseAlternativeFormula = ParentPriceList.UseAlternativeFormula;
            CostMode = ParentPriceList.CostMode;
            StorageId = ParentPriceList.Storage?.Id;

            SeedDefaultValues();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            // UI properties
            this.SeedValue(nameof(StartDate), StartDate);
            this.SeedValue(nameof(EndDate), EndDate);
            // Hidden defaults
            this.SeedValue(nameof(EditablePrice), EditablePrice);
            this.SeedValue(nameof(IsActiveFlag), IsActiveFlag);
            this.SeedValue(nameof(AutoApplyDiscount), AutoApplyDiscount);
            this.SeedValue(nameof(IsPublic), IsPublic);
            this.SeedValue(nameof(AllowNewUsersAccess), AllowNewUsersAccess);
            this.SeedValue(nameof(ListUpdateBehaviorOnCostChange), ListUpdateBehaviorOnCostChange);
            this.SeedValue(nameof(IsTaxable), IsTaxable);
            this.SeedValue(nameof(PriceListIncludeTax), PriceListIncludeTax);
            this.SeedValue(nameof(UseAlternativeFormula), UseAlternativeFormula);
            this.SeedValue(nameof(CostMode), CostMode);
            this.SeedValue(nameof(StorageId), StorageId);
            this.SeedValue(nameof(ParentId), ParentId);
            this.AcceptChanges();
        }

        public CreatePromotionModalViewModel(
            Helpers.IDialogService dialogService,
            IEventAggregator eventAggregator,
            PriceListGraphQLModel parentPriceList,
            IRepository<PriceListGraphQLModel> priceListService,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _errors = [];
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            ParentPriceList = parentPriceList;
            _priceListService = priceListService;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
        }

        public int NameMaxLength => _stringLengthCache.GetMaxLength<PriceListGraphQLModel>(nameof(PriceListGraphQLModel.Name));

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? value)) return Enumerable.Empty<string>();
            return value;
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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
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

            var fragment = new GraphQLQueryFragment("createPriceList",
                [new("input", "CreatePriceListInput!")], fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });
    }
}
