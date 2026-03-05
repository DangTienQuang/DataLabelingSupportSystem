using BLL.Interfaces;
using Core.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    /// <summary>
    /// Controller for handling financial operations related to projects, such as invoicing.
    /// </summary>
    [Route("api/projects")]
    [ApiController]
    [Authorize]
    [Tags("3. Project Management")]
    public class ProjectFinanceController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectFinanceController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        /// <summary>
        /// Generates invoices for a specific project based on completed annotations and reviews.
        /// </summary>
        /// <remarks>
        /// Accessible by Admins only. This process calculates the payouts for annotators and reviewers based on approved tasks.
        /// </remarks>
        /// <param name="projectId">The ID of the target project to generate invoices for.</param>
        /// <returns>A confirmation message indicating successful generation.</returns>
        /// <response code="200">Invoices generated successfully.</response>
        /// <response code="400">Generation failed (e.g., project not found or no billable tasks).</response>
        /// <response code="401">User is not authenticated or not authorized.</response>
        [HttpPost("{projectId}/invoices")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GenerateInvoice(int projectId)
        {
            try
            {
                await _projectService.GenerateInvoicesAsync(projectId);
                return Ok(new { Message = "Invoices generated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }
    }
}