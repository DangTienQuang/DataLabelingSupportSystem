namespace DTOs.Responses
{
    /// <summary>
    /// Response model representing a label class.
    /// </summary>
    public class LabelResponse
    {
        /// <summary>
        /// The unique identifier of the label.
        /// </summary>
        /// <example>1</example>
        public int Id { get; set; }

        /// <summary>
        /// The name of the label.
        /// </summary>
        /// <example>Car</example>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The color of the label in hex format.
        /// </summary>
        /// <example>#0000FF</example>
        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// Guidelines or description for the label.
        /// </summary>
        /// <example>Mark all cars including suvs.</example>
        public string? GuideLine { get; set; }
    }
}
