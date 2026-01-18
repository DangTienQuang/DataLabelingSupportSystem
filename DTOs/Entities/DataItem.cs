using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs.Entities
{
    public class DataItem
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [Required]
        public string StorageUrl { get; set; } = string.Empty;

        public string? MetaData { get; set; }

        public string Status { get; set; } = "New";

        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}