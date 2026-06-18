using ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetDetailPatientDemographicsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Handles endpoints for retrieving detailed patient demographics information.
    /// </summary>
    [ApiController]
    [Route("api/v1/medical-record/demographics")]
    [Authorize(Roles = "DOCTOR,RECEPTIONIST")]
    public class GetDetailPatientDemographicsController : ControllerBase
    {
        private readonly IGetDetailPatientDemographicsService _getDetailPatientDemographicsService;

        /// <summary>
        /// Initializes a new instance of <see cref="GetPatientDemographicsController"/> with required dependencies.
        /// </summary>
        /// <param name="getDetailPatientDemographicsService">The service handling patient demographics retrieval.</param>
        public GetDetailPatientDemographicsController(IGetDetailPatientDemographicsService getDetailPatientDemographicsService)
        {
            _getDetailPatientDemographicsService = getDetailPatientDemographicsService;
        }

        /// <summary>
        /// Retrieves detailed demographics information for a specific patient by patient ID.
        /// </summary>
        /// <param name="patientId">The unique identifier of the patient.</param>
        /// <returns>
        /// <c>200 OK</c> with patient demographics details if found;
        /// <c>401 Unauthorized</c> if the user lacks valid authentication;
        /// <c>403 Forbidden</c> if the user lacks the required role;
        /// <c>404 Not Found</c> if the patient ID does not exist.
        /// </returns>
        [HttpGet("{patientId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDetailPatientDemographics([FromRoute] Guid patientId)
        {
            var request = new GetDetailPatientDemographicsRequest
            {
                PatientId = patientId
            };
            var result = await _getDetailPatientDemographicsService.Process(request);
            if (result.Data == null)
            {
                return NotFound(result);
            }
            return Ok(result);
        }
    }
}
