using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public class User
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string PasswordHash { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        [Required]
        public string Role { get; set; } = "Annotator";

        public string? ManagerId { get; set; }

        [ForeignKey("ManagerId")]
        [InverseProperty("ManagedUsers")]
        public virtual User? Manager { get; set; }

        [InverseProperty("Manager")]
        public virtual ICollection<User> ManagedUsers { get; set; } = new List<User>();

        public virtual ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public virtual ICollection<ReviewLog> ReviewsGiven { get; set; } = new List<ReviewLog>();
        public virtual ICollection<UserProjectStat> ProjectStats { get; set; } = new List<UserProjectStat>();
    }
}