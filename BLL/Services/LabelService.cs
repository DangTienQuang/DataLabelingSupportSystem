using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using DAL.Interfaces;
using Core.Entities;
using System.Text.Json;

namespace BLL.Services
{
    public class LabelService : ILabelService
    {
        private readonly ILabelRepository _labelRepo;
        private readonly IAssignmentRepository _assignmentRepo;
        private readonly IActivityLogService _logService;

        public LabelService(
            ILabelRepository labelRepo,
            IAssignmentRepository assignmentRepo,
            IActivityLogService logService)
        {
            _labelRepo = labelRepo;
            _assignmentRepo = assignmentRepo;
            _logService = logService;
        }

        public async Task<LabelResponse> CreateLabelAsync(CreateLabelRequest request)
        {
            if (await _labelRepo.ExistsInProjectAsync(request.ProjectId, request.Name))
                throw new Exception("Label name already exists in this project.");

            var label = new LabelClass
            {
                ProjectId = request.ProjectId,
                Name = request.Name,
                Color = request.Color,
                GuideLine = request.GuideLine,
                ExampleImageUrl = request.ExampleImageUrl,
                IsDefault = request.IsDefault,
                DefaultChecklist = (request.Checklist != null && request.Checklist.Any())
                                    ? JsonSerializer.Serialize(request.Checklist)
                                    : "[]"
            };

            await _labelRepo.AddAsync(label);
            await _labelRepo.SaveChangesAsync();

            await _logService.LogActionAsync(
                "System",
                "Create",
                "LabelClass",
                label.Id.ToString(),
                $"Created label '{label.Name}' for Project {label.ProjectId}"
            );

            return new LabelResponse
            {
                Id = label.Id,
                Name = label.Name,
                Color = label.Color,
                GuideLine = label.GuideLine,
                ExampleImageUrl = label.ExampleImageUrl,
                IsDefault = label.IsDefault,
                Checklist = request.Checklist ?? new List<string>()
            };
        }

        public async Task<LabelResponse> UpdateLabelAsync(int labelId, UpdateLabelRequest request)
        {
            var label = await _labelRepo.GetByIdAsync(labelId);
            if (label == null) throw new Exception("Label not found");

            bool isCriticalChange = label.Name != request.Name || label.GuideLine != request.GuideLine;
            string oldName = label.Name;

            label.Name = request.Name;
            label.Color = request.Color;
            label.GuideLine = request.GuideLine;
            label.ExampleImageUrl = request.ExampleImageUrl;
            label.IsDefault = request.IsDefault;

            if (request.Checklist != null)
            {
                label.DefaultChecklist = request.Checklist.Any()
                                         ? JsonSerializer.Serialize(request.Checklist)
                                         : "[]";
            }

            _labelRepo.Update(label);
            await _labelRepo.SaveChangesAsync();

            if (isCriticalChange)
            {
                int activeTasks = await _assignmentRepo.CountActiveTasksAsync(label.ProjectId);

                if (activeTasks > 0)
                {
                    await _assignmentRepo.ResetAssignmentsByProjectAsync(
                        label.ProjectId,
                        $"AUTO-RESET: Label '{oldName}' updated to '{request.Name}'. Please review."
                    );

                    await _logService.LogActionAsync(
                        "System",
                        "ResetTasks",
                        "Project",
                        label.ProjectId.ToString(),
                        $"Reset {activeTasks} tasks because Label '{oldName}' was updated."
                    );
                }
            }

            return new LabelResponse
            {
                Id = label.Id,
                Name = label.Name,
                Color = label.Color,
                GuideLine = label.GuideLine,
                ExampleImageUrl = label.ExampleImageUrl,
                IsDefault = label.IsDefault,
                Checklist = !string.IsNullOrEmpty(label.DefaultChecklist)
                            ? JsonSerializer.Deserialize<List<string>>(label.DefaultChecklist, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<string>()
                            : new List<string>()
            };
        }

        public async Task DeleteLabelAsync(int labelId)
        {
            var label = await _labelRepo.GetByIdAsync(labelId);
            if (label == null) throw new Exception("Label not found");
            int activeTasks = await _assignmentRepo.CountActiveTasksAsync(label.ProjectId);

            if (activeTasks > 0)
            {
                await _assignmentRepo.ResetAssignmentsByProjectAsync(
                    label.ProjectId,
                    $"AUTO-RESET: Label '{label.Name}' was deleted. Please review annotations."
                );

                await _logService.LogActionAsync(
                    "System",
                    "ResetTasks",
                    "Project",
                    label.ProjectId.ToString(),
                    $"Reset {activeTasks} tasks because Label '{label.Name}' was deleted."
                );
            }

            _labelRepo.Delete(label);
            await _labelRepo.SaveChangesAsync();
        }
    }
}