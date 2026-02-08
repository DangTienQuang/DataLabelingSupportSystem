using BLL.Interfaces;
using Core.Entities;
using DAL.Interfaces;

namespace BLL.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IActivityLogRepository _logRepo;

        public ActivityLogService(IActivityLogRepository logRepo)
        {
            _logRepo = logRepo;
        }

        public async Task LogActionAsync(string userId, string actionType, string entityName, string entityId, string description)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                ActionType = actionType,
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                Timestamp = DateTime.UtcNow
            };

            await _logRepo.AddAsync(log);
            await _logRepo.SaveChangesAsync();
        }

        public async Task<List<ActivityLog>> GetSystemLogsAsync()
        {
            return await _logRepo.GetSystemLogsAsync(200);
        }

        public async Task<List<ActivityLog>> GetProjectLogsAsync(int projectId)
        {
            return await _logRepo.GetLogsByEntityAsync("Project", projectId.ToString());
        }
    }
}