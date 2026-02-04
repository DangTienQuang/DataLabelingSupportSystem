namespace Core.DTOs.Responses
{
    /// <summary>
    /// Response model containing statistics for a specific project.
    /// </summary>
    public class ProjectStatisticsResponse
    {
        /// <summary>
        /// The unique identifier of the project.
        /// </summary>
        /// <example>10</example>
        public int ProjectId { get; set; }

        /// <summary>
        /// The name of the project.
        /// </summary>
        /// <example>Object Detection Phase 1</example>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// The total number of items in the project.
        /// </summary>
        /// <example>1000</example>
        public int TotalItems { get; set; }

        /// <summary>
        /// The number of completed items.
        /// </summary>
        /// <example>800</example>
        public int CompletedItems { get; set; }

        /// <summary>
        /// The completion progress percentage.
        /// </summary>
        /// <example>80.0</example>
        public decimal ProgressPercentage { get; set; }

        /// <summary>
        /// The total number of assignments made.
        /// </summary>
        /// <example>1200</example>
        public int TotalAssignments { get; set; }

        /// <summary>
        /// The number of assignments currently pending.
        /// </summary>
        /// <example>200</example>
        public int PendingAssignments { get; set; }

        /// <summary>
        /// The number of assignments submitted but not yet reviewed.
        /// </summary>
        /// <example>150</example>
        public int SubmittedAssignments { get; set; }

        /// <summary>
        /// The number of approved assignments.
        /// </summary>
        /// <example>700</example>
        public int ApprovedAssignments { get; set; }

        /// <summary>
        /// The number of rejected assignments.
        /// </summary>
        /// <example>150</example>
        public int RejectedAssignments { get; set; }

        /// <summary>
        /// The total cost incurred so far.
        /// </summary>
        /// <example>35.00</example>
        public decimal CostIncurred { get; set; }

        /// <summary>
        /// The rejection rate percentage based on review history.
        /// </summary>
        /// <example>15.5</example>
        public double RejectionRate { get; set; }

        /// <summary>
        /// Breakdown of error types from rejected reviews.
        /// </summary>
        public Dictionary<string, int> ErrorBreakdown { get; set; } = new();

        /// <summary>
        /// Performance statistics for each annotator on the project.
        /// </summary>
        public List<AnnotatorPerformance> AnnotatorPerformances { get; set; } = new();

        /// <summary>
        /// Distribution of labels across the project's annotations.
        /// </summary>
        public List<LabelDistribution> LabelDistributions { get; set; } = new();
    }

    /// <summary>
    /// Performance metrics for an individual annotator.
    /// </summary>
    public class AnnotatorPerformance
    {
        /// <summary>
        /// The unique identifier of the annotator.
        /// </summary>
        /// <example>87654321-cba0-4321-dcba-0987654321ba</example>
        public string AnnotatorId { get; set; } = string.Empty;

        /// <summary>
        /// The name of the annotator.
        /// </summary>
        /// <example>Bob Annotator</example>
        public string AnnotatorName { get; set; } = string.Empty;

        /// <summary>
        /// The number of tasks assigned to the annotator.
        /// </summary>
        /// <example>100</example>
        public int TasksAssigned { get; set; }

        /// <summary>
        /// The number of tasks completed by the annotator.
        /// </summary>
        /// <example>90</example>
        public int TasksCompleted { get; set; }

        /// <summary>
        /// The number of tasks rejected for the annotator.
        /// </summary>
        /// <example>5</example>
        public int TasksRejected { get; set; }

        /// <summary>
        /// The average duration in seconds taken to complete a task.
        /// </summary>
        /// <example>120.5</example>
        public double AverageDurationSeconds { get; set; }

        /// <summary>
        /// The average quality score based on review results (0-100).
        /// </summary>
        public double AverageQualityScore { get; set; }

        /// <summary>
        /// Total number of critical errors committed by this annotator.
        /// </summary>
        public int TotalCriticalErrors { get; set; }
    }

    /// <summary>
    /// Model representing the count of a specific label class.
    /// </summary>
    public class LabelDistribution
    {
        /// <summary>
        /// The name of the label class.
        /// </summary>
        /// <example>Car</example>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// The number of times this label has been used.
        /// </summary>
        /// <example>500</example>
        public int Count { get; set; }
    }
}
