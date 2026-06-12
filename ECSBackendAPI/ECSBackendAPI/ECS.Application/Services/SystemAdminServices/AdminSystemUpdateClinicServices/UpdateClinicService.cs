using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemUpdateClinicServices
{
    /// <summary>
    /// Handles the business logic for updating clinic information.
    /// </summary>
    public class UpdateClinicService : IUpdateClinicService
    {
        private readonly IRepositoryBaseAsync<Clinic, Guid, AppDbContext> _repository;

        /// <summary>
        /// Initializes a new instance of <see cref="UpdateClinicService"/> with required dependencies.
        /// </summary>
        /// <param name="repository">Repository for data persistence operations.</param>
        public UpdateClinicService(IRepositoryBaseAsync<Clinic, Guid, AppDbContext> repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Processes the clinic update by fetching, mutating, saving the entity, and returning the result.
        /// </summary>
        /// <param name="id">The unique identifier of the clinic.</param>
        /// <param name="request">The clinic update data payload.</param>
        /// <returns>An <see cref="ApiResponse{UpdateClinicResponse}"/> indicating success.</returns>
        public async Task<ApiResponse<UpdateClinicResponse>> Process(Guid id, UpdateClinicRequest request)
        {
            // Step 1: Retrieve the existing clinic entity or throw an exception if missing
            var existingClinic = await FetchClinicEntity(id);
            // Step 2: Map and mutate fields with incoming request data
            var updatedClinic = MapAndMutateProperties(existingClinic, request);
            // Step 3: Persist data modifications to the database
            await SaveDataChanges(updatedClinic);
            // Step 4: Construct the response DTO wrapper
            var responseDto = BuildResponseDto(updatedClinic);
            return CreateApiResponse(responseDto);
        }

        /// <summary>
        /// Queries the repository for an existing clinic entity matching the given ID.
        /// </summary>
        /// <param name="id">The unique identifier of the clinic.</param>
        /// <returns>The resolved <see cref="Clinic"/> instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no matching entity is found.</exception>
        private async Task<Clinic> FetchClinicEntity(Guid id)
        {
            var clinic = await _repository.GetByIdAsync(id);
            return clinic ?? throw new KeyNotFoundException($"Clinic with ID {id} not found.");
        }

        /// <summary>
        /// Maps incoming request payloads to mutate properties of the original entity.
        /// </summary>
        /// <param name="clinic">The original domain entity instance.</param>
        /// <param name="request">The request payload data to apply.</param>
        /// <returns>The mutated <see cref="Clinic"/> instance.</returns>
        private Clinic MapAndMutateProperties(Clinic clinic, UpdateClinicRequest request)
        {
            clinic.Name = request.Name.Trim();
            clinic.Address = request.Address.Trim();
            clinic.Phone = request.Phone.Trim();
            clinic.Email = request.Email?.Trim();
            clinic.LogoUrl = request.LogoUrl;
            clinic.Description = request.Description?.Trim();
            clinic.UpdatedAt = DateTime.UtcNow;
            return clinic;
        }

        /// <summary>
        /// Commits the modified entity states asynchronously into database context.
        /// </summary>
        /// <param name="clinic">The updated clinic entity instance.</param>
        private async Task SaveDataChanges(Clinic clinic)
        {
            await _repository.UpdateAsync(clinic);
            await _repository.SaveChangesAsync();
        }

        /// <summary>
        /// Builds a summary data payload from the updated entity properties.
        /// </summary>
        /// <param name="clinic">The updated clinic domain entity.</param>
        /// <returns>A configured <see cref="UpdateClinicResponse"/> data payload.</returns>
        private UpdateClinicResponse BuildResponseDto(Clinic clinic)
        {
            return new UpdateClinicResponse
            {
                Id_clinic = clinic.Id.ToString(),
                ClinicName = clinic.Name,
                Status = clinic.IsActive ? "ACTIVE" : "INACTIVE",
                UpdatedAt = clinic.UpdatedAt.ToString("dd/MM/yyyy HH:mm:ss")
            };
        }

        /// <summary>
        /// Wraps the processed response model into a structured API response payload.
        /// </summary>
        /// <param name="data">The built transaction response data.</param>
        /// <returns>A successful <see cref="ApiResponse{UpdateClinicResponse}"/> variant wrapper.</returns>
        private ApiResponse<UpdateClinicResponse> CreateApiResponse(UpdateClinicResponse data)
        {
            return ApiResponse<UpdateClinicResponse>.Success("APP_MESSAGE_2000", data);
        }
    }
}