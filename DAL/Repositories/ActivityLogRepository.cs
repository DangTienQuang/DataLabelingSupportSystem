using DAL.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class ActivityLogRepository : Repository<ActivityLog>, IActivityLogRepository
    {
        private ApplicationDbContext AppContext => (ApplicationDbContext)_context;

        public ActivityLogRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<ActivityLog>> GetSystemLogsAsync(int limit = 100)
        {
            return await AppContext.ActivityLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetLogsByUserAsync(string userId)
        {
            return await AppContext.ActivityLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetLogsByEntityAsync(string entityName, string entityId)
        {
            return await AppContext.ActivityLogs
                .Include(l => l.User)
                .Where(l => l.EntityName == entityName && l.EntityId == entityId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}