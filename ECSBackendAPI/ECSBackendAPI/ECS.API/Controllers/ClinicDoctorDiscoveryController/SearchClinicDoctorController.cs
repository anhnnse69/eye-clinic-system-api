using ECS.Application.Services.ClinicDoctorDiscoveryService.SearchClinicDoctorServices;
using Microsoft.AspNetCore.Mvc;

namespace ECS.API.Controllers.ClinicDoctorDiscoveryController
{
    /// <summary>
    /// Controller for searching clinics and doctors.
    /// </summary>
    [ApiController]
    [Route("api/v1/search")]
    public class SearchClinicDoctorController : ControllerBase
    {
        private readonly ISearchClinicDoctorService _searchService;

        public SearchClinicDoctorController(
            ISearchClinicDoctorService searchService)
        {
            _searchService = searchService;
        }

        /// <summary>
        /// Searches clinics and doctors by keyword.
        /// Returns all active records when keyword is omitted.
        /// </summary>
        /// <param name="keyword">Optional text to search by name.</param>
        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string? keyword)
        {
            var request = new SearchClinicDoctorRequest
            {
                Keyword = keyword
            };

            var result = await _searchService.Process(request);
            return Ok(result);
        }
    }
}