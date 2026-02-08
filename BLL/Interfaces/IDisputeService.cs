using Core.DTOs.Requests;
using Core.Entities;

namespace BLL.Interfaces
{
    public interface IDisputeService
    {
        Task CreateDisputeAsync(string annotatorId, CreateDisputeRequest request);
        Task ResolveDisputeAsync(ResolveDisputeRequest request);
        Task<List<Dispute>> GetDisputesAsync(int projectId, string userId, string role);
    }
}