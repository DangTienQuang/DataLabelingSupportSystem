using System.ComponentModel.DataAnnotations;

namespace DTOs.Requests
{
    /// <summary>
    /// Request model for assigning tasks to an annotator.
    /// </summary>
    public class AssignTaskRequest
    {
        /// <summary>
        /// The unique identifier of the project.
        /// </summary>
        /// <example>5</example>
        [Required]
        public int ProjectId { get; set; }

        /// <summary>
        /// The unique identifier (GUID) of the annotator.
        /// </summary>
        /// <example>d290f1ee-6c54-4b01-90e6-d701748f0851</example>
        [Required]
        public string AnnotatorId { get; set; } = string.Empty;

        /// <summary>
        /// The number of tasks (data items) to assign.
        /// </summary>
        /// <example>10</example>
        [Required]
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Request model for submitting annotations for an assignment.
    /// </summary>
    public class SubmitAnnotationRequest
    {
        /// <summary>
        /// The unique identifier of the assignment.
        /// </summary>
        /// <example>202</example>
        [Required]
        public int AssignmentId { get; set; }

        /// <summary>
        /// A list of annotation details containing the label class and annotation value.
        /// </summary>
        public List<AnnotationDetail> Annotations { get; set; } = new List<AnnotationDetail>();
    }

    /// <summary>
    /// Detail of a single annotation within a submission.
    /// </summary>
    public class AnnotationDetail
    {
        /// <summary>
        /// The identifier of the label class used for this annotation.
        /// </summary>
        /// <example>3</example>
        public int LabelClassId { get; set; }

        /// <summary>
        /// The JSON string representation of the annotation value (e.g., coordinates).
        /// </summary>
        /// <example>{ "x": 10, "y": 20, "width": 100, "height": 100 }</example>
        public string ValueJson { get; set; } = string.Empty;
    }
}
