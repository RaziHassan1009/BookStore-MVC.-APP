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
    /// ViewModel for PDF Export and Email (User Story 11)
    /// </summary>
    public class PdfExportViewModel : ViewModelBase
    {
        private readonly PdfExportService _pdfService;
        private EmailService _emailService;
        private readonly FeedbackService _feedbackService;
        private readonly Guid _userId;

        private ObservableCollection<CompletedFeedback> _completedFeedbacks;
        private CompletedFeedback? _selectedFeedback;
        private bool _isLoading;
        private string _smtpHost = "smtp.gmail.com";
        private int _smtpPort = 587;
        private string _smtpUsername = string.Empty;
        private string _smtpPassword = string.Empty;
        private string _emailSubject = "Your Feedback Report";
        private string _emailBody = "Please find attached your feedback report.";

        public ObservableCollection<CompletedFeedback> CompletedFeedbacks
        {
            get => _completedFeedbacks;
            set { _completedFeedbacks = value; OnPropertyChanged(); }
        }

        public CompletedFeedback? SelectedFeedback
        {
            get => _selectedFeedback;
            set { _selectedFeedback = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string SmtpHost
        {
            get => _smtpHost;
            set { _smtpHost = value; OnPropertyChanged(); }
        }

        public int SmtpPort
        {
            get => _smtpPort;
            set { _smtpPort = value; OnPropertyChanged(); }
        }

        public string SmtpUsername
        {
            get => _smtpUsername;
            set { _smtpUsername = value; OnPropertyChanged(); }
        }

        public string SmtpPassword
        {
            get => _smtpPassword;
            set { _smtpPassword = value; OnPropertyChanged(); }
        }

        public string EmailSubject
        {
            get => _emailSubject;
            set { _emailSubject = value; OnPropertyChanged(); }
        }

        public string EmailBody
        {
            get => _emailBody;
            set { _emailBody = value; OnPropertyChanged(); }
        }

        public ICommand GeneratePdfCommand { get; }
        public ICommand SendEmailCommand { get; }
        public ICommand SendBulkEmailCommand { get; }
        public ICommand RefreshCommand { get; }

        public PdfExportViewModel(Guid userId)
        {
            _pdfService = new PdfExportService();
            _feedbackService = new FeedbackService();
            _userId = userId;
            _completedFeedbacks = new ObservableCollection<CompletedFeedback>();

            GeneratePdfCommand = new RelayCommand(async _ => await GeneratePdfAsync(), _ => SelectedFeedback != null);
            SendEmailCommand = new RelayCommand(async _ => await SendEmailAsync(), _ => SelectedFeedback != null && !string.IsNullOrWhiteSpace(SmtpUsername));
            SendBulkEmailCommand = new RelayCommand(async _ => await SendBulkEmailAsync(), _ => CompletedFeedbacks.Count > 0 && !string.IsNullOrWhiteSpace(SmtpUsername));
            RefreshCommand = new RelayCommand(async _ => await LoadCompletedFeedbacksAsync());

            _emailService = new EmailService(SmtpHost, SmtpPort, SmtpUsername, SmtpPassword);

            _ = LoadCompletedFeedbacksAsync();
        }

        private async Task LoadCompletedFeedbacksAsync()
        {
            try
            {
                IsLoading = true;
                var feedbacks = await _feedbackService.GetCompletedFeedbacksAsync(_userId);

                CompletedFeedbacks.Clear();
                foreach (var feedback in feedbacks.OrderByDescending(f => f.CreatedDate))
                {
                    CompletedFeedbacks.Add(feedback);
                }

                Logger.Instance.LogInfo($"Loaded {feedbacks.Count} completed feedbacks", "PdfExportVM");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error loading completed feedbacks", ex, "PdfExportVM");
                MessageBox.Show("Failed to load completed feedbacks.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GeneratePdfAsync()
        {
            if (SelectedFeedback == null) return;

            try
            {
                IsLoading = true;

                var pdfResult = await _pdfService.ExportFeedbackToPdfAsync(
                    SelectedFeedback.ApplicantName,
                    SelectedFeedback.TemplateName,
                    SelectedFeedback.FeedbackData);

                if (pdfResult.success && !string.IsNullOrWhiteSpace(pdfResult.filePath))
                {
                    var result = MessageBox.Show(
                        $"PDF generated successfully!\n\nPath: {pdfResult.filePath}\n\nWould you like to open it?",
                        "Success",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        _pdfService.OpenPdf(pdfResult.filePath!);
                    }

                    Logger.Instance.LogInfo($"PDF generated: {pdfResult.filePath}", "PdfExportVM");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error generating PDF", ex, "PdfExportVM");
                MessageBox.Show("Failed to generate PDF.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SendEmailAsync()
        {
            if (SelectedFeedback == null || string.IsNullOrWhiteSpace(SmtpUsername)) return;

            try
            {
                IsLoading = true;

                // Update email service configuration
                _emailService = new EmailService(SmtpHost, SmtpPort, SmtpUsername, SmtpPassword);

                // Generate PDF first
                var pdfResult = await _pdfService.ExportFeedbackToPdfAsync(
                    SelectedFeedback.ApplicantName,
                    SelectedFeedback.TemplateName,
                    SelectedFeedback.FeedbackData);

                if (!pdfResult.success || string.IsNullOrWhiteSpace(pdfResult.filePath))
                {
                    MessageBox.Show("Failed to generate PDF for email.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Send email (need recipient email - using a placeholder)
                var recipient = "recipient@example.com"; // TODO: Get from applicant data
                var emailResult = await _emailService.SendFeedbackEmailAsync(
                    recipient,
                    EmailSubject,
                    EmailBody,
                    pdfResult.filePath!);

                if (emailResult.success)
                {
                    MessageBox.Show("Email sent successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    Logger.Instance.LogInfo($"Email sent to {recipient}", "PdfExportVM");
                }
                else
                {
                    MessageBox.Show("Failed to send email. Check SMTP settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error sending email", ex, "PdfExportVM");
                MessageBox.Show($"Failed to send email: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SendBulkEmailAsync()
        {
            if (CompletedFeedbacks.Count == 0 || string.IsNullOrWhiteSpace(SmtpUsername)) return;

            var result = MessageBox.Show(
                $"Are you sure you want to send emails to all {CompletedFeedbacks.Count} recipients?",
                "Confirm Bulk Email",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;

                    // Update email service
                    _emailService = new EmailService(SmtpHost, SmtpPort, SmtpUsername, SmtpPassword);

                    // Prepare recipient list (placeholder)
                    var recipients = CompletedFeedbacks.Select(f => ("recipient@example.com", f.ApplicantName, "")).ToList();

                    var bulkResult = await _emailService.SendBulkFeedbackEmailsAsync(recipients);

                    MessageBox.Show($"Bulk email process completed!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    Logger.Instance.LogInfo($"Bulk email sent to {recipients.Count} recipients", "PdfExportVM");
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogError("Error sending bulk email", ex, "PdfExportVM");
                    MessageBox.Show($"Failed to send bulk emails: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
    }
}
