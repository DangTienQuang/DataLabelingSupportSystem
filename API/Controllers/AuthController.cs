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

                return Ok(new { Token = token });
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
    }
}