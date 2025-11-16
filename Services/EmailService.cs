using GrapheneSensore.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GrapheneSensore.Services
{
    /// <summary>
    /// Service for sending bulk emails (User Story 11)
    /// </summary>
    public class EmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly bool _enableSsl;

        public EmailService(string smtpHost = "smtp.gmail.com", int smtpPort = 587, 
            string smtpUsername = "", string smtpPassword = "", bool enableSsl = true)
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _smtpUsername = smtpUsername;
            _smtpPassword = smtpPassword;
            _enableSsl = enableSsl;
        }

        /// <summary>
        /// Sends email with PDF attachment (User Story 11)
        /// </summary>
        public async Task<(bool success, string message)> SendFeedbackEmailAsync(
            string recipientEmail, 
            string applicantName,
            string pdfFilePath,
            string? additionalMessage = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_smtpUsername) || string.IsNullOrWhiteSpace(_smtpPassword))
                {
                    return (false, "SMTP credentials not configured. Please configure email settings.");
                }

                using var smtpClient = new SmtpClient(_smtpHost)
                {
                    Port = _smtpPort,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    EnableSsl = _enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUsername),
                    Subject = $"Feedback Report - {applicantName}",
                    Body = $"Dear {applicantName},\n\n" +
                           $"Please find attached your feedback report.\n\n" +
                           (string.IsNullOrWhiteSpace(additionalMessage) ? "" : $"{additionalMessage}\n\n") +
                           $"Best regards,\n" +
                           $"Graphene Feedback System",
                    IsBodyHtml = false
                };

                mailMessage.To.Add(recipientEmail);

                if (!string.IsNullOrWhiteSpace(pdfFilePath) && System.IO.File.Exists(pdfFilePath))
                {
                    mailMessage.Attachments.Add(new Attachment(pdfFilePath));
                }

                await smtpClient.SendMailAsync(mailMessage);

                Logger.Instance.LogInfo($"Email sent successfully to {recipientEmail}", "EmailService");
                return (true, "Email sent successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error sending email to {recipientEmail}", ex, "EmailService");
                return (false, $"An error occurred while sending email: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends bulk emails (User Story 11)
        /// </summary>
        public async Task<(int successCount, int failCount, List<string> errors)> SendBulkFeedbackEmailsAsync(
            List<(string email, string applicantName, string pdfPath)> recipients,
            string? additionalMessage = null)
        {
            var successCount = 0;
            var failCount = 0;
            var errors = new List<string>();

            foreach (var recipient in recipients)
            {
                var result = await SendFeedbackEmailAsync(
                    recipient.email, 
                    recipient.applicantName, 
                    recipient.pdfPath, 
                    additionalMessage);

                if (result.success)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    errors.Add($"{recipient.applicantName}: {result.message}");
                }

                // Add small delay to avoid overwhelming SMTP server
                await Task.Delay(1000);
            }

            Logger.Instance.LogInfo($"Bulk email completed. Success: {successCount}, Failed: {failCount}", "EmailService");
            return (successCount, failCount, errors);
        }

        /// <summary>
        /// Validates email configuration
        /// </summary>
        public bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(_smtpUsername) && 
                   !string.IsNullOrWhiteSpace(_smtpPassword);
        }
    }
}
