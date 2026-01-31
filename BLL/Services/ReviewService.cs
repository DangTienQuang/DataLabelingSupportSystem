using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using DAL.Interfaces;
using DTOs.Constants;
using DTOs.Entities;
using System.Text.Json;

namespace BLL.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IAssignmentRepository _assignmentRepo;
        private readonly IRepository<ReviewLog> _reviewLogRepo;
        private readonly IRepository<DataItem> _dataItemRepo;
        private readonly IRepository<UserProjectStat> _statsRepo;
        private readonly IRepository<Project> _projectRepo;

        public ReviewService(
            IAssignmentRepository assignmentRepo,
            IRepository<ReviewLog> reviewLogRepo,
            IRepository<DataItem> dataItemRepo,
            IRepository<UserProjectStat> statsRepo,
            IRepository<Project> projectRepo)
        {
            _assignmentRepo = assignmentRepo;
            _reviewLogRepo = reviewLogRepo;
            _dataItemRepo = dataItemRepo;
            _statsRepo = statsRepo;
            _projectRepo = projectRepo;
        }

        public async Task ReviewAssignmentAsync(string reviewerId, ReviewRequest request)
        {
            var assignment = await _assignmentRepo.GetByIdAsync(request.AssignmentId);
            if (assignment == null) throw new Exception("Assignment not found");
            if (assignment.Status != "Submitted")
                throw new Exception("This task is not ready for review.");

            var project = await _projectRepo.GetByIdAsync(assignment.ProjectId);
            if (project == null) throw new Exception("Project info not found");

            var allStats = await _statsRepo.GetAllAsync();
            var stats = allStats.FirstOrDefault(s => s.UserId == assignment.AnnotatorId && s.ProjectId == assignment.ProjectId);

            if (stats == null)
            {
                stats = new UserProjectStat
                {
                    UserId = assignment.AnnotatorId,
                    ProjectId = assignment.ProjectId,
                    TotalAssigned = 0,
                    EfficiencyScore = 100,
                    EstimatedEarnings = 0,
                    AverageQualityScore = 100,
                    TotalReviewedTasks = 0,
                    TotalCriticalErrors = 0
                };
                await _statsRepo.AddAsync(stats);
            }

            double currentTaskScore = 0;
            int penaltyScore = 0;

            if (request.IsApproved)
            {
                currentTaskScore = 100;
                assignment.Status = "Completed";
                stats.TotalApproved++;
                stats.EstimatedEarnings = stats.TotalApproved * project.PricePerLabel;

                if (assignment.DataItemId > 0)
                {
                    var dataItem = await _dataItemRepo.GetByIdAsync(assignment.DataItemId);
                    if (dataItem != null)
                    {
                        dataItem.Status = "Done";
                        _dataItemRepo.Update(dataItem);
                    }
                }
            }
            else
            {
                assignment.Status = "Rejected";
                stats.TotalRejected++;

                int weight = 0;
                if (!string.IsNullOrEmpty(request.ErrorCategory))
                {
                    weight = ErrorCategories.GetSeverityWeight(request.ErrorCategory);
                }

                if (weight >= 10)
                {
                    stats.TotalCriticalErrors++;
                }

                penaltyScore = weight * 10;
                currentTaskScore = Math.Max(0, 100 - penaltyScore);
            }

            double totalScoreSoFar = (stats.AverageQualityScore * stats.TotalReviewedTasks) + currentTaskScore;
            stats.TotalReviewedTasks++;
            stats.AverageQualityScore = Math.Round(totalScoreSoFar / stats.TotalReviewedTasks, 2);

            if (stats.TotalAssigned > 0)
            {
                stats.EfficiencyScore = ((float)stats.TotalApproved / stats.TotalAssigned) * 100;
            }
            stats.Date = DateTime.UtcNow;

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
            _statsRepo.Update(stats);
            _assignmentRepo.Update(assignment);

            await _assignmentRepo.SaveChangesAsync();
        }
        public async Task AuditReviewAsync(string managerId, AuditReviewRequest request)
        {
            var log = await _reviewLogRepo.GetByIdAsync(request.ReviewLogId);
            if (log == null) throw new Exception("Review log not found");
            if (log.IsAudited) throw new Exception("This review has already been audited");

            var assignment = await _assignmentRepo.GetByIdAsync(log.AssignmentId);
            if (assignment == null) throw new Exception("Assignment not found");

            var allStats = await _statsRepo.GetAllAsync();
            var reviewerStats = allStats.FirstOrDefault(s => s.UserId == log.ReviewerId && s.ProjectId == assignment.ProjectId);

            if (reviewerStats == null)
            {
                reviewerStats = new UserProjectStat
                {
                    UserId = log.ReviewerId,
                    ProjectId = assignment.ProjectId,
                    ReviewerQualityScore = 100,
                    TotalReviewsDone = 0,
                    TotalAuditedReviews = 0,
                    TotalCorrectDecisions = 0
                };
                await _statsRepo.AddAsync(reviewerStats);
            }

            log.IsAudited = true;
            log.AuditResult = request.IsCorrectDecision ? "Agree" : "Disagree";

            reviewerStats.TotalAuditedReviews++;

            if (request.IsCorrectDecision)
            {
                reviewerStats.TotalCorrectDecisions++;
            }

            if (reviewerStats.TotalAuditedReviews > 0)
            {
                double accuracy = (double)reviewerStats.TotalCorrectDecisions / reviewerStats.TotalAuditedReviews;
                reviewerStats.ReviewerQualityScore = Math.Round(accuracy * 100, 2);
            }

            await _reviewLogRepo.SaveChangesAsync();
            _statsRepo.Update(reviewerStats);
            await _statsRepo.SaveChangesAsync();
        }
        public async Task<List<TaskResponse>> GetTasksForReviewAsync(int projectId)
        {
            var assignments = await _assignmentRepo.GetAssignmentsForReviewerAsync(projectId);

            return assignments.Select(a => new TaskResponse
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

                ExistingAnnotations = a.Annotations.Select(an =>
                {
                    if (!string.IsNullOrEmpty(an.DataJSON))
                    {
                        return (object)JsonDocument.Parse(an.DataJSON).RootElement;
                    }
                    else if (!string.IsNullOrEmpty(an.Value))
                    {
                        return (object)JsonDocument.Parse(an.Value).RootElement;
                    }
                    return null;
                }).Where(x => x != null).ToList()
            }).ToList();
        }
    }
}