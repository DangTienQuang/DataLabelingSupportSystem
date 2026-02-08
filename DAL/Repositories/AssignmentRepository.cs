using Core.DTOs.Responses;
using DAL.Interfaces;
using Core.Constants;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class AssignmentRepository : Repository<Assignment>, IAssignmentRepository
    {
        private ApplicationDbContext AppContext => (ApplicationDbContext)_context;

        public AssignmentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Assignment>> GetAssignmentsByBucketAsync(int projectId, int bucketId, string userId)
        {
            return await AppContext.Assignments
                .Include(a => a.DataItem)
                .Include(a => a.Annotations)
                .Where(a => a.DataItem.ProjectId == projectId &&
                            a.DataItem.BucketId == bucketId &&
                            a.AnnotatorId == userId)
                .OrderBy(a => a.DataItem.Id)
                .ToListAsync();
        }
        public async Task<int> CountActiveTasksAsync(int projectId)
        {
            return await AppContext.Assignments
                .CountAsync(a => a.ProjectId == projectId &&
                                 (a.Status == TaskStatusConstants.Submitted ||
                                  a.Status == TaskStatusConstants.Approved));
        }

        public async Task ResetAssignmentsByProjectAsync(int projectId, string reason)
        {
            var assignments = await AppContext.Assignments
                .Where(a => a.ProjectId == projectId &&
                            (a.Status == TaskStatusConstants.Submitted ||
                             a.Status == TaskStatusConstants.Approved))
                .ToListAsync();

            if (!assignments.Any()) return;
            foreach (var task in assignments)
            {
                task.Status = TaskStatusConstants.Rejected;
                AppContext.ReviewLogs.Add(new ReviewLog
                {
                    AssignmentId = task.Id,
                    ReviewerId = task.Project?.ManagerId ?? "System",
                    Verdict = "System Reset",
                    Comment = $"AUTO: {reason}. Please check labels.",
                    CreatedAt = DateTime.UtcNow,
                    ScorePenalty = 0
                });
            }

            var dataItemIds = assignments.Select(a => a.DataItemId).Distinct().ToList();
            var dataItems = await AppContext.DataItems
                .Where(d => dataItemIds.Contains(d.Id))
                .ToListAsync();

            foreach (var item in dataItems)
            {
                item.Status = TaskStatusConstants.New;
            }

            await AppContext.SaveChangesAsync();
        }
        public async Task<List<Assignment>> GetAssignmentsForReviewerAsync(int projectId, string reviewerId)
        {
            return await AppContext.Assignments
                .Include(a => a.DataItem)
                .Include(a => a.Project)
                    .ThenInclude(p => p.LabelClasses)
                .Include(a => a.Annotations)
                .Where(a => a.ProjectId == projectId &&
                            a.Status == TaskStatusConstants.Submitted)
                .OrderBy(a => a.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<Assignment>> GetAssignmentsByAnnotatorAsync(string annotatorId, int projectId = 0, string? status = null)
        {
            var query = AppContext.Assignments
                .Include(a => a.DataItem)
                .Include(a => a.Project)
                    .ThenInclude(p => p.LabelClasses)
                .Include(a => a.Annotations)
                .Include(a => a.ReviewLogs)
                .Where(a => a.AnnotatorId == annotatorId);

            if (projectId > 0)
            {
                query = query.Where(a => a.ProjectId == projectId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status == status);
            }
            return await query.OrderBy(a => a.Id).ToListAsync();
        }

        public async Task<List<DataItem>> GetUnassignedDataItemsAsync(int projectId, int quantity)
        {
            return await AppContext.DataItems
                .Where(d => d.ProjectId == projectId && d.Status == TaskStatusConstants.New)
                .Take(quantity)
                .ToListAsync();
        }

        public async Task<Assignment?> GetAssignmentWithDetailsAsync(int assignmentId)
        {
            return await AppContext.Assignments
                .Include(a => a.DataItem)
                .Include(a => a.Project)
                    .ThenInclude(p => p.LabelClasses)
                .Include(a => a.Annotations)
                .Include(a => a.ReviewLogs)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);
        }

        public async Task<AnnotatorStatsResponse> GetAnnotatorStatsAsync(string annotatorId)
        {
            var rawStats = await AppContext.Assignments
                .Where(a => a.AnnotatorId == annotatorId)
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var stats = new AnnotatorStatsResponse();
            foreach (var item in rawStats)
            {
                string status = item.Status?.Trim() ?? "";
                int count = item.Count;
                stats.TotalAssigned += count;
                if (string.Equals(status, TaskStatusConstants.Submitted, StringComparison.OrdinalIgnoreCase)) stats.Submitted += count;
                else if (string.Equals(status, TaskStatusConstants.Rejected, StringComparison.OrdinalIgnoreCase)) stats.Rejected += count;
                else if (string.Equals(status, TaskStatusConstants.Approved, StringComparison.OrdinalIgnoreCase)) stats.Completed += count;
                else stats.Pending += count;
            }
            return stats;
        }
    }
}