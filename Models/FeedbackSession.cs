using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    /// <summary>
    /// Represents a feedback session for an applicant
    /// </summary>
    public class FeedbackSession
    {
        [Key]
        public Guid SessionId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid ApplicantId { get; set; }

        [Required]
        public Guid TemplateId { get; set; }

        public int CurrentSectionIndex { get; set; } = 0;

        public DateTime StartedDate { get; set; } = DateTime.Now;

        public DateTime? CompletedDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "InProgress"; // InProgress, Completed, Aborted

        public bool IsSaved { get; set; } = false;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("ApplicantId")]
        public virtual Applicant? Applicant { get; set; }

        [ForeignKey("TemplateId")]
        public virtual Template? Template { get; set; }
    }

    /// <summary>
    /// Enum for feedback session status
    /// </summary>
    public enum FeedbackSessionStatus
    {
        InProgress,
        Completed,
        Aborted
    }
}
