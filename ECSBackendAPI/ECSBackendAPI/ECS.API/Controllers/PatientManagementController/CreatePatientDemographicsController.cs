using ECS.Application.Common.Response;
using ECS.Application.Services.PatientProfileManagementServices.CreatePatientDemographicsServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientManagementController
{
    /// <summary>
    /// Handles patient demographics-related endpoints.
    /// </summary>
    [ApiController]
    [Route("api/v1/medical-record")]
    public class CreatePatientDemographicsController : ControllerBase
    {
        private readonly ICreatePatientDemographicsService _createPatientDemographicsService;

        /// <summary>
        /// Initializes a new instance of <see cref="CreatePatientDemographicsController"/>.
        /// </summary>
        /// <param name="createPatientDemographicsService">The create patient demographics service handling business logic.</param>
        public CreatePatientDemographicsController(ICreatePatientDemographicsService createPatientDemographicsService)
        {
            _createPatientDemographicsService = createPatientDemographicsService;
        }

        /// <summary>
        /// Creates a new patient demographics record.
        /// </summary>
        /// <param name="request">The patient demographics data to create.</param>
        /// <returns>
        /// <c>200 OK</c> with the created patient demographics data if successful;
        /// <c>400 Bad Request</c> if validation fails or required fields are missing;
        /// <c>409 Conflict</c> if duplicate ID/email is detected.
        /// </returns>
        [HttpPost("demographics")]
        [ProducesResponseType(typeof(ApiResponse<CreatePatientDemographicsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CreatePatientDemographicsResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<CreatePatientDemographicsResponse>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreatePatientDemographics([FromBody] CreatePatientDemographicsRequest request)
        {
            var result = await _createPatientDemographicsService.Process(request);

            if (result.Data == null)
            {
                // Check if it's a conflict error (duplicate)
                if (result.CodeMessage == "4023" || result.CodeMessage == "4017" || result.CodeMessage == "4018")
                {
                    return Conflict(result);
                }
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
