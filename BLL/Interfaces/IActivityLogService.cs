using Core.Entities;

namespace BLL.Interfaces
{
    public interface IActivityLogService
    {
        Task LogActionAsync(string userId, string actionType, string entityName, string entityId, string description);
        Task<List<ActivityLog>> GetSystemLogsAsync();
        Task<List<ActivityLog>> GetProjectLogsAsync(int projectId);
    }
}