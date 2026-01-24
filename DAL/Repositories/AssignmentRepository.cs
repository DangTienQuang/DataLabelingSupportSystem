using DAL.Interfaces;
using DTOs.Entities;
using DTOs.Responses;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class AssignmentRepository : Repository<Assignment>, IAssignmentRepository
    {
        private readonly ApplicationDbContext _context;

        public AssignmentRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Assignment>> GetAssignmentsByAnnotatorAsync(string annotatorId, int projectId = 0, string? status = null)
        {
            var query = _context.Assignments
                .Include(a => a.DataItem)
                .Include(a => a.Project)
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

            return await query.OrderByDescending(a => a.AssignedDate).ToListAsync();
        }

        public async Task<List<Assignment>> GetAssignmentsForReviewerAsync(int projectId)
        {
            return await _context.Assignments
                .Include(a => a.DataItem)
                .Include(a => a.Project)
                    .ThenInclude(p => p.LabelClasses)
                .Include(a => a.Annotations)
                .Where(a => a.ProjectId == projectId && a.Status == "Submitted")
                .ToListAsync();
        }

        public async Task<Assignment?> GetAssignmentWithDetailsAsync(int assignmentId)
        {
            return await _context.Assignments
                .Include(a => a.DataItem)
                .Include(a => a.Project)
                    .ThenInclude(p => p.LabelClasses)
                .Include(a => a.Annotations)
                .Include(a => a.ReviewLogs)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);
        }
        public async Task<List<DataItem>> GetUnassignedDataItemsAsync(int projectId, int quantity)
        {
            return await _context.DataItems
                .Where(d => d.ProjectId == projectId && d.Status == "New")
                .Take(quantity)
                .ToListAsync();
        }

        public async Task<AnnotatorStatsResponse> GetAnnotatorStatsAsync(string annotatorId)
        {
            var rawStats = await _context.Assignments
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

                if (string.Equals(status, "Submitted", StringComparison.OrdinalIgnoreCase))
                {
                    stats.Submitted += count;
                }
                else if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    stats.Rejected += count;
                }
                else if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(status, "Done", StringComparison.OrdinalIgnoreCase))
                {
                    stats.Completed += count;
                }
                else
                {
                    stats.Pending += count;
                }
            }

            return stats;
        }
    }
}