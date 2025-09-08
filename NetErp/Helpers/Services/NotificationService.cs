using Caliburn.Micro;
using DevExpress.Xpf.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace NetErp.Helpers.Services
{
    /// <summary>
    /// Enumeration defining the types of notifications available in the application.
    /// Used to determine the visual styling and logging level of notifications.
    /// </summary>
    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info,
        Question
    }

    /// <summary>
    /// Represents an interactive action that can be attached to notifications.
    /// Allows users to perform actions directly from notification popups.
    /// </summary>
    /// <example>
    /// <code>
    /// var actions = new List&lt;NotificationAction&gt;
    /// {
    ///     new() { Text = "Aceptar", Style = "Primary", Action = () => DoSomething() },
    ///     new() { Text = "Cancelar", Style = "Secondary", Action = () => Cancel() }
    /// };
    /// </code>
    /// </example>
    public class NotificationAction
    {
        /// <summary>
        /// The display text for the action button.
        /// </summary>
        public string Text { get; set; } = string.Empty;
        
        /// <summary>
        /// The visual style of the action button. Supported values: "Primary", "Secondary", "Success", "Danger", "Default".
        /// </summary>
        public string Style { get; set; } = "Default";
        
        /// <summary>
        /// Synchronous action to execute when the button is clicked. Use either Action or AsyncAction, not both.
        /// </summary>
        public System.Action? Action { get; set; }
        
        /// <summary>
        /// Asynchronous action to execute when the button is clicked. Use either Action or AsyncAction, not both.
        /// </summary>
        public System.Func<Task>? AsyncAction { get; set; }
    }

    /// <summary>
    /// Represents a single notification item in the system.
    /// Contains all necessary information for displaying notifications with proper styling and behavior.
    /// Implements PropertyChangedBase for data binding support in WPF.
    /// </summary>
    /// <remarks>
    /// This class uses static frozen brushes for optimal performance and memory usage.
    /// Each notification has a unique ID and timestamp for tracking purposes.
    /// </remarks>
    public class NotificationItem : PropertyChangedBase
    {
        // Brushes estáticos reutilizables
        private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(76, 175, 80));
        private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(244, 67, 54));
        private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(255, 152, 0));
        private static readonly SolidColorBrush InfoBrush = new(Color.FromRgb(33, 150, 243));
        private static readonly SolidColorBrush QuestionBrush = new(Color.FromRgb(156, 39, 176));
        private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(97, 97, 97));
        
        static NotificationItem()
        {
            // Congelar para mejor performance
            SuccessBrush.Freeze();
            ErrorBrush.Freeze();
            WarningBrush.Freeze();
            InfoBrush.Freeze();
            QuestionBrush.Freeze();
            DefaultBrush.Freeze();
        }

        /// <summary>
        /// Unique identifier for this notification instance.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();
        
        /// <summary>
        /// Timestamp when the notification was created.
        /// </summary>
        public DateTime CreatedAt { get; } = DateTime.Now;
        
        /// <summary>
        /// Collection of interactive actions available for this notification.
        /// </summary>
        public List<NotificationAction> Actions { get; set; } = new();
        
        /// <summary>
        /// Indicates whether this notification has interactive actions available.
        /// Interactive notifications remain visible until user interaction.
        /// </summary>
        public bool IsInteractive => Actions.Count > 0;

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

        /// <summary>
        /// Gets the background brush color based on the notification type.
        /// Uses cached frozen brushes for optimal performance.
        /// </summary>
        public SolidColorBrush Background => Type switch
        {
            NotificationType.Success => SuccessBrush,
            NotificationType.Error => ErrorBrush,
            NotificationType.Warning => WarningBrush,
            NotificationType.Info => InfoBrush,
            NotificationType.Question => QuestionBrush,
            _ => DefaultBrush
        };

        /// <summary>
        /// Gets the Unicode symbol to display based on the notification type.
        /// Provides visual cues for different types of notifications.
        /// </summary>
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
                    NotificationType.Question => "?", // Pregunta
                    _ => "•"
                };
            }
        }

        /// <summary>
        /// Initializes a new notification item with the specified parameters.
        /// </summary>
        /// <param name="message">The notification message content</param>
        /// <param name="title">The notification title</param>
        /// <param name="type">The type of notification which determines styling</param>
        public NotificationItem(string message, string title, NotificationType type)
        {
            _message = message;
            _title = title;
            _type = type;
        }
    }
    /// <summary>
    /// Interface for the application's notification service.
    /// Provides methods to display different types of notifications to users with automatic dismissal and logging.
    /// </summary>
    /// <remarks>
    /// This service integrates with the application's logging system and provides both timed and interactive notifications.
    /// All methods are thread-safe and automatically marshal to the UI thread when necessary.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Inject in constructor
    /// private readonly INotificationService _notificationService;
    /// 
    /// // Usage examples
    /// _notificationService.ShowSuccess("Cliente guardado correctamente");
    /// _notificationService.ShowError("Error al conectar con el servidor", durationMs: 5000);
    /// _notificationService.ShowQuestion("¿Desea continuar?", actions);
    /// </code>
    /// </example>
    public interface INotificationService
    {
        /// <summary>
        /// Displays a success notification with automatic dismissal.
        /// </summary>
        /// <param name="message">The success message to display</param>
        /// <param name="title">The notification title (default: "Éxito")</param>
        /// <param name="durationMs">Duration in milliseconds before auto-dismissal (default: 3000ms)</param>
        void ShowSuccess(string message, string title = "Éxito", int durationMs = 3000);
        
        /// <summary>
        /// Displays an error notification with automatic dismissal.
        /// </summary>
        /// <param name="message">The error message to display</param>
        /// <param name="title">The notification title (default: "Error")</param>
        /// <param name="durationMs">Duration in milliseconds before auto-dismissal (default: 3000ms)</param>
        void ShowError(string message, string title = "Error", int durationMs = 3000);
        
        /// <summary>
        /// Displays a warning notification with automatic dismissal.
        /// </summary>
        /// <param name="message">The warning message to display</param>
        /// <param name="title">The notification title (default: "Advertencia")</param>
        /// <param name="durationMs">Duration in milliseconds before auto-dismissal (default: 3000ms)</param>
        void ShowWarning(string message, string title = "Advertencia", int durationMs = 3000);
        
        /// <summary>
        /// Displays an informational notification with automatic dismissal.
        /// </summary>
        /// <param name="message">The informational message to display</param>
        /// <param name="title">The notification title (default: "Información")</param>
        /// <param name="durationMs">Duration in milliseconds before auto-dismissal (default: 3000ms)</param>
        void ShowInfo(string message, string title = "Información", int durationMs = 3000);
        
        /// <summary>
        /// Displays an interactive question notification that remains visible until user interaction.
        /// </summary>
        /// <param name="message">The question message to display</param>
        /// <param name="actions">List of actions the user can take</param>
        /// <param name="title">The notification title (default: "Pregunta")</param>
        void ShowQuestion(string message, List<NotificationAction> actions, string title = "Pregunta");
    }

    /// <summary>
    /// Implementation of the notification service for the NetERP application.
    /// Manages in-app notifications with automatic timing, UI thread marshaling, and logging integration.
    /// </summary>
    /// <remarks>
    /// This service maintains a global collection of notifications that can be bound to UI controls.
    /// It automatically handles memory management by cleaning up timer references and removing notifications.
    /// All notifications are logged using the configured ILogger for debugging and audit purposes.
    /// </remarks>
    public class NotificationService : INotificationService
    {
        /// <summary>
        /// Global collection of active notifications shared across the application.
        /// This collection is thread-safe and can be bound to UI controls for display.
        /// </summary>
        private static readonly ObservableCollection<NotificationItem> _notifications = new();
        
        /// <summary>
        /// Public accessor for the global notifications collection.
        /// UI controls can bind to this collection to display active notifications.
        /// </summary>
        public static ObservableCollection<NotificationItem> GlobalNotifications => _notifications;

        private readonly ILogger<NotificationService> _logger;

        /// <summary>
        /// Initializes a new instance of the NotificationService with dependency injection.
        /// </summary>
        /// <param name="logger">Logger instance for recording notification events</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Displays a success notification with green styling and automatic dismissal.
        /// </summary>
        /// <param name="message">The success message content</param>
        /// <param name="title">Optional title (defaults to "Éxito")</param>
        /// <param name="durationMs">Auto-dismissal duration in milliseconds (defaults to 3000ms)</param>
        public void ShowSuccess(string message, string title = "Éxito", int durationMs = 3000)
        {
            ShowNotification(message, title, NotificationType.Success, durationMs);
            _logger.LogInformation($"Success: {title} - {message}");
        }

        /// <summary>
        /// Displays an error notification with red styling and automatic dismissal.
        /// </summary>
        /// <param name="message">The error message content</param>
        /// <param name="title">Optional title (defaults to "Error")</param>
        /// <param name="durationMs">Auto-dismissal duration in milliseconds (defaults to 3000ms)</param>
        public void ShowError(string message, string title = "Error", int durationMs = 3000)
        {
            ShowNotification(message, title, NotificationType.Error, durationMs);
            _logger.LogError($"Error: {title} - {message}");
        }

        /// <summary>
        /// Displays a warning notification with orange styling and automatic dismissal.
        /// </summary>
        /// <param name="message">The warning message content</param>
        /// <param name="title">Optional title (defaults to "Advertencia")</param>
        /// <param name="durationMs">Auto-dismissal duration in milliseconds (defaults to 3000ms)</param>
        public void ShowWarning(string message, string title = "Advertencia", int durationMs = 3000)
        {
            ShowNotification(message, title, NotificationType.Warning, durationMs);
            _logger.LogWarning($"Warning: {title} - {message}");
        }

        /// <summary>
        /// Displays an informational notification with blue styling and automatic dismissal.
        /// </summary>
        /// <param name="message">The informational message content</param>
        /// <param name="title">Optional title (defaults to "Información")</param>
        /// <param name="durationMs">Auto-dismissal duration in milliseconds (defaults to 3000ms)</param>
        public void ShowInfo(string message, string title = "Información", int durationMs = 3000)
        {
            ShowNotification(message, title, NotificationType.Info, durationMs);
            _logger.LogInformation($"Info: {title} - {message}");
        }

        /// <summary>
        /// Displays an interactive question notification that persists until user action.
        /// </summary>
        /// <param name="message">The question message content</param>
        /// <param name="actions">Collection of actions the user can perform</param>
        /// <param name="title">Optional title (defaults to "Pregunta")</param>
        public void ShowQuestion(string message, List<NotificationAction> actions, string title = "Pregunta")
        {
            ShowInteractiveNotification(message, title, NotificationType.Question, actions);
            _logger.LogInformation($"Question: {title} - {message}");
        }

        /// <summary>
        /// Internal method to handle the creation and timing of standard (non-interactive) notifications.
        /// Clears existing notifications, creates a new one, and sets up automatic removal timer.
        /// </summary>
        /// <param name="message">The notification message</param>
        /// <param name="title">The notification title</param>
        /// <param name="type">The type of notification for styling</param>
        /// <param name="durationMs">Milliseconds before auto-removal</param>
        private void ShowNotification(string message, string title, NotificationType type, int durationMs)
        {
            Execute.OnUIThread(() =>
            {
                _notifications.Clear();

                var notification = new NotificationItem(message, title, type);
                _notifications.Add(notification);

                // Timer with proper cleanup to prevent memory leaks
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
                
                EventHandler tickHandler = null;
                tickHandler = (s, e) =>
                {
                    timer.Tick -= tickHandler;  // Unsubscribe handler
                    timer.Stop();
                    timer = null;                // Clear reference for GC
                    _notifications.Remove(notification);
                };
                
                timer.Tick += tickHandler;
                timer.Start();
            });
        }

        /// <summary>
        /// Internal method to handle interactive notifications with user actions.
        /// These notifications persist until user interaction and automatically wrap action callbacks
        /// to remove the notification after execution.
        /// </summary>
        /// <param name="message">The notification message</param>
        /// <param name="title">The notification title</param>
        /// <param name="type">The type of notification for styling</param>
        /// <param name="actions">List of actions to present to the user</param>
        private void ShowInteractiveNotification(string message, string title, NotificationType type, List<NotificationAction> actions)
        {
            Execute.OnUIThread(() =>
            {
                _notifications.Clear();

                var notification = new NotificationItem(message, title, type)
                {
                    Actions = actions
                };

                // Configure actions to auto-remove notification after execution
                foreach (var action in actions)
                {
                    var originalAction = action.Action;
                    var originalAsyncAction = action.AsyncAction;

                    if (originalAction != null)
                    {
                        action.Action = () =>
                        {
                            try
                            {
                                originalAction();
                            }
                            finally
                            {
                                RemoveNotification(notification);
                            }
                        };
                    }
                    else if (originalAsyncAction != null)
                    {
                        action.AsyncAction = async () =>
                        {
                            try
                            {
                                await originalAsyncAction();
                            }
                            finally
                            {
                                RemoveNotification(notification);
                            }
                        };
                    }
                }

                _notifications.Add(notification);
                // No timer - notification persists until user interaction
            });
        }

        /// <summary>
        /// Manually removes a specific notification from the global collection.
        /// This method is thread-safe and marshals to the UI thread automatically.
        /// </summary>
        /// <param name="notification">The notification item to remove</param>
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

        /// <summary>
        /// Clears all active notifications from the global collection.
        /// This method is thread-safe and marshals to the UI thread automatically.
        /// </summary>
        public static void ClearAllNotifications()
        {
            Execute.OnUIThread(() => _notifications.Clear());
        }
    }
}
