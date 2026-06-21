using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDemographicsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    /// <summary>
    /// Controller for viewing patient demographics information.
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
        /// <param name="viewPatientDemographicsService">The service for viewing patient demographics.</param>
        public ViewPatientDemographicsController(IViewPatientDemographicsService viewPatientDemographicsService)
        {
            _viewPatientDemographicsService = viewPatientDemographicsService;
        }

        /// <summary>
        /// Gets patient demographics information.
        /// </summary>
        /// <param name="request">The request containing patient identifiers.</param>
        /// <returns>Patient demographics data or error response.</returns>
        [HttpGet("patient-demographics")]
        public async Task<IActionResult> GetPatientDemographics([FromQuery] ViewPatientDemographicsRequest request)
        {
            var result = await _viewPatientDemographicsService.Process(request);
            if (result.Data == null)
                return BadRequest(result);
            return Ok(result);
        }
    }
}

