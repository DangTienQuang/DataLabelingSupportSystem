using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing annotation tasks,
    /// including task assignment by Managers and task execution by Annotators.
    /// </summary>
    [Route("api/tasks")]
    [ApiController]
    [Authorize]
    [Tags("4. Task & Annotation")]
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
        /// Assigns annotation tasks to an Annotator and a Reviewer.
        /// </summary>
        /// <remarks>
        /// Used by Managers to distribute data items to team members.
        /// </remarks>
        /// <param name="request">Assignment payload including ProjectId, AnnotatorId, ReviewerId, and the number of images.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Tasks assigned successfully.</response>
        /// <response code="400">Assignment failed (e.g., insufficient images, user not found).</response>
        /// <response code="401">User is not authorized as a Manager.</response>
        [HttpPost("assignments")]
        [Authorize(Roles = "Manager")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> AssignTasks([FromBody] AssignTaskRequest request)
        {
            try
            {
                await _taskService.AssignTasksToAnnotatorAsync(request);
                return Ok(new { Message = "Tasks assigned successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves tasks filtered by a specific bucket within a project.
        /// </summary>
        /// <remarks>
        /// Useful for paginated or bucketed navigation of tasks.
        /// </remarks>
        /// <param name="projectId">The target project ID.</param>
        /// <param name="bucketId">The target bucket ID.</param>
        /// <returns>A list of tasks inside the requested bucket.</returns>
        /// <response code="200">Tasks retrieved successfully.</response>
        /// <response code="400">Failed to retrieve tasks.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet("projects/{projectId}/buckets/{bucketId}")]
        [Authorize(Roles = "Annotator,Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)] // Consider replacing 'object' with your specific DTO
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetTasksByBucket(int projectId, int bucketId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var tasks = await _taskService.GetTasksByBucketAsync(projectId, bucketId, userId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        // ======================================================
        // ANNOTATOR - DASHBOARD APIs
        // ======================================================

        /// <summary>
        /// Gets a list of assigned projects for the current Annotator.
        /// </summary>
        /// <remarks>
        /// Used on the main Dashboard screen. Assignments are grouped into Project Cards.
        /// Returns overall progress, deadlines, and project status.
        /// </remarks>
        /// <returns>A list of projects assigned to the current user.</returns>
        /// <response code="200">Projects retrieved successfully.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet("projects")]
        [ProducesResponseType(typeof(List<AssignedProjectResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
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
        /// Submits multiple tasks at once (Batch Submission).
        /// </summary>
        /// <remarks>
        /// Typically used when an annotator selects multiple images and submits them simultaneously.
        /// </remarks>
        /// <param name="request">Payload containing a list of Assignment IDs.</param>
        /// <returns>A result summary of the batch submission.</returns>
        /// <response code="200">Batch submission successful.</response>
        /// <response code="400">Assignment list is empty or submission failed.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpPost("submissions/batch")]
        [Authorize(Roles = "Annotator")]
        [ProducesResponseType(typeof(object), 200)] // Consider replacing 'object' with your specific DTO
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> SubmitMultipleTasks([FromBody] SubmitMultipleTasksRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (request.AssignmentIds == null || !request.AssignmentIds.Any())
                return BadRequest(new ErrorResponse { Message = "Assignment list cannot be empty." });

            try
            {
                var result = await _taskService.SubmitMultipleTasksAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves all images (assignments) for a specific project.
        /// </summary>
        /// <remarks>
        /// Called when the user enters a project workspace. 
        /// Returns the full assignment list so the frontend can handle Next/Previous navigation locally.
        /// </remarks>
        /// <param name="projectId">The target project ID.</param>
        /// <returns>A list of assignments with statuses and existing annotation data.</returns>
        /// <response code="200">Images retrieved successfully.</response>
        /// <response code="400">Failed to retrieve images.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet("projects/{projectId}/images")]
        [ProducesResponseType(typeof(List<AssignmentResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetProjectImages(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var images = await _taskService.GetTaskImagesAsync(projectId, userId);
                return Ok(images);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves assignment details for a specific data item to jump directly to it.
        /// </summary>
        /// <remarks>
        /// Used for navigation scenarios such as clicking an error notification or refreshing the page.
        /// </remarks>
        /// <param name="projectId">The project ID.</param>
        /// <param name="dataItemId">The target data item ID.</param>
        /// <returns>The detailed assignment for the requested image.</returns>
        /// <response code="200">Assignment retrieved successfully.</response>
        /// <response code="400">Failed to retrieve assignment.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet("projects/{projectId}/items/{dataItemId}")]
        [ProducesResponseType(typeof(AssignmentResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
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
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a single assignment by its ID.
        /// </summary>
        /// <remarks>
        /// Used when navigating directly to a specific assignment.
        /// </remarks>
        /// <param name="id">The assignment ID.</param>
        /// <returns>Assignment details including image and annotation data.</returns>
        /// <response code="200">Assignment retrieved successfully.</response>
        /// <response code="400">Invalid request parameters.</response>
        /// <response code="401">User is not authenticated.</response>
        /// <response code="404">Assignment not found.</response>
        [HttpGet("assignments/{id}")]
        [ProducesResponseType(typeof(AssignmentResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
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
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        // ======================================================
        // ANNOTATOR - SAVE & SUBMIT APIs
        // ======================================================

        /// <summary>
        /// Saves an annotation draft without submitting it.
        /// </summary>
        /// <remarks>
        /// Called when the user clicks "Next" or "Save". Updates the annotation data (Canvas JSON) 
        /// and sets the status to 'InProgress'.
        /// </remarks>
        /// <param name="request">Payload containing AssignmentId and annotation data.</param>
        /// <returns>A save confirmation message.</returns>
        /// <response code="200">Draft saved successfully.</response>
        /// <response code="400">Invalid input data.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpPut("drafts")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
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
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Submits an annotation task for review.
        /// </summary>
        /// <remarks>
        /// Called when the user clicks "Submit". Updates the annotation data and sets the status to 'Submitted'.
        /// </remarks>
        /// <param name="request">Payload containing AssignmentId and final annotation data.</param>
        /// <returns>A submit confirmation message.</returns>
        /// <response code="200">Task submitted successfully.</response>
        /// <response code="400">Submission failed (e.g., missing required labels).</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpPost("submissions")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
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
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }
    }
}