using ECS.Application.Services.SystemAdminServices.AdminSystemDeleteClinicServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    /// <summary>
    /// Handles system administrator endpoints for toggling clinic operational status (soft-delete).
    /// </summary>
    [ApiController]
    [Route("api/v1/system-admin/clinics")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class AdminSystemDeleteClinicController : ControllerBase
    {
        private readonly IAdminSystemDeleteClinicService _toggleClinicStatusService;

        /// <summary>
        /// Initializes a new instance of <see cref="AdminSystemToggleClinicStatusController"/> with required dependencies.
        /// </summary>
        /// <param name="toggleClinicStatusService">The service handling clinic status modification.</param>
        public AdminSystemDeleteClinicController(IAdminSystemDeleteClinicService toggleClinicStatusService)
        {
            _toggleClinicStatusService = toggleClinicStatusService;
        }

        /// <summary>
        /// Toggles the active status of a specific clinic (Vô hiệu hóa / Xóa mềm).
        /// </summary>
        /// <param name="id">The unique identifier of the clinic.</param>
        /// <returns><c>200 OK</c> with the updated clinic summary status.</returns>
        [HttpDelete("{id:guid}")] // Khớp với hành động xóa mềm/vô hiệu hóa từ UI
        public async Task<IActionResult> ToggleClinicStatus([FromRoute] Guid id)
        {
            // Execute business logic process flow
            var result = await _toggleClinicStatusService.Process(id);
            return Ok(result);
        }
    }
}