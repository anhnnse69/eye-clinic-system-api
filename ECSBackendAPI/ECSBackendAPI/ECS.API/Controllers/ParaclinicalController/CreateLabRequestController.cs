using ECS.Application.Common.Response;
using ECS.Application.Services.ParaclinicalServices.CreateLabRequestServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ParaclinicalController
{
    /// <summary>
    /// UC42 / UC43 / UC44 — create a paraclinical request (OCT, Visual Field,
    /// Ultrasound B-scan, general lab) attached to an existing medical record.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctor-appointment/paraclinical")]
    [Authorize(Roles = "DOCTOR")]
    public class CreateLabRequestController : ControllerBase
    {
        private readonly ICreateLabRequestService _service;

        public CreateLabRequestController(ICreateLabRequestService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        [ProducesResponseType(typeof(ApiResponse<CreateLabRequestResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CreateLabRequestResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateLabRequestRequest request)
        {
            var result = await _service.Process(request);
            if (result.Data is null) return BadRequest(result);
            return Ok(result);
        }
    }
}