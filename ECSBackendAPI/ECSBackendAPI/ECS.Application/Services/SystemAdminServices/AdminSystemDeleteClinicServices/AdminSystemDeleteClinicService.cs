using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemDeleteClinicServices
{
    /// <summary>
    /// Handles the business logic for changing a clinic's operational status.
    /// </summary>
    public class AdminSystemDeleteClinicService : IAdminSystemDeleteClinicService
    {
        private readonly IRepositoryBaseAsync<Clinic, Guid, AppDbContext> _repository;

        /// <summary>
        /// Initializes a new instance of <see cref="ToggleClinicStatusService"/> with required dependencies.
        /// </summary>
        /// <param name="repository">Repository for data persistence operations.</param>
        public AdminSystemDeleteClinicService(IRepositoryBaseAsync<Clinic, Guid, AppDbContext> repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Processes the status change by fetching, mutating, saving the entity, and returning the status.
        /// </summary>
        public async Task<ApiResponse<AdminSystemDeleteClinicResponse>> Process(Guid id)
        {
            // Step 1: Retrieve the existing clinic entity
            var existingClinic = await FetchClinicEntity(id);
            // Step 2: Mutate status properties 
            var updatedClinic = MutateStatusToInactive(existingClinic);
            // Step 3: Persist state changes into the database
            await SaveDataChanges(updatedClinic);
            // Step 4: Build response model map matching frontend contract
            var responseDto = BuildResponseDto(updatedClinic);
            return CreateApiResponse(responseDto);
        }

        /// <summary>
        /// Queries the repository for an existing clinic entity matching the given ID.
        /// </summary>
        private async Task<Clinic> FetchClinicEntity(Guid id)
        {
            var clinic = await _repository.GetByIdAsync(id);
            return clinic ?? throw new KeyNotFoundException($"Clinic with ID {id} not found.");
        }

        /// <summary>
        /// Mutates properties to turn the clinic into INACTIVE state.
        /// </summary>
        private Clinic MutateStatusToInactive(Clinic clinic)
        {
            clinic.IsActive = false; // deactive
            clinic.UpdatedAt = DateTime.UtcNow;
            return clinic;
        }

        /// <summary>
        /// Commits the modified entity states asynchronously.
        /// </summary>
        private async Task SaveDataChanges(Clinic clinic)
        {
            await _repository.UpdateAsync(clinic);
            await _repository.SaveChangesAsync();
        }

        /// <summary>
        /// Builds a summary data payload matching frontend expected format.
        /// </summary>
        private AdminSystemDeleteClinicResponse BuildResponseDto(Clinic clinic)
        {
            return new AdminSystemDeleteClinicResponse
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
        private ApiResponse<AdminSystemDeleteClinicResponse> CreateApiResponse(AdminSystemDeleteClinicResponse data)
        {
            return ApiResponse<AdminSystemDeleteClinicResponse>.Success("APP_MESSAGE_2000", data);
        }
    }
}
