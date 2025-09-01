using Caliburn.Micro;
using Common.Interfaces;
using Models.Login;
using NetErp.Helpers.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NetErp.Login.ViewModels
{
    public class LoginViewModel : Screen
    {
        private readonly ILoginService _loginService;
        private readonly INotificationService _notificationService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ISQLiteEmailStorageService _emailStorageService;

        private string _email = string.Empty;
        private string _password = string.Empty;
        private bool _isLoading = false;
        private string _loginButtonText = "INICIAR SESIÓN";
        
        // Propiedades para autocompletar emails
        private ObservableCollection<string> _savedEmails = new();
        private string _emailFilter = string.Empty;
        
        public ObservableCollection<string> FilteredEmails { get; } = new();

        public string Email
        {
            get { return _email; }
            set
            {
                if (_email != value)
                {
                    _email = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange(nameof(CanLogin));
                    
                    // Actualizar filtro para autocompletar
                    _emailFilter = value;
                    UpdateFilteredEmails();
                }
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange(nameof(CanLogin));
                }
            }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange(nameof(CanLogin));
                    LoginButtonText = value ? "AUTENTICANDO..." : "INICIAR SESIÓN";
                }
            }
        }

        public string LoginButtonText
        {
            get { return _loginButtonText; }
            set
            {
                if (_loginButtonText != value)
                {
                    _loginButtonText = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool CanLogin => !IsLoading && 
                               !string.IsNullOrWhiteSpace(Email) && 
                               !string.IsNullOrWhiteSpace(Password);

        public LoginViewModel(
            ILoginService loginService,
            INotificationService notificationService,
            IEventAggregator eventAggregator,
            ISQLiteEmailStorageService emailStorageService)
        {
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _emailStorageService = emailStorageService ?? throw new ArgumentNullException(nameof(emailStorageService));
            
            DisplayName = "Iniciar Sesión";
        }

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            await LoadSavedEmailsAsync();
        }

        private async Task LoadSavedEmailsAsync()
        {
            try
            {
                var emails = await _emailStorageService.GetSavedEmailsAsync();
                _savedEmails.Clear();
                foreach (var email in emails)
                {
                    _savedEmails.Add(email);
                }
                UpdateFilteredEmails();
            }
            catch (Exception ex)
            {
                // Log error but don't show to user - not critical functionality
                System.Diagnostics.Debug.WriteLine($"Error loading saved emails: {ex.Message}");
            }
        }

        private void UpdateFilteredEmails()
        {
            FilteredEmails.Clear();
            
            if (string.IsNullOrWhiteSpace(_emailFilter))
            {
                // Show top 5 most used emails when no filter
                foreach (var email in _savedEmails.Take(5))
                {
                    FilteredEmails.Add(email);
                }
            }
            else
            {
                // Show filtered emails that contain the typed text
                foreach (var email in _savedEmails.Where(e => 
                    e.Contains(_emailFilter, StringComparison.OrdinalIgnoreCase)).Take(5))
                {
                    FilteredEmails.Add(email);
                }
            }
        }

        public async Task LoginAsync()
        {
            if (!CanLogin) return;

            try
            {
                IsLoading = true;

                var loginResult = await _loginService.AuthenticateAsync(Email, Password);

                if (loginResult.Success && loginResult.Account != null)
                {
                    // Login exitoso - manejar guardado de email
                    await HandleLoginSuccessAsync(Email);
                    
                    // Publicar mensaje para que Shell navegue a CompanySelection
                    await _eventAggregator.PublishOnUIThreadAsync(new LoginSuccessMessage
                    {
                        Account = loginResult.Account,
                        Companies = loginResult.Companies
                    });
                    // Removido: _notificationService.ShowSuccess - ya no es necesario
                }
                else
                {
                    // Mostrar errores de la API
                    if (loginResult.Errors?.Count > 0)
                    {
                        foreach (var error in loginResult.Errors)
                        {
                            _notificationService.ShowError(error.Message, "Error de autenticación");
                        }
                    }
                    else
                    {
                        _notificationService.ShowError(loginResult.Message ?? "Credenciales incorrectas", "Error de autenticación");
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error de conexión: {ex.Message}", "Error de red");
            }
            finally
            {
                IsLoading = false;
                Password = string.Empty; // Limpiar contraseña por seguridad
            }
        }

        private async Task HandleLoginSuccessAsync(string email)
        {
            try
            {
                if (await _emailStorageService.ShouldPromptToSaveEmailAsync(email))
                {
                    // Email nuevo - preguntar si desea guardarlo
                    await PromptToSaveEmailAsync(email);
                }
                else
                {
                    // Email existente - actualizar contador de uso
                    await _emailStorageService.SaveEmailAsync(email);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't interrupt login flow
                System.Diagnostics.Debug.WriteLine($"Error handling email save: {ex.Message}");
            }
        }

        private async Task PromptToSaveEmailAsync(string email)
        {
            try
            {
                var actions = new List<NotificationAction>
                {
                    new() {
                        Text = "Sí, guardar",
                        Style = "Success",
                        AsyncAction = async () =>
                        {
                            try
                            {
                                await _emailStorageService.SaveEmailAsync(email);
                                await LoadSavedEmailsAsync(); // Refrescar lista
                                _notificationService.ShowSuccess("Email guardado correctamente");
                            }
                            catch (Exception ex)
                            {
                                _notificationService.ShowError("No se pudo guardar el email");
                                System.Diagnostics.Debug.WriteLine($"Error saving email: {ex.Message}");
                            }
                        }
                    },
                    new() {
                        Text = "No guardar",
                        Style = "Secondary",
                        Action = () => { } // No hacer nada, solo cerrar
                    }
                };

                _notificationService.ShowQuestion(
                    $"¿Deseas guardar el email '{email}' para futuros inicios de sesión?",
                    actions,
                    "Guardar Email");
            }
            catch (Exception ex)
            {
                _notificationService.ShowWarning("Error al procesar la solicitud");
                System.Diagnostics.Debug.WriteLine($"Error in prompt: {ex.Message}");
            }
        }

        public void OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.PasswordBox passwordBox)
            {
                Password = passwordBox.Password;
            }
        }

        public void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter && CanLogin)
            {
                _ = LoginAsync();
            }
        }

        public async Task RemoveEmailAsync(string email)
        {
            try
            {
                var actions = new List<NotificationAction>
                {
                    new() {
                        Text = "Sí, eliminar",
                        Style = "Danger",
                        AsyncAction = async () =>
                        {
                            try
                            {
                                await _emailStorageService.RemoveEmailAsync(email);
                                await LoadSavedEmailsAsync(); // Refrescar lista
                                _notificationService.ShowSuccess($"Email '{email}' eliminado correctamente");
                            }
                            catch (Exception ex)
                            {
                                _notificationService.ShowError("No se pudo eliminar el email");
                                System.Diagnostics.Debug.WriteLine($"Error removing email: {ex.Message}");
                            }
                        }
                    },
                    new() {
                        Text = "Cancelar",
                        Style = "Secondary",
                        Action = () => { } // No hacer nada, solo cerrar
                    }
                };

                _notificationService.ShowQuestion(
                    $"¿Estás seguro de que deseas eliminar el email '{email}' de la lista guardada?",
                    actions,
                    "Eliminar Email");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Error al procesar la solicitud");
                System.Diagnostics.Debug.WriteLine($"Error in remove email prompt: {ex.Message}");
            }
        }
    }

    // Mensaje para comunicar login exitoso al Shell
    public class LoginSuccessMessage
    {
        public LoginAccountGraphQLModel Account { get; set; } = new();
        public List<LoginCompanyGraphQLModel> Companies { get; set; } = [];
    }
}
