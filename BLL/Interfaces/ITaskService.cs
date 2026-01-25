using DTOs.Requests;
using DTOs.Responses;

namespace BLL.Interfaces
{
    public interface ITaskService
    {
        // --- 1. MANAGER ---
        Task AssignTasksToAnnotatorAsync(AssignTaskRequest request);
        Task<AnnotatorStatsResponse> GetAnnotatorStatsAsync(string annotatorId);

        // --- 2. ANNOTATOR (FE MỚI) ---
        // Thay thế GetMyTasksAsync cũ
        Task<List<AssignedProjectResponse>> GetAssignedProjectsAsync(string annotatorId);

        // Thay thế GetTaskDetailAsync cũ
        Task<List<AssignmentResponse>> GetTaskImagesAsync(int projectId, string annotatorId);

        // --- 3. SUBMIT & SAVE ---
        Task SaveDraftAsync(string userId, SubmitAnnotationRequest request);
        Task SubmitTaskAsync(string userId, SubmitAnnotationRequest request);
    }
}