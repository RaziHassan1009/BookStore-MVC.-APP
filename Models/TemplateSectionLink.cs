using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    /// <summary>
    /// Links sections to templates with ordering and requirements
    /// </summary>
    public class TemplateSectionLink
    {
        [Key]
        public Guid LinkId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TemplateId { get; set; }

        [Required]
        public Guid SectionId { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsRequired { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("TemplateId")]
        public virtual Template? Template { get; set; }

        [ForeignKey("SectionId")]
        public virtual Section? Section { get; set; }
    }
}
