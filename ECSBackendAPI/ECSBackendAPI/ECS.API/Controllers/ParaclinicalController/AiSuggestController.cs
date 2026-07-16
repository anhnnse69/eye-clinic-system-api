using ECS.Application.Common.Response;
using ECS.Application.Services.ParaclinicalServices.AiSuggestionServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ParaclinicalController
{
    /// <summary>
    /// AI Suggestion — submit an OCT image to the FastAI classifier and
    /// persist the resulting prediction for the doctor.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctor-appointment/paraclinical/ai")]
    [Authorize(Roles = "DOCTOR")]
    public class AiSuggestController : ControllerBase
    {
        private readonly IAiSuggestService _service;

        public AiSuggestController(IAiSuggestService service)
        {
            _service = service;
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
    }
}