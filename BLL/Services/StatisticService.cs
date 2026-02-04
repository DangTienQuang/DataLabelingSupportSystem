using BLL.Interfaces;
using DAL.Interfaces;
using Core.Entities;

namespace BLL.Services
{
    public class StatisticService : IStatisticService
    {
        private readonly IRepository<UserProjectStat> _statsRepo;

        public StatisticService(IRepository<UserProjectStat> statsRepo)
        {
            _statsRepo = statsRepo;
        }

        private async Task<UserProjectStat> GetOrCreateStatAsync(string userId, int projectId, bool isReviewer = false)
        {
            var allStats = await _statsRepo.GetAllAsync();
            var stat = allStats.FirstOrDefault(s => s.UserId == userId && s.ProjectId == projectId);

            if (stat == null)
            {
                stat = new UserProjectStat
                {
                    UserId = userId,
                    ProjectId = projectId,
                    Date = DateTime.UtcNow
                };

                if (!isReviewer)
                {
                    stat.EfficiencyScore = 100;
                    stat.AverageQualityScore = 100;
                }
                else
                {
                    stat.ReviewerQualityScore = 100;
                }

                await _statsRepo.AddAsync(stat);
            }
            return stat;
        }

        public async Task TrackNewAssignmentAsync(string annotatorId, int projectId, int count)
        {
            var stat = await GetOrCreateStatAsync(annotatorId, projectId);

            stat.TotalAssigned += count;

            if (stat.TotalAssigned > 0)
            {
                stat.EfficiencyScore = ((float)stat.TotalApproved / stat.TotalAssigned) * 100;
            }

            stat.Date = DateTime.UtcNow;
            await _statsRepo.SaveChangesAsync();
        }

        public async Task TrackReviewResultAsync(
              string annotatorId,
              string reviewerId,
              int projectId,
              bool isApproved,
              double taskScore,
              decimal pricePerLabel,
              bool isCriticalError)
        {
            var annotatorStat = await GetOrCreateStatAsync(annotatorId, projectId);

            if (isApproved)
            {
                annotatorStat.TotalApproved++;
                annotatorStat.EstimatedEarnings = annotatorStat.TotalApproved * pricePerLabel;
            }
            else
            {
                annotatorStat.TotalRejected++;
                if (isCriticalError) annotatorStat.TotalCriticalErrors++;
            }

            double totalScoreSoFar = (annotatorStat.AverageQualityScore * annotatorStat.TotalReviewedTasks) + taskScore;
            annotatorStat.TotalReviewedTasks++;
            annotatorStat.AverageQualityScore = Math.Round(totalScoreSoFar / annotatorStat.TotalReviewedTasks, 2);

            if (annotatorStat.TotalAssigned > 0)
            {
                annotatorStat.EfficiencyScore = ((float)annotatorStat.TotalApproved / annotatorStat.TotalAssigned) * 100;
            }
            annotatorStat.Date = DateTime.UtcNow;

            var reviewerStat = await GetOrCreateStatAsync(reviewerId, projectId, isReviewer: true);
            reviewerStat.TotalReviewsDone++;
            reviewerStat.Date = DateTime.UtcNow;

            await _statsRepo.SaveChangesAsync();
        }

        public async Task TrackAuditResultAsync(string reviewerId, int projectId, bool isCorrectDecision)
        {
            var reviewerStat = await GetOrCreateStatAsync(reviewerId, projectId, isReviewer: true);

            reviewerStat.TotalAuditedReviews++;

            if (isCorrectDecision)
            {
                reviewerStat.TotalCorrectDecisions++;
            }

            if (reviewerStat.TotalAuditedReviews > 0)
            {
                double accuracy = (double)reviewerStat.TotalCorrectDecisions / reviewerStat.TotalAuditedReviews;
                reviewerStat.ReviewerQualityScore = Math.Round(accuracy * 100, 2);
            }

            await _statsRepo.SaveChangesAsync();
        }
    }
}