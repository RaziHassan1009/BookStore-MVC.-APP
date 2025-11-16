using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    /// <summary>
    /// Represents a feedback template that can be used to evaluate applicants
    /// </summary>
    public class Template
    {
        [Key]
        public Guid TemplateId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string TemplateName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public Guid CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
    }
}
