namespace Core.DTOs.Responses
{
    public class AnnotatorProjectStatsResponse
    {
        public int? AssignmentId { get; set; }

        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;

        public int TotalImages { get; set; }

        public int CompletedImages { get; set; }

        public string Status { get; set; } = "Active";
        public DateTime Deadline { get; set; }
    }
}