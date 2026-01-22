using BLL.Interfaces;
using DTOs.Requests;
using DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing projects.
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

        /// <summary>
        /// Creates a new project.
        /// </summary>
        /// <param name="request">The project creation request.</param>
        /// <returns>A confirmation message and the new project's ID.</returns>
        /// <response code="200">Project created successfully.</response>
        /// <response code="400">If the creation fails.</response>
        /// <response code="401">If the user is not authenticated.</response>
        [HttpPost]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
        {
            try
            {
                var managerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(managerId)) return Unauthorized(new { Message = "Invalid token" });

                var project = await _projectService.CreateProjectAsync(managerId, request);
                return Ok(new { Message = "Project created successfully", ProjectId = project.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Uploads data files directly to a project.
        /// </summary>
        /// <param name="id">The unique identifier of the project.</param>
        /// <param name="files">The list of files to upload.</param>
        /// <returns>A confirmation message and the URLs of uploaded files.</returns>
        /// <response code="200">Files uploaded successfully.</response>
        /// <response code="400">If upload fails.</response>
        [HttpPost("{id}/upload-direct")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> UploadDataDirect(int id, [FromForm] List<IFormFile> files) 
        {
            var urls = new List<string>();
            try
            {
                var folderName = Path.Combine("wwwroot", "uploads", $"project-{id}");
                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);

                if (!Directory.Exists(pathToSave)) Directory.CreateDirectory(pathToSave);

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var fileName = $"{DateTime.Now.Ticks}_{file.FileName}";
                        var fullPath = Path.Combine(pathToSave, fileName);

                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        var dbUrl = $"/uploads/project-{id}/{fileName}";
                        urls.Add(dbUrl);
                    }
                }

                if (urls.Any())
                {
                    await _projectService.ImportDataItemsAsync(id, urls);
                }

                return Ok(new { Message = $"{urls.Count} files uploaded successfully", Urls = urls });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Gets detailed information about a project.
        /// </summary>
        /// <param name="id">The unique identifier of the project.</param>
        /// <returns>The project details.</returns>
        /// <response code="200">Returns project details.</response>
        /// <response code="404">If the project is not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProjectDetailResponse), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> GetProject(int id)
        {
            var projectDto = await _projectService.GetProjectDetailsAsync(id);
            if (projectDto == null) return NotFound(new { Message = "Project not found" });
            return Ok(projectDto);
        }

        /// <summary>
        /// Imports data items into a project from URLs.
        /// </summary>
        /// <param name="id">The unique identifier of the project.</param>
        /// <param name="request">The import request containing URLs.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Items imported successfully.</response>
        /// <response code="400">If import fails.</response>
        [HttpPost("{id}/import-data")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> ImportData(int id, [FromBody] ImportDataRequest request)
        {
            try
            {
                await _projectService.ImportDataItemsAsync(id, request.StorageUrls);
                return Ok(new { Message = $"{request.StorageUrls.Count} items imported successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Gets projects assigned to the current annotator.
        /// </summary>
        /// <returns>A list of projects assigned to the user.</returns>
        /// <response code="200">Returns list of projects.</response>
        /// <response code="401">If user is unauthorized.</response>
        [HttpGet("annotator/assigned")]
        [ProducesResponseType(typeof(IEnumerable<ProjectSummaryResponse>), 200)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> GetAssignedProjects()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var projects = await _projectService.GetAssignedProjectsAsync(userId);
            return Ok(projects);
        }

        /// <summary>
        /// Gets projects managed by the current user (Manager).
        /// </summary>
        /// <returns>A list of projects managed by the user.</returns>
        /// <response code="200">Returns list of projects.</response>
        /// <response code="401">If user is unauthorized.</response>
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

        /// <summary>
        /// Updates a project.
        /// </summary>
        /// <param name="id">The unique identifier of the project to update.</param>
        /// <param name="request">The update request details.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Project updated successfully.</response>
        /// <response code="400">If update fails.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectRequest request)
        {
            try
            {
                await _projectService.UpdateProjectAsync(id, request);
                return Ok(new { Message = "Project updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a project.
        /// </summary>
        /// <param name="id">The unique identifier of the project to delete.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Project deleted successfully.</response>
        /// <response code="400">If deletion fails.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                await _projectService.DeleteProjectAsync(id);
                return Ok(new { Message = "Project deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Exports project data (annotations) as a JSON file.
        /// </summary>
        /// <param name="id">The unique identifier of the project.</param>
        /// <returns>A JSON file containing project export data.</returns>
        /// <response code="200">Returns the export file.</response>
        /// <response code="400">If export fails.</response>
        /// <response code="401">If user is unauthorized.</response>
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
                return File(fileContent, "application/json", $"project-{id}-export.json");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Gets statistics for a project.
        /// </summary>
        /// <param name="id">The unique identifier of the project.</param>
        /// <returns>The project statistics.</returns>
        /// <response code="200">Returns project statistics.</response>
        /// <response code="400">If retrieval fails.</response>
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
        /// Generates invoices for a project based on progress (Admin/Manager only).
        /// </summary>
        /// <param name="id">The unique identifier of the project.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Invoices generated successfully.</response>
        /// <response code="400">If generation fails.</response>
        [HttpPost("{id}/invoices/generate")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> GenerateInvoices(int id)
        {
            try
            {
                await _projectService.GenerateInvoicesAsync(id);
                return Ok(new { Message = "Invoices generated successfully based on current progress." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
