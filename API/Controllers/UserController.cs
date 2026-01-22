using BLL.Interfaces;
using DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing user profiles and administration.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Gets the profile of the current logged-in user.
        /// </summary>
        /// <returns>The user profile details.</returns>
        /// <response code="200">Returns user profile.</response>
        /// <response code="404">If user is not found.</response>
        /// <response code="401">If user is unauthorized.</response>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound(new { Message = "User not found" });

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                PaymentInfo = user.PaymentInfo != null ? new
                {
                    user.PaymentInfo.BankName,
                    user.PaymentInfo.BankAccountNumber,
                    user.PaymentInfo.TaxCode
                } : null
            });
        }

        /// <summary>
        /// Updates the payment information of the current user.
        /// </summary>
        /// <param name="request">The payment update request.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Payment info updated successfully.</response>
        /// <response code="400">If update fails.</response>
        /// <response code="401">If user is unauthorized.</response>
        [HttpPut("payment")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> UpdateMyPaymentInfo([FromBody] UpdatePaymentRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                await _userService.UpdatePaymentInfoAsync(userId, request.BankName, request.BankAccountNumber, request.TaxCode);
                return Ok(new { Message = "Payment info updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Gets a list of all users (Admin only).
        /// </summary>
        /// <returns>A list of users.</returns>
        /// <response code="200">Returns list of users.</response>
        /// <response code="401">If user is unauthorized or not an Admin.</response>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<object>), 200)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// Creates a new user (Admin only).
        /// </summary>
        /// <param name="request">The registration request.</param>
        /// <returns>A confirmation message and the new user's ID.</returns>
        /// <response code="200">User created successfully.</response>
        /// <response code="400">If creation fails.</response>
        /// <response code="401">If user is unauthorized or not an Admin.</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequest request)
        {
            try
            {
                var user = await _userService.RegisterAsync(request.FullName, request.Email, request.Password, request.Role);
                return Ok(new { Message = "User created successfully", UserId = user.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing user (Admin only).
        /// </summary>
        /// <param name="id">The unique identifier of the user to update.</param>
        /// <param name="request">The update request details.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">User updated successfully.</response>
        /// <response code="400">If update fails.</response>
        /// <response code="401">If user is unauthorized or not an Admin.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                await _userService.UpdateUserAsync(id, request);
                return Ok(new { Message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a user (Admin only).
        /// </summary>
        /// <param name="id">The unique identifier of the user to delete.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">User deleted successfully.</response>
        /// <response code="400">If deletion fails.</response>
        /// <response code="401">If user is unauthorized or not an Admin.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return Ok(new { Message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
