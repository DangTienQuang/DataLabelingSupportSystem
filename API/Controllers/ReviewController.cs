using BLL.Interfaces;
using DTOs.Requests;
using DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller for reviewing assignments.
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

        /// <summary>
        /// Submits a review for an assignment (approve/reject).
        /// </summary>
        /// <param name="request">The review details.</param>
        /// <returns>A message indicating approval or rejection.</returns>
        /// <response code="200">Review submitted successfully.</response>
        /// <response code="400">If review submission fails.</response>
        [HttpPost]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> ReviewTask([FromBody] ReviewRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            try
            {
                await _reviewService.ReviewAssignmentAsync(userId, request);
                return Ok(new { Message = request.IsApproved ? "Approved" : "Rejected" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Gets a list of tasks (assignments) that need to be reviewed for a specific project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <returns>A list of tasks awaiting review.</returns>
        /// <response code="200">Returns list of tasks.</response>
        /// <response code="400">If retrieval fails.</response>
        [HttpGet("project/{projectId}")]
        [ProducesResponseType(typeof(IEnumerable<TaskResponse>), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> GetTasksForReview(int projectId)
        {
            try
            {
                var tasks = await _reviewService.GetTasksForReviewAsync(projectId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
