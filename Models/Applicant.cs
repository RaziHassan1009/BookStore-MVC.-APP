using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    /// <summary>
    /// Represents an applicant for feedback evaluation
    /// </summary>
    public class Applicant
    {
        [Key]
        public Guid ApplicantId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SessionUserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? ReferenceNumber { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("SessionUserId")]
        public virtual User? SessionUser { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}
