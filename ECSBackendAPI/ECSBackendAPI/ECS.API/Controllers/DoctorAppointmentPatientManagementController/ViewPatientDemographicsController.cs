using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDemographicsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.DoctorAppointmentPatientManagementController
{
    [ApiController]
    [Route("api/v1/medical-record")]
    [Authorize(Roles = "DOCTOR,CLINIC_ADMIN,RECEPTIONIST,PATIENT")]
    public class ViewPatientDemographicsController : ControllerBase
    {
        private readonly IViewPatientDemographicsService _viewPatientDemographicsService;

        public ViewPatientDemographicsController(IViewPatientDemographicsService viewPatientDemographicsService)
        {
            _viewPatientDemographicsService = viewPatientDemographicsService;
        }

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

