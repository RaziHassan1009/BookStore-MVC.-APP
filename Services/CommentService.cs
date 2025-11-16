using GrapheneSensore.Data;
using GrapheneSensore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrapheneSensore.Services
{
    public class CommentService
    {
        public async Task<List<Comment>> GetCommentsForDataAsync(long dataId)
        {
            using var context = new SensoreDbContext();
            return await context.Comments
                .Include(c => c.User)
                .Include(c => c.ParentComment)
                .Where(c => c.DataId == dataId)
                .OrderBy(c => c.CommentDateTime)
                .ToListAsync();
        }

        public async Task<(bool success, string message, Comment? comment)> AddCommentAsync(
            long dataId, 
            Guid userId, 
            string commentText, 
            bool isClinicianReply = false,
            long? parentCommentId = null)
        {
            try
            {
                using var context = new SensoreDbContext();
                
                // Verify data exists
                var data = await context.PressureMapData.FindAsync(dataId);
                if (data == null)
                {
                    return (false, "Data record not found", null);
                }

                var comment = new Comment
                {
                    DataId = dataId,
                    UserId = userId,
                    CommentText = commentText,
                    CommentDateTime = DateTime.Now,
                    ParentCommentId = parentCommentId,
                    IsClinicianReply = isClinicianReply
                };

                await context.Comments.AddAsync(comment);
                await context.SaveChangesAsync();

                return (true, "Comment added successfully", comment);
            }
            catch (Exception ex)
            {
                return (false, $"Error adding comment: {ex.Message}", null);
            }
        }

        public async Task<List<Comment>> GetUserCommentsAsync(Guid userId)
        {
            using var context = new SensoreDbContext();
            return await context.Comments
                .Include(c => c.User)
                .Include(c => c.PressureMapData)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CommentDateTime)
                .ToListAsync();
        }

        public async Task<Dictionary<long, int>> GetCommentCountsAsync(List<long> dataIds)
        {
            using var context = new SensoreDbContext();
            return await context.Comments
                .Where(c => dataIds.Contains(c.DataId))
                .GroupBy(c => c.DataId)
                .Select(g => new { DataId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DataId, x => x.Count);
        }

        public async Task<(bool success, string message)> DeleteCommentAsync(long commentId, Guid userId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var comment = await context.Comments.FindAsync(commentId);
                
                if (comment == null)
                {
                    return (false, "Comment not found");
                }

                // Only allow user who created comment or admin to delete
                if (comment.UserId != userId)
                {
                    var user = await context.Users.FindAsync(userId);
                    if (user?.UserType != "Admin")
                    {
                        return (false, "You don't have permission to delete this comment");
                    }
                }

                context.Comments.Remove(comment);
                await context.SaveChangesAsync();

                return (true, "Comment deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting comment: {ex.Message}");
            }
        }
    }
}
