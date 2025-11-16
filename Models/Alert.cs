using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    public class Alert
    {
        [Key]
        public long AlertId { get; set; }

        [Required]
        public long DataId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string AlertType { get; set; } = string.Empty;

        public DateTime AlertDateTime { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string? Severity { get; set; }

        [MaxLength(500)]
        public string? Message { get; set; }

        public bool IsAcknowledged { get; set; } = false;

        public Guid? AcknowledgedBy { get; set; }

        public DateTime? AcknowledgedDate { get; set; }

        [ForeignKey("DataId")]
        public virtual PressureMapData? PressureMapData { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("AcknowledgedBy")]
        public virtual User? Acknowledger { get; set; }
    }

    public enum AlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum AlertType
    {
        HighPressure,
        ExtendedPressure,
        LowContactArea,
        PressureSpike
    }
}
