using BLL.Interfaces;
using Core.DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing user profiles and user administration.
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

        // ======================================================
        // CURRENT USER (SELF-SERVICE)
        // ======================================================

        /// <summary>
        /// Get the profile of the currently logged-in user.
        /// </summary>
        /// <remarks>
        /// Returns basic user information and payment details (if available).
        /// </remarks>
        /// <returns>User profile data.</returns>
        /// <response code="200">Profile retrieved successfully.</response>
        /// <response code="401">User is not authenticated.</response>
        /// <response code="404">User not found.</response>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound(new { Message = "User not found." });

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.AvatarUrl,
                PaymentInfo = user.PaymentInfo != null
                    ? new
                    {
                        user.PaymentInfo.BankName,
                        user.PaymentInfo.BankAccountNumber,
                        user.PaymentInfo.TaxCode
                    }
                    : null
            });
        }

        /// <summary>
        /// Update profile information of the current user.
        /// </summary>
        /// <remarks>
        /// Allows updating full name and avatar.
        /// </remarks>
        /// <param name="request">Profile update payload.</param>
        /// <response code="200">Profile updated successfully.</response>
        /// <response code="400">Update failed.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                await _userService.UpdateProfileAsync(userId, request);
                return Ok(new { Message = "Profile updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "Vui lòng chọn file ảnh." });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                var avatarUrl = $"/avatars/{uniqueFileName}";
                await _userService.UpdateAvatarAsync(userId, avatarUrl);

                return Ok(new { Message = "Upload thành công.", AvatarUrl = avatarUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi server: " + ex.Message });
            }
        }
        /// <summary>
        /// Update payment information of the current user.
        /// </summary>
        /// <param name="request">Payment information update request.</param>
        /// <response code="200">Payment information updated successfully.</response>
        /// <response code="400">Update failed.</response>
        /// <response code="401">User is not authenticated.</response>
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
                await _userService.UpdatePaymentInfoAsync(
                    userId,
                    request.BankName,
                    request.BankAccountNumber,
                    request.TaxCode
                );
                return Ok(new { Message = "Payment info updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ======================================================
        // ACCOUNT SECURITY
        // ======================================================

        /// <summary>
        /// Change password of the current user.
        /// </summary>
        /// <param name="request">Old and new password.</param>
        /// <response code="200">Password changed successfully.</response>
        /// <response code="400">Password change failed.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                await _userService.ChangePasswordAsync(
                    userId,
                    request.OldPassword,
                    request.NewPassword
                );
                return Ok(new { Message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ======================================================
        // ADMIN / MANAGER MANAGEMENT
        // ======================================================

        /// <summary>
        /// Get all users in the system.
        /// </summary>
        /// <remarks>
        /// Accessible by Admin and Manager roles.
        /// </remarks>
        /// <returns>List of users.</returns>
        /// <response code="200">Users retrieved successfully.</response>
        /// <response code="401">User is not authorized.</response>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// Create a new user account.
        /// </summary>
        /// <remarks>
        /// Admin only.
        /// </remarks>
        /// <param name="request">User registration data.</param>
        /// <response code="200">User created successfully.</response>
        /// <response code="400">User creation failed.</response>
        /// <response code="401">User is not authorized.</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(void), 401)]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequest request)
        {
            try
            {
                var user = await _userService.RegisterAsync(
                    request.FullName,
                    request.Email,
                    request.Password,
                    request.Role
                );

                return Ok(new
                {
                    Message = "User created successfully.",
                    UserId = user.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing user.
        /// </summary>
        /// <remarks>
        /// Admin only.
        /// </remarks>
        /// <param name="id">Target user ID.</param>
        /// <param name="request">Update payload.</param>
        /// <response code="200">User updated successfully.</response>
        /// <response code="400">Update failed.</response>
        /// <response code="401">User is not authorized.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                await _userService.UpdateUserAsync(id, request);
                return Ok(new { Message = "User updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Activate or deactivate a user account.
        /// </summary>
        /// <remarks>
        /// Admin only.
        /// </remarks>
        /// <param name="id">Target user ID.</param>
        /// <param name="isActive">Desired account status.</param>
        /// <response code="200">User status updated successfully.</response>
        /// <response code="400">Operation failed.</response>
        /// <response code="401">User is not authorized.</response>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleUserStatus(string id, [FromQuery] bool isActive)
        {
            try
            {
                await _userService.ToggleUserStatusAsync(id, isActive);
                var status = isActive ? "activated" : "deactivated";
                return Ok(new { Message = $"User has been {status} successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Delete (deactivate) a user account.
        /// </summary>
        /// <remarks>
        /// Admin only.
        /// </remarks>
        /// <param name="id">Target user ID.</param>
        /// <response code="200">User deleted successfully.</response>
        /// <response code="400">Deletion failed.</response>
        /// <response code="401">User is not authorized.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return Ok(new { Message = "User has been deactivated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
