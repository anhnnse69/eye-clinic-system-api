using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.EditClinicProfileServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    [ApiController]
    [Route("api/v1/clinic-admin/clinic")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class ClinicEditClinicProfileController : ControllerBase
    {
        private readonly IEditClinicProfileService _editClinicProfileService;

        /// <summary>
        /// Initializes a new instance of <see cref="ClinicEditClinicProfileController"/>.
        /// </summary>
        /// <param name="editClinicProfileService">
        /// The service responsible for updating clinic profile information.
        /// </param>
        public ClinicEditClinicProfileController(
            IEditClinicProfileService editClinicProfileService)
        {
            _editClinicProfileService = editClinicProfileService;
        }

        /// <summary>
        /// Updates the profile information of the clinic associated with
        /// the authenticated administrator.
        /// </summary>
        /// <param name="request">
        /// The clinic profile update request.
        /// </param>
        /// <returns>
        /// <c>200 OK</c> if the update succeeds;
        /// <c>400 Bad Request</c> if validation fails or clinic data cannot be resolved.
        /// </returns>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] EditClinicProfileRequest request)
        {
            var result =
                await _editClinicProfileService.Process(request);

            if (!result.Data)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
