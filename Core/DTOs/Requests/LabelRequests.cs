using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Requests
{
    /// <summary>
    /// Request model for creating a new label.
    /// </summary>
    public class CreateLabelRequest
    {
        /// <summary>
        /// The unique identifier of the project the label belongs to.
        /// </summary>
        /// <example>1</example>
        [Required]
        public int ProjectId { get; set; }

        /// <summary>
        /// The name of the label.
        /// </summary>
        /// <example>Pedestrian</example>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The color of the label in hex format. Defaults to "#FFFFFF".
        /// </summary>
        /// <example>#FF0000</example>
        public string Color { get; set; } = "#FFFFFF";

        /// <summary>
        /// Optional guidelines or description for the label.
        /// </summary>
        /// <example>Mark all pedestrians visible in the frame.</example>
        public string? GuideLine { get; set; }
    }

    /// <summary>
    /// Request model for updating an existing label.
    /// </summary>
    public class UpdateLabelRequest
    {
        /// <summary>
        /// The new name of the label.
        /// </summary>
        /// <example>Vehicle</example>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The new color of the label in hex format.
        /// </summary>
        /// <example>#00FF00</example>
        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// The new guidelines for the label.
        /// </summary>
        /// <example>Mark all types of vehicles.</example>
        public string? GuideLine { get; set; }
    }
}
