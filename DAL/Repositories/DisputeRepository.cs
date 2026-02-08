using DAL.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class DisputeRepository : Repository<Dispute>, IDisputeRepository
    {
        private ApplicationDbContext AppContext => (ApplicationDbContext)_context;

        public DisputeRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Dispute>> GetDisputesByProjectAsync(int projectId)
        {
            return await AppContext.Disputes
                .Include(d => d.Annotator)
                .Include(d => d.Assignment)
                    .ThenInclude(a => a.DataItem)
                .Where(d => d.Assignment.ProjectId == projectId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Dispute>> GetDisputesByAnnotatorAsync(string annotatorId)
        {
            return await AppContext.Disputes
                .Include(d => d.Assignment)
                    .ThenInclude(a => a.DataItem)
                .Where(d => d.AnnotatorId == annotatorId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dispute?> GetDisputeWithDetailsAsync(int disputeId)
        {
            return await AppContext.Disputes
                .Include(d => d.Annotator)
                .Include(d => d.Assignment)
                .FirstOrDefaultAsync(d => d.Id == disputeId);
        }
    }
}