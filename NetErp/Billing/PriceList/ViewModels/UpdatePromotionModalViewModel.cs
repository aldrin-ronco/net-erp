using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using NetErp.Helpers;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class UpdatePromotionModalViewModel<TModel>: Screen
    {
        private readonly Helpers.IDialogService _dialogService;
        private IGenericDataAccess<PriceListGraphQLModel> PriceListService { get; set; } = IoC.Get<IGenericDataAccess<PriceListGraphQLModel>>();

        public DateTime? MinimumDate { get; set; } 

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

        private string _Name = string.Empty;

        public string Name
        {
            get { return _Name; }
            set
            {
                if (_Name != value)
                {
                    _Name = value;
                    NotifyOfPropertyChange(nameof(Name));
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
                    EndDate = StartDate;
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
                }
            }
        }

        public int Id { get; set; }
        public async Task SaveAsync()
        {
            try
            {
                string query = @"
                    mutation ($data: UpdatePriceListInput!, $id: Int!) {
                      UpdateResponse: updatePriceList(data: $data, id: $id) {
                        id
                        name
                        editablePrice
                        isActive
                        autoApplyDiscount
                        isPublic
                        allowNewUsersAccess
                        listUpdateBehaviorOnCostChange
                        isTaxable
                        startDate
                        endDate
                        priceListIncludeTax
                        useAlternativeFormula
                        parent{
                            id
                            name
                        }
                        storage {
                          id
                          name
                        }
                        paymentMethods{
                          id
                          name
                          abbreviation
                        }
                      }
                    }
                    ";
                dynamic variables = new ExpandoObject();
                variables.Data = new ExpandoObject();
                variables.Id = Id;
                variables.Data.Name = Name.Trim().RemoveExtraSpaces();
                variables.Data.StartDate = DateTimeHelper.DateTimeKindUTC(StartDate);
                variables.Data.EndDate = DateTimeHelper.DateTimeKindUTC(EndDate);
                var result = await PriceListService.Update(query, variables);

                Messenger.Default.Send(message: new ReturnedDataFromUpdatePromotionModalViewMessage<TModel>() { ReturnedData = result }, token: "UpdatePromotion");
                await _dialogService.CloseDialogAsync(this, true);
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

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
        private ICommand _saveCommand;

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }
        public UpdatePromotionModalViewModel(Helpers.IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = Application.Current.Dispatcher.BeginInvoke(new System.Action(() => this.SetFocus(() => Name)), DispatcherPriority.Render);
        }
    }

    public class ReturnedDataFromUpdatePromotionModalViewMessage<TModel>
    {
        public TModel? ReturnedData { get; set; }
    }
}
