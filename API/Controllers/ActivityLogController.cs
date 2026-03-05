using BLL.Interfaces;
using Core.Constants;
using Core.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    /// <summary>
    /// Provides APIs for retrieving system and project-level activity logs.
    /// </summary>
    [Route("api/logs")]
    [ApiController]
    [Authorize]
    [Tags("6. Dispute & Logs")]
    public class ActivityLogController : ControllerBase
    {
        private readonly IActivityLogService _logService;

        public ActivityLogController(IActivityLogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// Retrieves system-wide activity logs.
        /// </summary>
        /// <remarks>
        /// Only accessible by Admins. Used for auditing global system events.
        /// </remarks>
        /// <returns>A list of system activity logs.</returns>
        /// <response code="200">System logs retrieved successfully.</response>
        /// <response code="400">Failed to retrieve logs.</response>
        /// <response code="401">User is not authenticated or not authorized.</response>
        [HttpGet("system")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(object), 200)] // Consider replacing 'object' with your specific Log DTO list
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetSystemLogs()
        {
            try
            {
                var logs = await _logService.GetSystemLogsAsync();
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves activity logs for a specific project.
        /// </summary>
        /// <remarks>
        /// Accessible by Admins and Managers. Used to track actions and changes within a specific project.
        /// </remarks>
        /// <param name="projectId">The ID of the target project.</param>
        /// <returns>A list of project-specific activity logs.</returns>
        /// <response code="200">Project logs retrieved successfully.</response>
        /// <response code="400">Failed to retrieve project logs.</response>
        /// <response code="401">User is not authenticated or not authorized.</response>
        [HttpGet("projects/{projectId}")]
        [Authorize(Roles = "Admin,Manager")]
        [ProducesResponseType(typeof(object), 200)] // Consider replacing 'object' with your specific Log DTO list
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetProjectLogs(int projectId)
        {
            try
            {
                var logs = await _logService.GetProjectLogsAsync(projectId);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }
    }
}