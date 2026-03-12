using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller responsible for managing task reviews, fetching review queues, 
    /// and auditing reviewer quality.
    /// </summary>
    [Route("api/reviews")]
    [ApiController]
    [Authorize]
    [Tags("5. Review & QA")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // ======================================================
        // REVIEW QUEUE & LOOKUP
        // ======================================================

        /// <summary>
        /// Retrieves a list of assigned projects that have tasks pending review.
        /// </summary>
        /// <remarks>
        /// Used by Reviewers to view their project queue on the dashboard.
        /// </remarks>
        /// <returns>A list of projects with pending review tasks.</returns>
        /// <response code="200">Projects retrieved successfully.</response>
        /// <response code="400">Failed to retrieve projects.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet("projects")]
        [Authorize(Roles = "Reviewer,Manager,Admin")]
        [ProducesResponseType(typeof(IEnumerable<AssignedProjectResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetReviewerProjects()
        {
            var reviewerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(reviewerId)) return Unauthorized();

            try
            {
                var projects = await _reviewService.GetReviewerProjectsAsync(reviewerId);
                return Ok(projects);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Gets tasks that are pending review for a specific project.
        /// </summary>
        /// <remarks>
        /// Used for loading the review workspace for a specific project.
        /// </remarks>
        /// <param name="projectId">The ID of the target project.</param>
        /// <returns>A list of assignments awaiting review.</returns>
        /// <response code="200">Tasks retrieved successfully.</response>
        /// <response code="400">Failed to retrieve tasks.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet("projects/{projectId}/tasks")]
        [Authorize(Roles = "Reviewer,Manager,Admin,Annotator")]
        [ProducesResponseType(typeof(IEnumerable<TaskResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetTasksForReview(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var tasks = await _reviewService.GetTasksForReviewAsync(projectId, userId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        // ======================================================
        // REVIEWER – TASK REVIEW
        // ======================================================

        /// <summary>
        /// Submits a review decision (Approve/Reject) for an assignment.
        /// </summary>
        /// <remarks>
        /// Submitting a rejection requires specifying an error category and leaving a comment.
        /// </remarks>
        /// <param name="request">Review payload including the assignment ID, decision, error categories, and comments.</param>
        /// <returns>A confirmation message indicating the result.</returns>
        /// <response code="200">Review submitted successfully.</response>
        /// <response code="400">Validation failed or invalid task status.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpPost]
        [Authorize(Roles = "Reviewer,Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)] // Consider changing 'object' to a specific response DTO if available
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> ReviewTask([FromBody] ReviewRequest request)
        {
            var reviewerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(reviewerId)) return Unauthorized();

            try
            {
                await _reviewService.ReviewAssignmentAsync(reviewerId, request);
                return Ok(new
                {
                    Message = request.IsApproved ? "Approved" : "Rejected"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        // ======================================================
        // MANAGER – REVIEW AUDIT (RQS)
        // ======================================================

        /// <summary>
        /// Audits a past review to evaluate reviewer quality (RQS).
        /// </summary>
        /// <remarks>
        /// Managers evaluate whether they agree with the reviewer's decision. 
        /// This data dynamically updates the Reviewer Quality Score (RQS).
        /// </remarks>
        /// <param name="request">Payload containing the review log ID and audit decision.</param>
        /// <returns>An audit confirmation message.</returns>
        /// <response code="200">Audit recorded successfully.</response>
        /// <response code="400">Audit failed or already audited.</response>
        /// <response code="401">User is not authorized.</response>
        [HttpPost("audits")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> AuditReview([FromBody] AuditReviewRequest request)
        {
            var managerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(managerId)) return Unauthorized();

            try
            {
                await _reviewService.AuditReviewAsync(managerId, request);
                return Ok(new { Message = "Audit submitted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }
    }
}