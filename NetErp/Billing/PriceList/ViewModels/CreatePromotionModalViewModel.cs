using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using NetErp.Helpers;
using Ninject;
using Services.Billing.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace NetErp.Billing.PriceList.ViewModels
{
    public class CreatePromotionModalViewModel<TModel>: Screen
    {
        private IGenericDataAccess<PriceListGraphQLModel> PriceListService { get; set; } = IoC.Get<IGenericDataAccess<PriceListGraphQLModel>>();
        private readonly Helpers.IDialogService _dialogService;
        public DateTime MinimumDate { get; set; } = DateTime.Now;

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

		private DateTime _startDate = DateTime.Now;

		public DateTime StartDate
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

		private DateTime _endDate = DateTime.Now;
		public DateTime EndDate
		{
			get { return _endDate; }
			set
			{
				if( _endDate != value)
				{
					_endDate = value;
					NotifyOfPropertyChange(nameof(EndDate));
				}
			}
		}

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
                string query = @"
                    mutation ($data: CreatePriceListInput!) {
                      CreateResponse: createPriceList(data: $data) {
                        id
                        name
                        editablePrice
                        isActive
                        autoApplyDiscount
                        isPublic
                        allowNewUsersAccess
                        listUpdateBehaviorOnCostChange
                        parent{
                            id
                            name
                        }
                        isTaxable
                        priceListIncludeTax
                        useAlternativeFormula
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
                variables.Data.Name = Name.Trim().RemoveExtraSpaces(); //capture the name from the UI
                variables.Data.EditablePrice = true; //static value
                variables.Data.IsActive = true; //static value
                variables.Data.AutoApplyDiscount = true; //static value
                variables.Data.IsPublic = true; //static value
                variables.Data.AllowNewUsersAccess = true; //static value
                variables.Data.ListUpdateBehaviorOnCostChange = "UPDATE_PROFIT_MARGIN"; //static value
                variables.Data.ParentId = ParentPriceList.Id; //static value
                variables.Data.StartDate = DateTimeHelper.DateTimeKindUTC(StartDate);
                variables.Data.EndDate = DateTimeHelper.DateTimeKindUTC(EndDate);
                variables.Data.IsTaxable = true; //capture the value from the UI
                variables.Data.PriceListIncludeTax = true; //capture the value from the UI
                variables.Data.UseAlternativeFormula = false; //capture the value from the UI
                variables.Data.StorageId = 0; //capture the value from the UI
                var result = await PriceListService.Create(query, variables);

                Messenger.Default.Send(message: new ReturnedDataFromCreatePriceListModalViewMessage<TModel>() { ReturnedData = result }, token: "CreatePriceList");
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

        public PriceListGraphQLModel ParentPriceList { get; set; }

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
        }

		public CreatePromotionModalViewModel(Helpers.IDialogService dialogService, PriceListGraphQLModel parentPriceList)
		{
			_dialogService = dialogService;
            ParentPriceList = parentPriceList;
        }
    }
}
