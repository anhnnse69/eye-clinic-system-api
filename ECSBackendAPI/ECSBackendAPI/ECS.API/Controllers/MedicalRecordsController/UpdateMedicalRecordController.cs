using ECS.Application.Common.Response;
using ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices;
using ECS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.MedicalRecordsController
{
    /// <summary>
    /// Handles medical record update endpoints.
    /// UC41 - Edit Medical Record
    /// **Refactored (2026-07-20)**: follows the same MongoDB-backed JSON envelope
    /// pattern as CreateMedicalRecordController.
    /// </summary>
    [ApiController]
    [Route("api/v1/doctor-appointment/medical-record")]
    [Authorize(Roles = "DOCTOR")]
    public class UpdateMedicalRecordController : ControllerBase
    {
        private readonly IUpdateMedicalRecordService _updateMedicalRecordService;

        /// <summary>
        /// Initializes a new instance of <see cref="UpdateMedicalRecordController"/>.
        /// </summary>
        /// <param name="updateMedicalRecordService">The medical record update service.</param>
        public UpdateMedicalRecordController(IUpdateMedicalRecordService updateMedicalRecordService)
        {
            _updateMedicalRecordService = updateMedicalRecordService;
        }

        /// <summary>
        /// Updates an existing medical record.
        /// UC41 - Edit Medical Record
        /// </summary>
        /// <param name="id">The medical record ID to update.</param>
        /// <param name="request">The medical record update request containing all examination data.</param>
        /// <returns>
        /// <c>200 OK</c> with updated medical record details if successful;
        /// <c>400 Bad Request</c> if validation fails;
        /// <c>403 Forbidden</c> if the user is not authorized to edit this record;
        /// <c>404 Not Found</c> if the medical record is not found;
        /// <c>409 Conflict</c> if concurrency conflict occurs.
        /// </returns>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UpdateMedicalRecordResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UpdateMedicalRecordResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<UpdateMedicalRecordResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<UpdateMedicalRecordResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<UpdateMedicalRecordResponse>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateMedicalRecord([FromRoute] Guid id, [FromBody] UpdateMedicalRecordRequest request)
        {
            var result = await _updateMedicalRecordService.Process(id, request);
            if (result.Data != null)
            {
                return Ok(result);
            }
            var codeMessage = result.CodeMessage;
            if (codeMessage.Contains(GeneralCode.APP_MESSAGE_4004.ToString()) ||
                codeMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }
            if (codeMessage.Contains(GeneralCode.APP_MESSAGE_4014.ToString()) ||
                codeMessage.Contains("forbidden", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            if (codeMessage.Contains(GeneralCode.APP_MESSAGE_4028.ToString()))
            {
                return BadRequest(result);
            }
            if (codeMessage.Contains(GeneralCode.APP_MESSAGE_5001.ToString()))
            {
                return Conflict(result);
            }
            return BadRequest(result);
        }
    }
}
