namespace Core.DTOs.Responses
{
    public class AssignedProjectResponse
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string ThumbnailUrl { get; set; } = string.Empty;
        public DateTime? AssignedDate { get; set; }
        public DateTime? Deadline { get; set; } 

        public int TotalImages { get; set; }
        public int CompletedImages { get; set; }
        public int ProgressPercent => TotalImages == 0 ? 0 : CompletedImages * 100 / TotalImages;
        public string Status { get; set; } = "Assigned";
    }
}