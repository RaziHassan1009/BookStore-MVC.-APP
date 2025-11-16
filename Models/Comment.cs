using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    public class Comment
    {
        [Key]
        public long CommentId { get; set; }

        [Required]
        public long DataId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string CommentText { get; set; } = string.Empty;

        public DateTime CommentDateTime { get; set; } = DateTime.Now;

        public long? ParentCommentId { get; set; }

        public bool IsClinicianReply { get; set; } = false;

        [ForeignKey("DataId")]
        public virtual PressureMapData? PressureMapData { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("ParentCommentId")]
        public virtual Comment? ParentComment { get; set; }
    }
}
