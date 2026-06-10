using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.SystemAdminServices.RejectClinicApplicationServices
{
    /// <summary>
    /// Handles the business logic for rejecting a clinic registration application.
    /// </summary>
    public class RejectClinicApplicationService : IRejectClinicApplicationService
    {
        private readonly IRepositoryBaseAsync<ClinicRegistrationRequest, Guid, AppDbContext> _repository;

        /// <summary>
        /// Initializes a new instance of <see cref="RejectClinicApplicationService"/> with required dependencies.
        /// </summary>
        /// <param name="repository">Repository for state persistence of clinic registration requests.</param>
        public RejectClinicApplicationService(IRepositoryBaseAsync<ClinicRegistrationRequest, Guid, AppDbContext> repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Processes the rejection request by validating parameters, checking application status, and updating persistence storage.
        /// </summary>
        /// <param name="id">The unique identifier of the clinic registration application.</param>
        /// <param name="request">The request payload containing the review note justification.</param>
        /// <param name="adminId">The unique identifier of the system administrator performing the action.</param>
        /// <returns>An <see cref="ApiResponse{Boolean}"/> indicating the structural success of the operation.</returns>
        public async Task<ApiResponse<bool>> Process(Guid id, RejectClinicApplicationRequest request, Guid adminId)
        {
            // Validate incoming business requirements
            ValidateRequest(request);
            // Retrieve active persistence entity tracking changes
            var application = await FetchAndValidateApplication(id);
            // Apply mutation details onto the target record
            UpdateToRejectedState(application, request.ReviewNote, adminId);
            // Persist modified entity state to data storage
            await SaveChanges();
            // Return standardized API envelope response
            return CreateApiResponse(true);
        }

        /// <summary>
        /// Validates required input properties for the rejection workflow.
        /// </summary>
        private void ValidateRequest(RejectClinicApplicationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ReviewNote))
            {
                throw new ArgumentException(GeneralCode.APP_MESSAGE_4003.ToString());
            }
        }

        /// <summary>
        /// Fetches the target registration request and ensures it is in a valid state for rejection.
        /// </summary>
        private async Task<ClinicRegistrationRequest> FetchAndValidateApplication(Guid id)
        {
            var application = await _repository.FindByCondition(x => x.Id == id, trackChanges: true)
                                               .FirstOrDefaultAsync();
            if (application == null)
            {
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4029.ToString());
            }
            if (application.Status != "PENDING")
            {
                throw new InvalidOperationException(GeneralCode.APP_MESSAGE_4030.ToString());
            }
            return application;
        }

        /// <summary>
        /// Updates the internal persistence values into a rejected state.
        /// </summary>
        private void UpdateToRejectedState(ClinicRegistrationRequest application, string reviewNote, Guid adminId)
        {
            application.Status = "REJECTED";
            application.ReviewNote = reviewNote;
            application.ReviewedBy = adminId;
            application.ReviewedAt = DateTime.UtcNow;
            _repository.UpdateAsync(application);
        }

        /// <summary>
        /// Saves all pending state modifications onto the database context.
        /// </summary>
        private async Task SaveChanges()
        {
            await _repository.SaveChangesAsync();
        }

        /// <summary>
        /// Packages mapped operation status into the official ApiResponse wrapper structure.
        /// </summary>
        private ApiResponse<bool> CreateApiResponse(bool result)
        {
            return ApiResponse<bool>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result
            );
        }
    }
}