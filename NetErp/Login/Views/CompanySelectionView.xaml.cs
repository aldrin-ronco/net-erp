using NetErp.Login.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Login.Views
{
    public partial class CompanySelectionView : UserControl
    {
        public CompanySelectionView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            FocusSearchBox();
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
            if (e.PropertyName == nameof(CompanySelectionViewModel.FilteredOrganizationGroups))
            {
                Dispatcher.BeginInvoke(() => FocusSearchBox(),
                    System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void FocusSearchBox()
        {
            if (DataContext is CompanySelectionViewModel vm && vm.IsAdminMode)
                AdminSearchBox.Focus();
            else
                RegularSearchBox.Focus();
        }
    }
}
