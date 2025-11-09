using System.Windows;
using System;
using GrapheneSensore.Services;
using GrapheneSensore.Logging;
using GrapheneSensore.Views;

namespace GrapheneSensore
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                // Initialize database and ensure admin user exists
                // IMPORTANT: Wait for this to complete before showing login window
                await DatabaseInitializationService.InitializeDatabaseAsync();
                
                Logger.Instance.LogInfo("Application started successfully", "App");
                
                // Now that database is initialized, show the login window
                var loginWindow = new LoginWindow();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Failed to start application", ex, "App");
                
                MessageBox.Show(
                    $"Failed to initialize application:\n\n{ex.Message}\n\nPlease check:\n" +
                    "1. SQL Server is running (MUZAMIL-WORLD\\SQLEXPRESS)\n" +
                    "2. Database 'Grephene' exists or can be created\n" +
                    "3. Connection string in appsettings.json is correct\n\n" +
                    "Check the Logs folder for more details.",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Shutdown(1);
            }
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Instance.LogInfo("Application shutting down", "App");
            base.OnExit(e);
        }
    }
}
