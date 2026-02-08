using BLL.Interfaces;
using Core.Constants;
using Core.DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/disputes")]
    [ApiController]
    [Authorize]
    public class DisputeController : ControllerBase
    {
        private readonly IDisputeService _disputeService;

        public DisputeController(IDisputeService disputeService)
        {
            _disputeService = disputeService;
        }

        [HttpPost]
        [Authorize(Roles = "Annotator")]
        public async Task<IActionResult> CreateDispute([FromBody] CreateDisputeRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _disputeService.CreateDisputeAsync(userId, request);
            return Ok(new { Message = "Dispute submitted successfully." });
        }

        [HttpPost("resolve")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> ResolveDispute([FromBody] ResolveDisputeRequest request)
        {
            await _disputeService.ResolveDisputeAsync(request);
            return Ok(new { Message = "Dispute resolved." });
        }

        [HttpGet]
        public async Task<IActionResult> GetDisputes([FromQuery] int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var disputes = await _disputeService.GetDisputesAsync(projectId, userId, role);
            return Ok(disputes);
        }
    }
}