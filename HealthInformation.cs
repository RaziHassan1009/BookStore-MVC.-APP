using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    /// <summary>
    /// Represents health information for a user
    /// </summary>
    public class HealthInformation
    {
        [Key]
        public Guid HealthId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public DateTime RecordDate { get; set; } = DateTime.Today;

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Weight { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Height { get; set; }

        public int? BloodPressureSystolic { get; set; }

        public int? BloodPressureDiastolic { get; set; }

        public int? HeartRate { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [NotMapped]
        public string BloodPressure => BloodPressureSystolic.HasValue && BloodPressureDiastolic.HasValue
            ? $"{BloodPressureSystolic}/{BloodPressureDiastolic}"
            : "N/A";

        [NotMapped]
        public decimal? BMI => Weight.HasValue && Height.HasValue && Height.Value > 0
            ? Weight.Value / (Height.Value * Height.Value)
            : null;
    }
}
