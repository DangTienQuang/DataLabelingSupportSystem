using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using DAL.Interfaces;
using DTOs.Constants;
using DTOs.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BLL.Services
{
    public class TaskService : ITaskService
    {
        private readonly IAssignmentRepository _assignmentRepo;
        private readonly IRepository<DataItem> _dataItemRepo;
        private readonly IRepository<Annotation> _annotationRepo;
        private readonly IRepository<UserProjectStat> _statsRepo;

        public TaskService(
            IAssignmentRepository assignmentRepo,
            IRepository<DataItem> dataItemRepo,
            IRepository<Annotation> annotationRepo,
            IRepository<UserProjectStat> statsRepo)
        {
            _assignmentRepo = assignmentRepo;
            _dataItemRepo = dataItemRepo;
            _annotationRepo = annotationRepo;
            _statsRepo = statsRepo;
        }

        public async Task AssignTasksToAnnotatorAsync(AssignTaskRequest request)
        {
            var dataItems = await _assignmentRepo.GetUnassignedDataItemsAsync(request.ProjectId, request.Quantity);
            if (!dataItems.Any()) throw new Exception("Not enough available data items.");

            foreach (var item in dataItems)
            {
                var assignment = new Assignment
                {
                    ProjectId = request.ProjectId,
                    DataItemId = item.Id,
                    AnnotatorId = request.AnnotatorId,
                    Status = "Assigned",
                    AssignedDate = DateTime.UtcNow
                };

                item.Status = "Assigned";
                _dataItemRepo.Update(item);
                await _assignmentRepo.AddAsync(assignment);
            }

            var allStats = await _statsRepo.GetAllAsync();
            var stats = allStats.FirstOrDefault(s => s.UserId == request.AnnotatorId && s.ProjectId == request.ProjectId);

            if (stats == null)
            {
                stats = new UserProjectStat
                {
                    UserId = request.AnnotatorId,
                    ProjectId = request.ProjectId,
                    TotalAssigned = dataItems.Count,
                    EfficiencyScore = 100,
                    EstimatedEarnings = 0,
                    Date = DateTime.UtcNow
                };
                await _statsRepo.AddAsync(stats);
            }
            else
            {
                stats.TotalAssigned += dataItems.Count;
                stats.Date = DateTime.UtcNow;
                _statsRepo.Update(stats);
            }

            await _assignmentRepo.SaveChangesAsync();
        }
        public async Task<AssignmentResponse> GetAssignmentByIdAsync(int assignmentId, string userId)
        {
            var assignment = await _assignmentRepo.GetAssignmentWithDetailsAsync(assignmentId);

            if (assignment == null) throw new KeyNotFoundException("Task not found");
            if (assignment.AnnotatorId != userId) throw new UnauthorizedAccessException("Unauthorized access to this task");

            return new AssignmentResponse
            {
                Id = assignment.Id,
                DataItemId = assignment.DataItemId,
                DataItemUrl = assignment.DataItem.StorageUrl,
                Status = assignment.Status,
                AnnotationData = assignment.Annotations?.OrderByDescending(an => an.CreatedAt).FirstOrDefault()?.DataJSON,
                AssignedDate = assignment.AssignedDate,
                Deadline = assignment.Project.Deadline,
                RejectionReason = assignment.Status == "Rejected"
                    ? assignment.ReviewLogs?.OrderByDescending(r => r.CreatedAt).FirstOrDefault()?.Comment
                    : null
            };
        }
        public async Task<AnnotatorStatsResponse> GetAnnotatorStatsAsync(string annotatorId)
        {
            return await _assignmentRepo.GetAnnotatorStatsAsync(annotatorId);
        }
        public async Task<List<AssignedProjectResponse>> GetAssignedProjectsAsync(string annotatorId)
        {
            var allAssignments = await _assignmentRepo.GetAssignmentsByAnnotatorAsync(annotatorId);

            var grouped = allAssignments
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
                    CompletedImages = g.Count(a => a.Status == "Submitted" || a.Status == "Approved"),
                    Status = g.All(a => a.Status == "Approved") ? "Completed"
                           : g.Any(a => a.Status != "Assigned") ? "InProgress" : "Assigned"
                })
                .ToList();

            return grouped;
        }

        public async Task<List<AssignmentResponse>> GetTaskImagesAsync(int projectId, string annotatorId)
        {
            var assignments = await _assignmentRepo.GetAssignmentsByAnnotatorAsync(annotatorId, projectId);

            return assignments.Select(a => new AssignmentResponse
            {
                Id = a.Id,
                DataItemId = a.DataItemId,
                DataItemUrl = a.DataItem.StorageUrl,
                Status = a.Status,
                AnnotationData = a.Annotations?.OrderByDescending(an => an.CreatedAt).FirstOrDefault()?.DataJSON,

                AssignedDate = a.AssignedDate,
                Deadline = a.Project.Deadline,
                RejectionReason = a.Status == "Rejected"
                    ? a.ReviewLogs?.OrderByDescending(r => r.CreatedAt).FirstOrDefault()?.Comment
                    : null
            }).ToList();
        }
        public async Task<AssignmentResponse> JumpToDataItemAsync(int projectId, int dataItemId, string userId)
        {
            var assignment = await _assignmentRepo.GetAllAsync();
            var target = assignment.FirstOrDefault(a =>
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
            {
                return;
            }

            var assignment = await _assignmentRepo.GetAssignmentWithDetailsAsync(request.AssignmentId);
            if (assignment == null) throw new KeyNotFoundException("Task not found");
            if (assignment.AnnotatorId != userId) throw new UnauthorizedAccessException("Unauthorized");
            if (assignment.Status == "Approved") throw new InvalidOperationException("Cannot edit approved task");
            var existingAnnotation = assignment.Annotations?
                                     .OrderByDescending(a => a.CreatedAt)
                                     .FirstOrDefault();

            if (existingAnnotation != null)
            {
                existingAnnotation.DataJSON = request.DataJSON;
                existingAnnotation.CreatedAt = DateTime.UtcNow;
                _annotationRepo.Update(existingAnnotation);
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

            if (assignment.Status == "New" || assignment.Status == "Assigned" || assignment.Status == "Rejected")
            {
                assignment.Status = "InProgress";
                _assignmentRepo.Update(assignment);
            }
            await _annotationRepo.SaveChangesAsync();
            await _assignmentRepo.SaveChangesAsync();
        }
        public async Task SubmitTaskAsync(string userId, SubmitAnnotationRequest request)
        {
            var assignment = await _assignmentRepo.GetAssignmentWithDetailsAsync(request.AssignmentId);
            if (assignment == null) throw new KeyNotFoundException("Task not found");
            if (assignment.AnnotatorId != userId) throw new UnauthorizedAccessException("Unauthorized");

            if (assignment.Annotations != null && assignment.Annotations.Any())
            {
                foreach (var oldAnno in assignment.Annotations)
                {
                    _annotationRepo.Delete(oldAnno);
                }
            }

            var annotation = new Annotation
            {
                AssignmentId = assignment.Id,
                DataJSON = request.DataJSON,
                CreatedAt = DateTime.UtcNow
            };
            await _annotationRepo.AddAsync(annotation);

            assignment.Status = "Submitted";
            assignment.SubmittedAt = DateTime.UtcNow;

            _assignmentRepo.Update(assignment);
            await _assignmentRepo.SaveChangesAsync();
        }
    }
}