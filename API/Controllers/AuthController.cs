using BLL.Interfaces;
using Core.DTOs.Requests;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Provides APIs for user authentication and account registration.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
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
        [HttpPost("register")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
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
                    Message = "Registration successful",
                    UserId = user.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
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
        /// <response code="401">Authentication failed due to invalid credentials.</response>
        /// <response code="400">Login request is invalid.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var token = await _userService.LoginAsync(request.Email, request.Password);

                if (token == null)
                {
                    return Unauthorized(new { Message = "Invalid email or password" });
                }

                return Ok(new
                {
                    Message = "Login successful",
                    AccessToken = token,
                    TokenType = "Bearer"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
