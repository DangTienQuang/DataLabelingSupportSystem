using BLL.Interfaces;
using Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/logs")]
    [ApiController]
    [Authorize]
    public class ActivityLogController : ControllerBase
    {
        private readonly IActivityLogService _logService;

        public ActivityLogController(IActivityLogService logService)
        {
            _logService = logService;
        }

        [HttpGet("system")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemLogs()
        {
            var logs = await _logService.GetSystemLogsAsync();
            return Ok(logs);
        }

        [HttpGet("project/{projectId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetProjectLogs(int projectId)
        {
            var logs = await _logService.GetProjectLogsAsync(projectId);
            return Ok(logs);
        }
    }
}