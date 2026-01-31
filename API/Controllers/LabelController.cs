using BLL.Interfaces;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Provides APIs for managing label definitions within a project.
    /// Labels are used by annotators during the data labeling process.
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
        /// Creates a new label within a project.
        /// </summary>
        /// <remarks>
        /// This API is typically used by Managers to define the label set
        /// before annotation begins.
        /// </remarks>
        /// <param name="request">
        /// The label creation request containing:
        /// - ProjectId
        /// - Label name
        /// - Description
        /// - Optional metadata (color, shortcut, etc.)
        /// </param>
        /// <returns>The newly created label information.</returns>
        /// <response code="200">Label created successfully.</response>
        /// <response code="400">Label creation failed due to validation or business rules.</response>
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
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing label.
        /// </summary>
        /// <remarks>
        /// This operation modifies label attributes such as name or description.
        /// Updating a label may affect existing annotations depending on project rules.
        /// </remarks>
        /// <param name="id">The unique identifier of the label.</param>
        /// <param name="request">The updated label information.</param>
        /// <returns>The updated label details.</returns>
        /// <response code="200">Label updated successfully.</response>
        /// <response code="400">Update failed (e.g., label not found or invalid data).</response>
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
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a label from the system.
        /// </summary>
        /// <remarks>
        /// A label cannot be deleted if it is currently being used
        /// in any existing annotation.
        /// </remarks>
        /// <param name="id">The unique identifier of the label.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Label deleted successfully.</response>
        /// <response code="400">Deletion failed because the label is in use or does not exist.</response>
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
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
