using System.ComponentModel.DataAnnotations;

namespace DTOs.Requests
{
    /// <summary>
    /// Request model for creating a new project.
    /// </summary>
    public class CreateProjectRequest
    {
        /// <summary>
        /// The name of the project.
        /// </summary>
        /// <example>Traffic Sign Detection</example>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A description of the project.
        /// </summary>
        /// <example>Detect traffic signs in urban environments.</example>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The price paid per label.
        /// </summary>
        /// <example>0.05</example>
        public decimal PricePerLabel { get; set; }

        /// <summary>
        /// The total budget allocated for the project.
        /// </summary>
        /// <example>1000.00</example>
        public decimal TotalBudget { get; set; }

        /// <summary>
        /// The deadline for the project.
        /// </summary>
        /// <example>2023-12-31T23:59:59</example>
        public DateTime Deadline { get; set; }

        /// <summary>
        /// The allowed geometry types for annotations (e.g., Rectangle, Polygon). Defaults to "Rectangle".
        /// </summary>
        /// <example>Rectangle</example>
        public string AllowGeometryTypes { get; set; } = "Rectangle";

        /// <summary>
        /// A list of initial label classes for the project.
        /// </summary>
        public List<LabelRequest> LabelClasses { get; set; } = new List<LabelRequest>();
    }

    /// <summary>
    /// Request model for updating a project.
    /// </summary>
    public class UpdateProjectRequest
    {
        /// <summary>
        /// The new name of the project.
        /// </summary>
        /// <example>Updated Project Name</example>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The new description of the project.
        /// </summary>
        /// <example>Updated description.</example>
        public string? Description { get; set; }

        /// <summary>
        /// The new price per label.
        /// </summary>
        /// <example>0.10</example>
        public decimal PricePerLabel { get; set; }

        /// <summary>
        /// The new total budget.
        /// </summary>
        /// <example>2000.00</example>
        public decimal TotalBudget { get; set; }

        /// <summary>
        /// The new deadline for the project.
        /// </summary>
        /// <example>2024-06-30T23:59:59</example>
        public DateTime Deadline { get; set; }
    }

    /// <summary>
    /// Model defining a label class within a project creation request.
    /// </summary>
    public class LabelRequest
    {
        /// <summary>
        /// The name of the label.
        /// </summary>
        /// <example>Stop Sign</example>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The color of the label in hex format. Defaults to "#000000".
        /// </summary>
        /// <example>#FF0000</example>
        public string Color { get; set; } = "#000000";

        /// <summary>
        /// Guidelines for using this label.
        /// </summary>
        /// <example>Mark only red octagonal stop signs.</example>
        public string GuideLine { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for importing data items into a project.
    /// </summary>
    public class ImportDataRequest
    {
        /// <summary>
        /// A list of URLs pointing to the data items (images, etc.) to be imported.
        /// </summary>
        /// <example>["https://example.com/image1.jpg", "https://example.com/image2.jpg"]</example>
        public List<string> StorageUrls { get; set; } = new List<string>();
    }
}
