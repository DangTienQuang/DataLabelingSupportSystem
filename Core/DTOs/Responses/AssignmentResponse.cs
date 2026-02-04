namespace Core.DTOs.Responses
{
    public class AssignmentResponse
    {
        public int Id { get; set; } 
        public int DataItemId { get; set; }
        public string DataItemUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; 
        public string? AnnotationData { get; set; }
        public DateTime? AssignedDate { get; set; }
        public DateTime? Deadline { get; set; }

        public string? RejectionReason { get; set; }
    }
}