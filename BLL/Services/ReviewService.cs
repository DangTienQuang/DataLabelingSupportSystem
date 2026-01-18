using BLL.Interfaces;
using DAL.Interfaces;
using DTOs.Entities;
using DTOs.Requests;
using DTOs.Responses;
using System.Text.Json;

namespace BLL.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IAssignmentRepository _assignmentRepo;
        private readonly IRepository<ReviewLog> _reviewLogRepo;
        private readonly IRepository<DataItem> _dataItemRepo;

        public ReviewService(
            IAssignmentRepository assignmentRepo,
            IRepository<ReviewLog> reviewLogRepo,
            IRepository<DataItem> dataItemRepo)
        {
            _assignmentRepo = assignmentRepo;
            _reviewLogRepo = reviewLogRepo;
            _dataItemRepo = dataItemRepo;
        }

        public async Task ReviewAssignmentAsync(string reviewerId, ReviewRequest request)
        {
            var assignment = await _assignmentRepo.GetByIdAsync(request.AssignmentId);
            if (assignment == null) throw new Exception("Assignment not found");

            if (assignment.Status != "Submitted")
                throw new Exception("This task is not ready for review.");

            var log = new ReviewLog
            {
                AssignmentId = assignment.Id,
                ReviewerId = reviewerId,
                Decision = request.IsApproved ? "Approve" : "Reject",
                Comment = request.Comment,
                ErrorCategory = request.ErrorCategory,
                CreatedAt = DateTime.UtcNow
            };
            await _reviewLogRepo.AddAsync(log);

            if (request.IsApproved)
            {
                assignment.Status = "Completed";
                if (assignment.DataItemId > 0)
                {
                    var dataItem = await _dataItemRepo.GetByIdAsync(assignment.DataItemId);
                    if (dataItem != null)
                    {
                        dataItem.Status = "Done";
                        _dataItemRepo.Update(dataItem);
                    }
                }
            }
            else
            {
                assignment.Status = "Rejected";
            }

            _assignmentRepo.Update(assignment);
            await _assignmentRepo.SaveChangesAsync();
        }

        public async Task<List<TaskResponse>> GetTasksForReviewAsync(int projectId)
        {
            var assignments = await _assignmentRepo.GetAssignmentsForReviewerAsync(projectId);

            return assignments.Select(a => new TaskResponse
            {
                AssignmentId = a.Id,
                DataItemId = a.DataItemId,
                StorageUrl = a.DataItem?.StorageUrl ?? "",
                ProjectName = a.Project?.Name ?? "",
                Status = a.Status,
                Labels = a.Project?.LabelClasses.Select(l => new LabelResponse
                {
                    Id = l.Id,
                    Name = l.Name,
                    Color = l.Color,
                    GuideLine = l.GuideLine
                }).ToList() ?? new List<LabelResponse>(),
                ExistingAnnotations = a.Annotations.Select(an => new
                {
                    an.ClassId,
                    Value = JsonDocument.Parse(an.Value).RootElement
                }).ToList<object>()
            }).ToList();
        }
    }
}