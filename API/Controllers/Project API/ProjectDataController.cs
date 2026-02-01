using BLL.Interfaces;
using Core.DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/projects")]
    [ApiController]
    [Authorize]
    public class ProjectDataController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectDataController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpPost("{projectId}/import")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> ImportData(int projectId, [FromBody] ImportDataRequest request)
        {
            try
            {
                await _projectService.ImportDataItemsAsync(projectId, request.StorageUrls);
                return Ok(new { Message = "Data items imported successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("{projectId}/export")]
        [Authorize(Roles = "Manager,Admin")]
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
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}