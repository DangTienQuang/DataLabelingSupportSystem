using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using DAL.Interfaces;
using Core.Constants;
using Core.Entities;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class TaskService : ITaskService
    {
        private readonly IAssignmentRepository _assignmentRepo;
        private readonly IRepository<DataItem> _dataItemRepo;
        private readonly IRepository<Annotation> _annotationRepo;
        private readonly IStatisticService _statisticService;
        private readonly IUserRepository _userRepo;
        private readonly IProjectRepository _projectRepo;
        private readonly IActivityLogRepository _activityLogRepo;

        public TaskService(
            IAssignmentRepository assignmentRepo,
            IRepository<DataItem> dataItemRepo,
            IRepository<Annotation> annotationRepo,
            IStatisticService statisticService,
            IUserRepository userRepo,
            IProjectRepository projectRepo,
            IActivityLogRepository activityLogRepo)
        {
            _assignmentRepo = assignmentRepo;
            _dataItemRepo = dataItemRepo;
            _annotationRepo = annotationRepo;
            _statisticService = statisticService;
            _userRepo = userRepo;
            _projectRepo = projectRepo;
            _activityLogRepo = activityLogRepo;
        }

        private int? ExtractClassIdFromJSON(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("classId", out var prop) || root.TryGetProperty("ClassId", out prop))
                    {
                        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out int id))
                            return id;

                        if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out int parsedId))
                            return parsedId;
                    }
                }
                else if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    foreach (var element in root.EnumerateArray())
                    {
                        if (element.ValueKind == JsonValueKind.Object)
                        {
                            if (element.TryGetProperty("classId", out var prop) || element.TryGetProperty("ClassId", out prop))
                            {
                                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out int id))
                                    return id;

                                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out int parsedId))
                                    return parsedId;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        public async Task<List<AssignmentResponse>> GetTasksByBucketAsync(int projectId, int bucketId, string userId)
        {
            var existingAssignments = await _assignmentRepo.GetAssignmentsByBucketAsync(projectId, bucketId, userId);
            if (existingAssignments.Any())
            {
                return existingAssignments.Select(a => new AssignmentResponse
                {
                    Id = a.Id,
                    DataItemId = a.DataItemId,
                    DataItemUrl = a.DataItem.StorageUrl,
                    Status = a.Status,
                    AssignedDate = a.AssignedDate,
                    AnnotationData = a.Annotations?
                        .OrderByDescending(an => an.CreatedAt)
                        .FirstOrDefault()
                        ?.DataJSON ?? ""
                }).ToList();
            }

            var dataItems = await _projectRepo.GetDataItemsByBucketIdAsync(projectId, bucketId);

            if (!dataItems.Any())
                return new List<AssignmentResponse>();

            var newAssignments = new List<Assignment>();
            foreach (var item in dataItems)
            {
                newAssignments.Add(new Assignment
                {
                    ProjectId = projectId,
                    DataItemId = item.Id,
                    AnnotatorId = userId,
                    ReviewerId = null,
                    Status = TaskStatusConstants.Assigned,
                    AssignedDate = DateTime.UtcNow
                });
            }

            foreach (var assign in newAssignments)
            {
                await _assignmentRepo.AddAsync(assign);
            }
            await _assignmentRepo.SaveChangesAsync();

            return newAssignments.Select(a => new AssignmentResponse
            {
                Id = a.Id,
                DataItemId = a.DataItemId,
                DataItemUrl = dataItems.First(d => d.Id == a.DataItemId).StorageUrl,
                Status = a.Status,
                AssignedDate = a.AssignedDate,
                AnnotationData = ""
            }).ToList();
        }

        public async Task AssignTasksToAnnotatorAsync(AssignTaskRequest request)
        {
            if (!string.IsNullOrEmpty(request.ReviewerId))
            {
                var reviewer = await _userRepo.GetByIdAsync(request.ReviewerId);
                if (reviewer == null)
                    throw new Exception("Reviewer not found");
            }

            var annotator = await _userRepo.GetByIdAsync(request.AnnotatorId);
            if (annotator == null)
                throw new Exception("Annotator not found");

            var dataItems = await _assignmentRepo
                .GetUnassignedDataItemsAsync(request.ProjectId, request.Quantity);

            if (!dataItems.Any())
                throw new Exception("Not enough available data items.");

            foreach (var item in dataItems)
            {
                var assignment = new Assignment
                {
                    ProjectId = request.ProjectId,
                    DataItemId = item.Id,
                    AnnotatorId = request.AnnotatorId,
                    ReviewerId = request.ReviewerId,
                    Status = TaskStatusConstants.Assigned,
                    AssignedDate = DateTime.UtcNow
                };

                await _assignmentRepo.AddAsync(assignment);
            }

            await _statisticService.TrackNewAssignmentAsync(
                request.AnnotatorId,
                request.ProjectId,
                dataItems.Count
            );

            await _assignmentRepo.SaveChangesAsync();
        }

        public async Task<AssignmentResponse> GetAssignmentByIdAsync(int assignmentId, string userId)
        {
            var assignment = await _assignmentRepo.GetAssignmentWithDetailsAsync(assignmentId);

            if (assignment == null)
                throw new KeyNotFoundException("Task not found");

            if (assignment.AnnotatorId != userId)
                throw new UnauthorizedAccessException("Unauthorized access to this task");

            var taskDeadline = assignment.AssignedDate.AddHours(assignment.Project.MaxTaskDurationHours);

            var effectiveDeadline = taskDeadline < assignment.Project.Deadline
                ? taskDeadline
                : assignment.Project.Deadline;

            return new AssignmentResponse
            {
                Id = assignment.Id,
                DataItemId = assignment.DataItemId,
                DataItemUrl = assignment.DataItem.StorageUrl,
                Status = assignment.Status,
                AnnotationData = assignment.Annotations?
                    .OrderByDescending(an => an.CreatedAt)
                    .FirstOrDefault()
                    ?.DataJSON ?? "",
                AssignedDate = assignment.AssignedDate,
                Deadline = effectiveDeadline,
                RejectionReason = assignment.Status == TaskStatusConstants.Rejected
                    ? (assignment.ReviewLogs?
                        .OrderByDescending(r => r.CreatedAt)
                        .FirstOrDefault()
                        ?.Comment ?? "")
                    : ""
            };
        }

        public async Task<AnnotatorStatsResponse> GetAnnotatorStatsAsync(string annotatorId)
        {
            return await _assignmentRepo.GetAnnotatorStatsAsync(annotatorId);
        }

        public async Task<List<AssignedProjectResponse>> GetAssignedProjectsAsync(string annotatorId)
        {
            var allAssignments = await _assignmentRepo.GetAssignmentsByAnnotatorAsync(annotatorId);

            return allAssignments
                .GroupBy(a => a.ProjectId)
                .Select(g => new AssignedProjectResponse
                {
                    ProjectId = g.Key,
                    ProjectName = g.First().Project.Name,
                    Description = g.First().Project.Description,
                    ThumbnailUrl = g.First().DataItem.StorageUrl,
                    AssignedDate = g.Min(a => a.AssignedDate),
                    Deadline = g.First().Project.Deadline,
                    TotalImages = g.Count(),
                    CompletedImages = g.Count(a =>
                        a.Status == TaskStatusConstants.Submitted ||
                        a.Status == TaskStatusConstants.Approved),
                    Status = g.All(a => a.Status == TaskStatusConstants.Approved)
                        ? "Completed"
                        : g.Any(a => a.Status != TaskStatusConstants.Assigned)
                            ? "InProgress"
                            : "Assigned"
                })
                .ToList();
        }

        public async Task<List<AssignmentResponse>> GetTaskImagesAsync(int projectId, string annotatorId)
        {
            var assignments = await _assignmentRepo.GetAssignmentsByAnnotatorAsync(annotatorId, projectId);

            return assignments.Select(a =>
            {
                var taskDeadline = a.AssignedDate.AddHours(a.Project.MaxTaskDurationHours);
                var effectiveDeadline = taskDeadline < a.Project.Deadline
                    ? taskDeadline
                    : a.Project.Deadline;

                return new AssignmentResponse
                {
                    Id = a.Id,
                    DataItemId = a.DataItemId,
                    DataItemUrl = a.DataItem.StorageUrl,
                    Status = a.Status,
                    AnnotationData = a.Annotations?
                        .OrderByDescending(an => an.CreatedAt)
                        .FirstOrDefault()
                        ?.DataJSON ?? "",
                    AssignedDate = a.AssignedDate,
                    Deadline = effectiveDeadline,
                    RejectionReason = a.Status == TaskStatusConstants.Rejected
                        ? (a.ReviewLogs?
                            .OrderByDescending(r => r.CreatedAt)
                            .FirstOrDefault()
                            ?.Comment ?? "")
                        : ""
                };
            }).ToList();
        }

        public async Task<AssignmentResponse> JumpToDataItemAsync(int projectId, int dataItemId, string userId)
        {
            var assignments = await _assignmentRepo.GetAllAsync();

            var target = assignments.FirstOrDefault(a =>
                a.ProjectId == projectId &&
                a.DataItemId == dataItemId &&
                a.AnnotatorId == userId);

            if (target == null)
                throw new Exception("You are not assigned to this image in this project.");

            return await GetAssignmentByIdAsync(target.Id, userId);
        }

        public async Task SaveDraftAsync(string userId, SubmitAnnotationRequest request)
        {
            if (string.IsNullOrEmpty(request.DataJSON) || request.DataJSON == "[]")
                return;

            var assignment = await _assignmentRepo.GetAssignmentWithDetailsAsync(request.AssignmentId);

            if (assignment == null)
                throw new KeyNotFoundException("Task not found");

            if (assignment.AnnotatorId != userId)
                throw new UnauthorizedAccessException("Unauthorized");

            if (assignment.Status == TaskStatusConstants.Approved)
                throw new InvalidOperationException("Cannot edit approved task");

            var latestAnnotation = assignment.Annotations?
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault();

            bool shouldCreateNew = assignment.Status == TaskStatusConstants.Rejected;

            if (latestAnnotation != null && !shouldCreateNew)
            {
                latestAnnotation.DataJSON = request.DataJSON;
                latestAnnotation.CreatedAt = DateTime.UtcNow;
                latestAnnotation.ClassId = ExtractClassIdFromJSON(request.DataJSON);
                _annotationRepo.Update(latestAnnotation);
            }
            else
            {
                var annotation = new Annotation
                {
                    AssignmentId = assignment.Id,
                    DataJSON = request.DataJSON,
                    CreatedAt = DateTime.UtcNow,
                    ClassId = ExtractClassIdFromJSON(request.DataJSON)
                };
                await _annotationRepo.AddAsync(annotation);
            }

            if (assignment.Status == TaskStatusConstants.New ||
                assignment.Status == TaskStatusConstants.Assigned ||
                assignment.Status == TaskStatusConstants.Rejected)
            {
                assignment.Status = TaskStatusConstants.InProgress;
                _assignmentRepo.Update(assignment);
            }

            await _annotationRepo.SaveChangesAsync();
            await _assignmentRepo.SaveChangesAsync();
        }

        public async Task SubmitTaskAsync(string userId, SubmitAnnotationRequest request)
        {
            var assignment = await _assignmentRepo.GetAssignmentWithDetailsAsync(request.AssignmentId);

            if (assignment == null)
                throw new KeyNotFoundException("Task not found");

            if (assignment.AnnotatorId != userId)
                throw new UnauthorizedAccessException("Unauthorized");

            if (string.IsNullOrEmpty(request.DataJSON) || request.DataJSON == "[]")
                throw new InvalidOperationException("Annotation data is empty. Please save a draft before submitting.");

            var annotation = new Annotation
            {
                AssignmentId = assignment.Id,
                DataJSON = request.DataJSON,
                CreatedAt = DateTime.UtcNow,
                ClassId = ExtractClassIdFromJSON(request.DataJSON)
            };

            await _annotationRepo.AddAsync(annotation);

            assignment.Status = TaskStatusConstants.Submitted;
            assignment.SubmittedAt = DateTime.UtcNow;

            _assignmentRepo.Update(assignment);

            var log = new ActivityLog
            {
                UserId = userId,
                ActionType = "SubmitTask",
                EntityName = "Project",
                EntityId = assignment.ProjectId.ToString(),
                Description = $"Annotator submitted task {assignment.Id} for review.",
                Timestamp = DateTime.UtcNow
            };
            await _activityLogRepo.AddAsync(log);

            await _assignmentRepo.SaveChangesAsync();
        }

        public async Task<SubmitMultipleTasksResponse> SubmitMultipleTasksAsync(string userId, SubmitMultipleTasksRequest request)
        {
            var response = new SubmitMultipleTasksResponse();
            int? lastProjectId = null;

            foreach (var id in request.AssignmentIds)
            {
                var assignment = await _assignmentRepo.GetAssignmentWithDetailsAsync(id);

                if (assignment == null)
                {
                    response.FailureCount++;
                    response.Errors.Add($"Task ID {id}: Not found.");
                    continue;
                }

                if (assignment.AnnotatorId != userId)
                {
                    response.FailureCount++;
                    response.Errors.Add($"Task ID {id}: Unauthorized access.");
                    continue;
                }

                if (assignment.Status == TaskStatusConstants.Submitted || assignment.Status == TaskStatusConstants.Approved)
                {
                    response.FailureCount++;
                    response.Errors.Add($"Task ID {id}: Task is already submitted or approved.");
                    continue;
                }

                var latestAnnotation = assignment.Annotations?.OrderByDescending(a => a.CreatedAt).FirstOrDefault();

                if (latestAnnotation == null || string.IsNullOrEmpty(latestAnnotation.DataJSON) || latestAnnotation.DataJSON == "[]")
                {
                    response.FailureCount++;
                    response.Errors.Add($"Task ID {id}: Missing annotation data. Please save draft before submitting.");
                    continue;
                }

                assignment.Status = TaskStatusConstants.Submitted;
                assignment.SubmittedAt = DateTime.UtcNow;

                _assignmentRepo.Update(assignment);
                response.SuccessCount++;
                lastProjectId = assignment.ProjectId;
            }

            if (response.SuccessCount > 0)
            {
                if (lastProjectId.HasValue)
                {
                    var log = new ActivityLog
                    {
                        UserId = userId,
                        ActionType = "SubmitBatchTasks",
                        EntityName = "Project",
                        EntityId = lastProjectId.Value.ToString(),
                        Description = $"Annotator batch submitted {response.SuccessCount} tasks for review.",
                        Timestamp = DateTime.UtcNow
                    };
                    await _activityLogRepo.AddAsync(log);
                }

                await _assignmentRepo.SaveChangesAsync();
            }

            return response;
        }
    }
}