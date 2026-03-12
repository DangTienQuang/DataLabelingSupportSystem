using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Handles operations related to task disputes between Annotators and Managers/Reviewers.
    /// </summary>
    [Route("api/disputes")]
    [ApiController]
    [Authorize]
    [Tags("6. Dispute & Logs")]
    public class DisputeController : ControllerBase
    {
        private readonly IDisputeService _disputeService;

        public DisputeController(IDisputeService disputeService)
        {
            _disputeService = disputeService;
        }

        /// <summary>
        /// Submits a new dispute for a rejected task.
        /// </summary>
        /// <remarks>
        /// Only users with the 'Annotator' role can create a dispute.
        /// </remarks>
        /// <param name="request">The payload containing the assignment ID and the reason for the dispute.</param>
        /// <response code="200">Dispute submitted successfully.</response>
        /// <response code="400">Invalid request data or dispute already exists.</response>
        /// <response code="401">User is not authenticated or not authorized.</response>
        [HttpPost]
        [Authorize(Roles = "Annotator")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> CreateDispute([FromBody] CreateDisputeRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _disputeService.CreateDisputeAsync(userId, request);
            return Ok(new { Message = "Dispute submitted successfully." });
        }

        /// <summary>
        /// Resolves an existing dispute.
        /// </summary>
        /// <remarks>
        /// Only users with 'Manager' or 'Admin' roles can resolve disputes.
        /// </remarks>
        /// <param name="request">The payload containing the dispute ID and the resolution decision (approve/reject).</param>
        /// <response code="200">Dispute resolved successfully.</response>
        /// <response code="400">Invalid dispute data or dispute already resolved.</response>
        /// <response code="401">User is not authenticated or not authorized.</response>
        [HttpPost("resolve")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> ResolveDispute([FromBody] ResolveDisputeRequest request)
        {
            var managerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(managerId)) return Unauthorized();

            await _disputeService.ResolveDisputeAsync(managerId, request);
            return Ok(new { Message = "Dispute resolved." });
        }

        /// <summary>
        /// Retrieves a list of disputes based on the specified project.
        /// </summary>
        /// <remarks>
        /// Returns disputes relevant to the user's role (Annotators see their own, Managers/Admins see all for the project).
        /// </remarks>
        /// <param name="projectId">The ID of the project to filter disputes by.</param>
        /// <response code="200">A list of disputes for the requested project.</response>
        /// <response code="400">Invalid project ID.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet]
        [ProducesResponseType(200)] // You can replace '200' with typeof(List<DisputeResponse>) if you have a specific DTO
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetDisputes([FromQuery] int projectId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                var disputes = await _disputeService.GetDisputesAsync(projectId, userId, role);
                return Ok(disputes);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }
    }
}