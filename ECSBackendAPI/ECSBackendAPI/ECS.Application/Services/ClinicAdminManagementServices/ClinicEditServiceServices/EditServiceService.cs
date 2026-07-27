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
using System.Threading.Tasks;

namespace ECS.Application.Services.ClinicAdminManagementServices.EditServiceServices
{
    /// <summary>
    /// Handles the business logic for updating service attributes owned by a clinic after verifying clinic admin authorization and uniqueness rules.
    /// </summary>
    public class EditServiceService : IEditServiceService
    {
        private readonly IRepositoryBaseAsync<Service, Guid, AppDbContext> _serviceRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditServiceService"/> class with required infrastructure repository boundaries.
        /// </summary>
        /// <param name="serviceRepository">The persistence layer boundary reference executing transactional updates on services.</param>
        /// <param name="staffClinicRepository">Repository managing mappings checking what clinic the administrator is assigned onto.</param>
        /// <param name="httpContextAccessor">Accessor fetching user validation claims maps out of HTTP execution routing lanes.</param>
        public EditServiceService(
            IRepositoryBaseAsync<Service, Guid, AppDbContext> serviceRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _serviceRepository = serviceRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal operations sequence flow to match, validate, check duplicate name, update, and commit clinic service entities.
        /// </summary>
        /// <param name="request">The parameters tracking specific targets and data properties modification payload models.</param>
        /// <returns>An <see cref="ApiResponse{EditServiceResponse}"/> enclosing execution data attributes on success; or error flags.</returns>
        public async Task<ApiResponse<EditServiceResponse>> Process(EditServiceRequest request)
        {
            // Step 1: Initialize consecutive state evaluation validation tracking flags
            bool isUserValid = true;
            bool isClinicValid = true;
            bool isServiceExist = true;
            bool isOwnershipValid = true;
            bool isNameUnique = true;

            // Step 2: Extract identity metrics coordinate maps from current token claim token environments
            var userId = RetrieveUserId(out isUserValid);

            // Step 3: Probe physical tables tracking the specific context location assigned onto the active admin
            var clinicId = await RetrieveClinicId(userId, isUserValid);
            ValidateClinicContext(clinicId, ref isClinicValid);

            // Step 4: Retrieve target persistent model state rows corresponding directly to requested ID keys
            var existingService = await RetrieveServiceEntity(request.ServiceId, isClinicValid);
            ValidateServiceExistence(existingService, ref isServiceExist);

            // Step 5: Verify the retrieved service belongs completely inside the administration zone of the target clinic
            ValidateServiceOwnership(existingService, clinicId, isServiceExist, ref isOwnershipValid);

            // Step 6: Validate that the new service name does not conflict with another existing service in the same clinic
            isNameUnique = await VerifyServiceNameUniqueness(request.ServiceId, request.ServiceName, clinicId, isOwnershipValid);

            // Step 7: Apply structural data transitions onto domain states and push modifications onto data stores
            await ApplyStateChanges(existingService, request, isUserValid && isClinicValid && isServiceExist && isOwnershipValid && isNameUnique);

            // Step 8: Map modified internal persistent model properties directly onto decoupled presentation outputs
            var result = MapToResponseDto(existingService);

            // Step 9: Analyze monitoring metric parameters to formulate structural envelope results
            return CreateResponse(result, isUserValid, isClinicValid, isServiceExist, isOwnershipValid, isNameUnique);
        }

        /// <summary>
        /// Extracts the logged-in administrator context keys using HTTP context authentication headers.
        /// </summary>
        /// <param name="isUserValid">Output execution state tracking flag marked false if the claim fails mapping loops.</param>
        /// <returns>The unique identifier signature matching user system accounts.</returns>
        private Guid RetrieveUserId(out bool isUserValid)
        {
            isUserValid = true;
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }
            return userId;
        }

        /// <summary>
        /// Locates the specific workplace boundary mapped onto the current admin profile token.
        /// </summary>
        /// <param name="userId">The parsed user account primary database identification keys.</param>
        /// <param name="isUserValid">Precondition flag gating lookup pipelines to drop unexpected lookups.</param>
        /// <returns>The clinic target reference key value sequence if validated; otherwise null.</returns>
        private async Task<Guid?> RetrieveClinicId(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }

            var staffClinic = await _staffClinicRepository
                .FindByCondition(x => x.UserId == userId && x.IsActive)
                .AsQueryable()
                .FirstOrDefaultAsync<StaffClinic>();

            return staffClinic?.ClinicId;
        }

        /// <summary>
        /// Evaluates if the admin operator profile maps effectively onto an active enterprise zone.
        /// </summary>
        /// <param name="clinicId">The evaluated target workspace identity mapping reference.</param>
        /// <param name="isClinicValid">Reference flag tracking logical operation boundaries.</param>
        private void ValidateClinicContext(Guid? clinicId, ref bool isClinicValid)
        {
            if (!clinicId.HasValue)
            {
                isClinicValid = false;
            }
        }

        /// <summary>
        /// Loads targeted physical record information from database blocks using primary identity keys.
        /// </summary>
        /// <param name="serviceId">The reference primary key assigned onto the specific service.</param>
        /// <param name="isClinicValid">Precondition check to safely bypass data calls if context rules fail.</param>
        /// <returns>The active service model matching conditions if discovered; otherwise null.</returns>
        private async Task<Service?> RetrieveServiceEntity(Guid serviceId, bool isClinicValid)
        {
            if (!isClinicValid)
            {
                return null;
            }

            return await _serviceRepository.GetByIdAsync(serviceId);
        }

        /// <summary>
        /// Checks the presence of physical service units pulled out of persistent systems.
        /// </summary>
        /// <param name="service">The pulled entity container model object instance.</param>
        /// <param name="isServiceExist">Reference tracker updated false if lookups return empty rows.</param>
        private void ValidateServiceExistence(Service? service, ref bool isServiceExist)
        {
            if (service == null)
            {
                isServiceExist = false;
            }
        }

        /// <summary>
        /// Assures security cross-checks that the targeted entity exists inside the exact boundaries of the logged clinic.
        /// </summary>
        /// <param name="service">The verified physical service entity instance.</param>
        /// <param name="clinicId">The verified administrator operational workplace identity signature.</param>
        /// <param name="isServiceExist">Precondition indicating whether lookup row validation succeeded.</param>
        /// <param name="isOwnershipValid">Reference flag changed false if unauthorized access boundaries breach across clinics.</param>
        private void ValidateServiceOwnership(Service? service, Guid? clinicId, bool isServiceExist, ref bool isOwnershipValid)
        {
            // Reorganized from `!isServiceExist || service!.ClinicId != clinicId` so every
            // branch is independently coverable. Behavior is identical: the OR-short-circuit
            // collapses to "true if existence check failed OR clinic mismatches".
            // Using `isServiceExist` to short-circuit avoids the `service!` null-forgiving
            // dereference and the lifted `Guid?` comparison's nullable result handling.
            bool ownershipBreached = !isServiceExist;
            if (isServiceExist && service != null && clinicId.HasValue)
            {
                ownershipBreached = service.ClinicId != clinicId.Value;
            }

            if (ownershipBreached)
            {
                isOwnershipValid = false;
            }
        }

        /// <summary>
        /// Checks if another service with the same name already exists within the target clinic boundaries.
        /// </summary>
        /// <param name="serviceId">The current service identifier to exclude from conflict validation checks.</param>
        /// <param name="requestedName">The incoming service name value intended for modification.</param>
        /// <param name="clinicId">The active administrative workspace boundary identifier.</param>
        /// <param name="isOwnershipValid">Precondition mapping indicating whether authorization checks succeeded.</param>
        /// <returns>True if the name is unique and can be assigned; otherwise false if a conflict occurs.</returns>
        private async Task<bool> VerifyServiceNameUniqueness(Guid serviceId, string requestedName, Guid? clinicId, bool isOwnershipValid)
        {
            if (!isOwnershipValid)
            {
                return false;
            }

            string cleanName = requestedName.Trim().ToLower();

            // Query storage to see if another distinct record shares the same name within the clinic scope
            bool hasConflict = await _serviceRepository
                .FindByCondition(x => x.ClinicId == clinicId.Value &&
                                      x.Id != serviceId &&
                                      x.ServiceName.Trim().ToLower() == cleanName)
                .AsQueryable()
                .AnyAsync();

            return !hasConflict;
        }

        /// <summary>
        /// Synchronizes update parameters into domain properties and commits transactions downstream.
        /// </summary>
        /// <param name="service">The tracked domain instance data row targeted for modification maps.</param>
        /// <param name="request">The data container transport mapping new property attributes.</param>
        /// <param name="canExecute">A composite precondition flag controlling execution commits to block corrupt or duplicate inputs.</param>
        private async Task ApplyStateChanges(Service? service, EditServiceRequest request, bool canExecute)
        {
            if (!canExecute)
            {
                return;
            }

            // Sync updated attributes onto the active tracked domain row entity (Excluding IsActive status intentionally)
            service!.ServiceName = request.ServiceName.Trim();
            service.Price = request.Price;
            service.DurationMinutes = request.DurationMinutes;

            // Commit transaction changes to storage systems via repository interfaces
            await _serviceRepository.UpdateAsync(service);
            await _serviceRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Transforms tracked persistent internal structures directly onto transport serialization data contracts.
        /// </summary>
        /// <param name="service">The loaded domain data model snapshot entity context.</param>
        /// <returns>The presentation data view transport data payload schema structure.</returns>
        private EditServiceResponse? MapToResponseDto(Service? service)
        {
            if (service == null)
            {
                return null;
            }

            return new EditServiceResponse
            {
                // Mapping absolute system data coordinates into decoupled string configurations
                ServiceId = service.Id.ToString(),
                ClinicId = service.ClinicId.ToString(),
                ServiceName = service.ServiceName,
                Price = service.Price,
                DurationMinutes = service.DurationMinutes,
                // Appending structured logs timestamp updates representing tracking checks
                UpdatedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")
            };
        }

        /// <summary>
        /// Packages standard outcome blocks analyzing operational processing evaluation metrics variables.
        /// </summary>
        /// <param name="result">The finalized data response payload framework model object representation.</param>
        /// <param name="isUserValid">Operational flag detailing claim credential parse metrics.</param>
        /// <param name="isClinicValid">Operational flag capturing admin context alignment.</param>
        /// <param name="isServiceExist">Operational tracking flag defining record presence.</param>
        /// <param name="isOwnershipValid">Security configuration mapping authorization boundaries.</param>
        /// <param name="isNameUnique">Operational flag validating corporate enterprise uniqueness constraints.</param>
        /// <returns>The configured unified standardized encapsulation results block container payload.</returns>
        private ApiResponse<EditServiceResponse> CreateResponse(
            EditServiceResponse? result,
            bool isUserValid,
            bool isClinicValid,
            bool isServiceExist,
            bool isOwnershipValid,
            bool isNameUnique)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isClinicValid, isServiceExist, isOwnershipValid, isNameUnique);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<EditServiceResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result!);
        }

        /// <summary>
        /// Tracks consecutive verification matrix bounds to dispatch appropriate system-wide execution failure payloads.
        /// </summary>
        /// <param name="isUserValid">Fails validation maps when user contextual parameters parse values incorrectly.</param>
        /// <param name="isClinicValid">Fails validation maps when tracking context configurations drop records.</param>
        /// <param name="isServiceExist">Fails validation maps when looking up data target results drop empty.</param>
        /// <param name="isOwnershipValid">Fails validation maps when operations attempt touching non-owned records.</param>
        /// <param name="isNameUnique">Fails validation maps when the requested service name creates a collision in the clinic.</param>
        /// <returns>The tailored failed standard execution packaging structure if a threshold trips; otherwise null.</returns>
        private ApiResponse<EditServiceResponse>? CreateErrorResponse(
            bool isUserValid,
            bool isClinicValid,
            bool isServiceExist,
            bool isOwnershipValid,
            bool isNameUnique)
        {
            if (!isUserValid)
            {
                return ApiResponse<EditServiceResponse>.Fail(GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isClinicValid)
            {
                return ApiResponse<EditServiceResponse>.Fail(GeneralCode.APP_MESSAGE_4020.ToString());
            }

            if (!isServiceExist)
            {
                return ApiResponse<EditServiceResponse>.Fail(GeneralCode.APP_MESSAGE_4012.ToString());
            }

            if (!isOwnershipValid)
            {
                return ApiResponse<EditServiceResponse>.Fail(GeneralCode.APP_MESSAGE_4014.ToString());
            }

            if (!isNameUnique)
            {
                return ApiResponse<EditServiceResponse>.Fail(GeneralCode.APP_MESSAGE_4015.ToString());
            }

            return null;
        }
    }
}