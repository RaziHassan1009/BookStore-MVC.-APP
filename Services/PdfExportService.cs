using GrapheneSensore.Logging;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GrapheneSensore.Services
{
    /// <summary>
    /// Service for exporting feedback to PDF (User Story 11)
    /// </summary>
    public class PdfExportService
    {
        private readonly string _outputDirectory;

        public PdfExportService(string? outputDirectory = null)
        {
            _outputDirectory = outputDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                "GrapheneFeedbacks");

            // Create directory if it doesn't exist
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        /// <summary>
        /// Exports feedback to PDF (User Story 11)
        /// </summary>
        public async Task<(bool success, string message, string? filePath)> ExportFeedbackToPdfAsync(
            string applicantName, 
            string templateName, 
            string feedbackDataJson)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var fileName = $"Feedback_{applicantName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    var filePath = Path.Combine(_outputDirectory, fileName);

                    using (var writer = new PdfWriter(filePath))
                    using (var pdf = new PdfDocument(writer))
                    using (var document = new Document(pdf))
                    {
                        // Add title
                        var title = new Paragraph("Applicant Feedback Report")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(20)
                            .SetBold()
                            .SetMarginBottom(20);
                        document.Add(title);

                        // Parse JSON data
                        var data = JObject.Parse(feedbackDataJson);

                        // Add applicant information
                        AddSectionHeader(document, "Applicant Information");
                        AddInfoRow(document, "Name", $"{data["Applicant"]?["FirstName"]} {data["Applicant"]?["LastName"]}");
                        AddInfoRow(document, "Email", data["Applicant"]?["Email"]?.ToString() ?? "N/A");
                        AddInfoRow(document, "Reference Number", data["Applicant"]?["ReferenceNumber"]?.ToString() ?? "N/A");
                        
                        // Add session information
                        AddSectionHeader(document, "Session Information");
                        AddInfoRow(document, "Template", data["Template"]?.ToString() ?? "N/A");
                        AddInfoRow(document, "Session Date", data["SessionDate"]?.ToString() ?? "N/A");
                        AddInfoRow(document, "Completed Date", data["CompletedDate"]?.ToString() ?? "N/A");

                        // Add feedback responses
                        AddSectionHeader(document, "Feedback Details");
                        var responses = data["Responses"] as JArray;
                        if (responses != null)
                        {
                            var currentSection = "";
                            foreach (var response in responses)
                            {
                                var section = response["Section"]?.ToString();
                                var code = response["Code"]?.ToString();
                                var isChecked = response["IsChecked"]?.ToObject<bool>() ?? false;
                                var responseText = response["ResponseText"]?.ToString();

                                if (section != currentSection)
                                {
                                    currentSection = section ?? "";
                                    var sectionPara = new Paragraph(currentSection)
                                        .SetFontSize(14)
                                        .SetBold()
                                        .SetMarginTop(15)
                                        .SetMarginBottom(5);
                                    document.Add(sectionPara);
                                }

                                if (!string.IsNullOrEmpty(code))
                                {
                                    var checkMark = isChecked ? "☑" : "☐";
                                    var codePara = new Paragraph($"  {checkMark} {code}")
                                        .SetFontSize(11)
                                        .SetMarginLeft(20);
                                    document.Add(codePara);
                                }

                                if (!string.IsNullOrEmpty(responseText))
                                {
                                    var textPara = new Paragraph(responseText)
                                        .SetFontSize(10)
                                        .SetMarginLeft(40)
                                        .SetMarginBottom(5)
                                        .SetItalic();
                                    document.Add(textPara);
                                }
                            }
                        }

                        // Add footer
                        var footer = new Paragraph($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(8)
                            .SetMarginTop(30)
                            .SetFontColor(ColorConstants.GRAY);
                        document.Add(footer);
                    }

                    Logger.Instance.LogInfo($"PDF generated successfully: {filePath}", "PdfExportService");
                    return (true, "PDF generated successfully", filePath);
                });
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error generating PDF for {applicantName}", ex, "PdfExportService");
                return (false, $"An error occurred while generating the PDF: {ex.Message}", null);
            }
        }

        private void AddSectionHeader(Document document, string text)
        {
            var header = new Paragraph(text)
                .SetFontSize(16)
                .SetBold()
                .SetMarginTop(20)
                .SetMarginBottom(10)
                .SetBackgroundColor(new DeviceRgb(240, 240, 240))
                .SetPadding(5);
            document.Add(header);
        }

        private void AddInfoRow(Document document, string label, string value)
        {
            var para = new Paragraph($"{label}: {value}")
                .SetFontSize(11)
                .SetMarginLeft(10)
                .SetMarginBottom(5);
            document.Add(para);
        }

        /// <summary>
        /// Opens PDF in default viewer
        /// </summary>
        public void OpenPdf(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error opening PDF: {filePath}", ex, "PdfExportService");
            }
        }
    }
}
