using BLL.Interfaces;
using DTOs.Requests;
using DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing labels within projects.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class LabelController : ControllerBase
    {
        private readonly ILabelService _labelService;

        public LabelController(ILabelService labelService)
        {
            _labelService = labelService;
        }

        /// <summary>
        /// Creates a new label in a project.
        /// </summary>
        /// <param name="request">The request containing label details.</param>
        /// <returns>The created label details.</returns>
        /// <response code="200">Label created successfully.</response>
        /// <response code="400">If the label creation fails.</response>
        [HttpPost]
        [ProducesResponseType(typeof(LabelResponse), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> CreateLabel([FromBody] CreateLabelRequest request)
        {
            try
            {
                var result = await _labelService.CreateLabelAsync(request);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        /// <summary>
        /// Updates an existing label.
        /// </summary>
        /// <param name="id">The unique identifier of the label to update.</param>
        /// <param name="request">The request containing updated label details.</param>
        /// <returns>The updated label details.</returns>
        /// <response code="200">Label updated successfully.</response>
        /// <response code="400">If the update fails.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(LabelResponse), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> UpdateLabel(int id, [FromBody] UpdateLabelRequest request)
        {
            try
            {
                var result = await _labelService.UpdateLabelAsync(id, request);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        /// <summary>
        /// Deletes a label.
        /// </summary>
        /// <param name="id">The unique identifier of the label to delete.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Label deleted successfully.</response>
        /// <response code="400">If the deletion fails (e.g., label in use).</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> DeleteLabel(int id)
        {
            try
            {
                await _labelService.DeleteLabelAsync(id);
                return Ok(new { Message = "Label deleted successfully" });
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }
    }
}
