using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs.Entities
{
    public class ProjectMember
    {
        [Key]
        public int ProjectMemberId { get; set; }

        public int ProjectId { get; set; }
        public int UserId { get; set; }

        // Role within this specific project: "Manager", "Annotator", "Reviewer"
        [Required]
        [MaxLength(20)]
        public required string ProjectRole { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
