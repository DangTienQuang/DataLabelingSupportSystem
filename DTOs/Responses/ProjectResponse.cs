namespace DTOs.Responses
{
    /// <summary>
    /// Detailed response model for a project.
    /// </summary>
    public class ProjectDetailResponse
    {
        /// <summary>
        /// The unique identifier of the project.
        /// </summary>
        /// <example>10</example>
        public int Id { get; set; }

        /// <summary>
        /// The name of the project.
        /// </summary>
        /// <example>Object Detection Phase 1</example>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The description of the project.
        /// </summary>
        /// <example>Phase 1 of object detection including cars and pedestrians.</example>
        public string? Description { get; set; }

        /// <summary>
        /// The price paid per label.
        /// </summary>
        /// <example>0.05</example>
        public decimal PricePerLabel { get; set; }

        /// <summary>
        /// The total budget allocated for the project.
        /// </summary>
        /// <example>500.00</example>
        public decimal TotalBudget { get; set; }

        /// <summary>
        /// The deadline for the project.
        /// </summary>
        /// <example>2024-01-31T23:59:59</example>
        public DateTime Deadline { get; set; }

        /// <summary>
        /// The unique identifier of the project manager.
        /// </summary>
        /// <example>12345678-abcd-1234-abcd-1234567890ab</example>
        public string ManagerId { get; set; } = string.Empty;

        /// <summary>
        /// The name of the project manager.
        /// </summary>
        /// <example>Alice Manager</example>
        public string ManagerName { get; set; } = string.Empty;

        /// <summary>
        /// The email of the project manager.
        /// </summary>
        /// <example>alice@example.com</example>
        public string ManagerEmail { get; set; } = string.Empty;

        /// <summary>
        /// The list of label classes defined for the project.
        /// </summary>
        public List<LabelResponse> Labels { get; set; } = new List<LabelResponse>();

        /// <summary>
        /// The total number of data items in the project.
        /// </summary>
        /// <example>1000</example>
        public int TotalDataItems { get; set; }

        /// <summary>
        /// The number of data items that have been processed.
        /// </summary>
        /// <example>250</example>
        public int ProcessedItems { get; set; }

        /// <summary>
        /// The progress percentage of the project (0-100).
        /// </summary>
        /// <example>25</example>
        public int Progress { get; set; }

        /// <summary>
        /// The list of members (annotators, reviewers) assigned to the project.
        /// </summary>
        public List<MemberResponse> Members { get; set; } = new List<MemberResponse>();
    }

    /// <summary>
    /// Response model representing a member of a project.
    /// </summary>
    public class MemberResponse
    {
        /// <summary>
        /// The unique identifier of the member.
        /// </summary>
        /// <example>87654321-cba0-4321-dcba-0987654321ba</example>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The full name of the member.
        /// </summary>
        /// <example>Bob Annotator</example>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// The email address of the member.
        /// </summary>
        /// <example>bob@example.com</example>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The role of the member in the project (e.g., Annotator).
        /// </summary>
        /// <example>Annotator</example>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// The number of tasks assigned to this member.
        /// </summary>
        /// <example>50</example>
        public int TasksAssigned { get; set; }

        /// <summary>
        /// The number of tasks completed by this member.
        /// </summary>
        /// <example>45</example>
        public int TasksCompleted { get; set; }

        /// <summary>
        /// The completion progress of the member (0.0 - 1.0 or percentage).
        /// </summary>
        /// <example>90.0</example>
        public decimal Progress { get; set; }
    }

    /// <summary>
    /// Summary response model for a project.
    /// </summary>
    public class ProjectSummaryResponse
    {
        /// <summary>
        /// The unique identifier of the project.
        /// </summary>
        /// <example>10</example>
        public int Id { get; set; }

        /// <summary>
        /// The name of the project.
        /// </summary>
        /// <example>Object Detection Phase 1</example>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The deadline for the project.
        /// </summary>
        /// <example>2024-01-31T23:59:59</example>
        public DateTime Deadline { get; set; }

        /// <summary>
        /// The current status of the project (e.g., Active, Completed).
        /// </summary>
        /// <example>Active</example>
        public string Status { get; set; } = "Active";

        /// <summary>
        /// The total number of data items in the project.
        /// </summary>
        /// <example>1000</example>
        public int TotalDataItems { get; set; }

        /// <summary>
        /// The overall progress of the project.
        /// </summary>
        /// <example>25.5</example>
        public decimal Progress { get; set; }

        /// <summary>
        /// The total number of members working on the project.
        /// </summary>
        /// <example>5</example>
        public int TotalMembers { get; set; }
    }
}
