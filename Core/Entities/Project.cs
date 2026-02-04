namespace Core.Entities
{
    public class Project
    {
        public int Id { get; set; }
        public string ManagerId { get; set; }
        public virtual User Manager { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal PricePerLabel { get; set; }
        public decimal TotalBudget { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime CreatedDate { get; set; }
        public string AllowGeometryTypes { get; set; }
        public string AnnotationGuide { get; set; }
        public int MaxTaskDurationHours { get; set; } = 24;

        public int PenaltyUnit { get; set; } = 10;

        public virtual ICollection<ReviewChecklistItem> ChecklistItems { get; set; } = new List<ReviewChecklistItem>();

        public virtual ICollection<LabelClass> LabelClasses { get; set; } = new List<LabelClass>();
        public virtual ICollection<DataItem> DataItems { get; set; } = new List<DataItem>();
    }
}