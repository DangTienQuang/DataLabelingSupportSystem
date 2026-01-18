namespace DTOs.Responses
{
    public class ProjectStatisticsResponse
    {
        public int ProjectId { get; set; }
        public int TotalItems { get; set; }
        public int CompletedItems { get; set; }
        public decimal ProgressPercentage { get; set; }

        public int TotalAssignments { get; set; }
        public int PendingAssignments { get; set; }
        public int SubmittedAssignments { get; set; }
        public int ApprovedAssignments { get; set; }
        public int RejectedAssignments { get; set; }

        public List<AnnotatorPerformance> AnnotatorPerformances { get; set; } = new List<AnnotatorPerformance>();
        public List<LabelDistribution> LabelDistributions { get; set; } = new List<LabelDistribution>();
    }

    public class AnnotatorPerformance
    {
        public string AnnotatorId { get; set; } = string.Empty;
        public string AnnotatorName { get; set; } = string.Empty;
        public int TasksAssigned { get; set; }
        public int TasksCompleted { get; set; }
        public int TasksRejected { get; set; }
        public double AverageDurationSeconds { get; set; }
    }

    public class LabelDistribution
    {
        public string ClassName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}