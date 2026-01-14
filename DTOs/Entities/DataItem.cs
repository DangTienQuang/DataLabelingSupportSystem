using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs.Entities
{
    public class DataItem
    {
        [Key]
        public int DataItemId { get; set; }

        public int ProjectId { get; set; }

        [Required]
        public required string FileName { get; set; }

        [Required]
        public required string StorageUrl { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        public ICollection<Annotation>? Annotations { get; set; }
        public ICollection<Assignment>? Assignments { get; set; } // This tracks who is working on it
        public ICollection<WorkflowLog>? WorkflowLogs { get; set; }
    }
}