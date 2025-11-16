using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string UserType { get; set; } = string.Empty; // Patient, Clinician, Admin

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastLoginDate { get; set; }

        public bool IsActive { get; set; } = true;

        public Guid? AssignedClinicianId { get; set; }

        [ForeignKey("AssignedClinicianId")]
        public virtual User? AssignedClinician { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName ?? ""} {LastName ?? ""}".Trim();
    }

    public enum UserTypeEnum
    {
        Patient,
        Clinician,
        Admin
    }
}
