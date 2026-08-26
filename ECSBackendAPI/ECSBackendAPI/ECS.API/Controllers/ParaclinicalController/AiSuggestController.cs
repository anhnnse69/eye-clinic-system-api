using ECS.Application.Common.Response;
using ECS.Application.Services.ParaclinicalServices.AiSymptomSuggestionServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ParaclinicalController
{
    /// <summary>
    /// AI Suggestion — submit symptom questionnaire to AI classifiers
    /// and persist the resulting prediction for doctors/clinic staff.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctor-appointment/paraclinical/ai")]
    [Authorize(Roles = "DOCTOR,RECEPTIONIST")]
    public class AiSuggestController : ControllerBase
    {
        private readonly IAiSymptomSuggestService _symptomService;

        public AiSuggestController(
            IAiSymptomSuggestService symptomService)
        {
            _symptomService = symptomService;
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