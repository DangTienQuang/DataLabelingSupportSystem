namespace DTOs.Responses
{
    public class TaskResponse
    {
        public int AssignmentId { get; set; }
        public int DataItemId { get; set; }
        public string StorageUrl { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? RejectReason { get; set; }
        public DateTime Deadline { get; set; }

        public List<LabelResponse> Labels { get; set; } = new List<LabelResponse>();
        public List<object>? ExistingAnnotations { get; set; }
    }

    public class AnnotatorStatsResponse
    {
        public int TotalAssigned { get; set; }
        public int Pending { get; set; }
        public int Submitted { get; set; }
        public int Rejected { get; set; }
        public int Completed { get; set; }
    }
    public class ManagerStatsResponse
    {
        public int TotalProjects { get; set; }   
        public int ActiveProjects { get; set; }    
        public int TotalDataItems { get; set; }     
        public decimal TotalBudget { get; set; }    
        public int TotalMembers { get; set; }  
    }
}