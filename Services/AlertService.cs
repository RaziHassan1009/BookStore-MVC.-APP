using GrapheneSensore.Data;
using GrapheneSensore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrapheneSensore.Services
{
    public class AlertService
    {
        public async Task<List<Alert>> GetUserAlertsAsync(Guid userId, bool unacknowledgedOnly = false)
        {
            using var context = new SensoreDbContext();
            var query = context.Alerts
                .Include(a => a.User)
                .Include(a => a.PressureMapData)
                .Where(a => a.UserId == userId);

            if (unacknowledgedOnly)
            {
                query = query.Where(a => !a.IsAcknowledged);
            }

            return await query
                .OrderByDescending(a => a.AlertDateTime)
                .ToListAsync();
        }

        public async Task<List<Alert>> GetAllAlertsAsync(bool unacknowledgedOnly = false)
        {
            using var context = new SensoreDbContext();
            var query = context.Alerts
                .Include(a => a.User)
                .Include(a => a.PressureMapData)
                .AsQueryable();

            if (unacknowledgedOnly)
            {
                query = query.Where(a => !a.IsAcknowledged);
            }

            return await query
                .OrderByDescending(a => a.AlertDateTime)
                .ToListAsync();
        }

        public async Task<(bool success, string message)> AcknowledgeAlertAsync(long alertId, Guid acknowledgedBy)
        {
            try
            {
                using var context = new SensoreDbContext();
                var alert = await context.Alerts.FindAsync(alertId);
                
                if (alert == null)
                {
                    return (false, "Alert not found");
                }

                alert.IsAcknowledged = true;
                alert.AcknowledgedBy = acknowledgedBy;
                alert.AcknowledgedDate = DateTime.Now;

                await context.SaveChangesAsync();
                return (true, "Alert acknowledged successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error acknowledging alert: {ex.Message}");
            }
        }

        public async Task<int> GetUnacknowledgedAlertCountAsync(Guid userId)
        {
            using var context = new SensoreDbContext();
            return await context.Alerts
                .Where(a => a.UserId == userId && !a.IsAcknowledged)
                .CountAsync();
        }

        public async Task<List<Alert>> GetAlertsByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            using var context = new SensoreDbContext();
            return await context.Alerts
                .Include(a => a.User)
                .Include(a => a.PressureMapData)
                .Where(a => a.UserId == userId && 
                           a.AlertDateTime >= startDate && 
                           a.AlertDateTime <= endDate)
                .OrderByDescending(a => a.AlertDateTime)
                .ToListAsync();
        }
    }
}
