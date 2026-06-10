using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicProfileServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Handles clinic profile management endpoints for clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/clinic")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class ClinicProfileController : ControllerBase
    {
        private readonly IViewClinicService _viewClinicService;

        /// <summary>
        /// Initializes a new instance of <see cref="ClinicProfileController"/>.
        /// </summary>
        /// <param name="viewClinicService">The service handling clinic profile business logic.</param>
        public ClinicProfileController(IViewClinicService viewClinicService)
        {
            _viewClinicService = viewClinicService;
        }

        /// <summary>
        /// Retrieves the profile information of the clinic associated with the authenticated administrator.
        /// </summary>
        /// <returns>
        /// <c>200 OK</c> with clinic details if found;
        /// <c>400 Bad Request</c> if user validation fails or the clinic does not exist.
        /// </returns>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(ApiResponse<ViewClinicResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ViewClinicResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProfile()
        {
            // Execute the retrieval process with an empty request object
            var result = await _viewClinicService.Process(new ViewClinicRequest());
            if (result.Data is null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}