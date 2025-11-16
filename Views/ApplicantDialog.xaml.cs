using GrapheneSensore.Models;
using System;
using System.Windows;

namespace GrapheneSensore.Views
{
    /// <summary>
    /// Interaction logic for ApplicantDialog.xaml
    /// </summary>
    public partial class ApplicantDialog : Window
    {
        public Applicant? Applicant { get; private set; }
        private readonly Guid _sessionUserId;
        private readonly bool _isEditMode;

        public ApplicantDialog(Guid sessionUserId, Applicant? existingApplicant = null)
        {
            InitializeComponent();
            _sessionUserId = sessionUserId;
            _isEditMode = existingApplicant != null;

            if (_isEditMode && existingApplicant != null)
            {
                SubtitleText.Text = "Edit applicant details";
                SaveButton.Content = "✓ Update Applicant";
                
                FirstNameTextBox.Text = existingApplicant.FirstName;
                LastNameTextBox.Text = existingApplicant.LastName;
                EmailTextBox.Text = existingApplicant.Email;
                ReferenceNumberTextBox.Text = existingApplicant.ReferenceNumber;
                NotesTextBox.Text = existingApplicant.Notes;
                
                Applicant = existingApplicant;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                MessageBox.Show("Please enter the first name.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Please enter the last name.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameTextBox.Focus();
                return;
            }

            // Create or update applicant
            if (Applicant == null)
            {
                Applicant = new Applicant
                {
                    SessionUserId = _sessionUserId
                };
            }

            Applicant.FirstName = FirstNameTextBox.Text.Trim();
            Applicant.LastName = LastNameTextBox.Text.Trim();
            Applicant.Email = EmailTextBox.Text.Trim();
            Applicant.ReferenceNumber = ReferenceNumberTextBox.Text.Trim();
            Applicant.Notes = NotesTextBox.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
