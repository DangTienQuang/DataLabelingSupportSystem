using BLL.Interfaces;
using Core.Constants;
using Core.DTOs.Requests;
using Core.Entities;
using DAL.Interfaces;

namespace BLL.Services
{
    public class DisputeService : IDisputeService
    {
        private readonly IDisputeRepository _disputeRepo;
        private readonly IAssignmentRepository _assignmentRepo;

        public DisputeService(IDisputeRepository disputeRepo, IAssignmentRepository assignmentRepo)
        {
            _disputeRepo = disputeRepo;
            _assignmentRepo = assignmentRepo;
        }

        public async Task CreateDisputeAsync(string annotatorId, CreateDisputeRequest request)
        {
            var assignment = await _assignmentRepo.GetByIdAsync(request.AssignmentId);
            if (assignment == null) throw new Exception("Task not found");

            if (assignment.AnnotatorId != annotatorId)
                throw new Exception("Unauthorized: You do not own this task.");
            if (assignment.Status != TaskStatusConstants.Rejected)
                throw new Exception("You can only dispute rejected tasks.");
            var existingDisputes = await _disputeRepo.GetDisputesByAnnotatorAsync(annotatorId);
            if (existingDisputes.Any(d => d.AssignmentId == request.AssignmentId && d.Status == "Pending"))
            {
                throw new Exception("A dispute for this task is already pending.");
            }

            var dispute = new Dispute
            {
                AssignmentId = request.AssignmentId,
                AnnotatorId = annotatorId,
                Reason = request.Reason,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _disputeRepo.AddAsync(dispute);
            await _disputeRepo.SaveChangesAsync();
        }

        public async Task ResolveDisputeAsync(ResolveDisputeRequest request)
        {
            var dispute = await _disputeRepo.GetDisputeWithDetailsAsync(request.DisputeId);
            if (dispute == null) throw new Exception("Dispute not found");

            if (dispute.Status != "Pending") throw new Exception("This dispute has already been resolved.");

            dispute.ManagerComment = request.ManagerComment;
            dispute.ResolvedAt = DateTime.UtcNow;

            if (request.IsAccepted)
            {
                dispute.Status = "Accepted";
                if (dispute.Assignment != null)
                {
                    dispute.Assignment.Status = TaskStatusConstants.Approved;
                }
            }
            else
            {
                dispute.Status = "Rejected";
            }

            await _disputeRepo.SaveChangesAsync();
        }

        public async Task<List<Dispute>> GetDisputesAsync(int projectId, string userId, string role)
        {
            if (role == UserRoles.Manager || role == UserRoles.Admin)
            {
                return await _disputeRepo.GetDisputesByProjectAsync(projectId);
            }
            else
            {
                return await _disputeRepo.GetDisputesByAnnotatorAsync(userId);
            }
        }
    }
}