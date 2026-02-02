using DAL.Interfaces;
using DTOs.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        public ProjectRepository(ApplicationDbContext context) : base(context) { }

        public new async Task<Project?> GetByIdAsync(object id)
        {
            return await _context.Projects
                .Include(p => p.Manager)
                .Include(p => p.LabelClasses)
                .Include(p => p.ChecklistItems)
                .FirstOrDefaultAsync(p => p.Id == (int)id);
        }

        public async Task<Project?> GetProjectWithDetailsAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.Manager)
                .Include(p => p.LabelClasses)
                .Include(p => p.ChecklistItems)
                .Include(p => p.DataItems)
                    .ThenInclude(d => d.Assignments)
                        .ThenInclude(a => a.Annotator)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Project?> GetProjectForExportAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.LabelClasses)
                .Include(p => p.DataItems)
                    .ThenInclude(d => d.Assignments)
                        .ThenInclude(a => a.Annotations)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Project?> GetProjectWithStatsDataAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.LabelClasses)
                .Include(p => p.DataItems)
                    .ThenInclude(d => d.Assignments)
                        .ThenInclude(a => a.Annotator)
                 .Include(p => p.DataItems)
                    .ThenInclude(d => d.Assignments)
                        .ThenInclude(a => a.ReviewLogs)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Dictionary<int, int>> GetProjectLabelCountsAsync(int projectId)
        {
            return await _context.Annotations
                .Where(a => a.Assignment.ProjectId == projectId && a.ClassId.HasValue)
                .GroupBy(a => a.ClassId.Value)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassId, x => x.Count);
        }

        public async Task<List<Project>> GetProjectsByManagerIdAsync(string managerId)
        {
            return await _context.Projects
                .Where(p => p.ManagerId == managerId)
                .Select(p => new Project
                {
                    Id = p.Id,
                    Name = p.Name,
                    Deadline = p.Deadline,
                    TotalBudget = p.TotalBudget,
                    DataItems = p.DataItems.Select(d => new DataItem
                    {
                        Status = d.Status,
                        Assignments = d.Assignments.Select(a => new Assignment
                        {
                            Status = a.Status,
                            AnnotatorId = a.AnnotatorId
                        }).ToList()
                    }).ToList()
                })
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        public async Task<List<Project>> GetProjectsByAnnotatorAsync(string annotatorId)
        {
            return await _context.Assignments
                .Where(a => a.AnnotatorId == annotatorId)
                .Include(a => a.Project)
                .Select(a => a.Project)
                .Distinct()
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }
    }
}