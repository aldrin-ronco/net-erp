using NetErp.Global.Collaborator.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Global.Collaborator.Views
{
    public partial class CollaboratorInviteView : UserControl
    {
        public CollaboratorInviteView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            SearchEmailBox.Focus();
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
            if (e.PropertyName == nameof(CollaboratorInviteViewModel.SearchResults))
            {
                Dispatcher.BeginInvoke(() => SearchEmailBox.Focus(),
                    System.Windows.Threading.DispatcherPriority.Input);
            }
        }
    }
}
