using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.EditServiceServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Handles medical treatment services modification endpoints for authorized clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/clinic-services")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class ClinicEditServiceController : ControllerBase
    {
        private readonly IEditServiceService _editServiceService;

        /// <summary>
        /// Initializes a new instance of <see cref="ClinicServiceManagementController"/> with required logic services.
        /// </summary>
        /// <param name="editServiceService">The orchestration service boundary handling clinic service updates.</param>
        public ClinicEditServiceController(IEditServiceService editServiceService)
        {
            _editServiceService = editServiceService;
        }

        /// <summary>
        /// Updates the attributes of an existing treatment service belonging to the administrator's clinic.
        /// </summary>
        /// <param name="id">The unique identifier primary key of the target service in the route data parameters.</param>
        /// <param name="request">The data payload container carrying new tracking metadata information structures.</param>
        /// <returns>
        /// 200 OK containing tracking success data payloads if execution workflow completes;
        /// 400 Bad Request if validation rules match failures, records drop out, or access boundaries violate constraints.
        /// </returns>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<EditServiceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<EditServiceResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditService([FromRoute] Guid id, [FromBody] EditServiceRequest request)
        {
            // Inject route identifiers safely into internal fields to guarantee endpoint targets line up completely
            request.ServiceId = id;

            var result = await _editServiceService.Process(request);
            if (result.Data is null)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
