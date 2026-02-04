using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller responsible for reviewing annotation tasks
    /// and auditing reviewer quality.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // ======================================================
        // REVIEWER – TASK REVIEW
        // ======================================================

        /// <summary>
        /// Submit a review decision for an assignment.
        /// </summary>
        /// <remarks>
        /// Reviewer can approve or reject a submitted annotation.
        /// </remarks>
        /// <param name="request">
        /// Review payload including AssignmentId, approval decision,
        /// error categories, and reviewer comments.
        /// </param>
        /// <returns>Review result message.</returns>
        /// <response code="200">Review submitted successfully.</response>
        /// <response code="400">Review submission failed.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpPost("submit")]
        [Authorize(Roles = "Reviewer,Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
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
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ======================================================
        // MANAGER – REVIEW AUDIT (RQS)
        // ======================================================

        /// <summary>
        /// Audit a past review to evaluate reviewer quality (RQS).
        /// </summary>
        /// <remarks>
        /// Manager evaluates whether they agree with the reviewer's decision.
        /// This data is used to calculate Reviewer Quality Score (RQS).
        /// </remarks>
        /// <param name="request">
        /// Audit payload including ReviewLogId and audit decision.
        /// </param>
        /// <returns>Audit confirmation message.</returns>
        /// <response code="200">Audit recorded successfully.</response>
        /// <response code="400">Audit failed.</response>
        /// <response code="401">User is not authorized.</response>
        [HttpPost("audit")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
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
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ======================================================
        // REVIEW QUEUE & LOOKUP
        // ======================================================

        /// <summary>
        /// Get assignments that are pending review for a specific project.
        /// </summary>
        /// <remarks>
        /// Used for Reviewer / Manager review queue screens.
        /// </remarks>
        /// <param name="projectId">Target project ID.</param>
        /// <returns>List of assignments awaiting review.</returns>
        /// <response code="200">Tasks retrieved successfully.</response>
        /// <response code="400">Failed to retrieve tasks.</response>
        [HttpGet("project/{projectId}")]
        [Authorize(Roles = "Reviewer,Manager,Admin")]
        [ProducesResponseType(typeof(IEnumerable<TaskResponse>), 200)]
        [ProducesResponseType(typeof(object), 400)]
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
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
