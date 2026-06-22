using ECS.Domain.Enums;
using ECS.Application.Common.Response;
using ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordDetailServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.MedicalRecordsController
{
    /// <summary>
    /// API controller for medical record detail retrieval endpoints.
    /// UC39 - View Medical Record Detail
    /// </summary>
    [ApiController]
    [Route("api/v1/medical-records")]
    [Authorize(Roles = "DOCTOR,PATIENT,CLINIC_ADMIN,RECEPTIONIST,SYSTEM_ADMIN")]
    public class GetMedicalRecordDetailController : ControllerBase
    {
        private readonly IGetMedicalRecordDetailService _getMedicalRecordDetailService;

        /// <summary>
        /// Initializes a new instance of <see cref="GetMedicalRecordDetailController"/>.
        /// </summary>
        public GetMedicalRecordDetailController(IGetMedicalRecordDetailService getMedicalRecordDetailService)
        {
            _getMedicalRecordDetailService = getMedicalRecordDetailService;
        }

        /// <summary>
        /// Retrieves the complete details of a specific medical record.
        /// </summary>
        /// <param name="id">The medical record ID to retrieve.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing:
        /// - <c>200 OK</c> with complete medical record detail when successful
        /// - <c>400 Bad Request</c> if validation fails or user is not authorized
        /// - <c>403 Forbidden</c> if access is denied
        /// - <c>404 Not Found</c> if medical record does not exist
        /// </returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<GetMedicalRecordDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<GetMedicalRecordDetailResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<GetMedicalRecordDetailResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<GetMedicalRecordDetailResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMedicalRecordDetail([FromRoute] Guid id)
        {
            var request = new GetMedicalRecordDetailRequest { Id = id };
            var result = await _getMedicalRecordDetailService.Process(request);
            if (result.Data != null)
            {
                return Ok(result);
            }
            var codeMessage = result.CodeMessage;
            if (codeMessage.Contains(GeneralCode.APP_MESSAGE_4028.ToString()) ||
                codeMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }
            if (codeMessage.Contains(GeneralCode.APP_MESSAGE_4014.ToString()) ||
                codeMessage.Contains("forbidden", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return BadRequest(result);
        }
    }
}
