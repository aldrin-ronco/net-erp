using NetErp.Helpers.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetErp.Helpers.Controls
{
    /// <summary>
    /// Lógica de interacción para NotificationsControl.xaml
    /// </summary>
    public partial class NotificationsControl : UserControl
    {
        public NotificationsControl()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null && button.DataContext is NotificationItem notification)
            {
                NotificationService.RemoveNotification(notification);
            }
        }

        private async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is NotificationAction action)
            {
                await ExecuteActionAsync(action);
            }
        }

        public async Task ExecuteActionAsync(NotificationAction action)
        {
            try
            {
                if (action.AsyncAction != null)
                {
                    await action.AsyncAction();
                }
                else if (action.Action != null)
                {
                    action.Action();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                System.Diagnostics.Debug.WriteLine($"Error executing notification action: {ex.Message}");
            }
        }
    }
}
