using GrapheneSensore.Data;
using GrapheneSensore.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrapheneSensore.Services
{
    public class ReportService
    {
        public class MetricsReport
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int TotalFrames { get; set; }
            public decimal AvgPeakPressure { get; set; }
            public int MaxPeakPressure { get; set; }
            public decimal AvgContactArea { get; set; }
            public int TotalAlerts { get; set; }
            public List<HourlyMetric> HourlyMetrics { get; set; } = new();
            public ComparisonData? Comparison { get; set; }
        }

        public class HourlyMetric
        {
            public DateTime Hour { get; set; }
            public decimal AvgPeakPressure { get; set; }
            public decimal AvgContactArea { get; set; }
            public int AlertCount { get; set; }
        }

        public class ComparisonData
        {
            public decimal PeakPressureChange { get; set; }
            public decimal ContactAreaChange { get; set; }
            public int AlertCountChange { get; set; }
        }

        public async Task<MetricsReport> GenerateReportAsync(
            Guid userId, 
            DateTime startDate, 
            DateTime endDate,
            bool includeComparison = false,
            DateTime? comparisonStartDate = null,
            DateTime? comparisonEndDate = null)
        {
            using var context = new SensoreDbContext();
            
            var data = await context.PressureMapData
                .Where(p => p.UserId == userId && 
                           p.RecordedDateTime >= startDate && 
                           p.RecordedDateTime <= endDate)
                .ToListAsync();

            var alerts = await context.Alerts
                .Where(a => a.UserId == userId && 
                           a.AlertDateTime >= startDate && 
                           a.AlertDateTime <= endDate)
                .ToListAsync();

            var report = new MetricsReport
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalFrames = data.Count,
                AvgPeakPressure = data.Any() ? (decimal)data.Average(d => d.PeakPressure ?? 0) : 0,
                MaxPeakPressure = data.Any() ? data.Max(d => d.PeakPressure ?? 0) : 0,
                AvgContactArea = data.Any() ? data.Average(d => d.ContactAreaPercentage ?? 0) : 0,
                TotalAlerts = alerts.Count,
                HourlyMetrics = GetHourlyMetrics(data, alerts)
            };

            // Add comparison if requested
            if (includeComparison && comparisonStartDate.HasValue && comparisonEndDate.HasValue)
            {
                var comparisonReport = await GenerateReportAsync(
                    userId, 
                    comparisonStartDate.Value, 
                    comparisonEndDate.Value);

                report.Comparison = new ComparisonData
                {
                    PeakPressureChange = report.AvgPeakPressure - comparisonReport.AvgPeakPressure,
                    ContactAreaChange = report.AvgContactArea - comparisonReport.AvgContactArea,
                    AlertCountChange = report.TotalAlerts - comparisonReport.TotalAlerts
                };
            }

            return report;
        }

        private List<HourlyMetric> GetHourlyMetrics(List<PressureMapData> data, List<Alert> alerts)
        {
            var hourlyMetrics = new List<HourlyMetric>();
            
            var groupedData = data.GroupBy(d => new DateTime(
                d.RecordedDateTime.Year,
                d.RecordedDateTime.Month,
                d.RecordedDateTime.Day,
                d.RecordedDateTime.Hour,
                0, 0));

            foreach (var group in groupedData.OrderBy(g => g.Key))
            {
                var hourAlerts = alerts.Count(a => 
                    a.AlertDateTime.Year == group.Key.Year &&
                    a.AlertDateTime.Month == group.Key.Month &&
                    a.AlertDateTime.Day == group.Key.Day &&
                    a.AlertDateTime.Hour == group.Key.Hour);

                hourlyMetrics.Add(new HourlyMetric
                {
                    Hour = group.Key,
                    AvgPeakPressure = (decimal)group.Average(d => d.PeakPressure ?? 0),
                    AvgContactArea = group.Average(d => d.ContactAreaPercentage ?? 0),
                    AlertCount = hourAlerts
                });
            }

            return hourlyMetrics;
        }

        public async Task<MetricsReport> GetDailyReportAsync(Guid userId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1).AddSeconds(-1);

            return await GenerateReportAsync(userId, startDate, endDate);
        }

        public async Task<MetricsReport> GetLastHoursReportAsync(Guid userId, int hours)
        {
            var endDate = DateTime.Now;
            var startDate = endDate.AddHours(-hours);

            return await GenerateReportAsync(userId, startDate, endDate);
        }

        public async Task<MetricsReport> GetComparativeDailyReportAsync(Guid userId, DateTime currentDate)
        {
            var startDate = currentDate.Date;
            var endDate = startDate.AddDays(1).AddSeconds(-1);
            
            var previousDate = currentDate.AddDays(-1).Date;
            var previousEndDate = previousDate.AddDays(1).AddSeconds(-1);

            return await GenerateReportAsync(
                userId, 
                startDate, 
                endDate, 
                true, 
                previousDate, 
                previousEndDate);
        }

        public async Task UpdateMetricsSummaryAsync(Guid userId, DateTime date)
        {
            using var context = new SensoreDbContext();
            
            var startDate = date.Date;
            var endDate = startDate.AddDays(1).AddSeconds(-1);

            var data = await context.PressureMapData
                .Where(p => p.UserId == userId && 
                           p.RecordedDateTime >= startDate && 
                           p.RecordedDateTime <= endDate)
                .ToListAsync();

            var alerts = await context.Alerts
                .Where(a => a.UserId == userId && 
                           a.AlertDateTime >= startDate && 
                           a.AlertDateTime <= endDate)
                .ToListAsync();

            var hourlyGroups = data.GroupBy(d => d.RecordedDateTime.Hour);

            foreach (var group in hourlyGroups)
            {
                var summary = await context.MetricsSummaries
                    .FirstOrDefaultAsync(m => 
                        m.UserId == userId && 
                        m.SummaryDate == date.Date && 
                        m.SummaryHour == group.Key);

                if (summary == null)
                {
                    summary = new MetricsSummary
                    {
                        UserId = userId,
                        SummaryDate = date.Date,
                        SummaryHour = group.Key
                    };
                    await context.MetricsSummaries.AddAsync(summary);
                }

                summary.AvgPeakPressure = (decimal)group.Average(d => d.PeakPressure ?? 0);
                summary.MaxPeakPressure = group.Max(d => d.PeakPressure ?? 0);
                summary.AvgContactArea = group.Average(d => d.ContactAreaPercentage ?? 0);
                summary.FrameCount = group.Count();
                summary.AlertCount = alerts.Count(a => a.AlertDateTime.Hour == group.Key);
            }

            await context.SaveChangesAsync();
        }
    }
}
