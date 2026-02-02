using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Requests
{
    public class CreateProjectRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal PricePerLabel { get; set; }

        public decimal TotalBudget { get; set; }

        public DateTime Deadline { get; set; }
        public int PenaltyUnit { get; set; } = 10;
        public string AllowGeometryTypes { get; set; } = "Rectangle";

        public string? AnnotationGuide { get; set; }

        public List<ChecklistItemRequest>? ReviewChecklist { get; set; }

        public int MaxTaskDurationHours { get; set; } = 24;

        public List<LabelRequest> LabelClasses { get; set; } = new List<LabelRequest>();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class UpdateProjectRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal PricePerLabel { get; set; }
        public int PenaltyUnit { get; set; }
        public decimal TotalBudget { get; set; }

        public string? AnnotationGuide { get; set; }

        public List<ChecklistItemRequest>? ReviewChecklist { get; set; }

        public int? MaxTaskDurationHours { get; set; }

        public DateTime Deadline { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class LabelRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = "#000000";

        public string GuideLine { get; set; } = string.Empty;

        public List<string>? Checklist { get; set; }
    }

    public class ImportDataRequest
    {
        public List<string> StorageUrls { get; set; } = new List<string>();
    }

    public class ChecklistItemRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
    }
}