using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.MedicineCatalogServices.ViewList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Manages the operational endpoints handling catalog and generic items distribution rules inside active segments.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/medicine-catalog")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class ViewListMedicineCatalogController : ControllerBase
    {
        private readonly IGetMedicineCatalogService _getMedicineCatalogService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MedicineCatalogController"/> layout component structure.
        /// </summary>
        /// <param name="getMedicineCatalogService">The service logic coordinator manipulating medicine inventory definitions.</param>
        public ViewListMedicineCatalogController(IGetMedicineCatalogService getMedicineCatalogService)
        {
            _getMedicineCatalogService = getMedicineCatalogService;
        }

        /// <summary>
        /// Pulls a structural, paginated index of medicine catalog mappings corresponding strictly onto user roles.
        /// </summary>
        /// <param name="request">Filtering criteria definitions bundle payload details.</param>
        /// <returns>
        /// <c>200 OK</c> alongside structural data rows on index mapping success;
        /// <c>400 Bad Request</c> if runtime claims analysis boundaries fail.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<GetMedicineCatalogResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<GetMedicineCatalogResponse>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetList([FromQuery] GetMedicineCatalogRequest request)
        {
            var result = await _getMedicineCatalogService.Process(request);
            if (result.Data is null)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
