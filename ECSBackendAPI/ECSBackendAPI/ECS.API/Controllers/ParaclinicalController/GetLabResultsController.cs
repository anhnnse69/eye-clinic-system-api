using ECS.Application.Common.Response;
using ECS.Application.Services.ParaclinicalServices.GetLabResultsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ParaclinicalController
{
    /// <summary>
    /// UC42 / UC43 / UC44 — list paraclinical results for one medical record.
    /// Doctors, the owning patient, and clinic/system staff can read.
    /// </summary>
    [ApiController]
    [Route("api/v1/medical-record/paraclinical")]
    [Authorize(Roles = "DOCTOR,PATIENT,CLINIC_ADMIN,RECEPTIONIST,SYSTEM_ADMIN")]
    public class GetLabResultsController : ControllerBase
    {
        private readonly IGetLabResultsService _service;

        public GetLabResultsController(IGetLabResultsService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<GetLabResultsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<GetLabResultsResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> List(
            [FromQuery] string recordId,
            [FromQuery] string? labType,
            [FromQuery] string? side)
        {
            var result = await _service.Process(new GetLabResultsRequest
            {
                RecordId = recordId,
                LabType = labType,
                Side = side
            });
            if (result.Data is null) return BadRequest(result);
            return Ok(result);
        }
    }
}