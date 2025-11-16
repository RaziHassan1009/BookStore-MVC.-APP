using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    /// <summary>
    /// Represents a feedback code that can be selected within a section
    /// </summary>
    public class Code
    {
        [Key]
        public Guid CodeId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SectionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CodeText { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? CodeDescription { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [Required]
        public Guid CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("SectionId")]
        public virtual Section? Section { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
    }
}
