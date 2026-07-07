using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.SystemAdminServices.ApproveClinicPublicationServices
{
    /// <summary>
    /// Handles the business logic for approving a clinic's pending publication request.
    /// </summary>
    public class ApproveClinicPublicationService : IApproveClinicPublicationService
    {
        private readonly IRepositoryQueryBase<Clinic, Guid, AppDbContext> _queryRepo;
        private readonly IRepositoryBaseAsync<Clinic, Guid, AppDbContext> _commandRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="ApproveClinicPublicationService"/> with required dependencies.
        /// </summary>
        /// <param name="queryRepo">Repository for querying clinic persistence data.</param>
        /// <param name="commandRepo">Repository for persisting clinic changes.</param>
        public ApproveClinicPublicationService(
            IRepositoryQueryBase<Clinic, Guid, AppDbContext> queryRepo,
            IRepositoryBaseAsync<Clinic, Guid, AppDbContext> commandRepo)
        {
            _queryRepo = queryRepo;
            _commandRepo = commandRepo;
        }

        /// <summary>
        /// Processes the approval action by validating the clinic state and transitioning its publication status.
        /// </summary>
        /// <param name="id">The unique identifier of the target clinic.</param>
        /// <param name="adminId">The unique identifier of the system administrator performing the action.</param>
        /// <returns>An <see cref="ApiResponse{Boolean}"/> indicating the operational outcome.</returns>
        public async Task<ApiResponse<bool>> Process(Guid id, Guid adminId)
        {
            // Step 1: Fetch and validate the active clinic publication request record
            var clinic = await FetchAndValidateClinic(id);

            // Step 2: Update clinic parameters into published 
            UpdateToPublishedState(clinic);

            // Step 3: Execute atomicity unit of work persistence operations
            await SaveAllChanges();

            // Step 4: Return standardized API envelope response
            return CreateApiResponse(true);
        }

        /// <summary>
        /// Fetches the clinic request or throws contextual business rule violations exceptions.
        /// </summary>
        private async Task<Clinic> FetchAndValidateClinic(Guid id)
        {
            var clinic = await _queryRepo.FindByCondition(x => x.Id == id, trackChanges: true)
                                         .FirstOrDefaultAsync();

            if (clinic == null)
            {
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4004.ToString()); // Not Found
            }
            if (!clinic.IsPublicationRequested)
            {
                throw new InvalidOperationException(GeneralCode.APP_MESSAGE_4000.ToString()); // Invalid State / No request
            }
            return clinic;
        }

        /// <summary>
        /// Transitions the model status flags over into standard Published tracking parameters on RAM.
        /// </summary>
        private void UpdateToPublishedState(Clinic clinic)
        {
            clinic.IsPublished = true;
            clinic.IsPublicationRequested = false;
            clinic.UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Saves all shared active unit-of-work modifications across separate table sets simultaneously.
        /// </summary>
        private async Task SaveAllChanges()
        {
            await _commandRepo.SaveChangesAsync();
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