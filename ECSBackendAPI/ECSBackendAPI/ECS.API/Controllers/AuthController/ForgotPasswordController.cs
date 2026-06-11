using ECS.Application.Services.AuthServices.ForgotPasswordServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.AuthController
{
    /// <summary>
    /// Controller responsible for forgot password functionality.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class ForgotPasswordController : ControllerBase
    {
        private readonly IForgotPasswordService _forgotPasswordService;

        /// <summary>
        /// Initializes a new instance of <see cref="ForgotPasswordController"/>.
        /// </summary>
        /// <param name="forgotPasswordService">Service handling forgot password logic.</param>
        public ForgotPasswordController(IForgotPasswordService forgotPasswordService)
        {
            _forgotPasswordService = forgotPasswordService;
        }

        /// <summary>
        /// Sends OTP to user's email for password reset.
        /// </summary>
        /// <param name="request">Forgot password request containing email.</param>
        /// <returns>An <see cref="IActionResult"/> containing the result.</returns>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var result = await _forgotPasswordService.Process(request);
            return Ok(result);
        }
    }
}
