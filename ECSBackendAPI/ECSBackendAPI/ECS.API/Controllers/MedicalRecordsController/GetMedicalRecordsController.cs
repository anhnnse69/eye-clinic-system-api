using ECS.Application.Common.Response;
using ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordsServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ECS.API.Controllers.MedicalRecordsController
{
    /// <summary>
    /// Handles medical records-related endpoints for doctors.
    /// </summary>
    [ApiController]
    [Route("api/v1/medical-records")]
    [Authorize(Roles = "DOCTOR")]
    public class GetMedicalRecordsController : ControllerBase
    {
        private readonly IGetMedicalRecordsService _getMedicalRecordsService;

        /// <summary>
        /// Initializes a new instance of <see cref="GetMedicalRecordsController"/>.
        /// </summary>
        /// <param name="getMedicalRecordsService">The medical records service handling business logic.</param>
        public GetMedicalRecordsController(IGetMedicalRecordsService getMedicalRecordsService)
        {
            _getMedicalRecordsService = getMedicalRecordsService;
        }
        /// <summary>
        /// Gets paginated list of medical records for the authenticated doctor.
        /// Supports filtering by date range, record type, doctor, and search term.
        /// Each record includes edit/view permission flags indicating what actions are allowed.
        /// </summary>
        /// <param name="request">Filter and pagination parameters.</param>
        /// <returns>
        /// <c>200 OK</c> with list of medical records and pagination metadata;
        /// <c>400 Bad Request</c> if validation fails or user is not authorized.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<GetMedicalRecordsResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<GetMedicalRecordsResponse>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMedicalRecords([FromQuery] GetMedicalRecordsRequest request)
        {
            var result = await _getMedicalRecordsService.Process(request);
            if (result.Data == null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
