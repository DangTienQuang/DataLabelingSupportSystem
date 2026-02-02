using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IStatisticService
    {
        Task TrackNewAssignmentAsync(string annotatorId, int projectId, int count);
        Task TrackReviewResultAsync(string annotatorId, string reviewerId, int projectId, bool isApproved, double taskScore, decimal pricePerLabel, bool isCriticalError);
        Task TrackAuditResultAsync(string reviewerId, int projectId, bool isCorrectDecision);
    }
}
