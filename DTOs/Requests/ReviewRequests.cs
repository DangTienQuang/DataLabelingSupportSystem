namespace DTOs.Requests
{
    /// <summary>
    /// Request model for submitting a review of an assignment.
    /// </summary>
    public class ReviewRequest
    {
        /// <summary>
        /// The unique identifier of the assignment being reviewed.
        /// </summary>
        /// <example>101</example>
        public int AssignmentId { get; set; }

        /// <summary>
        /// Indicates whether the assignment is approved.
        /// </summary>
        /// <example>true</example>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Optional comment providing feedback on the review.
        /// </summary>
        /// <example>Great work, annotations are accurate.</example>
        public string? Comment { get; set; }

        /// <summary>
        /// The category of error if the assignment is rejected (e.g., Missed Label, Wrong Class).
        /// </summary>
        /// <example>Missed Label</example>
        public string? ErrorCategory { get; set; }
    }
}
