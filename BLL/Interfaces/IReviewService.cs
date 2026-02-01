using Core.DTOs.Requests;
using Core.DTOs.Responses;

namespace BLL.Interfaces
{
    public interface IReviewService
    {
        Task ReviewAssignmentAsync(string reviewerId, ReviewRequest request);
        Task AuditReviewAsync(string managerId, AuditReviewRequest request);
        Task<List<TaskResponse>> GetTasksForReviewAsync(int projectId, string reviewerId);
    }
}