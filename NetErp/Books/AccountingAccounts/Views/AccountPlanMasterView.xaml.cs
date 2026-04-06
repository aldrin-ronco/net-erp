using NetErp.Books.AccountingAccounts.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NetErp.Books.AccountingAccounts.Views
{
    public partial class AccountPlanMasterView : UserControl
    {
        public AccountPlanMasterView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldVm)
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;

            if (e.NewValue is INotifyPropertyChanged newVm)
                newVm.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AccountPlanMasterViewModel.IsBusy))
            {
                if (sender is AccountPlanMasterViewModel vm && !vm.IsBusy)
                {
                    Dispatcher.BeginInvoke(() => SearchTextBox.Focus(), DispatcherPriority.ApplicationIdle);
                }
            }
        }
    }
}
