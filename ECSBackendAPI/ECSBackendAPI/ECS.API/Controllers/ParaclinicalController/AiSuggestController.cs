using ECS.Application.Common.Response;
using ECS.Application.Services.ParaclinicalServices.AiSuggestionServices;
using ECS.Application.Services.ParaclinicalServices.AiSymptomSuggestionServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ParaclinicalController
{
    /// <summary>
    /// AI Suggestion — submit an OCT image or symptom questionnaire to AI classifiers
    /// and persist the resulting prediction for doctors/clinic staff.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctor-appointment/paraclinical/ai")]
    [Authorize(Roles = "DOCTOR,RECEPTIONIST")]
    public class AiSuggestController : ControllerBase
    {
        private readonly IAiSuggestService _service;
        private readonly IAiSymptomSuggestService _symptomService;

        public AiSuggestController(
            IAiSuggestService service,
            IAiSymptomSuggestService symptomService)
        {
            _service = service;
            _symptomService = symptomService;
        }

        /// <summary>
        /// Multipart upload: <c>image</c> (file), optional <c>recordId</c>,
        /// optional <c>labResultId</c>, optional <c>mimeType</c>.
        /// </summary>
        [HttpPost("suggest")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<AiSuggestResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AiSuggestResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Suggest(
            [FromForm] IFormFile? image,
            [FromForm] string? recordId,
            [FromForm] string? labResultId,
            [FromForm] string? mimeType,
            CancellationToken ct)
        {
            if (image == null || image.Length == 0)
                return BadRequest(ApiResponse<AiSuggestResponse>.Fail("APP_MESSAGE_4003"));

            await using var ms = new MemoryStream();
            await image.CopyToAsync(ms, ct);
            var req = new AiSuggestRequest
            {
                ImageBytes = ms.ToArray(),
                MimeType = string.IsNullOrWhiteSpace(mimeType) ? (image.ContentType ?? "image/jpeg") : mimeType,
                RecordId = recordId,
                LabResultId = labResultId
            };
            var result = await _service.Process(req);
            if (result.Data is null) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Symptom prediction: preliminary eye disease diagnosis based on patient symptom questionnaire.
        /// </summary>
        [HttpPost("suggest-symptoms")]
        [ProducesResponseType(typeof(ApiResponse<AiSymptomSuggestResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AiSymptomSuggestResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SuggestSymptoms(
            [FromBody] AiSymptomSuggestRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Symptom))
                return BadRequest(ApiResponse<AiSymptomSuggestResponse>.Fail("APP_MESSAGE_4003"));

            var result = await _symptomService.Process(request);
            if (result.Data is null) return BadRequest(result);
            return Ok(result);
        }
    }
}