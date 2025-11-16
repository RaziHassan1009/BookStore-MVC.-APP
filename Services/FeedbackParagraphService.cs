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
    /// Service for managing reusable feedback paragraphs (User Story 12)
    /// </summary>
    public class FeedbackParagraphService
    {
        /// <summary>
        /// Gets all active feedback paragraphs (User Story 12)
        /// </summary>
        public async Task<List<FeedbackParagraph>> GetAllParagraphsAsync(string? category = null, bool includeInactive = false)
        {
            try
            {
                using var context = new SensoreDbContext();
                var query = context.FeedbackParagraphs.AsQueryable();
                
                if (!includeInactive)
                {
                    query = query.Where(fp => fp.IsActive);
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(fp => fp.Category == category);
                }

                return await query.OrderBy(fp => fp.Category).ThenBy(fp => fp.Title).ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error retrieving feedback paragraphs", ex, "FeedbackParagraphService");
                throw;
            }
        }

        /// <summary>
        /// Adds a new feedback paragraph (User Story 12)
        /// </summary>
        public async Task<(bool success, string message, FeedbackParagraph? paragraph)> AddParagraphAsync(FeedbackParagraph paragraph)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(paragraph.Title) || string.IsNullOrWhiteSpace(paragraph.Content))
                {
                    return (false, "Title and content are required", null);
                }

                using var context = new SensoreDbContext();
                context.FeedbackParagraphs.Add(paragraph);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Feedback paragraph added: {paragraph.Title}", "FeedbackParagraphService");
                return (true, "Paragraph added successfully", paragraph);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error adding feedback paragraph: {paragraph.Title}", ex, "FeedbackParagraphService");
                return (false, "An error occurred while adding the paragraph", null);
            }
        }

        /// <summary>
        /// Updates a feedback paragraph (User Story 12)
        /// </summary>
        public async Task<(bool success, string message)> UpdateParagraphAsync(FeedbackParagraph paragraph)
        {
            try
            {
                using var context = new SensoreDbContext();
                paragraph.LastModifiedDate = DateTime.Now;
                context.FeedbackParagraphs.Update(paragraph);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Feedback paragraph updated: {paragraph.Title}", "FeedbackParagraphService");
                return (true, "Paragraph updated successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error updating feedback paragraph: {paragraph.ParagraphId}", ex, "FeedbackParagraphService");
                return (false, "An error occurred while updating the paragraph");
            }
        }

        /// <summary>
        /// Deletes a feedback paragraph (User Story 12)
        /// </summary>
        public async Task<(bool success, string message)> DeleteParagraphAsync(Guid paragraphId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var paragraph = await context.FeedbackParagraphs.FindAsync(paragraphId);
                
                if (paragraph == null)
                {
                    return (false, "Paragraph not found");
                }

                context.FeedbackParagraphs.Remove(paragraph);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Feedback paragraph deleted: {paragraphId}", "FeedbackParagraphService");
                return (true, "Paragraph deleted successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error deleting feedback paragraph: {paragraphId}", ex, "FeedbackParagraphService");
                return (false, "An error occurred while deleting the paragraph");
            }
        }

        /// <summary>
        /// Gets all categories
        /// </summary>
        public async Task<List<string>> GetCategoriesAsync()
        {
            try
            {
                using var context = new SensoreDbContext();
                return await context.FeedbackParagraphs
                    .Where(fp => fp.IsActive && !string.IsNullOrEmpty(fp.Category))
                    .Select(fp => fp.Category!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error retrieving paragraph categories", ex, "FeedbackParagraphService");
                throw;
            }
        }
    }
}
