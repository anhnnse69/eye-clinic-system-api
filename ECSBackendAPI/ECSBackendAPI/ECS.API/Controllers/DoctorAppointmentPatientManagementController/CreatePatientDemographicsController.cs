using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreatePatientDemographicsServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Handles patient medical demographics creation endpoints.
    /// Based on UC36 - Create Patient Demographics
    /// This endpoint handles ONLY medical/ophthalmology information that doctors enter.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient")]
    public class CreatePatientDemographicsController : ControllerBase
    {
        private readonly ICreatePatientDemographicsService _createPatientDemographicsService;

        /// <summary>
        /// Initializes a new instance of the controller.
        /// </summary>
        /// <param name="createPatientDemographicsService">Service for creating patient medical demographics.</param>
        public CreatePatientDemographicsController(ICreatePatientDemographicsService createPatientDemographicsService)
        {
            _createPatientDemographicsService = createPatientDemographicsService;
        }

        /// <summary>
        /// Creates a medical demographics record for an existing patient.
        /// BR1: Each patient may have only one medical demographics record; duplicates are not allowed.
        /// Only medical/ophthalmology information is handled here.
        /// </summary>
        /// <param name="request">Medical demographics creation request with patient ID and medical data.</param>
        /// <returns>
        /// 200 OK with demographics details on success;
        /// 400 Bad Request for validation errors;
        /// 404 Not Found if patient does not exist;
        /// 409 Conflict if medical demographics already exists for this patient.
        /// </returns>
        [HttpPost("demographics")]
        [ProducesResponseType(typeof(ApiResponse<CreatePatientDemographicsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CreatePatientDemographicsResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<CreatePatientDemographicsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<CreatePatientDemographicsResponse>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreatePatientDemographics([FromBody] CreatePatientDemographicsRequest request)
        {
            var result = await _createPatientDemographicsService.Process(request);

            if (result.Data == null)
            {
                // Check for specific error codes
                if (result.CodeMessage == "4010")
                {
                    // Patient not found
                    return NotFound(result);
                }
                if (result.CodeMessage == "4099")
                {
                    // Medical Demographics already exists (UC36 EX-01)
                    return Conflict(result);
                }
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
