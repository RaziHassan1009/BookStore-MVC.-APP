using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    /// <summary>
    /// Represents a reusable paragraph of feedback text
    /// </summary>
    public class FeedbackParagraph
    {
        [Key]
        public Guid ParagraphId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        [Required]
        public Guid CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
    }
}
