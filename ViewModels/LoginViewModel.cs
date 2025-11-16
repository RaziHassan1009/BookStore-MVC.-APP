using GrapheneSensore.Services;
using GrapheneSensore.Views;
using System;
using System.Windows;
using System.Windows.Input;

namespace GrapheneSensore.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AuthenticationService _authService;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoading;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            _authService = new AuthenticationService();
            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => !IsLoading);
        }

        private async System.Threading.Tasks.Task LoginAsync()
        {
            ErrorMessage = string.Empty;
            
            // Trim whitespace
            var username = Username?.Trim() ?? string.Empty;
            var password = Password ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(username))
            {
                ErrorMessage = "Please enter username";
                return;
            }
            
            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Please enter password";
                return;
            }

            IsLoading = true;

            try
            {
                var (success, message, user) = await _authService.LoginAsync(username, password);

                if (success && user != null)
                {
                    // Open appropriate window based on user type
                    Window mainWindow = user.UserType switch
                    {
                        "Admin" => new AdminWindow(_authService),
                        "Clinician" => new ClinicianWindow(_authService),
                        "Patient" => new PatientWindow(_authService),
                        _ => throw new InvalidOperationException("Unknown user type")
                    };

                    mainWindow.Show();
                    
                    // Close login window
                    Application.Current.MainWindow?.Close();
                }
                else
                {
                    ErrorMessage = message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login failed: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
