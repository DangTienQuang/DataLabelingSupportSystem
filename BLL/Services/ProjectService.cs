    using BLL.Interfaces;
    using DAL.Interfaces;
    using Core.Constants;
    using Core.Entities;
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
            private readonly IActivityLogRepository _activityLogRepo;

            public ProjectService(
                IProjectRepository projectRepository,
                IUserRepository userRepository,
                IRepository<UserProjectStat> statsRepo,
                IRepository<Invoice> invoiceRepo,
                IAssignmentRepository assignmentRepo,
                IActivityLogRepository activityLogRepo)
            {
                _projectRepository = projectRepository;
                _userRepository = userRepository;
                _statsRepo = statsRepo;
                _invoiceRepo = invoiceRepo;
                _assignmentRepo = assignmentRepo;
                _activityLogRepo = activityLogRepo;
            }

            public async Task<int> UploadDirectDataItemsAsync(int projectId, List<Microsoft.AspNetCore.Http.IFormFile> files, string webRootPath)
            {
                var project = await _projectRepository.GetByIdAsync(projectId);
                if (project == null) throw new Exception("Project not found");

                if (files == null || !files.Any()) throw new Exception("No files provided.");

                var uploadFolder = Path.Combine(webRootPath, "uploads", projectId.ToString());
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var storageUrls = new List<string>();

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var fileUrl = $"/uploads/{projectId}/{fileName}";
                        storageUrls.Add(fileUrl);
                    }
                }

                await ImportDataItemsAsync(projectId, storageUrls);

                return storageUrls.Count;
            }

        public async Task<ProjectDetailResponse> CreateProjectAsync(string managerId, CreateProjectRequest request)
        {
            var manager = await _userRepository.GetByIdAsync(managerId);
            if (manager == null) throw new Exception("User not found");

            if (manager.Role != UserRoles.Manager && manager.Role != UserRoles.Admin)
                throw new Exception("Only Manager or Admin can create projects.");

            var startDate = request.StartDate ?? DateTime.UtcNow;
            var deadline = request.Deadline ?? DateTime.UtcNow.AddDays(30);
            var endDate = request.EndDate ?? deadline;

            int penaltyUnit = request.PenaltyUnit > 0 ? request.PenaltyUnit : 10;

            var project = new Project
            {
                ManagerId = managerId,
                Name = request.Name,
                Description = request.Description ?? "",
                PricePerLabel = request.PricePerLabel,
                TotalBudget = request.TotalBudget,
                StartDate = startDate,
                EndDate = endDate,
                Deadline = deadline,
                CreatedDate = DateTime.UtcNow,
                AllowGeometryTypes = request.AllowGeometryTypes ?? "Rectangle",
                AnnotationGuide = request.AnnotationGuide ?? "",
                MaxTaskDurationHours = request.MaxTaskDurationHours > 0 ? request.MaxTaskDurationHours : 24,
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

            await _activityLogRepo.AddAsync(new ActivityLog
            {
                UserId = managerId,
                ActionType = "CreateProject",
                EntityName = "Project",
                EntityId = project.Id.ToString(),
                Description = $"Created a new project: {project.Name}",
                Timestamp = DateTime.UtcNow
            });
            await _activityLogRepo.SaveChangesAsync();

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

            if (request.Deadline.HasValue) project.Deadline = request.Deadline.Value;
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

            await _activityLogRepo.AddAsync(new ActivityLog
            {
                UserId = project.ManagerId,
                ActionType = "UpdateProject",
                EntityName = "Project",
                EntityId = project.Id.ToString(),
                Description = $"Updated project details: {project.Name}",
                Timestamp = DateTime.UtcNow
            });
            await _activityLogRepo.SaveChangesAsync();
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
                int currentTotalItems = project.DataItems.Count;
                int bucketSize = 50;
                foreach (var url in storageUrls)
                {
                    currentTotalItems++;
                    int bucketId = (currentTotalItems - 1) / bucketSize + 1;

                    var dataItem = new DataItem
                    {
                        ProjectId = projectId,
                        StorageUrl = url,
                        UploadedDate = DateTime.UtcNow,
                        Status = TaskStatusConstants.New,
                        BucketId = bucketId,
                        MetaData = "{}",
                        Assignments = new List<Assignment>()
                    };
                    project.DataItems.Add(dataItem);
                }
                await _projectRepository.SaveChangesAsync();

                await _activityLogRepo.AddAsync(new ActivityLog
                {
                    UserId = project.ManagerId,
                    ActionType = "ImportData",
                    EntityName = "Project",
                    EntityId = project.Id.ToString(),
                    Description = $"Imported {storageUrls.Count} data items into project {project.Name}",
                    Timestamp = DateTime.UtcNow
                });
                await _activityLogRepo.SaveChangesAsync();
            }

            public async Task<ProjectDetailResponse?> GetProjectDetailsAsync(int projectId)
            {
                var project = await _projectRepository.GetProjectWithDetailsAsync(projectId);
                if (project == null) return null;

                var allAssignments = project.DataItems.SelectMany(d => d.Assignments).ToList();
                int total = project.DataItems.Count;
                int done = project.DataItems.Count(d => d.Status == TaskStatusConstants.Approved);
                int progressPercent = (total > 0) ? (int)((double)done / total * 100) : 0;

                var annotators = allAssignments
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

                var reviewers = allAssignments
                    .Where(a => a.Reviewer != null)
                    .GroupBy(a => a.ReviewerId)
                    .Select(g => new MemberResponse
                    {
                        Id = g.Key!,
                        FullName = g.First().Reviewer!.FullName ?? g.First().Reviewer!.Email,
                        Email = g.First().Reviewer!.Email,
                        Role = g.First().Reviewer!.Role,
                        TasksAssigned = g.Count(),
                        TasksCompleted = g.Count(a => a.Status == TaskStatusConstants.Approved || a.Status == TaskStatusConstants.Rejected),
                        Progress = g.Count() > 0
                            ? Math.Round((decimal)g.Count(a => a.Status == TaskStatusConstants.Approved || a.Status == TaskStatusConstants.Rejected) / g.Count() * 100, 2)
                            : 0
                    }).ToList();

                var members = annotators.Concat(reviewers)
                    .GroupBy(m => m.Id)
                    .Select(g => g.First())
                    .ToList();

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
                Status = DateTime.UtcNow > p.Deadline ? "Expired" :
                         (p.DataItems.Count == 0 || p.DataItems.All(d => d.Status == TaskStatusConstants.New) ? "New" : "Active"),

                Progress = p.DataItems.Count > 0
                    ? (decimal)p.DataItems.Count(d =>
                        d.Status == TaskStatusConstants.Approved ||
                        (d.Assignments != null && d.Assignments.Any(a => a.Status == TaskStatusConstants.Approved))
                    ) / p.DataItems.Count * 100
                    : 0,
                TotalMembers = p.DataItems
                                .SelectMany(d => d.Assignments)
                                .SelectMany(a => new[] { a.AnnotatorId, a.ReviewerId })
                                .Where(id => !string.IsNullOrEmpty(id))
                                .Distinct()
                                .Count()
            }).ToList();
        }
        public async Task DeleteProjectAsync(int projectId)
            {
                var project = await _projectRepository.GetByIdAsync(projectId);
                if (project == null) throw new Exception("Project not found");

                var managerId = project.ManagerId;
                var projectName = project.Name;

                _projectRepository.Delete(project);
                await _projectRepository.SaveChangesAsync();

                await _activityLogRepo.AddAsync(new ActivityLog
                {
                    UserId = managerId,
                    ActionType = "DeleteProject",
                    EntityName = "Project",
                    EntityId = projectId.ToString(),
                    Description = $"Deleted project: {projectName}",
                    Timestamp = DateTime.UtcNow
                });
                await _activityLogRepo.SaveChangesAsync();
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

                await _activityLogRepo.AddAsync(new ActivityLog
                {
                    UserId = project.ManagerId,
                    ActionType = "GenerateInvoices",
                    EntityName = "Project",
                    EntityId = projectId.ToString(),
                    Description = $"Generated invoices for project: {project.Name}",
                    Timestamp = DateTime.UtcNow
                });
                await _activityLogRepo.SaveChangesAsync();
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
                    .Select(d => new
                    {
                        DataItemId = d.Id,
                        StorageUrl = d.StorageUrl,
                        ItemStatus = d.Status,
                        Assignments = d.Assignments.Select(a => new
                        {
                            AssignmentId = a.Id,
                            Annotator = a.Annotator?.Email ?? "Unknown",
                            Reviewer = a.Reviewer?.Email ?? "None",
                            TaskStatus = a.Status,
                            AssignedDate = a.AssignedDate,
                            SubmittedAt = a.SubmittedAt,
                            ReviewComment = a.ReviewLogs?
                                .OrderByDescending(r => r.CreatedAt)
                                .FirstOrDefault()?.Comment,
                            Annotation = a.Annotations?
                                .OrderByDescending(an => an.CreatedAt)
                                .Select(an => new
                                {
                                    ClassId = an.ClassId,
                                    ClassName = an.ClassId.HasValue
                                        ? project.LabelClasses.FirstOrDefault(l => l.Id == an.ClassId)?.Name
                                        : "See DataJSON",
                                    Data = !string.IsNullOrEmpty(an.DataJSON)
                                        ? JsonDocument.Parse(an.DataJSON).RootElement
                                        : (!string.IsNullOrEmpty(an.Value) ? JsonDocument.Parse(an.Value).RootElement : default)
                                }).FirstOrDefault()
                        }).ToList()
                    })
                    .ToList();

                int totalItems = project.DataItems.Count;
                int completedItems = project.DataItems.Count(d => d.Status == TaskStatusConstants.Approved);
                double progressPercent = totalItems > 0 ? Math.Round((double)completedItems / totalItems * 100, 2) : 0;
                string currentStatus = DateTime.UtcNow > project.Deadline ? "Expired" : (totalItems > 0 && totalItems == completedItems ? "Completed" : "Active");

                var projectMembers = project.DataItems
                    .SelectMany(d => d.Assignments)
                    .Where(a => a.Annotator != null)
                    .Select(a => new { UserId = a.AnnotatorId, Role = a.Annotator.Role, Email = a.Annotator.Email })
                    .Distinct()
                    .ToList();

                var exportData = new
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    ExportedAt = DateTime.UtcNow,
                    Description = project.Description,
                    Status = currentStatus,
                    Deadline = project.Deadline,
                    Progress = progressPercent,
                    TotalDataItems = totalItems,
                    Labels = project.LabelClasses.Select(l => new { l.Id, l.Name, l.Color }),
                    Members = projectMembers,
                    TotalImages = dataItems.Count,
                    Data = dataItems
                };

                var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });

                await _activityLogRepo.AddAsync(new ActivityLog
                {
                    UserId = userId,
                    ActionType = "ExportProject",
                    EntityName = "Project",
                    EntityId = projectId.ToString(),
                    Description = $"Exported data for project: {project.Name}",
                    Timestamp = DateTime.UtcNow
                });
                await _activityLogRepo.SaveChangesAsync();

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

                var totalFirstPassCorrect = projectUserStats
                    .Where(s => s.TotalFirstPassCorrect > 0)
                    .Sum(s => s.TotalFirstPassCorrect);
                var totalItems = project.DataItems.Count;
                double projectAccuracy = totalItems > 0
                    ? Math.Round((double)totalFirstPassCorrect / totalItems * 100, 2)
                    : 0;

                var stats = new ProjectStatisticsResponse
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    TotalItems = project.DataItems.Count,
                    CompletedItems = project.DataItems.Count(d => d.Status == TaskStatusConstants.Approved),
                    TotalAssignments = allAssignments.Count,
                    PendingAssignments = project.DataItems.Count(d => d.Status == TaskStatusConstants.New) +
                                     allAssignments.Count(a => a.Status == TaskStatusConstants.New || a.Status == TaskStatusConstants.Assigned || a.Status == TaskStatusConstants.InProgress),
                    SubmittedAssignments = allAssignments.Count(a => a.Status == TaskStatusConstants.Submitted),
                    ApprovedAssignments = allAssignments.Count(a => a.Status == TaskStatusConstants.Approved),
                    RejectedAssignments = allAssignments.Count(a => a.Status == TaskStatusConstants.Rejected),
                    RejectionRate = totalReviewed > 0
                        ? Math.Round((double)totalRejectedLogs / totalReviewed * 100, 2)
                        : 0,
                    ErrorBreakdown = allReviewLogs
                        .Where(l => (l.Verdict == "Rejected" || l.Verdict == "Reject") && !string.IsNullOrEmpty(l.ErrorCategory))
                        .GroupBy(l => l.ErrorCategory!)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    ProjectAccuracy = projectAccuracy
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
                        double annotatorAccuracy = 0;
                        if (userStat != null && userStat.TotalManagerDecisions > 0)
                        {
                            annotatorAccuracy = Math.Round((double)userStat.TotalCorrectByManager / userStat.TotalManagerDecisions * 100, 2);
                        }
                        else
                        {
                            var assignmentIds = g.Select(a => a.Id).ToHashSet();
                            var logsForAnnotator = allReviewLogs.Where(rl => assignmentIds.Contains(rl.AssignmentId)).ToList();
                            if (logsForAnnotator.Count > 0)
                            {
                                var approvedLogs = logsForAnnotator.Count(rl => rl.Verdict == "Approved" || rl.Verdict == "Approve");
                                annotatorAccuracy = Math.Round((double)approvedLogs / logsForAnnotator.Count * 100, 2);
                            }
                        }

                        return new AnnotatorPerformance
                        {
                            AnnotatorId = annotatorId,
                            AnnotatorName = g.FirstOrDefault()?.Annotator?.FullName ?? "Unknown",
                            TasksAssigned = g.Count(),
                            TasksCompleted = g.Count(a => a.Status == TaskStatusConstants.Approved),
                            TasksRejected = g.Count(a => a.Status == TaskStatusConstants.Rejected),
                            AverageDurationSeconds = 0,
                            AverageQualityScore = userStat?.AverageQualityScore ?? 100,
                            TotalCriticalErrors = userStat?.TotalCriticalErrors ?? 0,
                            AnnotatorAccuracy = annotatorAccuracy
                        };
                    }).ToList();


                var reviewerIds = allAssignments
                    .Where(a => !string.IsNullOrEmpty(a.ReviewerId))
                    .Select(a => a.ReviewerId!)
                    .Distinct()
                    .ToList();

                var reviewLogsByReviewer = allReviewLogs
                    .Where(rl => !string.IsNullOrEmpty(rl.ReviewerId))
                    .GroupBy(rl => rl.ReviewerId!)
                    .ToDictionary(g => g.Key, g => g.Count());

                stats.ReviewerPerformances = reviewerIds.Select(reviewerId =>
                {
                    var reviewerStat = projectUserStats.FirstOrDefault(s => s.UserId == reviewerId);
                    var reviewer = allAssignments.FirstOrDefault(a => a.ReviewerId == reviewerId)?.Reviewer;
                    int statReviewsDone = reviewerStat?.TotalReviewsDone ?? 0;
                    int logReviewsDone = reviewLogsByReviewer.ContainsKey(reviewerId) ? reviewLogsByReviewer[reviewerId] : 0;
                    int totalReviewsDone = Math.Max(statReviewsDone, logReviewsDone);
                    int correctDecisions = reviewerStat?.TotalReviewerCorrectByManager ?? 0;
                    int totalMgrDecisions = reviewerStat?.TotalReviewerManagerDecisions ?? 0;
                    double reviewerAccuracy = totalMgrDecisions > 0
                        ? Math.Round((double)correctDecisions / totalMgrDecisions * 100, 2)
                        : 0;

                    return new ReviewerPerformance
                    {
                        ReviewerId = reviewerId,
                        ReviewerName = reviewer?.FullName ?? reviewer?.Email ?? "Unknown",
                        TotalReviews = totalReviewsDone,
                        CorrectDecisions = correctDecisions,
                        TotalManagerDecisions = totalMgrDecisions,
                        ReviewerAccuracy = reviewerAccuracy
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
            var allUsers = await _userRepository.GetAllAsync();
            var totalMembers = allUsers.Count(u => u.ManagerId == managerId);

            return new ManagerStatsResponse
            {
                TotalProjects = projects.Count,
                ActiveProjects = projects.Count(p => p.Deadline >= DateTime.UtcNow),
                TotalBudget = projects.Sum(p => p.TotalBudget),
                TotalDataItems = projects.Sum(p => p.DataItems.Count),
                TotalMembers = totalMembers
            };
        }

        public async Task<List<Core.DTOs.Responses.BucketResponse>> GetBucketsAsync(int projectId, string userId)
            {
                var dataItems = await _projectRepository.GetProjectDataItemsAsync(projectId);

                if (!dataItems.Any()) return new List<Core.DTOs.Responses.BucketResponse>();
                var buckets = dataItems.GroupBy(d => d.BucketId)
                                    .OrderBy(g => g.Key)
                                    .Select(g => new Core.DTOs.Responses.BucketResponse
                                    {
                                        BucketId = g.Key,
                                        Name = $"Lô số {g.Key}",
                                        TotalItems = g.Count(),
                                        CompletedItems = 0,
                                        Status = "New"
                                    }).ToList();

                return buckets;
            }
        }
    }