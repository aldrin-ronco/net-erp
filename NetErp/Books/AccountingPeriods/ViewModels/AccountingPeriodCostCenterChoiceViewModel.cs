using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Billing;

using Models.Books;
using Models.Global;
using NetErp.Helpers.Cache;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;
using static NetErp.Helpers.PermissionCodes;

namespace NetErp.Books.AccountingPeriods.ViewModels
{
    public class AccountingPeriodCostCenterChoiceViewModel : Screen, INotifyDataErrorInfo
    {
        private readonly CostCenterCache _costCenterCache;
        private readonly IEventAggregator _eventAggregator;
        private int _costCenterId;
        public AccountingPeriodCostCenterChoiceViewModel(CostCenterCache costCenterCache, IEventAggregator eventAggregator, int costCenterId)
        {
            _costCenterCache = costCenterCache;
            _eventAggregator = eventAggregator;
            _costCenterId = costCenterId;

        }
        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;

                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        } = [];

        public bool HasErrors => false;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            return null;
        }
        private IList<object> _selectedCostCenters =[];
        public IList<object> SelectedCostCenters
        {
            get => _selectedCostCenters;
            set
            {
                if (_selectedCostCenters != value)
                {
                    if (value != null && value.OfType<CostCenterGraphQLModel>().Any())
                    {
                        var selectedIds = value.OfType<CostCenterGraphQLModel>().Select(s => s.Id).ToList();
                        if (!selectedIds.Contains(_costCenterId))
                        {

                            ThemedMessageBox.Show(
                                title: "Atención!",
                                text: $"Debe seleccionar el centro de costo asociado al periodo contable.",
                                messageBoxButtons: MessageBoxButton.OK,
                                image: MessageBoxImage.Warning);
                            _selectedCostCenters = _selectedCostCenters;
                            return;
                        }
                    }
                    _selectedCostCenters = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenters));
                    NotifyOfPropertyChange(nameof(CanSave));

                }
            }
        }
        private bool _isBusy;
        public bool CanSave => !IsBusy && SelectedCostCenters.Count > 0 ;

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);

            try
            {

                await _costCenterCache.EnsureLoadedAsync();

                CostCenters = [.. _costCenterCache.Items];
                SelectedCostCenters = [CostCenters.First(f => f.Id == _costCenterId)];

            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnViewReady)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
                await TryCloseAsync();
                return;
            }
        }
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
        #region Dialog Size

        public double DialogWidth
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogWidth));
                }
            }
        } = 400;

        public double DialogHeight
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogHeight));
                }
            }
        } = 250;

        #endregion
        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {

                await _eventAggregator.PublishOnCurrentThreadAsync(
                
                new SelectedCostCentersMessage { SelectedCostCenters= SelectedCostCenters
    .OfType<CostCenterGraphQLModel>()
    .ToList()
                }
                   ,
               CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
              
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
               
            }
        }

        

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }
        #endregion
    }
}