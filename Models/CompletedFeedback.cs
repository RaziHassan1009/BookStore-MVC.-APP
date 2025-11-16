using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    /// <summary>
    /// Represents a completed feedback with PDF and email tracking
    /// </summary>
    public class CompletedFeedback
    {
        [Key]
        public Guid FeedbackId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SessionId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ApplicantName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string TemplateName { get; set; } = string.Empty;

        [Required]
        public string FeedbackData { get; set; } = string.Empty; // JSON format

        [MaxLength(500)]
        public string? PdfPath { get; set; }

        public bool EmailSent { get; set; } = false;

        public DateTime? EmailSentDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        public Guid CreatedBy { get; set; }

        [ForeignKey("SessionId")]
        public virtual FeedbackSession? Session { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
    }
}
