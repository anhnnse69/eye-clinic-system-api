using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicEditMedicineServices;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicAdminManagementController
{
    /// <summary>
    /// Handles medicine catalog data management endpoints rules for clinic administrators.
    /// </summary>
    [ApiController]
    [Route("api/v1/clinic-admin/medicine-catalog")]
    [Authorize(Roles = "CLINIC_ADMIN")]
    public class ClinicEditMedicineCatalogController : ControllerBase
    {
        private readonly IUpdateMedicineCatalogService _updateMedicineCatalogService;
        private readonly IValidator<UpdateMedicineCatalogRequest> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="MedicineCatalogController"/>.
        /// </summary>
        /// <param name="updateMedicineCatalogService">The application service handling editing processes workflows structures.</param>
        /// <param name="validator">The validation framework processing rules structures checker.</param>
        public ClinicEditMedicineCatalogController(
            IUpdateMedicineCatalogService updateMedicineCatalogService,
            IValidator<UpdateMedicineCatalogRequest> validator)
        {
            _updateMedicineCatalogService = updateMedicineCatalogService;
            _validator = validator;
        }

        /// <summary>
        /// Modifies details parameters items matching targeted system index identities keys elements.
        /// </summary>
        /// <param name="request">The payload details carrying modification elements coordinates configuration attributes.</param>
        /// <returns>
        /// <c>200 OK</c> with modified information tracking payload blocks on success;
        /// <c>400 Bad Request</c> if verification routines trip system tracking code parameters failures.
        /// </returns>
        [HttpPut("edit")]
        [ProducesResponseType(typeof(ApiResponse<UpdateMedicineCatalogResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UpdateMedicineCatalogResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Edit([FromBody] UpdateMedicineCatalogRequest request)
        {
            // Validate incoming parameter payloads against design constraints 
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errorCode = validationResult.Errors.FirstOrDefault()?.ErrorCode
                                ?? Domain.Enums.GeneralCode.APP_MESSAGE_4019.ToString();

                return BadRequest(ApiResponse<UpdateMedicineCatalogResponse>.Fail(errorCode));
            }

            // Route process request pipeline details context onto service handler layers boundaries
            var result = await _updateMedicineCatalogService.Process(request);
            if (result.Data is null)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
