using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing data items, file uploads, buckets, and data exports within a project.
    /// </summary>
    [Route("api/projects")]
    [ApiController]
    [Authorize]
    [Tags("3. Project Management")]
    public class ProjectDataController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly IWebHostEnvironment _env;

        public ProjectDataController(IProjectService projectService, IWebHostEnvironment env)
        {
            _projectService = projectService;
            _env = env;
        }

        /// <summary>
        /// Imports data items via external storage URLs.
        /// </summary>
        /// <remarks>
        /// Accessible by Managers and Admins. Used when data is already hosted on external cloud storage (e.g., AWS S3, GCP).
        /// </remarks>
        /// <param name="projectId">The target project ID.</param>
        /// <param name="request">Payload containing the list of storage URLs to import.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Data items imported successfully.</response>
        /// <response code="400">Import failed due to validation or processing errors.</response>
        /// <response code="401">User is not authenticated or not authorized.</response>
        [HttpPost("{projectId}/imports")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> ImportData(int projectId, [FromBody] ImportDataRequest request)
        {
            try
            {
                await _projectService.ImportDataItemsAsync(projectId, request.StorageUrls);
                return Ok(new { Message = "Data items imported successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Directly uploads image files to the server for a specific project.
        /// </summary>
        /// <remarks>
        /// Accessible by Managers and Admins. Accepts a multipart/form-data request containing multiple files.
        /// </remarks>
        /// <param name="projectId">The target project ID.</param>
        /// <param name="files">The list of image files to upload.</param>
        /// <returns>A confirmation message indicating the number of uploaded files.</returns>
        /// <response code="200">Files uploaded successfully.</response>
        /// <response code="400">No files selected or file processing failed.</response>
        /// <response code="401">User is not authenticated or not authorized.</response>
        [HttpPost("{projectId}/uploads/direct")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> UploadDirect(int projectId, [FromForm] List<IFormFile> files)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (files == null || !files.Any())
                return BadRequest(new ErrorResponse { Message = "Please select at least one file to upload." });

            try
            {
                var webRootPath = _env.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRootPath))
                {
                    webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }
                var uploadedCount = await _projectService.UploadDirectDataItemsAsync(projectId, files, webRootPath);
                return Ok(new { Message = $"{uploadedCount} files uploaded successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves the data buckets for a specific project.
        /// </summary>
        /// <remarks>
        /// Used for pagination and grouping of large datasets within a project.
        /// </remarks>
        /// <param name="projectId">The target project ID.</param>
        /// <returns>A list of data buckets.</returns>
        /// <response code="200">Buckets retrieved successfully.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet("{projectId}/buckets")]
        [Authorize(Roles = "Annotator,Manager,Admin")]
        [ProducesResponseType(typeof(object), 200)] // Consider changing 'object' to a specific Bucket DTO list
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetBuckets(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var buckets = await _projectService.GetBucketsAsync(projectId, userId);
            return Ok(buckets);
        }

        /// <summary>
        /// Exports the project's data and annotations into a downloadable JSON file.
        /// </summary>
        /// <remarks>
        /// Accessible by Managers and Admins. Compiles all labels, project metadata, and annotation data.
        /// </remarks>
        /// <param name="projectId">The target project ID.</param>
        /// <returns>A JSON file download containing the project data.</returns>
        /// <response code="200">File successfully generated for download.</response>
        /// <response code="400">Export failed due to invalid data or missing project.</response>
        /// <response code="401">User is not authenticated or not authorized.</response>
        [HttpGet("{projectId}/exports")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> ExportData(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var fileContent = await _projectService.ExportProjectDataAsync(projectId, userId);
                var fileName = $"project_{projectId}_export_{DateTime.UtcNow:yyyyMMdd}.json";
                return File(fileContent, "application/json", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }
    }
}