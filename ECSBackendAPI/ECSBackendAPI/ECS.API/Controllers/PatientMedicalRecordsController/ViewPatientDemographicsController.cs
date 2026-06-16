using ECS.Application.Services.PatientMedicalRecordsServices.ViewPatientDemographicsServices;
using ECS.Application.Common.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientMedicalRecordsController
{
    /// <summary>
    /// Handles medical record management endpoints for viewing patient demographics.
    /// </summary>
    [ApiController]
    [Route("api/v1/medical-record")]
    [Authorize(Roles = "DOCTOR,CLINIC_ADMIN,RECEPTIONIST,PATIENT")]
    public class ViewPatientDemographicsController : ControllerBase
    {
        private readonly IViewPatientDemographicsService _viewPatientDemographicsService;

        /// <summary>
        /// Initializes a new instance of <see cref="ViewPatientDemographicsController"/>.
        /// </summary>
        /// <param name="viewPatientDemographicsService">The service handling patient demographics retrieval.</param>
        public ViewPatientDemographicsController(IViewPatientDemographicsService viewPatientDemographicsService)
        {
            _viewPatientDemographicsService = viewPatientDemographicsService;
        }

        /// <summary>
        /// Retrieves patient demographics and a paginated list of associated medical records.
        /// </summary>
        /// <param name="request">The request parameters including patient profile ID, optional record type filter, and search term.</param>
        /// <returns>
        /// <c>200 OK</c> with patient demographics and medical records list;
        /// <c>400 Bad Request</c> if validation fails or data is not accessible.
        /// </returns>
        [HttpGet("patient-demographics")]
        [ProducesResponseType(typeof(ApiResponse<ViewPatientDemographicsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ViewPatientDemographicsResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPatientDemographics([FromQuery] ViewPatientDemographicsRequest request)
        {
            var result = await _viewPatientDemographicsService.Process(request);
            if (result.Data == null || string.IsNullOrEmpty(result.Data.Id_PatientProfile))
                return BadRequest(result);
            return Ok(result);
        }
    }
}
