using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Requests
{

    public class CreateDisputeRequest
    {
        [Required]
        public int AssignmentId { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    public class ResolveDisputeRequest
    {
        [Required]
        public int DisputeId { get; set; }

        [Required]
        public bool IsAccepted { get; set; }

        public string? ManagerComment { get; set; }
    }
}