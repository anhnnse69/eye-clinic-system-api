using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreateMedicalRecordServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.MedicalRecordsController
{
    /// <summary>
    /// Handles medical record creation endpoints.
    /// UC40 - Create Medical Record
    /// </summary>
    [ApiController]
    [Route("api/v1/doctor-appointment/medical-record")]
    [Authorize(Roles = "DOCTOR")]
    public class CreateMedicalRecordController : ControllerBase
    {
        private readonly ICreateMedicalRecordService _createMedicalRecordService;

        /// <summary>
        /// Initializes a new instance of <see cref="CreateMedicalRecordController"/>.
        /// </summary>
        /// <param name="createMedicalRecordService">The medical record creation service.</param>
        public CreateMedicalRecordController(ICreateMedicalRecordService createMedicalRecordService)
        {
            _createMedicalRecordService = createMedicalRecordService;
        }

        /// <summary>
        /// Creates a new medical record for an appointment.
        /// UC40 - Create Medical Record
        /// </summary>
        /// <param name="request">The medical record creation request containing all examination data.</param>
        /// <returns>
        /// <c>200 OK</c> with created medical record details if successful;
        /// <c>400 Bad Request</c> if validation fails or medical record already exists.
        /// </returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(ApiResponse<CreateMedicalRecordResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CreateMedicalRecordResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateMedicalRecord([FromBody] CreateMedicalRecordRequest request)
        {
            var result = await _createMedicalRecordService.Process(request);
            if (result.Data is null)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
