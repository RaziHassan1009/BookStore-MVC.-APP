using GrapheneSensore.Data;
using GrapheneSensore.Models;
using GrapheneSensore.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrapheneSensore.Services
{
    /// <summary>
    /// Service for managing feedback sessions and responses (User Story 4, 7, 8, 10, 11)
    /// </summary>
    public class FeedbackService
    {
        /// <summary>
        /// Starts a new feedback session (User Story 4)
        /// </summary>
        public async Task<(bool success, string message, FeedbackSession? session)> StartFeedbackSessionAsync(
            Guid userId, Guid applicantId, Guid templateId)
        {
            try
            {
                using var context = new SensoreDbContext();
                
                // Verify applicant exists
                var applicant = await context.Applicants.FindAsync(applicantId);
                if (applicant == null)
                {
                    return (false, "Applicant not found", null);
                }

                // Verify template exists
                var template = await context.Templates.FindAsync(templateId);
                if (template == null)
                {
                    return (false, "Template not found", null);
                }

                var session = new FeedbackSession
                {
                    UserId = userId,
                    ApplicantId = applicantId,
                    TemplateId = templateId,
                    Status = "InProgress",
                    CurrentSectionIndex = 0
                };

                context.FeedbackSessions.Add(session);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Feedback session started for applicant: {applicantId}", "FeedbackService");
                return (true, "Feedback session started successfully", session);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error starting feedback session", ex, "FeedbackService");
                return (false, "An error occurred while starting the feedback session", null);
            }
        }

        /// <summary>
        /// Gets active feedback sessions for a user
        /// </summary>
        public async Task<List<FeedbackSession>> GetActiveFeedbackSessionsAsync(Guid userId)
        {
            try
            {
                using var context = new SensoreDbContext();
                return await context.FeedbackSessions
                    .Where(fs => fs.UserId == userId && fs.Status == "InProgress")
                    .Include(fs => fs.Applicant)
                    .Include(fs => fs.Template)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error retrieving active feedback sessions for user: {userId}", ex, "FeedbackService");
                throw;
            }
        }

        /// <summary>
        /// Updates section navigation (User Story 7)
        /// </summary>
        public async Task<(bool success, string message)> UpdateSectionIndexAsync(Guid sessionId, int newIndex)
        {
            try
            {
                using var context = new SensoreDbContext();
                var session = await context.FeedbackSessions.FindAsync(sessionId);
                
                if (session == null)
                {
                    return (false, "Session not found");
                }

                session.CurrentSectionIndex = newIndex;
                await context.SaveChangesAsync();

                return (true, "Section index updated successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error updating section index: {sessionId}", ex, "FeedbackService");
                return (false, "An error occurred while updating the section index");
            }
        }

        /// <summary>
        /// Saves a feedback response (User Story 8)
        /// </summary>
        public async Task<(bool success, string message)> SaveFeedbackResponseAsync(FeedbackResponse response)
        {
            try
            {
                using var context = new SensoreDbContext();
                
                // Check if response already exists for this session/section/code combination
                var existing = await context.FeedbackResponses
                    .FirstOrDefaultAsync(fr => fr.SessionId == response.SessionId && 
                                              fr.SectionId == response.SectionId && 
                                              fr.CodeId == response.CodeId);
                
                if (existing != null)
                {
                    existing.IsChecked = response.IsChecked;
                    existing.ResponseText = response.ResponseText;
                    existing.ResponseDate = DateTime.Now;
                }
                else
                {
                    context.FeedbackResponses.Add(response);
                }

                await context.SaveChangesAsync();
                return (true, "Response saved successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error saving feedback response", ex, "FeedbackService");
                return (false, "An error occurred while saving the response");
            }
        }

        /// <summary>
        /// Gets all responses for a feedback session
        /// </summary>
        public async Task<List<FeedbackResponse>> GetSessionResponsesAsync(Guid sessionId)
        {
            try
            {
                using var context = new SensoreDbContext();
                return await context.FeedbackResponses
                    .Where(fr => fr.SessionId == sessionId)
                    .Include(fr => fr.Code)
                    .Include(fr => fr.Section)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error retrieving session responses: {sessionId}", ex, "FeedbackService");
                throw;
            }
        }

        /// <summary>
        /// Aborts a feedback session (User Story 10)
        /// </summary>
        public async Task<(bool success, string message)> AbortFeedbackSessionAsync(Guid sessionId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var session = await context.FeedbackSessions.FindAsync(sessionId);
                
                if (session == null)
                {
                    return (false, "Session not found");
                }

                session.Status = "Aborted";
                session.CompletedDate = DateTime.Now;
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Feedback session aborted: {sessionId}", "FeedbackService");
                return (true, "Feedback session aborted successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error aborting feedback session: {sessionId}", ex, "FeedbackService");
                return (false, "An error occurred while aborting the session");
            }
        }

        /// <summary>
        /// Completes a feedback session (User Story 11)
        /// </summary>
        public async Task<(bool success, string message, CompletedFeedback? completedFeedback)> CompleteFeedbackSessionAsync(Guid sessionId, Guid userId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var session = await context.FeedbackSessions
                    .Include(fs => fs.Applicant)
                    .Include(fs => fs.Template)
                    .FirstOrDefaultAsync(fs => fs.SessionId == sessionId);
                
                if (session == null)
                {
                    return (false, "Session not found", null);
                }

                // Get all responses
                var responses = await context.FeedbackResponses
                    .Where(fr => fr.SessionId == sessionId)
                    .Include(fr => fr.Code)
                    .Include(fr => fr.Section)
                    .ToListAsync();

                // Create JSON data
                var feedbackData = new
                {
                    Applicant = new 
                    {
                        session.Applicant!.FirstName,
                        session.Applicant.LastName,
                        session.Applicant.Email,
                        session.Applicant.ReferenceNumber
                    },
                    Template = session.Template!.TemplateName,
                    SessionDate = session.StartedDate,
                    CompletedDate = DateTime.Now,
                    Responses = responses.Select(r => new
                    {
                        Section = r.Section!.SectionName,
                        Code = r.Code?.CodeText,
                        IsChecked = r.IsChecked,
                        ResponseText = r.ResponseText
                    }).ToList()
                };

                var completedFeedback = new CompletedFeedback
                {
                    SessionId = sessionId,
                    ApplicantName = session.Applicant.FullName,
                    TemplateName = session.Template.TemplateName,
                    FeedbackData = JsonConvert.SerializeObject(feedbackData, Formatting.Indented),
                    CreatedBy = userId
                };

                // Update session status
                session.Status = "Completed";
                session.CompletedDate = DateTime.Now;
                session.IsSaved = true;

                context.CompletedFeedbacks.Add(completedFeedback);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Feedback session completed: {sessionId}", "FeedbackService");
                return (true, "Feedback completed successfully", completedFeedback);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error completing feedback session: {sessionId}", ex, "FeedbackService");
                return (false, "An error occurred while completing the feedback", null);
            }
        }

        /// <summary>
        /// Gets completed feedbacks for a user
        /// </summary>
        public async Task<List<CompletedFeedback>> GetCompletedFeedbacksAsync(Guid userId)
        {
            try
            {
                using var context = new SensoreDbContext();
                return await context.CompletedFeedbacks
                    .Where(cf => cf.CreatedBy == userId)
                    .OrderByDescending(cf => cf.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error retrieving completed feedbacks for user: {userId}", ex, "FeedbackService");
                throw;
            }
        }

        /// <summary>
        /// Gets a specific completed feedback
        /// </summary>
        public async Task<CompletedFeedback?> GetCompletedFeedbackByIdAsync(Guid feedbackId)
        {
            try
            {
                using var context = new SensoreDbContext();
                return await context.CompletedFeedbacks.FindAsync(feedbackId);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error retrieving completed feedback: {feedbackId}", ex, "FeedbackService");
                return null;
            }
        }
    }
}
