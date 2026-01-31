using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller responsible for project management,
    /// data ingestion, statistics, and billing.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        // ======================================================
        // PROJECT LIFECYCLE
        // ======================================================

        /// <summary>
        /// Create a new project.
        /// </summary>
        /// <remarks>
        /// Only authenticated users with Manager role are allowed.
        /// </remarks>
        /// <param name="request">Project creation payload.</param>
        /// <response code="200">Project created successfully.</response>
        /// <response code="400">Project creation failed.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpPost]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
        {
            try
            {
                var managerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(managerId))
                    return Unauthorized(new { Message = "Invalid token." });

                var project = await _projectService.CreateProjectAsync(managerId, request);
                return Ok(new
                {
                    Message = "Project created successfully.",
                    ProjectId = project.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing project.
        /// </summary>
        /// <param name="id">Target project ID.</param>
        /// <param name="request">Project update payload.</param>
        /// <response code="200">Project updated successfully.</response>
        /// <response code="400">Project update failed.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectRequest request)
        {
            try
            {
                await _projectService.UpdateProjectAsync(id, request);
                return Ok(new { Message = "Project updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a project.
        /// </summary>
        /// <param name="id">Target project ID.</param>
        /// <response code="200">Project deleted successfully.</response>
        /// <response code="400">Project deletion failed.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                await _projectService.DeleteProjectAsync(id);
                return Ok(new { Message = "Project deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ======================================================
        // DATA INGESTION
        // ======================================================

        /// <summary>
        /// Upload data files directly to a project.
        /// </summary>
        /// <remarks>
        /// Files are stored under wwwroot/uploads/project-{id}.
        /// </remarks>
        /// <param name="id">Target project ID.</param>
        /// <param name="files">Files to upload.</param>
        /// <response code="200">Files uploaded successfully.</response>
        /// <response code="400">File upload failed.</response>
        [HttpPost("{id}/upload-direct")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> UploadDataDirect(
            int id,
            [FromForm] List<IFormFile> files)
        {
            var urls = new List<string>();

            try
            {
                var folderName = Path.Combine("wwwroot", "uploads", $"project-{id}");
                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);

                if (!Directory.Exists(pathToSave))
                    Directory.CreateDirectory(pathToSave);

                foreach (var file in files)
                {
                    if (file.Length <= 0) continue;

                    var fileName = $"{DateTime.Now.Ticks}_{file.FileName}";
                    var fullPath = Path.Combine(pathToSave, fileName);

                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    urls.Add($"/uploads/project-{id}/{fileName}");
                }

                if (urls.Any())
                    await _projectService.ImportDataItemsAsync(id, urls);

                return Ok(new
                {
                    Message = $"{urls.Count} files uploaded successfully.",
                    Urls = urls
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Import data items into a project from external storage URLs.
        /// </summary>
        /// <param name="id">Target project ID.</param>
        /// <param name="request">Import payload containing storage URLs.</param>
        /// <response code="200">Data items imported successfully.</response>
        /// <response code="400">Data import failed.</response>
        [HttpPost("{id}/import-data")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> ImportData(
            int id,
            [FromBody] ImportDataRequest request)
        {
            try
            {
                await _projectService.ImportDataItemsAsync(id, request.StorageUrls);
                return Ok(new
                {
                    Message = $"{request.StorageUrls.Count} items imported successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ======================================================
        // PROJECT ACCESS (MANAGER / ANNOTATOR)
        // ======================================================

        /// <summary>
        /// Get project details by ID.
        /// </summary>
        /// <param name="id">Project ID.</param>
        /// <returns>Project details.</returns>
        /// <response code="200">Project retrieved successfully.</response>
        /// <response code="404">Project not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProjectDetailResponse), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> GetProject(int id)
        {
            var project = await _projectService.GetProjectDetailsAsync(id);
            if (project == null)
                return NotFound(new { Message = "Project not found." });

            return Ok(project);
        }

        /// <summary>
        /// Get projects assigned to the current annotator.
        /// </summary>
        /// <returns>List of assigned projects with progress statistics.</returns>
        /// <response code="200">Projects retrieved successfully.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet("annotator/assigned")]
        [ProducesResponseType(typeof(IEnumerable<AnnotatorProjectStatsResponse>), 200)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> GetAssignedProjects()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var projects = await _projectService.GetAssignedProjectsAsync(userId);
            return Ok(projects);
        }

        /// <summary>
        /// Get projects managed by the current user.
        /// </summary>
        /// <remarks>
        /// Manager-only endpoint.
        /// </remarks>
        /// <returns>List of managed projects.</returns>
        [HttpGet("manager/me")]
        [ProducesResponseType(typeof(IEnumerable<ProjectSummaryResponse>), 200)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> GetMyProjects()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var projects = await _projectService.GetProjectsByManagerAsync(userId);
            return Ok(projects);
        }

        // ======================================================
        // ANALYTICS & EXPORT
        // ======================================================

        /// <summary>
        /// Get project statistics.
        /// </summary>
        /// <param name="id">Project ID.</param>
        /// <returns>Project statistics.</returns>
        /// <response code="200">Statistics retrieved successfully.</response>
        /// <response code="400">Failed to retrieve statistics.</response>
        [HttpGet("{id}/stats")]
        [ProducesResponseType(typeof(ProjectStatisticsResponse), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> GetProjectStats(int id)
        {
            try
            {
                var stats = await _projectService.GetProjectStatisticsAsync(id);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Export project annotation data as a JSON file.
        /// </summary>
        /// <param name="id">Project ID.</param>
        /// <returns>JSON export file.</returns>
        /// <response code="200">Export completed successfully.</response>
        /// <response code="400">Export failed.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet("{id}/export")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> ExportProject(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var fileContent = await _projectService.ExportProjectDataAsync(id, userId);
                return File(
                    fileContent,
                    "application/json",
                    $"project-{id}-export.json"
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ======================================================
        // BILLING
        // ======================================================

        /// <summary>
        /// Generate invoices for a project based on current progress.
        /// </summary>
        /// <remarks>
        /// Accessible by Manager and Admin roles only.
        /// </remarks>
        /// <param name="id">Project ID.</param>
        /// <response code="200">Invoices generated successfully.</response>
        /// <response code="400">Invoice generation failed.</response>
        [HttpPost("{id}/invoices/generate")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> GenerateInvoices(int id)
        {
            try
            {
                await _projectService.GenerateInvoicesAsync(id);
                return Ok(new
                {
                    Message = "Invoices generated successfully based on current progress."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
