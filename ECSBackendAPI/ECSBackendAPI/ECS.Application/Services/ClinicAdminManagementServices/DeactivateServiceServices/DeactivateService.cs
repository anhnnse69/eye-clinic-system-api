using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.DeactivateServiceServices
{
    /// <summary>
    /// Handles clinic service management administrative operations to safe-drop system availability tags.
    /// </summary>
    public class DeactivateService : IDeactivateService
    {
        private readonly IRepositoryBaseAsync<Service, Guid, AppDbContext> _serviceRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="DeactivateService"/> with infrastructure dependencies.
        /// </summary>
        /// <param name="serviceRepository">Repository boundary layer manipulation instance managing core entity state updates.</param>
        /// <param name="staffClinicRepository">Repository managing relationships connecting administration staff identities to clinics.</param>
        /// <param name="httpContextAccessor">Accessor layer retrieving authenticated context criteria metadata out of active request channels.</param>
        public DeactivateService(
            IRepositoryBaseAsync<Service, Guid, AppDbContext> serviceRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _serviceRepository = serviceRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal data update pipeline workflow to evaluate identity context, fetch service data, and commit state alterations.
        /// </summary>
        /// <param name="request">The dynamic tracking parameter containing target identification row metrics details.</param>
        /// <returns>An <see cref="ApiResponse{DeactivateServiceResponse}"/> enclosing execution state payload tracking data envelopes.</returns>
        public async Task<ApiResponse<DeactivateServiceResponse>> Process(DeactivateServiceRequest request)
        {
            // Step 1: Initialize sequential control status validation indicators
            bool isUserValid = true;
            bool isServiceExist = true;

            // Step 2: Extract administrator tracking parameters out of active claim token identity contexts
            var userId = RetrieveUserId(ref isUserValid);

            // Step 3: Fetch linked Clinic ID mappings based on administrative staff account records
            var clinicId = await RetrieveClinicId(userId, isUserValid);

            // Step 4: Query the physical persistence store to locate matching active service entity instances
            var targetService = await RetrieveServiceData(request.ServiceId, clinicId);

            // Step 5: Evaluate target data structures visibility constraints to flag structural validation presence anomalies
            ValidateServicePresence(targetService, ref isServiceExist);

            // Step 6: Commit state modification parameters to transform data property values internally
            await ApplyStateDeactivation(targetService!, isServiceExist);

            // Step 7: Project persistent model states directly mapping layout properties onto serialized target payload schemas
            var mappedResult = MapToResponseDto(targetService, isServiceExist);

            // Step 8: Assemble systemic structural response payloads alongside execution metrics metadata
            return CreateResponse(mappedResult, isUserValid, isServiceExist);
        }

        /// <summary>
        /// Decodes active security context descriptors to extract unique user identity strings.
        /// </summary>
        private Guid RetrieveUserId(ref bool isUserValid)
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }
            return userId;
        }

        /// <summary>
        /// Isolates target operation center locations mapped directly against the verified session profile.
        /// </summary>
        private async Task<Guid?> RetrieveClinicId(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }
            var staffClinic = await _staffClinicRepository
                .FindByCondition(x => x.UserId == userId && x.IsActive)
                .AsAsyncEnumerable()
                .FirstOrDefaultAsync();
            return staffClinic?.ClinicId;
        }

        /// <summary>
        /// Looks up operational target records matching individual service identities bounded by management criteria parameters.
        /// </summary>
        private async Task<Service?> RetrieveServiceData(Guid serviceId, Guid? clinicId)
        {
            if (!clinicId.HasValue)
            {
                return null;
            }
            return await _serviceRepository
                .FindByCondition(x => x.Id == serviceId && x.ClinicId == clinicId.Value)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Inspects entity presence attributes to flag structural integrity variations matching database lookups.
        /// </summary>
        private void ValidateServicePresence(Service? service, ref bool isServiceExist)
        {
            if (service == null)
            {
                isServiceExist = false;
            }
        }

        /// <summary>
        /// Modifies state properties safely and fires unit of work commands to persist changes.
        /// </summary>
        private async Task ApplyStateDeactivation(Service service, bool isServiceExist)
        {
            if (!isServiceExist)
            {
                return;
            }

            // CHỈ SỬA ĐÚNG DÒNG NÀY: Đảo ngược trạng thái hiện tại (Đang true -> false, Đang false -> true)
            service.IsActive = !service.IsActive;

            await _serviceRepository.UpdateAsync(service);
            await _serviceRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Transforms persistent business model states into clean, decoupled serialization layout representations.
        /// </summary>
        private DeactivateServiceResponse MapToResponseDto(Service? service, bool isServiceExist)
        {
            if (!isServiceExist || service == null)
            {
                return new DeactivateServiceResponse();
            }
            return new DeactivateServiceResponse
            {
                // Mapping operational unique identity reference keys onto the final payload response block
                ServiceId = service.Id,
                // Assigning the newly altered administrative workflow entity system field flags
                IsActive = service.IsActive
            };
        }

        /// <summary>
        /// Packages structural runtime process execution markers into standardized application layout schemas.
        /// </summary>
        private ApiResponse<DeactivateServiceResponse> CreateResponse(DeactivateServiceResponse result, bool isUserValid, bool isServiceExist)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isServiceExist);
            if (errorResponse != null)
            {
                return errorResponse;
            }
            return ApiResponse<DeactivateServiceResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result);
        }

        /// <summary>
        /// Screens runtime flags to isolate and surface descriptive transaction validation system errors.
        /// </summary>
        private ApiResponse<DeactivateServiceResponse>? CreateErrorResponse(bool isUserValid, bool isClinicExist)
        {
            if (!isUserValid)
            {
                return ApiResponse<DeactivateServiceResponse>.Fail(GeneralCode.APP_MESSAGE_4001.ToString());
            }
            if (!isClinicExist)
            {
                return ApiResponse<DeactivateServiceResponse>.Fail(GeneralCode.APP_MESSAGE_4020.ToString());
            }
            return null;
        }
    }
}