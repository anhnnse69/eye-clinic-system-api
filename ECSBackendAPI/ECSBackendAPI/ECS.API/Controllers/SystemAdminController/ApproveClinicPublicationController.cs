using System.Security.Claims;
using ECS.Application.Services.SystemAdminServices.ApproveClinicPublicationServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.SystemAdminController
{
    /// <summary>
    /// API endpoints providing administrative authorization structures to govern clinic profile publication workflows.
    /// </summary>
    [ApiController]
    [Route("api/v1/system-admin/clinics")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public class ApproveClinicPublicationController : ControllerBase
    {
        private readonly IApproveClinicPublicationService _approvePublicationService;

        /// <summary>
        /// Initializes a new instance of <see cref="ApproveClinicPublicationController"/> with essential system admin infrastructure services.
        /// </summary>
        /// <param name="approvePublicationService">The business logic workflow engine handling clinic publication approvals.</param>
        public ApproveClinicPublicationController(IApproveClinicPublicationService approvePublicationService)
        {
            _approvePublicationService = approvePublicationService;
        }

        /// <summary>
        /// Dispatches an administrative directive command to authorize and transition a targeted clinic's workspace layout into public status.
        /// </summary>
        /// <param name="id">The unique structural database record identifier of the clinic awaiting publication.</param>
        /// <returns>An asynchronous action result wrapping the unified api standardized envelope indicating mutation outcome states.</returns>
        [HttpPost("{id:guid}/approve-publication")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApprovePublication(Guid id)
        {
            // Extracts the identity claim token attached to the active System Administrator security context boundary
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(adminIdClaim, out Guid adminId);

            // Forwards operation parameters downstream directly into application service boundaries
            var result = await _approvePublicationService.Process(id, adminId);

            return Ok(result);
        }
    }
}