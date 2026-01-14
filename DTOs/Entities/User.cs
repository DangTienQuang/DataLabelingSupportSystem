using System.ComponentModel.DataAnnotations;

namespace DTOs.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Username { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        [MaxLength(100)]
        public string? FullName { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        // System Level Role (e.g., "Admin", "User"). 
        // Specific permissions are now in ProjectMembers.
        [Required]
        [MaxLength(20)]
        public required string Role { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<ProjectMember>? ProjectMemberships { get; set; }
        public ICollection<Assignment>? Assignments { get; set; } // Work assigned to this user
    }
}