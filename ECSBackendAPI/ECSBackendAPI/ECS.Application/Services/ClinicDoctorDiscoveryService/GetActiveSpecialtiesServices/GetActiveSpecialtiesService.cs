using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicDoctorDiscoveryService.GetActiveSpecialtiesServices
{
    /// <summary>
    /// Handles the business logic for fetching and formatting active medical specialties.
    /// </summary>
    public class GetActiveSpecialtiesService : IGetActiveSpecialtiesService
    {
        private readonly IRepositoryQueryBase<Specialty, Guid, AppDbContext> _specialtyQueryRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="GetActiveSpecialtiesService"/> with required dependencies.
        /// </summary>
        /// <param name="specialtyQueryRepo">Repository for read-only data query operations.</param>
        public GetActiveSpecialtiesService(IRepositoryQueryBase<Specialty, Guid, AppDbContext> specialtyQueryRepo)
        {
            _specialtyQueryRepo = specialtyQueryRepo;
        }

        /// <summary>
        /// Processes the active specialties retrieval by fetching, mapping the entities, and returning the result.
        /// </summary>
        /// <returns>An <see cref="ApiResponse{List{GetActiveSpecialtiesResponse}}"/> indicating success.</returns>
        public async Task<ApiResponse<List<GetActiveSpecialtiesResponse>>> Process()
        {
            // Step 1: Retrieve the existing active specialty entities
            var activeEntities = await FetchActiveSpecialtiesFromRepo();
            // Step 2: Map entity domain fields to response data payloads
            var responseDtos = MapToSpecialtyListResponse(activeEntities);
            // Step 3: Construct the response DTO wrapper
            return CreateApiResponse(responseDtos);
        }

        /// <summary>
        /// Queries the repository for all specialty entities configured as active.
        /// </summary>
        /// <returns>A read-only collection of active <see cref="Specialty"/> instances.</returns>
        private async Task<List<Specialty>> FetchActiveSpecialtiesFromRepo()
        {
            return await _specialtyQueryRepo.FindAll(trackChanges: false)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Maps domain entity properties to configure data transfer payload arrays.
        /// </summary>
        /// <param name="specialties">The raw collection of data entities.</param>
        /// <returns>A mapped list of <see cref="GetActiveSpecialtiesResponse"/> instances.</returns>
        private List<GetActiveSpecialtiesResponse> MapToSpecialtyListResponse(List<Specialty> specialties)
        {
            return specialties.Select(s => new GetActiveSpecialtiesResponse
            {
                Id = s.Id.ToString(),
                Name = s.Name,
                Description = s.Description
            }).ToList();
        }

        /// <summary>
        /// Wraps the processed response model into a structured API response payload.
        /// </summary>
        /// <param name="data">The built transaction response data.</param>
        /// <returns>A successful <see cref="ApiResponse{List{GetActiveSpecialtiesResponse}}"/> variant wrapper.</returns>
        private ApiResponse<List<GetActiveSpecialtiesResponse>> CreateApiResponse(List<GetActiveSpecialtiesResponse> data)
        {
            return ApiResponse<List<GetActiveSpecialtiesResponse>>.Success("APP_MESSAGE_2000", data);
        }
    }
}