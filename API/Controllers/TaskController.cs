using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing annotation tasks,
    /// including task assignment by Managers and task execution by Annotators.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TaskController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        // ======================================================
        // MANAGER APIs
        // ======================================================

        /// <summary>
        /// (Manager) Assign annotation tasks to an Annotator.
        /// </summary>
        /// <param name="request">
        /// Assignment information including ProjectId, AnnotatorId, and number of images.
        /// </param>
        /// <returns>Assignment result message.</returns>
        /// <response code="200">Tasks assigned successfully.</response>
        /// <response code="400">Assignment failed (e.g. insufficient images, user not found).</response>
        /// <response code="401">User is not authorized as Manager.</response>
        [HttpPost("assign")]
        [Authorize(Roles = "Manager")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> AssignTasks([FromBody] AssignTaskRequest request)
        {
            try
            {
                await _taskService.AssignTasksToAnnotatorAsync(request);
                return Ok(new { Message = "Tasks assigned successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ======================================================
        // ANNOTATOR - DASHBOARD APIs
        // ======================================================

        /// <summary>
        /// (Annotator - Dashboard) Get list of assigned projects.
        /// </summary>
        /// <remarks>
        /// Used on the main Dashboard screen.
        /// Assignments are grouped into Project Cards.
        /// Returns overall progress, deadline, and project status.
        /// </remarks>
        /// <returns>List of projects assigned to the current user.</returns>
        /// <response code="200">Projects retrieved successfully.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet("my-projects")]
        [ProducesResponseType(typeof(List<AssignedProjectResponse>), 200)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> GetMyProjects()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var projects = await _taskService.GetAssignedProjectsAsync(userId);
            return Ok(projects);
        }

        // ======================================================
        // ANNOTATOR - WORK AREA APIs
        // ======================================================

        /// <summary>
        /// (Annotator - Work Area) Get all images (assignments) of a project.
        /// </summary>
        /// <remarks>
        /// Called when the user enters a project.
        /// Returns the full assignment list so FE can handle Next / Previous navigation.
        /// </remarks>
        /// <param name="projectId">Target project ID.</param>
        /// <returns>List of assignments with status and existing annotation data.</returns>
        /// <response code="200">Images retrieved successfully.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet("project/{projectId}/images")]
        [ProducesResponseType(typeof(List<AssignmentResponse>), 200)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> GetProjectImages(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var images = await _taskService.GetTaskImagesAsync(projectId, userId);
            return Ok(images);
        }

        /// <summary>
        /// (Annotator) Jump to a specific image inside a project.
        /// </summary>
        /// <remarks>
        /// Used for navigation scenarios such as:
        /// - Clicking an error notification
        /// - Refreshing the page (F5)
        /// </remarks>
        /// <param name="projectId">Project ID.</param>
        /// <param name="dataItemId">Target data item ID.</param>
        /// <returns>Assignment detail.</returns>
        [HttpGet("project/{projectId}/jump/{dataItemId}")]
        [ProducesResponseType(typeof(AssignmentResponse), 200)]
        public async Task<IActionResult> JumpToImage(int projectId, int dataItemId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var result = await _taskService.JumpToDataItemAsync(projectId, dataItemId, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// (Annotator) Get a single assignment by AssignmentId.
        /// </summary>
        /// <remarks>
        /// Used when navigating directly to a specific image.
        /// </remarks>
        /// <param name="id">Assignment ID.</param>
        /// <returns>Assignment detail including image and annotation data.</returns>
        [HttpGet("assignment/{id}")]
        [ProducesResponseType(typeof(AssignmentResponse), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> GetSingleAssignment(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var assignment = await _taskService.GetAssignmentByIdAsync(id, userId);
                return Ok(assignment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ======================================================
        // ANNOTATOR - SAVE & SUBMIT APIs
        // ======================================================

        /// <summary>
        /// (Annotator) Save annotation draft.
        /// </summary>
        /// <remarks>
        /// Called when the user clicks "Next" or "Save".
        /// Updates annotation data (DataJSON) and sets status to 'InProgress'.
        /// </remarks>
        /// <param name="request">
        /// Contains AssignmentId and annotation data (Canvas JSON).
        /// </param>
        /// <returns>Save result.</returns>
        /// <response code="200">Draft saved successfully.</response>
        /// <response code="400">Invalid input data.</response>
        [HttpPost("save-draft")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> SaveDraft([FromBody] SubmitAnnotationRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                await _taskService.SaveDraftAsync(userId, request);
                return Ok(new { Message = "Draft saved successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// (Annotator) Submit annotation for review.
        /// </summary>
        /// <remarks>
        /// Called when the user clicks "Submit".
        /// Updates annotation data and sets status to 'Submitted'.
        /// </remarks>
        /// <param name="request">
        /// Contains AssignmentId and final annotation data.
        /// </param>
        /// <returns>Submit result.</returns>
        /// <response code="200">Task submitted successfully.</response>
        /// <response code="400">Submission failed.</response>
        [HttpPost("submit")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> SubmitTask([FromBody] SubmitAnnotationRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                await _taskService.SubmitTaskAsync(userId, request);
                return Ok(new { Message = "Task submitted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
