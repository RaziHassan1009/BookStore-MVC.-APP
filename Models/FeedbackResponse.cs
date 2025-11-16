using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    /// <summary>
    /// Represents a response to a feedback section
    /// </summary>
    public class FeedbackResponse
    {
        [Key]
        public Guid ResponseId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SessionId { get; set; }

        [Required]
        public Guid SectionId { get; set; }

        public Guid? CodeId { get; set; }

        public string? ResponseText { get; set; }

        public bool IsChecked { get; set; } = false;

        public DateTime ResponseDate { get; set; } = DateTime.Now;

        [ForeignKey("SessionId")]
        public virtual FeedbackSession? Session { get; set; }

        [ForeignKey("SectionId")]
        public virtual Section? Section { get; set; }

        [ForeignKey("CodeId")]
        public virtual Code? Code { get; set; }
    }
}
