using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Requests
{
    public class ReviewRequest
    {
        [Required]
        public int AssignmentId { get; set; }
        [Required]
        public bool IsApproved { get; set; }
        public string? Comment { get; set; }
        public string? ErrorCategory { get; set; }
    }

    public class AuditReviewRequest
    {
        [Required]
        public int ReviewLogId { get; set; }
        [Required]
        public bool IsCorrectDecision { get; set; }
        public string? AuditComment { get; set; }
    }
}