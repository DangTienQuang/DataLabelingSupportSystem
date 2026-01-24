using DTOs.Entities;
using DTOs.Responses;

namespace DAL.Interfaces
{
    public interface IAssignmentRepository : IRepository<Assignment>
    {
        Task<List<Assignment>> GetAssignmentsByAnnotatorAsync(string annotatorId, int projectId = 0, string? status = null);
        Task<List<Assignment>> GetAssignmentsForReviewerAsync(int projectId);
        Task<Assignment?> GetAssignmentWithDetailsAsync(int id);
        Task<List<DataItem>> GetUnassignedDataItemsAsync(int projectId, int quantity);
        Task<AnnotatorStatsResponse> GetAnnotatorStatsAsync(string annotatorId);
    }
}