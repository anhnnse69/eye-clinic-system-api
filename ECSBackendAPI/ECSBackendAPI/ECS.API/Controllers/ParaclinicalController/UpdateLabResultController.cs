using ECS.Application.Common.Response;
using ECS.Application.Services.ParaclinicalServices.UpdateLabResultServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ParaclinicalController
{
    /// <summary>
    /// UC42 / UC43 / UC44 — append measurements / conclusion / status to a
    /// previously created paraclinical request.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctor-appointment/paraclinical")]
    [Authorize(Roles = "DOCTOR")]
    public class UpdateLabResultController : ControllerBase
    {
        private readonly IUpdateLabResultService _service;

        public UpdateLabResultController(IUpdateLabResultService service)
        {
            _service = service;
        }

        [HttpPut("update")]
        [ProducesResponseType(typeof(ApiResponse<UpdateLabResultResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UpdateLabResultResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateLabResultRequest request)
        {
            var result = await _service.Process(request);
            if (result.Data is null) return BadRequest(result);
            return Ok(result);
        }
    }
}