using ECS.Application.Services.ClinicAdminManagementServices.RequestPublishClinicServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    /// <summary>
    /// Handles clinic publication request endpoints for clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/clinics")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class RequestPublishClinicController : ControllerBase
    {
        private readonly IRequestPublishClinicService _requestPublishService;

        /// <summary>
        /// Initializes a new instance of <see cref="RequestPublishClinicController"/>.
        /// </summary>
        /// <param name="requestPublishService">The service handling clinic publication request business logic.</param>
        public RequestPublishClinicController(IRequestPublishClinicService requestPublishService)
        {
            _requestPublishService = requestPublishService;
        }

        /// <summary>
        /// Submits a publication request for the specified clinic.
        /// </summary>
        /// <param name="id">The unique identifier of the clinic to request publication.</param>
        /// <returns>
        /// <c>200 OK</c> with success message if the publication request is successfully submitted;
        /// <c>400 Bad Request</c> if the clinic is already published or has a pending request;
        /// <c>401 Unauthorized</c> if the request lacks valid authentication credentials;
        /// <c>403 Forbidden</c> if the authenticated user is not a clinic administrator;
        /// <c>404 Not Found</c> if the clinic does not exist.
        /// </returns>
        [HttpPost("{id}/request-publish")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RequestPublishClinic([FromRoute] string id)
        {
            var request = new RequestPublishClinicRequest
            {
                ClinicId = id
            };

            var result = await _requestPublishService.Process(request);
            return Ok(result);
        }
    }
}
