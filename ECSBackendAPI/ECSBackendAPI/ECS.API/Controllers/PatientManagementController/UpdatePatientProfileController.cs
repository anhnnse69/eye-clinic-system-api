using ECS.Application.Services.PatientProfileManagementServices.UpdatePatientProfileServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.PatientManagementController
{
    /// <summary>
    /// Web API Controller endpoints managing operations for physical Patient Medical Profiles.
    /// </summary>
    [ApiController]
    [Route("api/v1/patient-profiles")]
    [Authorize(Roles = "PATIENT")]
    public class UpdatePatientProfileController : ControllerBase
    {
        private readonly IUpdatePatientProfileService _updatePatientProfileService;

        /// <summary>
        /// Initializes a new instance of <see cref="UpdatePatientProfileController"/> with required dependencies.
        /// </summary>
        /// <param name="updatePatientProfileService">Service that handles the patient profile modification business logic.</param>
        public UpdatePatientProfileController(IUpdatePatientProfileService updatePatientProfileService)
        {
            _updatePatientProfileService = updatePatientProfileService;
        }

        /// <summary>
        /// Modifies an existing medical profile linked inside authorization boundary context mappings.
        /// </summary>
        /// <param name="id">The explicit target identification configuration GUID code structure.</param>
        /// <param name="request">The detailed serialization structural payload containing modified data values attributes.</param>
        /// <returns>An <see cref="IActionResult"/> containing the profile mutation result wrapped in a standard api response.</returns>
        [HttpPut("{id:guid}/edit")]
        public async Task<IActionResult> UpdateProfile([FromRoute] Guid id, [FromBody] UpdatePatientProfileRequest request)
        {
            // Execute application workflows through asynchronous pipeline layers
            var result = await _updatePatientProfileService.Process(id, request);

            // Package data payload structure seamlessly into internal serialization response conduits
            return Ok(result);
        }
    }
}
