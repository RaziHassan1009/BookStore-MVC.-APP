using GrapheneSensore.Data;
using GrapheneSensore.Models;
using GrapheneSensore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GrapheneSensore.Views
{
    public partial class AdminWindow : Window
    {
        private readonly AuthenticationService _authService;
        private readonly UserService _userService;
        private readonly AlertService _alertService;
        private readonly PressureDataService _dataService;

        public User? CurrentUser => _authService.CurrentUser;

        public AdminWindow(AuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _userService = new UserService();
            _alertService = new AlertService();
            _dataService = new PressureDataService();

            // Verify current user exists
            if (_authService.CurrentUser == null)
            {
                MessageBox.Show("Authentication error: No user is logged in.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            DataContext = this;
            Loaded += AdminWindow_Loaded;
        }

        private async void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
            await LoadStatsAsync();
            await LoadAlertsAsync();
        }

        private async System.Threading.Tasks.Task LoadUsersAsync()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                
                if (users == null)
                {
                    MessageBox.Show("Failed to load users: No data returned from database.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                UsersDataGrid.ItemsSource = users;
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show($"Error loading users: A null reference was encountered.\n\nDetails: {ex.Message}\n\nPlease check:\n1. Database connection\n2. User data integrity\n3. Application configuration", "Database Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Try to log the full exception
                System.Diagnostics.Debug.WriteLine($"Full error: {ex}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Log the full exception
                System.Diagnostics.Debug.WriteLine($"Full error: {ex}");
            }
        }

        private async System.Threading.Tasks.Task LoadStatsAsync()
        {
            try
            {
                // Show loading state
                Dispatcher.Invoke(() =>
                {
                    TotalPatientsText.Text = "Loading...";
                    TotalCliniciansText.Text = "Loading...";
                    ActiveAlertsText.Text = "Loading...";
                    TotalDataPointsText.Text = "Loading...";
                });

                // Use separate context instances for each query to avoid threading issues
                int patientsCount = 0;
                int cliniciansCount = 0;
                int alertsCount = 0;
                int dataPointsCount = 0;

                // Execute queries sequentially with separate contexts
                using (var context = new SensoreDbContext())
                {
                    patientsCount = await context.Users
                        .Where(u => u.UserType == "Patient" && u.IsActive)
                        .CountAsync();
                }

                using (var context = new SensoreDbContext())
                {
                    cliniciansCount = await context.Users
                        .Where(u => u.UserType == "Clinician" && u.IsActive)
                        .CountAsync();
                }

                using (var context = new SensoreDbContext())
                {
                    alertsCount = await context.Alerts
                        .Where(a => !a.IsAcknowledged)
                        .CountAsync();
                }

                using (var context = new SensoreDbContext())
                {
                    dataPointsCount = await context.PressureMapData
                        .CountAsync();
                }

                // Update UI on UI thread with formatted numbers
                Dispatcher.Invoke(() =>
                {
                    TotalPatientsText.Text = patientsCount.ToString("N0");
                    TotalCliniciansText.Text = cliniciansCount.ToString("N0");
                    ActiveAlertsText.Text = alertsCount.ToString("N0");
                    TotalDataPointsText.Text = dataPointsCount.ToString("N0");
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    TotalPatientsText.Text = "Error";
                    TotalCliniciansText.Text = "Error";
                    ActiveAlertsText.Text = "Error";
                    TotalDataPointsText.Text = "Error";
                    
                    MessageBox.Show($"Error loading statistics: {ex.Message}\n\nPlease check your database connection.", "Statistics Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        private async System.Threading.Tasks.Task LoadAlertsAsync(bool recentOnly = true)
        {
            try
            {
                var alerts = await _alertService.GetAllAlertsAsync(unacknowledgedOnly: false);
                
                if (recentOnly)
                {
                    // Show recent alerts, ordered by date descending
                    var recentAlerts = alerts
                        .OrderByDescending(a => a.AlertDateTime)
                        .Take(100)
                        .ToList();
                    AlertsDataGrid.ItemsSource = recentAlerts;
                }
                else
                {
                    // Show all alerts, ordered by date descending
                    var allAlerts = alerts
                        .OrderByDescending(a => a.AlertDateTime)
                        .ToList();
                    AlertsDataGrid.ItemsSource = allAlerts;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading alerts: {ex.Message}\n\nPlease check your database connection.", "Alerts Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void AlertFilter_Changed(object sender, RoutedEventArgs e)
        {
            // Prevent execution during initialization
            if (RecentAlertsRadio == null || AllAlertsRadio == null)
                return;
                
            // Prevent execution if DataGrid is not loaded yet
            if (AlertsDataGrid == null)
                return;

            if (RecentAlertsRadio.IsChecked == true)
            {
                await LoadAlertsAsync(recentOnly: true);
            }
            else if (AllAlertsRadio.IsChecked == true)
            {
                await LoadAlertsAsync(recentOnly: false);
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserDialog(_userService);
            if (dialog.ShowDialog() == true)
            {
                _ = LoadUsersAsync();
                _ = LoadStatsAsync();
            }
        }

        private async void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid userId)
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    var dialog = new UserDialog(_userService, user);
                    if (dialog.ShowDialog() == true)
                    {
                        await LoadUsersAsync();
                    }
                }
            }
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid userId)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete this user?", 
                    "Confirm Delete", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var (success, message) = await _userService.DeleteUserAsync(userId);
                    
                    if (success)
                    {
                        await LoadUsersAsync();
                        await LoadStatsAsync();
                    }
                    else
                    {
                        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void ImportData_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid userId)
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    Title = "Select Pressure Data CSV File"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                        long frameCount = await _dataService.ImportCsvDataAsync(openFileDialog.FileName, userId);
                        
                        MessageBox.Show(
                            $"Successfully imported {frameCount} frames of data.", 
                            "Success", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);

                        await LoadStatsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error importing data: {ex.Message}", 
                            "Error", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
            }
        }

        private async void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }

        private async void RefreshStats_Click(object sender, RoutedEventArgs e)
        {
            await LoadStatsAsync();
            
            // Respect current alert filter selection
            bool recentOnly = RecentAlertsRadio?.IsChecked == true;
            await LoadAlertsAsync(recentOnly);
        }

        private async void UserTypeFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (UserTypeFilter.SelectedIndex == 0)
            {
                await LoadUsersAsync();
            }
            else
            {
                string userType = UserTypeFilter.SelectedIndex switch
                {
                    1 => "Patient",
                    2 => "Clinician",
                    3 => "Admin",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(userType))
                {
                    var users = await _userService.GetUsersByTypeAsync(userType);
                    UsersDataGrid.ItemsSource = users;
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _authService.Logout();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void OpenFeedbackSystem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser != null)
            {
                var feedbackWindow = new ApplicantManagementWindow(CurrentUser.UserId);
                feedbackWindow.Show();
            }
        }

        private async void UsersDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var user = e.Row.Item as User;
                if (user != null && e.Column.Header.ToString() == "Active")
                {
                    // Delay to allow the binding to update
                    await System.Threading.Tasks.Task.Delay(100);
                    
                    var (success, message) = await _userService.ToggleUserActiveStatusAsync(user.UserId);
                    
                    if (!success)
                    {
                        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        await LoadUsersAsync(); // Reload to revert changes
                    }
                    else
                    {
                        await LoadStatsAsync(); // Update stats
                    }
                }
            }
        }
    }
}
