using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.SystemAdminServices.ApproveClinicApplicationServices
{
    /// <summary>
    /// Handles the business logic for approving a clinic registration application and creating the corresponding clinic profile.
    /// </summary>
    public class ApproveClinicApplicationService : IApproveClinicApplicationService
    {
        private readonly IRepositoryBaseAsync<ClinicRegistrationRequest, Guid, AppDbContext> _requestRepository;
        private readonly IRepositoryBaseAsync<Clinic, Guid, AppDbContext> _clinicRepository;

        /// <summary>
        /// Initializes a new instance of <see cref="ApproveClinicApplicationService"/> with required dependencies.
        /// </summary>
        /// <param name="requestRepository">Repository for managing clinic registration requests status.</param>
        /// <param name="clinicRepository">Repository for persisting newly provisioned clinic models.</param>
        public ApproveClinicApplicationService(
            IRepositoryBaseAsync<ClinicRegistrationRequest, Guid, AppDbContext> requestRepository,
            IRepositoryBaseAsync<Clinic, Guid, AppDbContext> clinicRepository)
        {
            _requestRepository = requestRepository;
            _clinicRepository = clinicRepository;
        }

        /// <summary>
        /// Processes the approval action by updating application states and provisioning brand new minimal clinic environments.
        /// </summary>
        /// <param name="id">The unique identifier of the target clinic registration request.</param>
        /// <param name="adminId">The unique identifier of the system administrator performing the action.</param>
        /// <returns>An <see cref="ApiResponse{Boolean}"/> indicating the operational outcome.</returns>
        public async Task<ApiResponse<bool>> Process(Guid id, Guid adminId)
        {
            // Fetch and validate the active application request record
            var application = await FetchAndValidateApplication(id);

            // Update application parameters into approved state
            UpdateToApprovedState(application, adminId);

            // Provision a new minimal structural clinic space mapped from request parameters
            await ProvisionNewClinic(application);

            // Execute atomicity unit of work persistence operations
            await SaveAllChanges();

            // Return standardized API envelope response
            return CreateApiResponse(true);
        }

        /// <summary>
        /// Fetches the application request or throws contextual business rule violations exceptions.
        /// </summary>
        private async Task<ClinicRegistrationRequest> FetchAndValidateApplication(Guid id)
        {
            var application = await _requestRepository.FindByCondition(x => x.Id == id, trackChanges: true)
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
        /// Transitions the model status flags over into standard Approved tracking parameters.
        /// </summary>
        private void UpdateToApprovedState(ClinicRegistrationRequest application, Guid adminId)
        {
            application.Status = "APPROVED";
            application.ReviewedBy = adminId;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewNote = null;

            _requestRepository.UpdateAsync(application);
        }

        /// <summary>
        /// Maps existing application metrics to initialize a minimal workspace for a brand new Clinic entity.
        /// </summary>
        private async Task ProvisionNewClinic(ClinicRegistrationRequest application)
        {
            var newClinic = new Clinic
            {
                Id = Guid.NewGuid(),
                Name = application.ClinicName,
                Address = application.ClinicAddress,
                Phone = string.Empty,
                Email = null,
                LogoUrl = null,
                Description = null,
                IsActive = true,
                RatingAvg = 0,
                ReviewCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _clinicRepository.CreateAsync(newClinic);
        }

        /// <summary>
        /// Saves all shared active unit-of-work modifications across separate table sets simultaneously.
        /// </summary>
        private async Task SaveAllChanges()
        {
            await _requestRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Packages mapped validation outcome states into the official ApiResponse wrapper structure.
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