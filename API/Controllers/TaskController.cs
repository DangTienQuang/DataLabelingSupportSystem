using BLL.Interfaces;
using DTOs.Constants;
using DTOs.Requests;
using DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing tasks and assignments.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;

        public TaskController(ITaskService taskService, IProjectService projectService)
        {
            _taskService = taskService;
            _projectService = projectService;
        }

        /// <summary>
        /// Assigns tasks to an annotator.
        /// </summary>
        /// <param name="request">The assignment request details.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Tasks assigned successfully.</response>
        /// <response code="400">If assignment fails.</response>
        [HttpPost("assign")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> AssignTasks([FromBody] AssignTaskRequest request)
        {
            try
            {
                await _taskService.AssignTasksToAnnotatorAsync(request);
                return Ok(new { Message = "Tasks assigned successfully" });
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        /// <summary>
        /// Gets dashboard statistics for the current user.
        /// </summary>
        /// <returns>Statistics appropriate for the user's role (Manager or Annotator).</returns>
        /// <response code="200">Returns statistics.</response>
        /// <response code="401">If user is unauthorized.</response>
        [HttpGet("dashboard-stats")]
        [ProducesResponseType(typeof(ManagerStatsResponse), 200)]
        [ProducesResponseType(typeof(AnnotatorStatsResponse), 200)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> GetMyStats()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            if (role == UserRoles.Manager || role == UserRoles.Admin)
            {
                var managerStats = await _projectService.GetManagerStatsAsync(userId);
                return Ok(managerStats);
            }
            var stats = await _taskService.GetAnnotatorStatsAsync(userId);
            return Ok(stats);
        }

        /// <summary>
        /// Gets tasks assigned to the current user.
        /// </summary>
        /// <param name="projectId">Optional project ID to filter by.</param>
        /// <param name="status">Optional status to filter by (e.g., Assigned, InProgress).</param>
        /// <returns>A list of tasks.</returns>
        /// <response code="200">Returns list of tasks.</response>
        /// <response code="401">If user is unauthorized.</response>
        [HttpGet("my-tasks")]
        [ProducesResponseType(typeof(IEnumerable<TaskResponse>), 200)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> GetMyTasks([FromQuery] int projectId = 0, [FromQuery] string? status = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var tasks = await _taskService.GetMyTasksAsync(projectId, userId, status);
            return Ok(tasks);
        }

        /// <summary>
        /// Gets details of a specific assignment task.
        /// </summary>
        /// <param name="assignmentId">The unique identifier of the assignment.</param>
        /// <returns>The task details.</returns>
        /// <response code="200">Returns task details.</response>
        /// <response code="404">If task is not found.</response>
        /// <response code="400">If retrieval fails.</response>
        /// <response code="401">If user is unauthorized.</response>
        [HttpGet("detail/{assignmentId}")]
        [ProducesResponseType(typeof(TaskResponse), 200)]
        [ProducesResponseType(typeof(void), 404)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> GetTaskDetail(int assignmentId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var task = await _taskService.GetTaskDetailAsync(assignmentId, userId);
                if (task == null) return NotFound();
                return Ok(task);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        /// <summary>
        /// Submits annotations for a task.
        /// </summary>
        /// <param name="request">The submission request containing annotations.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Task submitted successfully.</response>
        /// <response code="400">If submission fails.</response>
        /// <response code="401">If user is unauthorized.</response>
        [HttpPost("submit")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> SubmitTask([FromBody] SubmitAnnotationRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                await _taskService.SubmitTaskAsync(userId, request);
                return Ok(new { Message = "Task submitted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
