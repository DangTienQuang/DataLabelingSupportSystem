using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs.Entities
{
    public class WorkflowLog
    {
        [Key]
        public int LogId { get; set; }

        public int DataItemId { get; set; }
        public int ActorId { get; set; } // The User performing the action

        // "Submitted", "Approved", "Rejected", "Reassigned"
        [Required]
        [MaxLength(50)]
        public required string Action { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [ForeignKey("DataItemId")]
        public DataItem? DataItem { get; set; }

        [ForeignKey("ActorId")]
        public User? Actor { get; set; }
    }
}
