using ECS.Application.Common.Response;
using ECS.Application.Services.MedicalRecordsServices.PreliminaryDiagnosisServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.MedicalRecordsController
{
    /// <summary>
    /// Handles preliminary diagnosis processing endpoints.
    /// UC41 - Preliminary Diagnosis
    /// </summary>
    [ApiController]
    [Route("api/v1/doctor-appointment/preliminary-diagnosis")]
    [Authorize(Roles = "DOCTOR")]
    public class PreliminaryDiagnosisController : ControllerBase
    {
        private readonly IPreliminaryDiagnosisService _preliminaryDiagnosisService;

        /// <summary>
        /// Initializes a new instance of <see cref="PreliminaryDiagnosisController"/>.
        /// </summary>
        /// <param name="preliminaryDiagnosisService">The preliminary diagnosis service.</param>
        public PreliminaryDiagnosisController(IPreliminaryDiagnosisService preliminaryDiagnosisService)
        {
            _preliminaryDiagnosisService = preliminaryDiagnosisService;
        }

        /// <summary>
        /// Processes preliminary diagnosis and creates a medical record.
        /// UC41 - Preliminary Diagnosis
        /// </summary>
        /// <param name="request">The preliminary diagnosis request.</param>
        /// <returns>
        /// <c>200 OK</c> with created medical record details if successful;
        /// <c>400 Bad Request</c> if validation fails or medical record already exists.
        /// </returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PreliminaryDiagnosisResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PreliminaryDiagnosisResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePreliminaryDiagnosis([FromBody] PreliminaryDiagnosisRequest request)
        {
            var result = await _preliminaryDiagnosisService.Process(request);
            if (result.Data is null)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
