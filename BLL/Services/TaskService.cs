using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using DAL.Interfaces;
using Core.Constants;
using Core.Entities;
using System.Text.Json;

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

        public TaskService(
            IAssignmentRepository assignmentRepo,
            IRepository<DataItem> dataItemRepo,
            IRepository<Annotation> annotationRepo,
            IStatisticService statisticService,
            IUserRepository userRepo,
            IProjectRepository projectRepo)
        {
            _assignmentRepo = assignmentRepo;
            _dataItemRepo = dataItemRepo;
            _annotationRepo = annotationRepo;
            _statisticService = statisticService;
            _userRepo = userRepo;
            _projectRepo = projectRepo;
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
                        ?.DataJSON
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
                AssignedDate = a.AssignedDate
            }).ToList();
        }

        public async Task AssignTasksToAnnotatorAsync(AssignTaskRequest request)
        {
            var reviewer = await _userRepo.GetByIdAsync(request.ReviewerId);
            if (reviewer == null)
                throw new Exception("Reviewer not found");

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

                item.Status = TaskStatusConstants.Assigned;

                _dataItemRepo.Update(item);
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

            var taskDeadline =
                assignment.AssignedDate.AddHours(assignment.Project.MaxTaskDurationHours);

            var effectiveDeadline =
                taskDeadline < assignment.Project.Deadline
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
                    ?.DataJSON,
                AssignedDate = assignment.AssignedDate,
                Deadline = effectiveDeadline,
                RejectionReason =
                    assignment.Status == TaskStatusConstants.Rejected
                        ? assignment.ReviewLogs?
                            .OrderByDescending(r => r.CreatedAt)
                            .FirstOrDefault()
                            ?.Comment
                        : null
            };
        }

        public async Task<AnnotatorStatsResponse> GetAnnotatorStatsAsync(string annotatorId)
        {
            return await _assignmentRepo.GetAnnotatorStatsAsync(annotatorId);
        }

        public async Task<List<AssignedProjectResponse>> GetAssignedProjectsAsync(string annotatorId)
        {
            var allAssignments =
                await _assignmentRepo.GetAssignmentsByAnnotatorAsync(annotatorId);

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
                    Status =
                        g.All(a => a.Status == TaskStatusConstants.Approved)
                            ? "Completed"
                            : g.Any(a => a.Status != TaskStatusConstants.Assigned)
                                ? "InProgress"
                                : "Assigned"
                })
                .ToList();
        }

        public async Task<List<AssignmentResponse>> GetTaskImagesAsync(
            int projectId,
            string annotatorId)
        {
            var assignments =
                await _assignmentRepo.GetAssignmentsByAnnotatorAsync(
                    annotatorId,
                    projectId);

            return assignments.Select(a =>
            {
                var taskDeadline =
                    a.AssignedDate.AddHours(a.Project.MaxTaskDurationHours);

                var effectiveDeadline =
                    taskDeadline < a.Project.Deadline
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
                        ?.DataJSON,
                    AssignedDate = a.AssignedDate,
                    Deadline = effectiveDeadline,
                    RejectionReason =
                        a.Status == TaskStatusConstants.Rejected
                            ? a.ReviewLogs?
                                .OrderByDescending(r => r.CreatedAt)
                                .FirstOrDefault()
                                ?.Comment
                            : null
                };
            }).ToList();
        }

        public async Task<AssignmentResponse> JumpToDataItemAsync(
            int projectId,
            int dataItemId,
            string userId)
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

            var assignment =
                await _assignmentRepo.GetAssignmentWithDetailsAsync(request.AssignmentId);

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
                _annotationRepo.Update(latestAnnotation);
            }
            else
            {
                var annotation = new Annotation
                {
                    AssignmentId = assignment.Id,
                    DataJSON = request.DataJSON,
                    CreatedAt = DateTime.UtcNow
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
            var assignment =
                await _assignmentRepo.GetAssignmentWithDetailsAsync(request.AssignmentId);

            if (assignment == null)
                throw new KeyNotFoundException("Task not found");

            if (assignment.AnnotatorId != userId)
                throw new UnauthorizedAccessException("Unauthorized");

            var annotation = new Annotation
            {
                AssignmentId = assignment.Id,
                DataJSON = request.DataJSON,
                CreatedAt = DateTime.UtcNow
            };

            await _annotationRepo.AddAsync(annotation);

            assignment.Status = TaskStatusConstants.Submitted;
            assignment.SubmittedAt = DateTime.UtcNow;

            _assignmentRepo.Update(assignment);
            await _assignmentRepo.SaveChangesAsync();
        }
    }
}