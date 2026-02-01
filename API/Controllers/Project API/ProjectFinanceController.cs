using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/projects")]
    [ApiController]
    [Authorize]
    public class ProjectFinanceController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectFinanceController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpPost("{projectId}/generate-invoice")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateInvoice(int projectId)
        {
            try
            {
                await _projectService.GenerateInvoicesAsync(projectId);
                return Ok(new { Message = "Invoices generated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}