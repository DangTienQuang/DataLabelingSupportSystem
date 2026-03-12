using Core.DTOs.Requests;
using Core.DTOs.Responses;
using Core.Entities;

namespace BLL.Interfaces
{
    public interface IDisputeService
    {
        Task CreateDisputeAsync(string annotatorId, CreateDisputeRequest request);
        Task ResolveDisputeAsync(string managerId, ResolveDisputeRequest request);
        Task<List<DisputeResponse>> GetDisputesAsync(int projectId, string userId, string role);
    }
}