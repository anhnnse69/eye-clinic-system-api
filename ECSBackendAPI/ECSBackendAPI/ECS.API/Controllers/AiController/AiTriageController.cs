using ECS.Application.Common.Response;
using ECS.Application.Services.AiTriageServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.AiController
{
    /// <summary>
    /// AI Triage — Symptom-based eye disease prediction endpoint
    /// 
    /// Replaces the old PreliminaryExamination form with AI-powered triage.
    /// Maps FE symptom input → AI Service /predict-symptoms → structured triage response
    /// </summary>
    [ApiController]
    [Route("api/v1/ai")]
    [Authorize(Roles = "DOCTOR")]
    public class AiTriageController : ControllerBase
    {
        private readonly IAiTriageService _service;

        public AiTriageController(IAiTriageService service)
        {
            _service = service;
        }

        /// <summary>
        /// Submit symptoms for AI triage prediction
        /// 
        /// Accepts 25 symptom fields and returns:
        /// - Primary predicted disease with confidence
        /// - Top 3 differential diagnoses
        /// - Risk level (LOW/MODERATE/HIGH)
        /// - Disclaimer
        /// </summary>
        /// <param name="request">Symptom input from examination</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>AI triage result with predicted disease</returns>
        [HttpPost("triage")]
        [ProducesResponseType(typeof(ApiResponse<AiTriageResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AiTriageResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<AiTriageResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Triage(
            [FromBody] AiTriageRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Symptom))
            {
                return BadRequest(ApiResponse<AiTriageResponse>.Fail("APP_MESSAGE_4003"));
            }

            var result = await _service.ProcessAsync(request, ct);
            
            if (result.Data == null)
            {
                return BadRequest(result);
            }
            
            return Ok(result);
        }
    }
}
