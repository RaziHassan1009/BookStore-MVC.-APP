using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    public class PressureMapData
    {
        [Key]
        public long DataId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public DateTime RecordedDateTime { get; set; }

        public int FrameNumber { get; set; }

        [Required]
        public string MatrixData { get; set; } = string.Empty; // JSON format of 32x32 matrix

        public int? PeakPressure { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? ContactAreaPercentage { get; set; }

        public bool HasAlert { get; set; } = false;

        [MaxLength(500)]
        public string? AlertMessage { get; set; }

        public bool IsReviewed { get; set; } = false;

        public Guid? ReviewedBy { get; set; }

        public DateTime? ReviewedDate { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("ReviewedBy")]
        public virtual User? Reviewer { get; set; }

        [NotMapped]
        public int[,]? Matrix { get; set; }
    }
}
