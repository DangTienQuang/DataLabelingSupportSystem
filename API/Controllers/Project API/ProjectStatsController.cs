using BLL.Interfaces;
using Core.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Controller for retrieving statistical data and performance metrics for projects and managers.
    /// </summary>
    [Route("api/projects")]
    [ApiController]
    [Authorize]
    [Tags("3. Project Management")]
    public class ProjectStatsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectStatsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        /// <summary>
        /// Retrieves comprehensive statistics for a specific project.
        /// </summary>
        /// <remarks>
        /// Accessible by Managers, Admins, and Reviewers. 
        /// Provides metrics such as completion percentage, task statuses, and reviewer performance.
        /// </remarks>
        /// <param name="projectId">The ID of the target project.</param>
        /// <returns>Statistical data for the requested project.</returns>
        /// <response code="200">Statistics retrieved successfully.</response>
        /// <response code="400">Failed to retrieve statistics (e.g., project not found).</response>
        /// <response code="401">User is not authenticated or not authorized.</response>
        [HttpGet("{projectId}/statistics")]
        [Authorize(Roles = "Manager,Admin,Reviewer,Annotator")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetProjectStatistics(int projectId)
        {
            try
            {
                var stats = await _projectService.GetProjectStatisticsAsync(projectId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves aggregated statistics for all projects managed by a specific user.
        /// </summary>
        /// <remarks>
        /// Accessible by Managers and Admins. Provides a high-level overview of the manager's portfolio performance.
        /// </remarks>
        /// <param name="managerId">The target manager's user ID.</param>
        /// <returns>Aggregated statistical data across all managed projects.</returns>
        /// <response code="200">Manager statistics retrieved successfully.</response>
        /// <response code="400">Failed to retrieve manager statistics.</response>
        /// <response code="401">User is not authenticated or not authorized.</response>
        [HttpGet("managers/{managerId}/statistics")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetManagerStats(string managerId)
        {
            try
            {
                var stats = await _projectService.GetManagerStatsAsync(managerId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }
    }
}