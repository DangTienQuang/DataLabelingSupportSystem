using BLL.Interfaces;
using DAL.Interfaces;
using DTOs.Constants;
using DTOs.Entities;
using System.Text.Json;
using System.Text;
using Core.DTOs.Requests;
using Core.DTOs.Responses;

namespace BLL.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRepository<UserProjectStat> _statsRepo;
        private readonly IRepository<Invoice> _invoiceRepo;
        private readonly IAssignmentRepository _assignmentRepo;

        public ProjectService(
            IProjectRepository projectRepository,
            IUserRepository userRepository,
            IRepository<UserProjectStat> statsRepo,
            IRepository<Invoice> invoiceRepo,
            IAssignmentRepository assignmentRepo)
        {
            _projectRepository = projectRepository;
            _userRepository = userRepository;
            _statsRepo = statsRepo;
            _invoiceRepo = invoiceRepo;
            _assignmentRepo = assignmentRepo;
        }

        public async Task<ProjectDetailResponse> CreateProjectAsync(string managerId, CreateProjectRequest request)
        {
            var manager = await _userRepository.GetByIdAsync(managerId);
            if (manager == null) throw new Exception("User not found");

            if (manager.Role != UserRoles.Manager && manager.Role != UserRoles.Admin)
                throw new Exception("Only Manager or Admin can create projects.");

            var startDate = request.StartDate ?? DateTime.UtcNow;
            var endDate = request.EndDate ?? DateTime.UtcNow.AddDays(30);

            int penaltyUnit = request.PenaltyUnit > 0 ? request.PenaltyUnit : 10;

            var project = new Project
            {
                ManagerId = managerId,
                Name = request.Name,
                Description = request.Description,
                PricePerLabel = request.PricePerLabel,
                TotalBudget = request.TotalBudget,
                StartDate = startDate,
                EndDate = endDate,
                Deadline = endDate,
                CreatedDate = DateTime.UtcNow,
                AllowGeometryTypes = request.AllowGeometryTypes ?? "Rectangle",
                AnnotationGuide = request.AnnotationGuide,
                MaxTaskDurationHours = request.MaxTaskDurationHours,
                PenaltyUnit = penaltyUnit 
            };

            if (request.ReviewChecklist != null && request.ReviewChecklist.Any())
            {
                foreach (var item in request.ReviewChecklist)
                {
                    project.ChecklistItems.Add(new ReviewChecklistItem
                    {
                        Code = item.Code,
                        Description = item.Description,
                        Weight = item.Weight,
                        IsCritical = item.Weight >= 3
                    });
                }
            }

            foreach (var label in request.LabelClasses)
            {
                project.LabelClasses.Add(new LabelClass
                {
                    Name = label.Name,
                    Color = label.Color,
                    GuideLine = label.GuideLine,
                    DefaultChecklist = (label.Checklist != null && label.Checklist.Any())
                                            ? JsonSerializer.Serialize(label.Checklist)
                                            : "[]"
                });
            }

            await _projectRepository.AddAsync(project);
            await _projectRepository.SaveChangesAsync();

            return new ProjectDetailResponse
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                PricePerLabel = project.PricePerLabel,
                TotalBudget = project.TotalBudget,
                Deadline = project.Deadline,
                ManagerId = project.ManagerId,
                ManagerName = manager.FullName,
                ManagerEmail = manager.Email,
                Labels = project.LabelClasses.Select(l => new LabelResponse
                {
                    Id = l.Id,
                    Name = l.Name,
                    Color = l.Color,
                    GuideLine = l.GuideLine,
                    Checklist = !string.IsNullOrEmpty(l.DefaultChecklist)
                                ? JsonSerializer.Deserialize<List<string>>(l.DefaultChecklist, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<string>()
                                : new List<string>()
                }).ToList(),
                TotalDataItems = 0,
                ProcessedItems = 0
            };
        }

        public async Task UpdateProjectAsync(int projectId, UpdateProjectRequest request)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null) throw new Exception("Project not found");

            project.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Description)) project.Description = request.Description;

            project.PricePerLabel = request.PricePerLabel;
            project.TotalBudget = request.TotalBudget;
            project.Deadline = request.Deadline;
            if (request.StartDate.HasValue) project.StartDate = request.StartDate.Value;
            if (request.EndDate.HasValue) project.EndDate = request.EndDate.Value;

            if (request.AnnotationGuide != null)
            {
                project.AnnotationGuide = request.AnnotationGuide;
            }

            if (request.MaxTaskDurationHours.HasValue)
            {
                project.MaxTaskDurationHours = request.MaxTaskDurationHours.Value;
            }

            if (request.PenaltyUnit > 0)
            {
                project.PenaltyUnit = request.PenaltyUnit;
            }

            if (request.ReviewChecklist != null)
            {

                project.ChecklistItems.Clear();

                foreach (var item in request.ReviewChecklist)
                {
                    project.ChecklistItems.Add(new ReviewChecklistItem
                    {
                        ProjectId = projectId,
                        Code = item.Code,
                        Description = item.Description,
                        Weight = item.Weight,
                        IsCritical = item.Weight >= 3
                    });
                }
            }

            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();
        }

        public async Task<List<AnnotatorProjectStatsResponse>> GetAssignedProjectsAsync(string annotatorId)
        {
            var projects = await _projectRepository.GetProjectsByAnnotatorAsync(annotatorId);
            var result = new List<AnnotatorProjectStatsResponse>();

            foreach (var p in projects)
            {
                var myAssignments = p.DataItems
                    .SelectMany(d => d.Assignments)
                    .Where(a => a.AnnotatorId == annotatorId)
                    .ToList();
                var total = myAssignments.Count;
                var completed = myAssignments.Count(a => a.Status == TaskStatusConstants.Submitted || a.Status == TaskStatusConstants.Approved);
                var nextTask = myAssignments
                    .OrderByDescending(a => a.Status == TaskStatusConstants.InProgress)
                    .ThenByDescending(a => a.Status == TaskStatusConstants.Rejected)
                    .ThenByDescending(a => a.Status == TaskStatusConstants.Assigned)
                    .FirstOrDefault(a => a.Status == TaskStatusConstants.InProgress || a.Status == TaskStatusConstants.Rejected || a.Status == TaskStatusConstants.Assigned);

                string status = "Active";
                if (DateTime.UtcNow > p.Deadline) status = "Expired";
                else if (total > 0 && total == completed) status = "Completed";

                result.Add(new AnnotatorProjectStatsResponse
                {
                    ProjectId = p.Id,
                    ProjectName = p.Name,
                    TotalImages = total,
                    CompletedImages = completed,
                    Status = status,
                    Deadline = p.Deadline,
                    AssignmentId = nextTask?.Id
                });
            }

            return result;
        }

        public async Task ImportDataItemsAsync(int projectId, List<string> storageUrls)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null) throw new Exception("Project not found");

            foreach (var url in storageUrls)
            {
                project.DataItems.Add(new DataItem
                {
                    ProjectId = projectId,
                    StorageUrl = url,
                    Status = TaskStatusConstants.New,
                    MetaData = "{}",
                    UploadedDate = DateTime.UtcNow
                });
            }

            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();
        }

        public async Task<ProjectDetailResponse?> GetProjectDetailsAsync(int projectId)
        {
            var project = await _projectRepository.GetProjectWithDetailsAsync(projectId);
            if (project == null) return null;

            var allAssignments = project.DataItems.SelectMany(d => d.Assignments).ToList();
            int total = project.DataItems.Count;
            int done = project.DataItems.Count(d => d.Status == TaskStatusConstants.Approved);
            int progressPercent = (total > 0) ? (int)((double)done / total * 100) : 0;

            var members = allAssignments
                .Where(a => a.Annotator != null)
                .GroupBy(a => a.AnnotatorId)
                .Select(g => new MemberResponse
                {
                    Id = g.Key,
                    FullName = g.First().Annotator.FullName ?? g.First().Annotator.Email,
                    Email = g.First().Annotator.Email,
                    Role = g.First().Annotator.Role,
                    TasksAssigned = g.Count(),
                    TasksCompleted = g.Count(a => a.Status == TaskStatusConstants.Approved),
                    Progress = g.Count() > 0
                        ? Math.Round((decimal)g.Count(a => a.Status == TaskStatusConstants.Approved) / g.Count() * 100, 2)
                        : 0
                }).ToList();

            return new ProjectDetailResponse
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                PricePerLabel = project.PricePerLabel,
                TotalBudget = project.TotalBudget,
                Deadline = project.Deadline,
                ManagerId = project.ManagerId,
                ManagerName = project.Manager?.FullName ?? "Unknown",
                ManagerEmail = project.Manager?.Email ?? "",
                Labels = project.LabelClasses.Select(l => new LabelResponse
                {
                    Id = l.Id,
                    Name = l.Name,
                    Color = l.Color,
                    GuideLine = l.GuideLine,
                    Checklist = !string.IsNullOrEmpty(l.DefaultChecklist)
                                ? JsonSerializer.Deserialize<List<string>>(l.DefaultChecklist, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<string>()
                                : new List<string>()
                }).ToList(),
                TotalDataItems = total,
                ProcessedItems = done,
                Progress = progressPercent,
                Members = members
            };
        }

        public async Task<List<ProjectSummaryResponse>> GetProjectsByManagerAsync(string managerId)
        {
            var projects = await _projectRepository.GetProjectsByManagerIdAsync(managerId);

            return projects.Select(p => new ProjectSummaryResponse
            {
                Id = p.Id,
                Name = p.Name,
                Deadline = p.Deadline,
                TotalDataItems = p.DataItems.Count,
                Status = DateTime.UtcNow > p.Deadline ? "Expired" : "Active",
                Progress = p.DataItems.Count > 0
                    ? (decimal)p.DataItems.Count(d =>
                        d.Status == TaskStatusConstants.Approved ||
                        (d.Assignments != null && d.Assignments.Any(a => a.Status == TaskStatusConstants.Approved))
                      ) / p.DataItems.Count * 100
                    : 0,
                TotalMembers = p.DataItems
                                .SelectMany(d => d.Assignments)
                                .Select(a => a.AnnotatorId)
                                .Distinct()
                                .Count()
            }).ToList();
        }

        public async Task DeleteProjectAsync(int projectId)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null) throw new Exception("Project not found");

            _projectRepository.Delete(project);
            await _projectRepository.SaveChangesAsync();
        }

        public async Task GenerateInvoicesAsync(int projectId)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null) throw new Exception("Project not found");

            var allStats = await _statsRepo.GetAllAsync();
            var projectStats = allStats.Where(s => s.ProjectId == projectId).ToList();

            foreach (var stat in projectStats)
            {
                if (stat.EstimatedEarnings > 0)
                {
                    var invoice = new Invoice
                    {
                        UserId = stat.UserId,
                        ProjectId = projectId,
                        TotalLabels = stat.TotalApproved,
                        UnitPrice = project.PricePerLabel,
                        TotalAmount = stat.EstimatedEarnings,
                        StartDate = project.StartDate ?? DateTime.UtcNow.AddMonths(-1),
                        EndDate = project.EndDate ?? DateTime.UtcNow,
                        Status = "Pending",
                        CreatedDate = DateTime.UtcNow
                    };
                    await _invoiceRepo.AddAsync(invoice);
                }
            }
            await _invoiceRepo.SaveChangesAsync();
        }

        public async Task<byte[]> ExportProjectDataAsync(int projectId, string userId)
        {
            var project = await _projectRepository.GetProjectForExportAsync(projectId);
            if (project == null) throw new Exception("Project not found");
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");
            if (user.Role != UserRoles.Admin && project.ManagerId != userId)
                throw new Exception("Unauthorized to export this project.");

            var dataItems = project.DataItems
                .Where(d => d.Status == TaskStatusConstants.Approved)
                .Select(d => new
                {
                    DataItemId = d.Id,
                    StorageUrl = d.StorageUrl,
                    Annotations = d.Assignments
                        .Where(a => a.Status == TaskStatusConstants.Approved)
                        .SelectMany(a => a.Annotations)
                        .Select(an => new
                        {
                            ClassId = an.ClassId,
                            ClassName = an.ClassId.HasValue
                                ? project.LabelClasses.FirstOrDefault(l => l.Id == an.ClassId)?.Name
                                : "See DataJSON",
                            Value = !string.IsNullOrEmpty(an.DataJSON)
                                ? JsonDocument.Parse(an.DataJSON).RootElement
                                : (!string.IsNullOrEmpty(an.Value) ? JsonDocument.Parse(an.Value).RootElement : default)
                        })
                        .ToList()
                })
                .ToList();

            var exportData = new
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                ExportedAt = DateTime.UtcNow,
                TotalImages = dataItems.Count,
                Data = dataItems
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
            return Encoding.UTF8.GetBytes(json);
        }

        public async Task<ProjectStatisticsResponse> GetProjectStatisticsAsync(int projectId)
        {
            var project = await _projectRepository.GetProjectWithStatsDataAsync(projectId);
            if (project == null) throw new Exception("Project not found");

            var allStats = await _statsRepo.GetAllAsync();
            var projectUserStats = allStats.Where(s => s.ProjectId == projectId).ToList();

            var allAssignments = project.DataItems.SelectMany(d => d.Assignments).ToList();
            var allReviewLogs = allAssignments.SelectMany(a => a.ReviewLogs ?? new List<ReviewLog>()).ToList();
            var totalReviewed = allReviewLogs.Count;
            var totalRejectedLogs = allReviewLogs.Count(l => l.Verdict == "Rejected" || l.Verdict == "Reject");

            var stats = new ProjectStatisticsResponse
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                TotalItems = project.DataItems.Count,
                CompletedItems = project.DataItems.Count(d => d.Status == TaskStatusConstants.Approved),
                TotalAssignments = allAssignments.Count,
                PendingAssignments = allAssignments.Count(a => a.Status == TaskStatusConstants.Assigned || a.Status == TaskStatusConstants.InProgress),
                SubmittedAssignments = allAssignments.Count(a => a.Status == TaskStatusConstants.Submitted),
                ApprovedAssignments = allAssignments.Count(a => a.Status == TaskStatusConstants.Approved),
                RejectedAssignments = allAssignments.Count(a => a.Status == TaskStatusConstants.Rejected),
                RejectionRate = totalReviewed > 0
                    ? Math.Round((double)totalRejectedLogs / totalReviewed * 100, 2)
                    : 0,
                ErrorBreakdown = allReviewLogs
                    .Where(l => (l.Verdict == "Rejected" || l.Verdict == "Reject") && !string.IsNullOrEmpty(l.ErrorCategory))
                    .GroupBy(l => l.ErrorCategory!)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            if (stats.TotalItems > 0)
            {
                stats.ProgressPercentage = Math.Round((decimal)stats.CompletedItems / stats.TotalItems * 100, 2);
            }
            stats.AnnotatorPerformances = allAssignments
                .GroupBy(a => a.AnnotatorId)
                .Select(g =>
                {
                    var annotatorId = g.Key;
                    var userStat = projectUserStats.FirstOrDefault(s => s.UserId == annotatorId);

                    return new AnnotatorPerformance
                    {
                        AnnotatorId = annotatorId,
                        AnnotatorName = g.FirstOrDefault()?.Annotator?.FullName ?? "Unknown",
                        TasksAssigned = g.Count(),
                        TasksCompleted = g.Count(a => a.Status == TaskStatusConstants.Approved),
                        TasksRejected = g.Count(a => a.Status == TaskStatusConstants.Rejected),
                        AverageDurationSeconds = 0,
                        AverageQualityScore = userStat?.AverageQualityScore ?? 100,
                        TotalCriticalErrors = userStat?.TotalCriticalErrors ?? 0
                    };
                }).ToList();

            var labelCounts = await _projectRepository.GetProjectLabelCountsAsync(projectId);

            stats.LabelDistributions = project.LabelClasses.Select(lc => new LabelDistribution
            {
                ClassName = lc.Name,
                Count = labelCounts.ContainsKey(lc.Id) ? labelCounts[lc.Id] : 0
            }).ToList();

            return stats;
        }

        public async Task<ManagerStatsResponse> GetManagerStatsAsync(string managerId)
        {
            var projects = await _projectRepository.GetProjectsByManagerIdAsync(managerId);

            return new ManagerStatsResponse
            {
                TotalProjects = projects.Count,
                ActiveProjects = projects.Count(p => p.Deadline >= DateTime.UtcNow),
                TotalBudget = projects.Sum(p => p.TotalBudget),
                TotalDataItems = projects.Sum(p => p.DataItems.Count),
                TotalMembers = projects.SelectMany(p => p.DataItems)
                                       .SelectMany(d => d.Assignments)
                                       .Select(a => a.AnnotatorId)
                                       .Distinct()
                                       .Count()
            };
        }
    }
}