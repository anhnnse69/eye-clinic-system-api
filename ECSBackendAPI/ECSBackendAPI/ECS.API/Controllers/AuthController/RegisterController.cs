using ECS.Application.Services.AuthServices.RegisterServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.AuthController
{
    /// <summary>
    /// Controller responsible for user registration.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class RegisterController : ControllerBase
    {
        private readonly IRegisterService _registerService;

        /// <summary>
        /// Initializes a new instance of <see cref="RegisterController"/> with required dependencies.
        /// </summary>
        /// <param name="registerService">Service that handles the registration business logic.</param>
        public RegisterController(IRegisterService registerService)
        {
            _registerService = registerService;
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        /// <param name="request">The registration data submitted by the user.</param>
        /// <returns>An <see cref="IActionResult"/> containing the registration result.</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _registerService.Process(request);

            return Ok(result);
        }
    }
}