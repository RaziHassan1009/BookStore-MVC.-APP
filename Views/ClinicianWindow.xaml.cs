using GrapheneSensore.Models;
using GrapheneSensore.Services;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GrapheneSensore.Views
{
    public partial class ClinicianWindow : Window
    {
        private readonly AuthenticationService _authService;
        private readonly UserService _userService;
        private readonly PressureDataService _dataService;
        private readonly AlertService _alertService;
        private readonly CommentService _commentService;
        private readonly ReportService _reportService;

        private List<User> _allPatients = new();
        private User? _selectedPatient;

        public User? CurrentUser => _authService.CurrentUser;
        public int UnacknowledgedAlertCount { get; set; }

        public ClinicianWindow(AuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
            _userService = new UserService();
            _dataService = new PressureDataService();
            _alertService = new AlertService();
            _commentService = new CommentService();
            _reportService = new ReportService();

            DataContext = this;

            Loaded += ClinicianWindow_Loaded;
        }

        private async void ClinicianWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (CurrentUser != null)
            {
                // Load patients assigned to this clinician
                await LoadPatientsAsync();
                
                // Load unacknowledged alerts count
                using var context = new Data.SensoreDbContext();
                var patientIds = _allPatients.Select(p => p.UserId).ToList();
                UnacknowledgedAlertCount = context.Alerts
                    .Where(a => patientIds.Contains(a.UserId) && !a.IsAcknowledged)
                    .Count();
            }
        }

        private async System.Threading.Tasks.Task LoadPatientsAsync()
        {
            if (CurrentUser == null) return;

            try
            {
                _allPatients = await _userService.GetPatientsByClinicianAsync(CurrentUser.UserId);
                PatientsListBox.ItemsSource = _allPatients;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading patients: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void PatientsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientsListBox.SelectedItem is User selectedPatient)
            {
                _selectedPatient = selectedPatient;
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                PatientDetailsPanel.Visibility = Visibility.Visible;

                // Update patient info
                PatientNameText.Text = selectedPatient.FullName;
                PatientEmailText.Text = selectedPatient.Email ?? "N/A";
                PatientPhoneText.Text = selectedPatient.PhoneNumber ?? "N/A";

                // Load patient data
                await LoadPatientDataAsync(1); // Default to last 1 hour
            }
        }

        private async System.Threading.Tasks.Task LoadPatientDataAsync(int hours)
        {
            if (_selectedPatient == null) return;

            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                // Get report for the selected time period
                var report = await _reportService.GetLastHoursReportAsync(_selectedPatient.UserId, hours);

                // Update metrics
                AvgPeakPressureText.Text = report.AvgPeakPressure.ToString("F1");
                AvgContactAreaText.Text = report.AvgContactArea.ToString("F1");
                TotalAlertsText.Text = report.TotalAlerts.ToString();

                // Update charts
                UpdateCharts(report);

                // Load alerts
                var endDate = DateTime.Now;
                var startDate = endDate.AddHours(-hours);
                var alerts = await _alertService.GetAlertsByDateRangeAsync(_selectedPatient.UserId, startDate, endDate);
                PatientAlertsDataGrid.ItemsSource = alerts;

                // Load comments
                var comments = await _commentService.GetUserCommentsAsync(_selectedPatient.UserId);
                PatientCommentsListBox.ItemsSource = comments.Take(20); // Show last 20 comments
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading patient data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void UpdateCharts(ReportService.MetricsReport report)
        {
            var peakPressureValues = new ChartValues<decimal>();
            var contactAreaValues = new ChartValues<decimal>();
            var labels = new List<string>();

            foreach (var metric in report.HourlyMetrics)
            {
                peakPressureValues.Add(metric.AvgPeakPressure);
                contactAreaValues.Add(metric.AvgContactArea);
                labels.Add(metric.Hour.ToString("HH:mm"));
            }

            ClinicianPeakPressureChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Peak Pressure",
                    Values = peakPressureValues,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Stroke = System.Windows.Media.Brushes.OrangeRed,
                    StrokeThickness = 2
                }
            };

            ClinicianContactAreaChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Contact Area",
                    Values = contactAreaValues,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Stroke = System.Windows.Media.Brushes.DodgerBlue,
                    StrokeThickness = 2
                }
            };
        }

        private async void TimeRangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedPatient == null) return;

            int hours = TimeRangeComboBox.SelectedIndex switch
            {
                0 => 1,
                1 => 6,
                2 => 24,
                3 => 168, // 7 days
                _ => 1
            };

            await LoadPatientDataAsync(hours);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTerm = SearchTextBox.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                PatientsListBox.ItemsSource = _allPatients;
            }
            else
            {
                var filtered = _allPatients.Where(p =>
                    (p.FirstName?.ToLower().Contains(searchTerm) ?? false) ||
                    (p.LastName?.ToLower().Contains(searchTerm) ?? false) ||
                    (p.Email?.ToLower().Contains(searchTerm) ?? false)
                ).ToList();

                PatientsListBox.ItemsSource = filtered;
            }
        }

        private async void AcknowledgeAlert_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long alertId && CurrentUser != null)
            {
                var (success, message) = await _alertService.AcknowledgeAlertAsync(alertId, CurrentUser.UserId);

                if (success)
                {
                    // Refresh alerts list
                    int hours = TimeRangeComboBox.SelectedIndex switch
                    {
                        0 => 1,
                        1 => 6,
                        2 => 24,
                        3 => 168,
                        _ => 1
                    };
                    await LoadPatientDataAsync(hours);
                }
                else
                {
                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ReplyToComment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Comment parentComment && _selectedPatient != null && CurrentUser != null)
            {
                var dialog = new CommentReplyDialog();
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ReplyText))
                {
                    var (success, message, comment) = await _commentService.AddCommentAsync(
                        parentComment.DataId,
                        CurrentUser.UserId,
                        dialog.ReplyText,
                        isClinicianReply: true,
                        parentCommentId: parentComment.CommentId);

                    if (success)
                    {
                        // Refresh comments
                        var comments = await _commentService.GetUserCommentsAsync(_selectedPatient.UserId);
                        PatientCommentsListBox.ItemsSource = comments.Take(20);
                    }
                    else
                    {
                        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPatient == null) return;

            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                int hours = TimeRangeComboBox.SelectedIndex switch
                {
                    0 => 1,
                    1 => 6,
                    2 => 24,
                    3 => 168,
                    _ => 1
                };

                var report = await _reportService.GetLastHoursReportAsync(_selectedPatient.UserId, hours);

                var reportWindow = new ReportWindow(report, _selectedPatient);
                reportWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ViewAllAlerts_Click(object sender, RoutedEventArgs e)
        {
            // Switch to first patient and alerts tab if any patients exist
            if (_allPatients.Any())
            {
                PatientsListBox.SelectedIndex = 0;
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _authService.Logout();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}
