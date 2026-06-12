using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemGetClinicDetailsServices
{
    /// <summary>
    /// Handles the queries and data construction for clinic records.
    /// </summary>
    public class GetClinicDetailsService : IGetClinicDetailsService
    {
        private readonly IRepositoryQueryBase<Clinic, Guid, AppDbContext> _queryRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="GetClinicDetailsService"/> with query repository mappings.
        /// </summary>
        /// <param name="queryRepo">The read-only data query abstraction layer.</param>
        public GetClinicDetailsService(IRepositoryQueryBase<Clinic, Guid, AppDbContext> queryRepo)
        {
            _queryRepo = queryRepo;
        }

        /// <summary>
        /// Fetches the domain entity state, converts it into a DTO format, and returns the response payload.
        /// </summary>
        /// <param name="id">The unique target clinic record identifier.</param>
        /// <returns>An active instance wrapper of <see cref="ApiResponse{GetClinicDetailsResponse}"/>.</returns>
        public async Task<ApiResponse<GetClinicDetailsResponse>> Process(Guid id)
        {
            // Step 1: Retrieve the core entity data model from storage layers
            var clinic = await FetchClinicFromDb(id);
            // Step 2: Map entity domain records into an expected contract interface DTO structure
            var responseDto = MapToGetByIdResponse(clinic);
            // Step 3: Bundle payloads securely within standard unified system responses
            return CreateApiResponse(responseDto);
        }

        /// <summary>
        /// Direct target lookup queries utilizing database reader operations.
        /// </summary>
        /// <param name="id">The identification tracking token value.</param>
        /// <returns>The matching valid <see cref="Clinic"/> entity record.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if database returns no records match.</exception>
        private async Task<Clinic> FetchClinicFromDb(Guid id)
        {
            var clinic = await _queryRepo.GetByIdAsync(id);
            if (clinic is null)
            {
                throw new KeyNotFoundException($"Không tìm thấy phòng khám với ID: {id}");
            }
            return clinic;
        }

        /// <summary>
        /// Maps internal data values out to tailored presentation structures.
        /// </summary>
        /// <param name="clinic">The resolved data entity record instance.</param>
        /// <returns>An integrated instances of <see cref="GetClinicDetailsResponse"/>.</returns>
        private GetClinicDetailsResponse MapToGetByIdResponse(Clinic clinic)
        {
            return new GetClinicDetailsResponse
            {
                Name = clinic.Name,
                Address = clinic.Address,
                Phone = clinic.Phone,
                Email = clinic.Email ?? "",
                LogoUrl = clinic.LogoUrl ?? "",
                Description = clinic.Description ?? "",
                IsActive = clinic.IsActive,
                RatingAvg = clinic.RatingAvg ?? 0,
                ReviewCount = clinic.ReviewCount ?? 0
            };
        }

        /// <summary>
        /// Configures wrapped responses returning operations outputs payloads.
        /// </summary>
        private ApiResponse<GetClinicDetailsResponse> CreateApiResponse(GetClinicDetailsResponse data)
        {
            return ApiResponse<GetClinicDetailsResponse>.Success("APP_MESSAGE_2000", data);
        }
    }
}