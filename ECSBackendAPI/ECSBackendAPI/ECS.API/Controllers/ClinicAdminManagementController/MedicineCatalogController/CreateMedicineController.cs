using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.MedicineCatalogServices.Create;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController.MedicineCatalogController
{
    /// <summary>
    /// Handles medicine catalog management operations for authorized clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/medicine-catalog/create")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class CreateMedicineController : ControllerBase
    {
        private readonly ICreateMedicineCatalogService _createMedicineCatalogService;

        /// <summary>
        /// Initializes a new instance of <see cref="MedicineCatalogController"/> mapping required pipelines.
        /// </summary>
        /// <param name="createMedicineCatalogService">The application service workflow engine orchestrator.</param>
        public CreateMedicineController(ICreateMedicineCatalogService createMedicineCatalogService)
        {
            _createMedicineCatalogService = createMedicineCatalogService;
        }

        /// <summary>
        /// Registers a new medicine entry context into the authenticated clinic administrator's profile domain.
        /// </summary>
        /// <param name="request">The data configurations to save inside core databases catalogs.</param>
        /// <returns>
        /// <c>200 OK</c> along with tracked created model records identifiers;
        /// <c>400 Bad Request</c> if verification errors block sequential flows.
        /// </returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CreateMedicineCatalogResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CreateMedicineCatalogResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateMedicineCatalogRequest request)
        {
            var result = await _createMedicineCatalogService.Process(request);
            if (result.Data is null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
