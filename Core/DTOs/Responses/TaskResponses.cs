namespace Core.DTOs.Responses
{
    /// <summary>
    /// Response model representing a task assignment.
    /// </summary>
    public class TaskResponse
    {
        /// <summary>
        /// The unique identifier of the assignment.
        /// </summary>
        /// <example>101</example>
        public int AssignmentId { get; set; }

        /// <summary>
        /// The unique identifier of the data item.
        /// </summary>
        /// <example>5001</example>
        public int DataItemId { get; set; }

        /// <summary>
        /// The URL where the data item is stored.
        /// </summary>
        /// <example>https://example.com/image.jpg</example>
        public string StorageUrl { get; set; } = string.Empty;

        /// <summary>
        /// The name of the project this task belongs to.
        /// </summary>
        /// <example>Object Detection Phase 1</example>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// The status of the task (e.g., Assigned, InProgress, Submitted, Completed, Rejected).
        /// </summary>
        /// <example>Assigned</example>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// The reason for rejection, if applicable.
        /// </summary>
        /// <example>Incorrect bounding box.</example>
        public string? RejectReason { get; set; }

        /// <summary>
        /// The deadline for the task.
        /// </summary>
        /// <example>2024-01-31T23:59:59</example>
        public DateTime Deadline { get; set; }

        /// <summary>
        /// The list of available label definitions for this task.
        /// </summary>
        public List<LabelResponse> Labels { get; set; } = new List<LabelResponse>();

        /// <summary>
        /// Existing annotations for this task, if any.
        /// </summary>
        public List<object>? ExistingAnnotations { get; set; }
    }

    /// <summary>
    /// Response model containing statistics for an annotator.
    /// </summary>
    public class AnnotatorStatsResponse
    {
        /// <summary>
        /// The total number of tasks assigned.
        /// </summary>
        /// <example>50</example>
        public int TotalAssigned { get; set; }

        /// <summary>
        /// The number of pending tasks (Assigned or InProgress).
        /// </summary>
        /// <example>10</example>
        public int Pending { get; set; }

        /// <summary>
        /// The number of submitted tasks waiting for review.
        /// </summary>
        /// <example>5</example>
        public int Submitted { get; set; }

        /// <summary>
        /// The number of rejected tasks.
        /// </summary>
        /// <example>2</example>
        public int Rejected { get; set; }

        /// <summary>
        /// The number of completed (approved) tasks.
        /// </summary>
        /// <example>33</example>
        public int Completed { get; set; }
    }

    /// <summary>
    /// Response model containing statistics for a manager.
    /// </summary>
    public class ManagerStatsResponse
    {
        /// <summary>
        /// The total number of projects managed.
        /// </summary>
        /// <example>5</example>
        public int TotalProjects { get; set; }   

        /// <summary>
        /// The number of currently active projects.
        /// </summary>
        /// <example>2</example>
        public int ActiveProjects { get; set; }    

        /// <summary>
        /// The total number of data items across all projects.
        /// </summary>
        /// <example>5000</example>
        public int TotalDataItems { get; set; }     

        /// <summary>
        /// The total budget managed.
        /// </summary>
        /// <example>10000.00</example>
        public decimal TotalBudget { get; set; }    

        /// <summary>
        /// The total number of members managed.
        /// </summary>
        /// <example>20</example>
        public int TotalMembers { get; set; }  
    }
}
