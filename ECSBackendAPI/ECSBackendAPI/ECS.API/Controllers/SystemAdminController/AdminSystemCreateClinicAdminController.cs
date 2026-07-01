using ECS.Application.Common.Response;
using ECS.Application.Services.SystemAdminServices.AdminSystemCreateClinicAdminServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Handles account provisioning operations performed by System Administrators.
/// </summary>
[ApiController]
[Route("api/v1/system-admin/accounts/clinic-admin")]
[Authorize(Roles = "SYSTEM_ADMIN")]
public class AdminSystemCreateClinicAdminController : ControllerBase
{
    private readonly ICreateClinicAdminService _createClinicAdminService;

    /// <summary>
    /// Initializes a new instance of <see cref="AdminSystemCreateClinicAdminController"/> with orchestration services.
    /// </summary>
    /// <param name="createClinicAdminService">The application workspace workflow handling account creation lines.</param>
    public AdminSystemCreateClinicAdminController(ICreateClinicAdminService createClinicAdminService)
    {
        _createClinicAdminService = createClinicAdminService;
    }

    /// <summary>
    /// Provisions a secure account setup allocating privileges for a targeted clinic's administrative operations.
    /// </summary>
    /// <param name="request">The context parameter request defining user settings inputs.</param>
    /// <returns>
    /// <c>200 OK</c> detailing successful mapping responses on execution outcomes;
    /// <c>400 Bad Request</c> if runtime validations or entity processing drops parameters.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateClinicAdminResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateClinicAdminResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateClinicAdmin([FromBody] CreateClinicAdminRequest request)
    {
        var result = await _createClinicAdminService.Process(request);

        if (result.Data is null)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}