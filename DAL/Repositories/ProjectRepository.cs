using DAL.Interfaces;
using Core.Entities;
using Core.Constants;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
        public async Task<List<DataItem>> GetProjectDataItemsAsync(int projectId)
        {
            return await _context.DataItems
                                 .Where(d => d.ProjectId == projectId)
                                 .Select(d => new DataItem { Id = d.Id, BucketId = d.BucketId })
                                 .ToListAsync();
        }
        public async Task<List<DataItem>> GetDataItemsByBucketIdAsync(int projectId, int bucketId)
        {
            return await _context.DataItems
                                 .Where(d => d.ProjectId == projectId && d.BucketId == bucketId)
                                 .ToListAsync();
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
                .Include(p => p.DataItems)
                    .ThenInclude(d => d.Assignments)
                        .ThenInclude(a => a.Reviewer)
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
            var annotations = await _context.Annotations
                .Where(a => a.Assignment.ProjectId == projectId
                         && a.Assignment.Status == TaskStatusConstants.Approved)
                .Select(a => a.DataJSON)
                .ToListAsync();

            var labelCounts = new Dictionary<int, int>();

            foreach (var json in annotations)
            {
                if (string.IsNullOrEmpty(json)) continue;

                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("annotations", out var annotationsArray)
                        && annotationsArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in annotationsArray.EnumerateArray())
                        {
                            if (item.TryGetProperty("labelId", out var labelIdProp)
                                && labelIdProp.TryGetInt32(out int labelId))
                            {
                                if (labelCounts.ContainsKey(labelId))
                                    labelCounts[labelId]++;
                                else
                                    labelCounts[labelId] = 1;
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    continue;
                }
            }

            return labelCounts;
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