using System.Threading.Tasks;
using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.EditStaffAccountServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Controller interface handling clinical operations management pathways reserved exclusively for verified Clinic Administrations.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/staff")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class EditStaffAccountController : ControllerBase
    {
        private readonly IEditStaffService _editStaffService;

        /// <summary>
        /// Initializes a new instance of <see cref="StaffManagementController"/> using injected system mutation handles.
        /// </summary>
        /// <param name="editStaffService">The targeted business application operational engine execution context layer handle.</param>
        public EditStaffAccountController(IEditStaffService editStaffService)
        {
            _editStaffService = editStaffService;
        }

        /// <summary>
        /// Executes continuous contextual data mutations targeting an existing active staff profile record within authorization boundaries.
        /// </summary>
        /// <param name="request">The data encapsulation payload model detailing exact transformation elements.</param>
        /// <returns>
        /// Returns HTTP Status 200 Success wrapped in standard API outcome structure matching active configurations, 
        /// or HTTP Status 400 Bad Request containing targeted domain evaluation error code blocks.
        /// </returns>
        [HttpPut("edit")]
        [ProducesResponseType(typeof(ApiResponse<EditStaffResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<EditStaffResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditStaffAccount([FromBody] EditStaffRequest request)
        {
            var proceduralResultEnvelope = await _editStaffService.Process(request);

            if (proceduralResultEnvelope.Data is null)
            {
                return BadRequest(proceduralResultEnvelope);
            }

            return Ok(proceduralResultEnvelope);
        }
    }
}