using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.DeactivateServiceServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Handles administrative endpoint management operations targeting individual clinic service definition configurations.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/clinic-services")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class ClinicDeactivateServiceController : ControllerBase
    {
        private readonly IDeactivateService _deactivateService;

        /// <summary>
        /// Initializes a new instance of <see cref="ClinicServiceController"/> mapping infrastructure workflows.
        /// </summary>
        /// <param name="deactivateService">The application handling boundary orchestration layer execution contracts.</param>
        public ClinicDeactivateServiceController(IDeactivateService deactivateService)
        {
            _deactivateService = deactivateService;
        }

        /// <summary>
        /// Transforms specific target application service components state identifiers to inactive availability settings.
        /// </summary>
        /// <param name="id">The explicit queryable target unique identification token parameter attributes.</param>
        /// <returns>
        /// A status code <c>200 OK</c> packing updating confirmation payloads, 
        /// or a <c>400 Bad Request</c> error matrix upon structural operational rule exceptions.
        /// </returns>
        [HttpPut("{id:guid}/deactivate")]
        [ProducesResponseType(typeof(ApiResponse<DeactivateServiceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<DeactivateServiceResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Deactivate([FromRoute] Guid id)
        {
            var request = new DeactivateServiceRequest { ServiceId = id };
            var result = await _deactivateService.Process(request);

            if (result.Data is null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
