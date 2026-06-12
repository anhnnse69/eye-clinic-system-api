using ECS.Application.Services.AuthServices.ResetPasswordServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.AuthController
{
    /// <summary>
    /// Controller responsible for reset password functionality.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class ResetPasswordController : ControllerBase
    {
        private readonly IResetPasswordService _resetPasswordService;

        /// <summary>
        /// Initializes a new instance of <see cref="ResetPasswordController"/>.
        /// </summary>
        /// <param name="resetPasswordService">Service handling reset password logic.</param>
        public ResetPasswordController(IResetPasswordService resetPasswordService)
        {
            _resetPasswordService = resetPasswordService;
        }

        /// <summary>
        /// Resets user password with OTP verification.
        /// </summary>
        /// <param name="request">Reset password request containing email, OTP, and new password.</param>
        /// <returns>An <see cref="IActionResult"/> containing the result.</returns>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _resetPasswordService.Process(request);
            return Ok(result);
        }
    }
}
