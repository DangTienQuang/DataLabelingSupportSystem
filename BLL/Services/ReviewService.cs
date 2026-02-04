using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using DAL.Interfaces;
using Core.Constants;
using Core.Entities;
using System.Text.Json;

namespace BLL.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IAssignmentRepository _assignmentRepo;
        private readonly IRepository<ReviewLog> _reviewLogRepo;
        private readonly IRepository<DataItem> _dataItemRepo;
        private readonly IStatisticService _statisticService;
        private readonly IRepository<Project> _projectRepo;
        private readonly IUserRepository _userRepo;

        public ReviewService(
            IAssignmentRepository assignmentRepo,
            IRepository<ReviewLog> reviewLogRepo,
            IRepository<DataItem> dataItemRepo,
            IStatisticService statisticService,
            IRepository<Project> projectRepo,
            IUserRepository userRepo)
        {
            _assignmentRepo = assignmentRepo;
            _reviewLogRepo = reviewLogRepo;
            _dataItemRepo = dataItemRepo;
            _statisticService = statisticService;
            _projectRepo = projectRepo;
            _userRepo = userRepo;
        }

        public async Task ReviewAssignmentAsync(string reviewerId, ReviewRequest request)
        {
            var assignment = await _assignmentRepo.GetByIdAsync(request.AssignmentId);
            if (assignment == null) throw new Exception("Assignment not found");

            var reviewer = await _userRepo.GetByIdAsync(reviewerId);
            if (reviewer == null) throw new Exception("User not found");

            if (reviewer.Role != UserRoles.Reviewer &&
                reviewer.Role != UserRoles.Manager &&
                reviewer.Role != UserRoles.Admin)
            {
                throw new Exception("Permission denied: Only Reviewers or Managers can review tasks.");
            }

            if (assignment.ReviewerId != reviewerId)
                throw new Exception("You are not assigned to review this task.");

            if (assignment.Status != TaskStatusConstants.Submitted)
                throw new Exception("This task is not ready for review.");

            var project = await _projectRepo.GetByIdAsync(assignment.ProjectId);
            if (project == null) throw new Exception("Project info not found");

            double currentTaskScore = 0;
            int penaltyScore = 0;
            bool isCritical = false;

            if (request.IsApproved)
            {
                currentTaskScore = 100;
                assignment.Status = TaskStatusConstants.Approved;

                if (assignment.DataItemId > 0)
                {
                    var dataItem = await _dataItemRepo.GetByIdAsync(assignment.DataItemId);
                    if (dataItem != null)
                    {
                        dataItem.Status = TaskStatusConstants.Approved;
                        _dataItemRepo.Update(dataItem);
                    }
                }
            }
            else
            {
                assignment.Status = TaskStatusConstants.Rejected;
                int weight = 0;

                if (project.ChecklistItems != null &&
                    project.ChecklistItems.Any() &&
                    !string.IsNullOrEmpty(request.ErrorCategory))
                {
                    var errorItem = project.ChecklistItems
                        .FirstOrDefault(c => c.Code == request.ErrorCategory);

                    if (errorItem != null)
                    {
                        weight = errorItem.Weight;
                        if (errorItem.IsCritical) isCritical = true;
                    }
                }

                int unit = project.PenaltyUnit > 0 ? project.PenaltyUnit : 10;
                penaltyScore = weight * unit;

                currentTaskScore = Math.Max(0, 100 - penaltyScore);
            }

            await _statisticService.TrackReviewResultAsync(
                assignment.AnnotatorId,
                reviewerId,
                assignment.ProjectId,
                request.IsApproved,
                currentTaskScore,
                project.PricePerLabel,
                isCritical
            );

            var log = new ReviewLog
            {
                AssignmentId = assignment.Id,
                ReviewerId = reviewerId,
                Verdict = request.IsApproved ? "Approved" : "Rejected",
                Comment = request.Comment,
                ErrorCategory = request.IsApproved ? null : request.ErrorCategory,
                ScorePenalty = penaltyScore,
                CreatedAt = DateTime.UtcNow
            };

            await _reviewLogRepo.AddAsync(log);
            _assignmentRepo.Update(assignment);

            await _assignmentRepo.SaveChangesAsync();
        }

        public async Task AuditReviewAsync(string managerId, AuditReviewRequest request)
        {
            var log = await _reviewLogRepo.GetByIdAsync(request.ReviewLogId);
            if (log == null) throw new Exception("Review log not found");
            if (log.IsAudited) throw new Exception("This review has already been audited");

            var assignment = await _assignmentRepo.GetByIdAsync(log.AssignmentId);
            if (assignment == null) throw new Exception("Related assignment not found");

            await _statisticService.TrackAuditResultAsync(log.ReviewerId, assignment.ProjectId, request.IsCorrectDecision);

            log.IsAudited = true;
            log.AuditResult = request.IsCorrectDecision ? "Agree" : "Disagree";

            await _reviewLogRepo.SaveChangesAsync();
        }

        public async Task<List<TaskResponse>> GetTasksForReviewAsync(int projectId, string reviewerId)
        {
            var assignments = await _assignmentRepo.GetAssignmentsForReviewerAsync(projectId, reviewerId);

            return assignments.Select(a =>
            {
                var latestAnnotation = a.Annotations?.OrderByDescending(an => an.CreatedAt).FirstOrDefault();
                object? annotationJson = null;
                if (latestAnnotation != null)
                {
                    if (!string.IsNullOrEmpty(latestAnnotation.DataJSON))
                    {
                        try { annotationJson = JsonDocument.Parse(latestAnnotation.DataJSON).RootElement; } catch { }
                    }
                    else if (!string.IsNullOrEmpty(latestAnnotation.Value))
                    {
                        try { annotationJson = JsonDocument.Parse(latestAnnotation.Value).RootElement; } catch { }
                    }
                }

                return new TaskResponse
                {
                    AssignmentId = a.Id,
                    DataItemId = a.DataItemId,
                    StorageUrl = a.DataItem?.StorageUrl ?? "",
                    ProjectName = a.Project?.Name ?? "",
                    Status = a.Status,
                    Deadline = a.Project?.Deadline ?? DateTime.MinValue,
                    Labels = a.Project?.LabelClasses.Select(l => new LabelResponse
                    {
                        Id = l.Id,
                        Name = l.Name,
                        Color = l.Color,
                        GuideLine = l.GuideLine
                    }).ToList() ?? new List<LabelResponse>(),
                    ExistingAnnotations = annotationJson != null ? new List<object> { annotationJson } : new List<object>()
                };
            }).ToList();
        }
    }
}