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

        public async Task<List<Assignment>> GetAssignmentsForReviewerAsync(int projectId, string reviewerId)
        {
            return await AppContext.Assignments
                .Include(a => a.DataItem)
                .Include(a => a.Project)
                    .ThenInclude(p => p.LabelClasses)
                .Include(a => a.Annotations)
                .Where(a => a.ProjectId == projectId && a.ReviewerId == reviewerId && a.Status == TaskStatusConstants.Submitted)
                .OrderBy(a => a.Id)
                .ToListAsync();
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
                else if (string.Equals(status, TaskStatusConstants.Approved, StringComparison.OrdinalIgnoreCase)) stats.Completed += count; // Approved chính là Completed
                else stats.Pending += count;
            }
            return stats;
        }
    }
}