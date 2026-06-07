using ECS.Application.Common.Response;
using ECS.Application.Services.AuthServices.LoginServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.AuthController
{
    /// <summary>
    /// Handles authentication-related endpoints.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginService _loginService;

        /// <summary>
        /// Initializes a new instance of <see cref="LoginController"/>.
        /// </summary>
        /// <param name="loginService">The login service handling authentication logic.</param>
        public LoginController(ILoginService loginService)
        {
            _loginService = loginService;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token on success.
        /// </summary>
        /// <param name="loginRequest">The login credentials (email and password).</param>
        /// <returns>
        /// <c>200 OK</c> with a JWT token if credentials are valid;
        /// <c>400 Bad Request</c> if validation fails or credentials are incorrect.
        /// </returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            var result = await _loginService.Proccess(loginRequest);
            if (result.Data is null)
                return BadRequest(result);
            return Ok(result);
        }
    }
}