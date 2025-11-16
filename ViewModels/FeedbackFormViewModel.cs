using GrapheneSensore.Models;
using GrapheneSensore.Services;
using GrapheneSensore.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GrapheneSensore.ViewModels
{
    /// <summary>
    /// ViewModel for Feedback Form (User Stories 7, 8, 10)
    /// </summary>
    public class FeedbackFormViewModel : ViewModelBase
    {
        private readonly FeedbackService _feedbackService;
        private readonly TemplateService _templateService;
        private readonly FeedbackParagraphService _paragraphService;
        private readonly Guid _sessionId;

        private FeedbackSession? _session;
        private Applicant? _applicant;
        private Template? _template;
        private ObservableCollection<Section> _sections;
        private Section? _currentSection;
        private ObservableCollection<CodeViewModel> _currentCodes;
        private int _currentSectionIndex;
        private string _additionalFeedback = string.Empty;
        private bool _isLoading;
        private ObservableCollection<FeedbackParagraph> _paragraphs;

        public FeedbackSession? Session
        {
            get => _session;
            set { _session = value; OnPropertyChanged(); }
        }

        public Applicant? Applicant
        {
            get => _applicant;
            set { _applicant = value; OnPropertyChanged(); }
        }

        public Template? Template
        {
            get => _template;
            set { _template = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Section> Sections
        {
            get => _sections;
            set { _sections = value; OnPropertyChanged(); }
        }

        public Section? CurrentSection
        {
            get => _currentSection;
            set { _currentSection = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CodeViewModel> CurrentCodes
        {
            get => _currentCodes;
            set { _currentCodes = value; OnPropertyChanged(); }
        }

        public int CurrentSectionIndex
        {
            get => _currentSectionIndex;
            set 
            { 
                _currentSectionIndex = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
            }
        }

        public string AdditionalFeedback
        {
            get => _additionalFeedback;
            set { _additionalFeedback = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FeedbackParagraph> Paragraphs
        {
            get => _paragraphs;
            set { _paragraphs = value; OnPropertyChanged(); }
        }

        public string ProgressText => $"Section {CurrentSectionIndex + 1} of {Sections.Count}";
        public bool CanGoPrevious => CurrentSectionIndex > 0;
        public bool CanGoNext => CurrentSectionIndex < Sections.Count - 1;

        public ICommand PreviousSectionCommand { get; }
        public ICommand NextSectionCommand { get; }
        public ICommand SaveDraftCommand { get; }
        public ICommand AbortCommand { get; }
        public ICommand CompleteCommand { get; }
        public ICommand InsertParagraphCommand { get; }
        public ICommand LoadParagraphsCommand { get; }

        public event EventHandler? FeedbackCompleted;
        public event EventHandler? FeedbackAborted;

        public FeedbackFormViewModel(Guid sessionId)
        {
            _feedbackService = new FeedbackService();
            _templateService = new TemplateService();
            _paragraphService = new FeedbackParagraphService();
            _sessionId = sessionId;
            _sections = new ObservableCollection<Section>();
            _currentCodes = new ObservableCollection<CodeViewModel>();
            _paragraphs = new ObservableCollection<FeedbackParagraph>();

            PreviousSectionCommand = new RelayCommand(_ => PreviousSection(), _ => CanGoPrevious);
            NextSectionCommand = new RelayCommand(_ => NextSection(), _ => CanGoNext);
            SaveDraftCommand = new RelayCommand(async _ => await SaveDraftAsync());
            AbortCommand = new RelayCommand(async _ => await AbortFeedbackAsync());
            CompleteCommand = new RelayCommand(async _ => await CompleteFeedbackAsync());
            InsertParagraphCommand = new RelayCommand(InsertParagraph);
            LoadParagraphsCommand = new RelayCommand(async _ => await LoadParagraphsAsync());

            _ = LoadSessionDataAsync();
        }

        private async Task LoadSessionDataAsync()
        {
            try
            {
                IsLoading = true;

                using var context = new Data.SensoreDbContext();
                Session = await context.FeedbackSessions
                    .Include(s => s.Applicant)
                    .Include(s => s.Template)
                    .FirstOrDefaultAsync(s => s.SessionId == _sessionId);

                if (Session == null)
                {
                    MessageBox.Show("Feedback session not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Applicant = Session.Applicant;
                Template = Session.Template;

                // Load sections for template
                var (template, sections) = await _templateService.GetTemplateWithSectionsAsync(Session.TemplateId);
                Sections.Clear();
                foreach (var section in sections)
                {
                    Sections.Add(section);
                }

                if (Sections.Count > 0)
                {
                    CurrentSectionIndex = Session.CurrentSectionIndex;
                    await LoadCurrentSectionAsync();
                }

                Logger.Instance.LogInfo($"Loaded feedback session: {_sessionId}", "FeedbackFormVM");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error loading session data", ex, "FeedbackFormVM");
                MessageBox.Show("Failed to load feedback session.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadCurrentSectionAsync()
        {
            if (CurrentSectionIndex < 0 || CurrentSectionIndex >= Sections.Count) return;

            try
            {
                CurrentSection = Sections[CurrentSectionIndex];

                // Load codes for current section
                var codes = await _templateService.GetCodesBySectionAsync(CurrentSection.SectionId);
                
                // Load existing responses
                var responses = await _feedbackService.GetSessionResponsesAsync(_sessionId);
                var sectionResponses = responses.Where(r => r.SectionId == CurrentSection.SectionId).ToList();

                CurrentCodes.Clear();
                foreach (var code in codes)
                {
                    var existingResponse = sectionResponses.FirstOrDefault(r => r.CodeId == code.CodeId);
                    CurrentCodes.Add(new CodeViewModel
                    {
                        Code = code,
                        IsChecked = existingResponse?.IsChecked ?? false,
                        ResponseText = existingResponse?.ResponseText ?? string.Empty
                    });
                }

                // Load section-level feedback
                var sectionFeedback = sectionResponses.FirstOrDefault(r => r.CodeId == null);
                AdditionalFeedback = sectionFeedback?.ResponseText ?? string.Empty;

                Logger.Instance.LogInfo($"Loaded section {CurrentSectionIndex + 1}: {CurrentSection.SectionName}", "FeedbackFormVM");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error loading section {CurrentSectionIndex}", ex, "FeedbackFormVM");
                MessageBox.Show("Failed to load section data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void PreviousSection()
        {
            await SaveCurrentSectionAsync();
            CurrentSectionIndex--;
            await LoadCurrentSectionAsync();
        }

        private async void NextSection()
        {
            await SaveCurrentSectionAsync();
            CurrentSectionIndex++;
            await LoadCurrentSectionAsync();
        }

        private async Task SaveCurrentSectionAsync()
        {
            if (CurrentSection == null) return;

            try
            {
                // Save code responses
                foreach (var codeVM in CurrentCodes)
                {
                    if (codeVM.Code == null) continue;

                    var response = new FeedbackResponse
                    {
                        SessionId = _sessionId,
                        SectionId = CurrentSection.SectionId,
                        CodeId = codeVM.Code.CodeId,
                        IsChecked = codeVM.IsChecked,
                        ResponseText = codeVM.ResponseText
                    };

                    await _feedbackService.SaveFeedbackResponseAsync(response);
                }

                // Save section-level feedback
                if (!string.IsNullOrWhiteSpace(AdditionalFeedback))
                {
                    var sectionResponse = new FeedbackResponse
                    {
                        SessionId = _sessionId,
                        SectionId = CurrentSection.SectionId,
                        CodeId = null,
                        ResponseText = AdditionalFeedback
                    };

                    await _feedbackService.SaveFeedbackResponseAsync(sectionResponse);
                }

                // Update section index in session
                await _feedbackService.UpdateSectionIndexAsync(_sessionId, CurrentSectionIndex);

                Logger.Instance.LogInfo($"Saved section {CurrentSectionIndex + 1}", "FeedbackFormVM");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error saving section {CurrentSectionIndex}", ex, "FeedbackFormVM");
            }
        }

        private async Task SaveDraftAsync()
        {
            try
            {
                IsLoading = true;
                await SaveCurrentSectionAsync();
                MessageBox.Show("Draft saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error saving draft", ex, "FeedbackFormVM");
                MessageBox.Show("Failed to save draft.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AbortFeedbackAsync()
        {
            var result = MessageBox.Show(
                "Are you sure you want to abort this feedback? All progress will be saved but marked as aborted.",
                "Confirm Abort",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    await SaveCurrentSectionAsync();
                    var abortResult = await _feedbackService.AbortFeedbackSessionAsync(_sessionId);

                    if (abortResult.success)
                    {
                        Logger.Instance.LogInfo($"Feedback aborted: {_sessionId}", "FeedbackFormVM");
                        FeedbackAborted?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        MessageBox.Show(abortResult.message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogError("Error aborting feedback", ex, "FeedbackFormVM");
                    MessageBox.Show("Failed to abort feedback.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task CompleteFeedbackAsync()
        {
            var result = MessageBox.Show(
                "Are you sure you want to complete this feedback? You can generate PDF and send emails after completion.",
                "Confirm Complete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    await SaveCurrentSectionAsync();

                    var completeResult = await _feedbackService.CompleteFeedbackSessionAsync(_sessionId, Session!.UserId);

                    if (completeResult.success)
                    {
                        Logger.Instance.LogInfo($"Feedback completed: {_sessionId}", "FeedbackFormVM");
                        MessageBox.Show("Feedback completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        FeedbackCompleted?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        MessageBox.Show(completeResult.message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogError("Error completing feedback", ex, "FeedbackFormVM");
                    MessageBox.Show("Failed to complete feedback.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task LoadParagraphsAsync()
        {
            try
            {
                var paragraphs = await _paragraphService.GetAllParagraphsAsync();
                Paragraphs.Clear();
                foreach (var paragraph in paragraphs)
                {
                    Paragraphs.Add(paragraph);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error loading paragraphs", ex, "FeedbackFormVM");
            }
        }

        private void InsertParagraph(object? parameter)
        {
            if (parameter is FeedbackParagraph paragraph)
            {
                if (!string.IsNullOrWhiteSpace(AdditionalFeedback))
                {
                    AdditionalFeedback += "\n\n";
                }
                AdditionalFeedback += paragraph.Content;
            }
        }
    }

    /// <summary>
    /// ViewModel for individual code with checkbox state
    /// </summary>
    public class CodeViewModel : ViewModelBase
    {
        private Code? _code;
        private bool _isChecked;
        private string _responseText = string.Empty;

        public Code? Code
        {
            get => _code;
            set { _code = value; OnPropertyChanged(); }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(); }
        }

        public string ResponseText
        {
            get => _responseText;
            set { _responseText = value; OnPropertyChanged(); }
        }
    }
}
