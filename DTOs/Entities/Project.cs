using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs.Entities
{
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Name { get; set; }

        public string? Description { get; set; }

        public string? Instruction { get; set; }

        [Required]
        [MaxLength(20)]
        public required string DataType { get; set; } // "Image", "Text"...

        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<ProjectMember>? Members { get; set; } // All members (Managers, Annotators, etc.)
        public ICollection<LabelClass>? LabelClasses { get; set; }
        public ICollection<DataItem>? DataItems { get; set; }
    }
}