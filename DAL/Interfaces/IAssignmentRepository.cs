using Core.DTOs.Responses;
using Core.Entities;

namespace DAL.Interfaces
{
    public interface IAssignmentRepository : IRepository<Assignment>
    {
        Task<List<Assignment>> GetAssignmentsByAnnotatorAsync(string annotatorId, int projectId = 0, string? status = null);
        Task<List<Assignment>> GetAssignmentsForReviewerAsync(int projectId, string reviewerId);
        Task<Assignment?> GetAssignmentWithDetailsAsync(int id);
        Task<List<DataItem>> GetUnassignedDataItemsAsync(int projectId, int quantity);
        Task<AnnotatorStatsResponse> GetAnnotatorStatsAsync(string annotatorId);
        Task<List<Assignment>> GetAssignmentsByBucketAsync(int projectId, int bucketId, string userId);
        Task ResetAssignmentsByProjectAsync(int projectId, string reason);
        Task<int> CountActiveTasksAsync(int projectId);
    }
}