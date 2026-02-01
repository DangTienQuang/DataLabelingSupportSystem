using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using DAL.Interfaces;
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
            if (assignment.ReviewerId != reviewerId)
                throw new Exception("You are not assigned to review this task.");

            if (assignment.Status != "Submitted")
                throw new Exception("This task is not ready for review.");

            var project = await _projectRepo.GetByIdAsync(assignment.ProjectId);
            if (project == null) throw new Exception("Project info not found");

            var allStats = await _statsRepo.GetAllAsync();
            var annotatorStats = allStats.FirstOrDefault(s => s.UserId == assignment.AnnotatorId && s.ProjectId == assignment.ProjectId);

            if (annotatorStats == null)
            {
                annotatorStats = new UserProjectStat
                {
                    UserId = assignment.AnnotatorId,
                    ProjectId = assignment.ProjectId,
                    EfficiencyScore = 100,
                    AverageQualityScore = 100
                };
                await _statsRepo.AddAsync(annotatorStats);
            }
            var reviewerStats = allStats.FirstOrDefault(s => s.UserId == reviewerId && s.ProjectId == assignment.ProjectId);
            if (reviewerStats == null)
            {
                reviewerStats = new UserProjectStat
                {
                    UserId = reviewerId,
                    ProjectId = assignment.ProjectId,
                    ReviewerQualityScore = 100,
                    TotalReviewsDone = 0
                };
                await _statsRepo.AddAsync(reviewerStats);
            }

            double currentTaskScore = 0;
            int penaltyScore = 0;

            if (request.IsApproved)
            {
                currentTaskScore = 100;
                assignment.Status = "Completed";
                annotatorStats.TotalApproved++;
                annotatorStats.EstimatedEarnings = annotatorStats.TotalApproved * project.PricePerLabel;

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
                annotatorStats.TotalRejected++;
                int weight = 0;
                if (!string.IsNullOrEmpty(project.ReviewChecklist) && !string.IsNullOrEmpty(request.ErrorCategory))
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var checklistItems = JsonSerializer.Deserialize<List<ChecklistItemRequest>>(project.ReviewChecklist, options);
                        var item = checklistItems?.FirstOrDefault(c => c.Code == request.ErrorCategory);
                        if (item != null)
                        {
                            weight = item.Weight;
                        }
                    }
                    catch { }
                }

                if (weight >= 10)
                {
                    annotatorStats.TotalCriticalErrors++;
                }

                penaltyScore = weight * 10;
                currentTaskScore = Math.Max(0, 100 - penaltyScore);
            }

            double totalScoreSoFar = (annotatorStats.AverageQualityScore * annotatorStats.TotalReviewedTasks) + currentTaskScore;
            annotatorStats.TotalReviewedTasks++;
            annotatorStats.AverageQualityScore = Math.Round(totalScoreSoFar / annotatorStats.TotalReviewedTasks, 2);

            if (annotatorStats.TotalAssigned > 0)
            {
                annotatorStats.EfficiencyScore = ((float)annotatorStats.TotalApproved / annotatorStats.TotalAssigned) * 100;
            }
            annotatorStats.Date = DateTime.UtcNow;
            reviewerStats.TotalReviewsDone++;
            reviewerStats.Date = DateTime.UtcNow;

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
            _statsRepo.Update(annotatorStats);
            _statsRepo.Update(reviewerStats);
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
                    ReviewerQualityScore = 100
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

        public async Task<List<TaskResponse>> GetTasksForReviewAsync(int projectId, string reviewerId)
        {
            var assignments = await _assignmentRepo.GetAssignmentsForReviewerAsync(projectId, reviewerId);

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