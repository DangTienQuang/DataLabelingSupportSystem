using Core.DTOs.Requests;
using Core.DTOs.Responses;

namespace BLL.Interfaces
{
    public interface IProjectService
    {
        Task<ProjectDetailResponse> CreateProjectAsync(string managerId, CreateProjectRequest request);
        Task<List<AnnotatorProjectStatsResponse>> GetAssignedProjectsAsync(string annotatorId);
        Task ImportDataItemsAsync(int projectId, List<string> storageUrls);
        Task<ProjectDetailResponse?> GetProjectDetailsAsync(int projectId);
        Task<ManagerStatsResponse> GetManagerStatsAsync(string managerId);
        Task<List<ProjectSummaryResponse>> GetProjectsByManagerAsync(string managerId);
        Task UpdateProjectAsync(int projectId, UpdateProjectRequest request);
        Task DeleteProjectAsync(int projectId);
        Task<List<Core.DTOs.Responses.BucketResponse>> GetBucketsAsync(int projectId, string userId);
        Task<byte[]> ExportProjectDataAsync(int projectId, string userId);
        Task<ProjectStatisticsResponse> GetProjectStatisticsAsync(int projectId);
        Task GenerateInvoicesAsync(int projectId);
 
    }
}