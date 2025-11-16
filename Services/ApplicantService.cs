using GrapheneSensore.Data;
using GrapheneSensore.Models;
using GrapheneSensore.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrapheneSensore.Services
{
    /// <summary>
    /// Service for managing applicant operations
    /// </summary>
    public class ApplicantService
    {
        /// <summary>
        /// Gets all applicants for a specific user session (User Story 3)
        /// </summary>
        public async Task<List<Applicant>> GetApplicantsBySessionUserAsync(Guid userId)
        {
            try
            {
                using var context = new SensoreDbContext();
                return await context.Applicants
                    .Where(a => a.SessionUserId == userId)
                    .OrderBy(a => a.LastName)
                    .ThenBy(a => a.FirstName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error retrieving applicants for user: {userId}", ex, "ApplicantService");
                throw;
            }
        }

        /// <summary>
        /// Adds a new applicant (User Story 3)
        /// </summary>
        public async Task<(bool success, string message, Applicant? applicant)> AddApplicantAsync(Applicant applicant)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(applicant.FirstName) || string.IsNullOrWhiteSpace(applicant.LastName))
                {
                    return (false, "First name and last name are required", null);
                }

                using var context = new SensoreDbContext();
                context.Applicants.Add(applicant);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Applicant added: {applicant.FullName}", "ApplicantService");
                return (true, "Applicant added successfully", applicant);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error adding applicant: {applicant.FullName}", ex, "ApplicantService");
                return (false, "An error occurred while adding the applicant", null);
            }
        }

        /// <summary>
        /// Updates an existing applicant
        /// </summary>
        public async Task<(bool success, string message)> UpdateApplicantAsync(Applicant applicant)
        {
            try
            {
                using var context = new SensoreDbContext();
                context.Applicants.Update(applicant);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Applicant updated: {applicant.FullName}", "ApplicantService");
                return (true, "Applicant updated successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error updating applicant: {applicant.ApplicantId}", ex, "ApplicantService");
                return (false, "An error occurred while updating the applicant");
            }
        }

        /// <summary>
        /// Deletes an applicant and all related data (feedback sessions, responses, completed feedbacks)
        /// </summary>
        public async Task<(bool success, string message)> DeleteApplicantAsync(Guid applicantId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var applicant = await context.Applicants.FindAsync(applicantId);
                
                if (applicant == null)
                {
                    return (false, "Applicant not found");
                }

                // Delete related feedback sessions and all their data
                var feedbackSessions = await context.FeedbackSessions
                    .Where(fs => fs.ApplicantId == applicantId)
                    .ToListAsync();

                foreach (var session in feedbackSessions)
                {
                    // Delete completed feedbacks for this session
                    var completedFeedbacks = await context.CompletedFeedbacks
                        .Where(cf => cf.SessionId == session.SessionId)
                        .ToListAsync();
                    context.CompletedFeedbacks.RemoveRange(completedFeedbacks);

                    // Delete feedback responses for this session
                    var responses = await context.FeedbackResponses
                        .Where(fr => fr.SessionId == session.SessionId)
                        .ToListAsync();
                    context.FeedbackResponses.RemoveRange(responses);
                }

                // Delete feedback sessions
                context.FeedbackSessions.RemoveRange(feedbackSessions);

                // Finally delete the applicant
                context.Applicants.Remove(applicant);
                
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Applicant and related data deleted: {applicantId}", "ApplicantService");
                return (true, "Applicant and all related feedback data deleted successfully");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                Logger.Instance.LogError($"Database error deleting applicant: {applicantId}", dbEx, "ApplicantService");
                var innerMsg = dbEx.InnerException?.Message ?? dbEx.Message;
                if (innerMsg.Contains("REFERENCE constraint"))
                {
                    return (false, "Cannot delete applicant: There are related records that prevent deletion. Please contact support.");
                }
                return (false, $"Database error: {innerMsg}");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error deleting applicant: {applicantId}", ex, "ApplicantService");
                return (false, $"An error occurred while deleting: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes all applicants for a user session and their related data (User Story 5, 6)
        /// </summary>
        public async Task<(bool success, string message)> DeleteAllApplicantsForSessionAsync(Guid userId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var applicants = await context.Applicants
                    .Where(a => a.SessionUserId == userId)
                    .ToListAsync();

                if (applicants.Count == 0)
                {
                    return (true, "No applicants to delete");
                }

                int totalDeleted = 0;
                foreach (var applicant in applicants)
                {
                    // Delete related feedback sessions and all their data
                    var feedbackSessions = await context.FeedbackSessions
                        .Where(fs => fs.ApplicantId == applicant.ApplicantId)
                        .ToListAsync();

                    foreach (var session in feedbackSessions)
                    {
                        // Delete completed feedbacks for this session
                        var completedFeedbacks = await context.CompletedFeedbacks
                            .Where(cf => cf.SessionId == session.SessionId)
                            .ToListAsync();
                        context.CompletedFeedbacks.RemoveRange(completedFeedbacks);

                        // Delete feedback responses for this session
                        var responses = await context.FeedbackResponses
                            .Where(fr => fr.SessionId == session.SessionId)
                            .ToListAsync();
                        context.FeedbackResponses.RemoveRange(responses);
                    }

                    // Delete feedback sessions
                    context.FeedbackSessions.RemoveRange(feedbackSessions);
                    totalDeleted++;
                }

                // Delete all applicants
                context.Applicants.RemoveRange(applicants);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Deleted {totalDeleted} applicants and related data for user: {userId}", "ApplicantService");
                return (true, $"Deleted {totalDeleted} applicant(s) and all related feedback data successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error deleting applicants for user: {userId}", ex, "ApplicantService");
                return (false, $"An error occurred while deleting applicants: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a specific applicant by ID
        /// </summary>
        public async Task<Applicant?> GetApplicantByIdAsync(Guid applicantId)
        {
            try
            {
                using var context = new SensoreDbContext();
                return await context.Applicants.FindAsync(applicantId);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error retrieving applicant: {applicantId}", ex, "ApplicantService");
                return null;
            }
        }
    }
}
