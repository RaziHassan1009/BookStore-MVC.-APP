using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    public class MetricsSummary
    {
        [Key]
        public long SummaryId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public DateTime SummaryDate { get; set; }

        public int SummaryHour { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? AvgPeakPressure { get; set; }

        public int? MaxPeakPressure { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? AvgContactArea { get; set; }

        public int AlertCount { get; set; } = 0;

        public int FrameCount { get; set; } = 0;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
