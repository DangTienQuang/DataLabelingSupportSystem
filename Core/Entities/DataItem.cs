using Core.Constants;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public class DataItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        [Required]
        public string StorageUrl { get; set; } = string.Empty;

        public int BucketId { get; set; } = 1;

        public string Status { get; set; } = TaskStatusConstants.New;

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
        public string? MetaData { get; set; }
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}