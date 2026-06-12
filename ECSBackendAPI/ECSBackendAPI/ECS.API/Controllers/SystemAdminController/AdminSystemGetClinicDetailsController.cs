using ECS.Application.Services.SystemAdminServices.AdminSystemGetClinicDetailsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    /// <summary>
    /// Handles system administrator endpoints for retrieving clinic information.
    /// </summary>
    [ApiController]
    [Route("api/v1/system-admin/clinics")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class AdminSystemGetClinicDetailsController : ControllerBase
    {
        private readonly IGetClinicDetailsService _getClinicByIdService;

        /// <summary>
        /// Initializes a new instance of <see cref="AdminSystemGetClinicDetailsController"/> with required dependencies.
        /// </summary>
        /// <param name="getClinicByIdService">The service handling clinic data queries.</param>
        public AdminSystemGetClinicDetailsController(IGetClinicDetailsService getClinicByIdService)
        {
            _getClinicByIdService = getClinicByIdService;
        }

        /// <summary>
        /// Retrieves the detailed records of a clinic using its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the clinic.</param>
        /// <returns>A structured API payload containing the targeted clinic model details.</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClinicById([FromRoute] Guid id)
        {
            var result = await _getClinicByIdService.Process(id);
            return Ok(result);
        }
    }
}