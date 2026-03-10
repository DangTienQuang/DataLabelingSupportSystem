using BLL.Interfaces;
using Core.Constants;
using Core.DTOs.Requests;
using Core.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    [AllowAnonymous]
    [Tags("1. Authentication")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        /// <remarks>
        /// This endpoint creates a new user with a specific role
        /// (e.g., Annotator, Reviewer, Manager, Admin).
        /// </remarks>
        /// <param name="request">
        /// The registration request containing:
        /// - Full name
        /// - Email address
        /// - Password
        /// - Role
        /// </param>
        /// <returns>
        /// A confirmation message and the newly created user's unique identifier.
        /// </returns>
        /// <response code="200">User registered successfully.</response>
        /// <response code="400">
        /// Registration failed due to validation errors
        /// (e.g., email already exists or invalid role).
        /// </response>
        /// <response code="409">Email is already in use.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 409)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                await _userService.RegisterAsync(request.FullName, request.Email, request.Password, UserRoles.Annotator);
                return Ok(new { Message = "Registration successful." });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Email already exists"))
                    return Conflict(new ErrorResponse { Message = "Email is already in use. Please use a different email." });
                return BadRequest(new ErrorResponse { Message = "Registration failed. Please check your information and try again." });
            }
        }

        /// <summary>
        /// Authenticates a user and issues a JWT access token.
        /// </summary>
        /// <remarks>
        /// The returned JWT token must be included in the
        /// <c>Authorization</c> header as:
        /// <br />
        /// <c>Authorization: Bearer {token}</c>
        /// </remarks>
        /// <param name="request">
        /// The login request containing the user's email and password.
        /// </param>
        /// <returns>
        /// A JWT access token along with token metadata.
        /// </returns>
        /// <response code="200">Login successful and token issued.</response>
        /// <response code="400">Login request is invalid.</response>
        /// <response code="401">Authentication failed due to invalid credentials.</response>
        /// <response code="403">Account is deactivated or banned.</response>
        /// <response code="500">Internal server error.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var token = await _userService.LoginAsync(request.Email, request.Password);
                if (token == null)
                    return Unauthorized(new ErrorResponse { Message = "Invalid email or password." });

                return Ok(new
                {
                    Message = "Login successful.",
                    AccessToken = token,
                    TokenType = "Bearer"
                });
            }
            catch (ArgumentException)
            {
                return StatusCode(403, new ErrorResponse { Message = "Account is deactivated or banned." });
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse { Message = "An error occurred during login. Please try again later." });
            }
        }
    }
}