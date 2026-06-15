using ECS.Application.Services.AuthServices.ViewPersonalProfileServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.AuthController
{
    /// <summary>
    /// Handles requests to view specific user profiles inside the Auth subsystem.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth/profile")]
    [Authorize(Roles = "DOCTOR,RECEPTIONIST")]
    public class ViewPersonalProfileController : ControllerBase
    {
        private readonly IGetPersonalProfileService _profileService;

        /// <summary>
        /// Initializes a new instance of <see cref="ViewPersonalProfileController"/> with required dependencies.
        /// </summary>
        /// <param name="profileService">The service handling personal profile data retrieval.</param>
        public ViewPersonalProfileController(IGetPersonalProfileService profileService)
        {
            _profileService = profileService;
        }

        /// <summary>
        /// Retrieves profile details using the specific user identifier dynamic path parameter.
        /// </summary>
        /// <param name="userId">The unique identifier of the targeted user record.</param>
        /// <returns>
        /// <c>200 OK</c> with the personal profile details;
        /// <c>401 Unauthorized</c> if the user is not authenticated;
        /// <c>403 Forbidden</c> if the user lacks the required roles;
        /// <c>404 NotFound</c> if the targeted user record does not exist.
        /// </returns>
        [HttpGet("{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile([FromRoute] Guid userId)
        {
            var result = await _profileService.Process(userId);
            return Ok(result);
        }
    }
}