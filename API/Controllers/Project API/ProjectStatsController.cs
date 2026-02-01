using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectStatsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectStatsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet("{projectId}")]
        [Authorize(Roles = "Manager,Admin,Reviewer")]
        public async Task<IActionResult> GetProjectStatistics(int projectId)
        {
            try
            {
                var stats = await _projectService.GetProjectStatisticsAsync(projectId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("manager/{managerId}")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> GetManagerStats(string managerId)
        {
            try
            {
                var stats = await _projectService.GetManagerStatsAsync(managerId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}