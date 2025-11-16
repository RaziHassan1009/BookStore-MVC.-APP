using GrapheneSensore.Models;
using GrapheneSensore.Services;
using GrapheneSensore.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GrapheneSensore.ViewModels
{
    /// <summary>
    /// ViewModel for Applicant Management (User Stories 3, 6)
    /// </summary>
    public class ApplicantManagementViewModel : ViewModelBase
    {
        private readonly ApplicantService _applicantService;
        private readonly Guid _currentUserId;

        private ObservableCollection<Applicant> _applicants;
        private Applicant? _selectedApplicant;
        private string _searchText = string.Empty;
        private bool _isLoading;

        public ObservableCollection<Applicant> Applicants
        {
            get => _applicants;
            set { _applicants = value; OnPropertyChanged(); }
        }

        public Applicant? SelectedApplicant
        {
            get => _selectedApplicant;
            set { _selectedApplicant = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterApplicants(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ICommand AddApplicantCommand { get; }
        public ICommand EditApplicantCommand { get; }
        public ICommand DeleteApplicantCommand { get; }
        public ICommand DeleteAllCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand StartFeedbackCommand { get; }

        public ApplicantManagementViewModel(Guid userId)
        {
            _applicantService = new ApplicantService();
            _currentUserId = userId;
            _applicants = new ObservableCollection<Applicant>();

            AddApplicantCommand = new RelayCommand(_ => AddApplicant());
            EditApplicantCommand = new RelayCommand(_ => EditApplicant(), _ => SelectedApplicant != null);
            DeleteApplicantCommand = new RelayCommand(_ => DeleteApplicant(), _ => SelectedApplicant != null);
            DeleteAllCommand = new RelayCommand(_ => DeleteAllApplicants());
            RefreshCommand = new RelayCommand(async _ => await LoadApplicantsAsync());
            StartFeedbackCommand = new RelayCommand(_ => StartFeedback(), _ => SelectedApplicant != null);

            _ = LoadApplicantsAsync();
        }

        private async Task LoadApplicantsAsync()
        {
            try
            {
                IsLoading = true;
                var applicants = await _applicantService.GetApplicantsBySessionUserAsync(_currentUserId);
                
                Applicants.Clear();
                foreach (var applicant in applicants)
                {
                    Applicants.Add(applicant);
                }

                Logger.Instance.LogInfo($"Loaded {applicants.Count} applicants", "ApplicantManagementVM");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error loading applicants", ex, "ApplicantManagementVM");
                MessageBox.Show("Failed to load applicants. Please try again.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddApplicant()
        {
            try
            {
                var dialog = new Views.ApplicantDialog(_currentUserId);
                if (dialog.ShowDialog() == true && dialog.Applicant != null)
                {
                    _ = SaveApplicantAsync(dialog.Applicant);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error opening add applicant dialog", ex, "ApplicantManagementVM");
                MessageBox.Show($"Failed to open dialog: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditApplicant()
        {
            if (SelectedApplicant == null) return;

            var dialog = new Views.ApplicantDialog(_currentUserId, SelectedApplicant);
            if (dialog.ShowDialog() == true && dialog.Applicant != null)
            {
                _ = UpdateApplicantAsync(dialog.Applicant);
            }
        }

        private async Task SaveApplicantAsync(Applicant applicant)
        {
            try
            {
                IsLoading = true;
                var result = await _applicantService.AddApplicantAsync(applicant);
                
                if (result.success)
                {
                    await LoadApplicantsAsync();
                    MessageBox.Show("Applicant added successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(result.message, "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error saving applicant", ex, "ApplicantManagementVM");
                MessageBox.Show("Failed to save applicant.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UpdateApplicantAsync(Applicant applicant)
        {
            try
            {
                IsLoading = true;
                var result = await _applicantService.UpdateApplicantAsync(applicant);
                
                if (result.success)
                {
                    await LoadApplicantsAsync();
                    MessageBox.Show("Applicant updated successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(result.message, "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void DeleteApplicant()
        {
            if (SelectedApplicant == null)
            {
                MessageBox.Show("Please select an applicant first.", "No Selection", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete {SelectedApplicant.FullName}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    var applicantName = SelectedApplicant.FullName;
                    var deleteResult = await _applicantService.DeleteApplicantAsync(SelectedApplicant.ApplicantId);
                    
                    if (deleteResult.success)
                    {
                        SelectedApplicant = null; // Clear selection
                        await LoadApplicantsAsync();
                        MessageBox.Show($"'{applicantName}' deleted successfully!", "Success", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        Logger.Instance.LogInfo($"Deleted applicant: {applicantName}", "ApplicantManagementVM");
                    }
                    else
                    {
                        MessageBox.Show(deleteResult.message, "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        Logger.Instance.LogError($"Failed to delete: {deleteResult.message}", null, "ApplicantManagementVM");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogError("Exception during delete", ex, "ApplicantManagementVM");
                    MessageBox.Show($"Error deleting applicant:\n\n{ex.Message}\n\nCheck logs for details.", 
                        "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async void DeleteAllApplicants()
        {
            if (Applicants.Count == 0)
            {
                MessageBox.Show("No applicants to delete.", "Information", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete ALL {Applicants.Count} applicant(s)? This cannot be undone!",
                "Confirm Delete All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    var deleteResult = await _applicantService.DeleteAllApplicantsForSessionAsync(_currentUserId);
                    
                    if (deleteResult.success)
                    {
                        await LoadApplicantsAsync();
                        MessageBox.Show(deleteResult.message, "Success", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(deleteResult.message, "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void StartFeedback()
        {
            if (SelectedApplicant == null) return;

            try
            {
                var templateWindow = new Views.TemplateSelectionWindow(_currentUserId, SelectedApplicant.ApplicantId);
                templateWindow.Show();
                Logger.Instance.LogInfo($"Started feedback for applicant: {SelectedApplicant.FullName}", "ApplicantManagementVM");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error starting feedback", ex, "ApplicantManagementVM");
                MessageBox.Show($"Failed to start feedback: {ex.Message}\n\nPlease check the logs for details.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void FilterApplicants()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadApplicantsAsync();
                    return;
                }

                // Filter in memory for quick response
                IsLoading = true;
                var allApplicants = await _applicantService.GetApplicantsBySessionUserAsync(_currentUserId);
                
                var searchLower = SearchText.ToLower();
                var filtered = allApplicants.Where(a =>
                    (a.FirstName?.ToLower().Contains(searchLower) ?? false) ||
                    (a.LastName?.ToLower().Contains(searchLower) ?? false) ||
                    (a.Email?.ToLower().Contains(searchLower) ?? false) ||
                    (a.ReferenceNumber?.ToLower().Contains(searchLower) ?? false)
                ).ToList();

                Applicants.Clear();
                foreach (var applicant in filtered)
                {
                    Applicants.Add(applicant);
                }

                Logger.Instance.LogInfo($"Filtered to {filtered.Count} applicants using search: '{SearchText}'", "ApplicantManagementVM");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error filtering applicants", ex, "ApplicantManagementVM");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
