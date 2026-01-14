using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs.Entities
{
    public class Assignment
    {
        [Key]
        public int AssignmentId { get; set; }

        public int DataItemId { get; set; }
        public int UserId { get; set; }

        // "Annotation", "Review"
        [Required]
        [MaxLength(20)]
        public required string TaskType { get; set; }

        // "Pending", "InProgress", "Completed", "Rejected"
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        [ForeignKey("DataItemId")]
        public DataItem? DataItem { get; set; }

        [ForeignKey("UserId")]
        public User? Assignee { get; set; }
    }
}