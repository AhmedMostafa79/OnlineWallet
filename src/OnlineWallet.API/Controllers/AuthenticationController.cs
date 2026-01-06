using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineWallet.Application.Helpers;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Application.Models;
using OnlineWallet.Application.Services;

namespace OnlineWallet.API.Controllers
{
    /// <summary>
    /// Controller for Register and Login
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthenticationController> _logger;
        private string GetFirstModelError()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Invalid request data";
        }
        /// <summary>
        /// Initializes a new instance of AuthenticationController
        /// </summary>
        /// <param name="authService">The authentication service</param>
        /// <param name="logger">The logger instance</param>
        public AuthenticationController(IAuthService authService,ILogger<AuthenticationController> logger)
        {
            _authService=authService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="model">The register model containing new user data</param>
        /// <returns>User claims and jwt token</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/auth/register
        ///     {
        ///         "email": "user@example.com",
        ///         "password": "Password123!",
        ///         "firstName": "John",
        ///         "lastName": "Doe",
        ///         "phoneNumber": "00123456789",
        ///         dateOfBirth: "2025-12-14"
        ///     }
        /// </remarks> 
        /// <response code="200">User registered successfully</response>
        /// <response code="400">Invalid model data</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseModel),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthResponseModel>> RegisterAsync([FromBody]RegisterModel model)
        {

            if(!ModelState.IsValid)
            {
                return BadRequest(GetFirstModelError());
            }
            try
            {
                var result = await _authService.RegisterAsync(model);
                return Ok(result);

            }
            catch(Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                // Check if it's email duplication error
                if (ex.Message.Contains("already registered", StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(new { Message = ex.Message });
                }
                return BadRequest(ex.Message);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to register user");
                return StatusCode(500, "Failed to register, please try again");
            }

        }
        /// <summary>
        /// Authenticates an existing user
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/auth/login
        ///     {
        ///         "email": "user@example.com",
        ///         "password": "Password123!",
        ///     }
        /// </remarks>
        /// <param name="model">Login request model</param>
        /// <returns>User claims and jwt token</returns>
        /// <response code="200">User authenticated successfully</response>
        /// <response code="400">Invalid model data</response>
        /// <response code="401">User is not authenticated or authorized</response>
        /// <response code="404">User is not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseModel),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthResponseModel>> LoginAsync([FromBody] TokenRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(GetFirstModelError());
            }
            try
            {
                var result = await _authService.LoginAsync(model);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to login user");
                return StatusCode(500, "Failed to login, please try again");
            }

        }

    }
}
