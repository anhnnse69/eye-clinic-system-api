using ECS.Application.Common.Response;
using ECS.Application.Services.AiOctPredictServices;
using ECS.Application.Services.AiTriageServices;
using ECS.Infrastructure.Ai;
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
        private readonly IAiOctPredictService _octService;

        public AiTriageController(
            IAiTriageService service,
            IAiOctPredictService octService)
        {
            _service = service;
            _octService = octService;
        }

        /// <summary>
        /// Submit symptoms for AI triage prediction
        /// </summary>
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

        /// <summary>
        /// Submit base64 OCT image for MobileNetV3 4-class classification
        /// </summary>
        [HttpPost("predict-oct")]
        [ProducesResponseType(typeof(ApiResponse<AiOctPredictResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AiOctPredictResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<AiOctPredictResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PredictOct(
            [FromBody] AiOctPredictRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ImageBase64))
            {
                return BadRequest(ApiResponse<AiOctPredictResponse>.Fail("APP_MESSAGE_4003"));
            }

            var result = await _octService.ProcessAsync(request, ct);

            if (result.Data == null)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
