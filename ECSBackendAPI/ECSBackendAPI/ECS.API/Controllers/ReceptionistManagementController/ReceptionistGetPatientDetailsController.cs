using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientDetailsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECS.API.Controllers.ReceptionistManagementController
{
    /// <summary>
    /// Handles receptionist endpoints for retrieving detailed patient clinical profiles and historical logs.
    /// </summary>
    [ApiController]
    [Route("api/v1/receptionist/patients")]
    [Authorize(Roles = "RECEPTIONIST")]
    public class ReceptionistGetPatientDetailsController : ControllerBase
    {
        private readonly IReceptionistGetPatientDetailsService _detailsService;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistGetPatientDetailsController"/> with required dependencies.
        /// </summary>
        /// <param name="detailsService">The service handling patient detailed data resolution.</param>
        public ReceptionistGetPatientDetailsController(IReceptionistGetPatientDetailsService detailsService)
        {
            _detailsService = detailsService;
        }

        /// <summary>
        /// Resolves a single clinical profile record with multi-tenant filtering on related local appointments.
        /// </summary>
        /// <param name="id">The unique unique identifier string of the core patient database record mapping.</param>
        /// <returns>
        /// <c>200 OK</c> with the patient details and authorized appointment logs;
        /// <c>401 Unauthorized</c> if the user context is invalid or missing name identifier;
        /// <c>403 Forbidden</c> if the user lacks the receptionist role;
        /// <c>404 Not Found</c> if the patient context target cannot be located.
        /// </returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPatientProfileDetails([FromRoute] Guid id)
        {
            var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
            {
                return Unauthorized();
            }
            var request = new ReceptionistGetPatientDetailsRequest
            {
                CurrentUserId = userId,
                PatientId = id
            };
            var result = await _detailsService.Process(request);
            // If the service indicates resource could not be found or staff assignment mismatch occurred
            if (result.Data == null)
            {
                return NotFound(result);
            }
            return Ok(result);
        }
    }
}