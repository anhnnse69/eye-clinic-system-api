using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.MedicineCatalogServices.Delete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController.MedicineCatalogController
{
    /// <summary>
    /// Handles inventory management endpoints for catalog control operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/medicine-catalog")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class DeleteMedicineCatalogController : ControllerBase
    {
        private readonly IDeleteMedicineCatalogService _deleteMedicineCatalogService;

        /// <summary>
        /// Initializes a new instance of <see cref="DeleteMedicineCatalogController"/> with business layers.
        /// </summary>
        /// <param name="deleteMedicineCatalogService">The application workflow service engine executor interface.</param>
        public DeleteMedicineCatalogController(IDeleteMedicineCatalogService deleteMedicineCatalogService)
        {
            _deleteMedicineCatalogService = deleteMedicineCatalogService;
        }

        /// <summary>
        /// Toggles the active status parameter of a medicine catalog entry (Locks/Unlocks).
        /// </summary>
        /// <param name="id">The unique database reference identity parameter of the catalog item.</param>
        /// <returns>
        /// <c>200 OK</c> detailing updated state configurations;
        /// <c>400 Bad Request</c> if verification layers step parameters map validation discrepancies.
        /// </returns>
        [HttpPut("{id:guid}/delete")]
        [ProducesResponseType(typeof(ApiResponse<DeleteMedicineCatalogResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<DeleteMedicineCatalogResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ToggleStatus([FromRoute] Guid id)
        {
            var request = new DeleteMedicineCatalogRequest { Id = id };
            var result = await _deleteMedicineCatalogService.Process(request);

            if (result.Data is null)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
