using Caliburn.Micro;
using DevExpress.Xpf.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;

namespace NetErp.Helpers.Services
{
    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class NotificationItem : PropertyChangedBase
    {
        // Brushes estáticos reutilizables
        private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(76, 175, 80));
        private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(244, 67, 54));
        private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(255, 152, 0));
        private static readonly SolidColorBrush InfoBrush = new(Color.FromRgb(33, 150, 243));
        private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(97, 97, 97));
        
        static NotificationItem()
        {
            // Congelar para mejor performance
            SuccessBrush.Freeze();
            ErrorBrush.Freeze();
            WarningBrush.Freeze();
            InfoBrush.Freeze();
            DefaultBrush.Freeze();
        }

        public Guid Id { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.Now;

        private string _message;
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                NotifyOfPropertyChange();
            }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                NotifyOfPropertyChange();
            }
        }

        private NotificationType _type;
        public NotificationType Type
        {
            get { return _type; }
            set
            {
                _type = value;
                NotifyOfPropertyChange();
            }
        }

        // Propiedades para UI
        public SolidColorBrush Background => Type switch
        {
            NotificationType.Success => SuccessBrush,
            NotificationType.Error => ErrorBrush,
            NotificationType.Warning => WarningBrush,
            NotificationType.Info => InfoBrush,
            _ => DefaultBrush
        };

        public string Symbol
        {
            get
            {
                return Type switch
                {
                    NotificationType.Success => "✓",  // Check
                    NotificationType.Error => "✗",    // X
                    NotificationType.Warning => "⚠",  // Triángulo
                    NotificationType.Info => "ℹ",     // i
                    _ => "•"
                };
            }
        }

        public NotificationItem(string message, string title, NotificationType type)
        {
            _message = message;
            _title = title;
            _type = type;
        }
    }
    public interface INotificationService
    {
        void ShowSuccess(string message, string title = "Éxito", int durationMs = 3000);
        void ShowError(string message, string title = "Error", int durationMs = 3000);
        void ShowWarning(string message, string title = "Advertencia", int durationMs = 3000);
        void ShowInfo(string message, string title = "Información", int durationMs = 3000);
    }

    public class NotificationService : INotificationService
    {
        // Colección compartida a nivel de aplicación
        private static readonly ObservableCollection<NotificationItem> _notifications = new();
        public static ObservableCollection<NotificationItem> GlobalNotifications => _notifications;

        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        public void ShowSuccess(string message, string title = "Éxito", int durationMs = 3000)
        {
            ShowNotification(message, title, NotificationType.Success, durationMs);
            _logger.LogInformation($"Success: {title} - {message}");
        }

        public void ShowError(string message, string title = "Error", int durationMs = 3000)
        {
            ShowNotification(message, title, NotificationType.Error, durationMs);
            _logger.LogError($"Error: {title} - {message}");
        }

        public void ShowWarning(string message, string title = "Advertencia", int durationMs = 3000)
        {
            ShowNotification(message, title, NotificationType.Warning, durationMs);
            _logger.LogWarning($"Warning: {title} - {message}");
        }

        public void ShowInfo(string message, string title = "Información", int durationMs = 3000)
        {
            ShowNotification(message, title, NotificationType.Info, durationMs);
            _logger.LogInformation($"Info: {title} - {message}");
        }

        private void ShowNotification(string message, string title, NotificationType type, int durationMs)
        {
            Execute.OnUIThread(() =>
            {
                _notifications.Clear();

                var notification = new NotificationItem(message, title, type);
                _notifications.Add(notification);

                // Timer con cleanup apropiado
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
                
                EventHandler tickHandler = null;
                tickHandler = (s, e) =>
                {
                    timer.Tick -= tickHandler;  // Desuscribir handler
                    timer.Stop();
                    timer = null;                // Limpiar referencia
                    _notifications.Remove(notification);
                };
                
                timer.Tick += tickHandler;
                timer.Start();
            });
        }

        // Método para eliminar manualmente una notificación
        public static void RemoveNotification(NotificationItem notification)
        {
            Execute.OnUIThread(() =>
            {
                if (_notifications.Contains(notification))
                {
                    _notifications.Remove(notification);
                }
            });
        }

        // Método para eliminar todas las notificaciones
        public static void ClearAllNotifications()
        {
            Execute.OnUIThread(() => _notifications.Clear());
        }
    }
}
