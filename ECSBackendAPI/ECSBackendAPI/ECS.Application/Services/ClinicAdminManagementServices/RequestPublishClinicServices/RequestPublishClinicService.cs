using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicAdminManagementServices.RequestPublishClinicServices
{
    /// <summary>
    /// Handles business operations for submitting clinic publication requests
    /// associated with the authenticated clinic administrator.
    /// </summary>
    public class RequestPublishClinicService : IRequestPublishClinicService
    {
        private readonly IRepositoryBaseAsync<Clinic, Guid, AppDbContext> _clinicRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestPublishClinicService"/> class
        /// with required repository boundaries and authentication context providers.
        /// </summary>
        /// <param name="clinicRepository">
        /// Repository boundary instance responsible for managing clinic persistence operations.
        /// </param>
        /// <param name="staffClinicRepository">
        /// Repository boundary instance providing staff-to-clinic relationship query capabilities.
        /// </param>
        /// <param name="httpContextAccessor">
        /// Accessor used to retrieve authenticated user identity information from active HTTP pipelines.
        /// </param>
        public RequestPublishClinicService(
            IRepositoryBaseAsync<Clinic, Guid, AppDbContext> clinicRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _clinicRepository = clinicRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the complete clinic publication request workflow including identity validation,
        /// clinic ownership verification, publication status checks,
        /// and persistence of publication request modifications.
        /// </summary>
        /// <param name="request">
        /// The request payload containing the clinic identifier.
        /// </param>
        /// <returns>
        /// An <see cref="ApiResponse{Boolean}"/> describing execution status
        /// and publication request outcome information.
        /// </returns>
        public async Task<ApiResponse<bool>> Process(RequestPublishClinicRequest request)
        {
            // Initialize status tracking flags
            bool isUserValid = true;
            bool isClinicExist = true;
            bool isAuthorizedClinic = true;
            bool isAlreadyPublished = false;
            bool isAlreadyRequested = false;

            // Step 1: Extract identity information parameter metrics from active security claim session context
            var userId = RetrieveUserId(ref isUserValid);

            // Step 2: Search operational relational databases to identify clinic bound tightly to active account
            var staffClinic = await RetrieveClinicData(userId, isUserValid);

            // Step 3: Track context validation parameters safely before hitting core processing pipelines
            ValidateClinicContext(staffClinic, isUserValid, ref isClinicExist);

            // Step 4: Retrieve target clinic entity
            var clinic = await RetrieveClinic(staffClinic, isClinicExist);

            ValidateClinicExistence(clinic, ref isClinicExist);

            // Step 5: Validate ownership relationship between clinic and staff
            ValidateClinicOwnership(clinic, staffClinic, isClinicExist, ref isAuthorizedClinic);

            // Step 6: Check publication status
            ValidatePublicationStatus(clinic, isClinicExist, isAuthorizedClinic, ref isAlreadyPublished, ref isAlreadyRequested);

            // Step 7: Apply modifications to entity
            UpdateClinicPublicationRequest(clinic, isClinicExist, isAuthorizedClinic, isAlreadyPublished, isAlreadyRequested);

            // Step 8: Persist changes and package processing outcome
            return await CreateResponse(clinic, isUserValid, isClinicExist, isAuthorizedClinic, isAlreadyPublished, isAlreadyRequested);
        }

        /// <summary>
        /// Resolves the authenticated user identifier from active security claim collections.
        /// </summary>
        /// <param name="isUserValid">
        /// Validation flag updated when claim extraction or identifier parsing fails.
        /// </param>
        /// <returns>
        /// The authenticated user identifier if successfully resolved; otherwise an empty GUID.
        /// </returns>
        private Guid RetrieveUserId(ref bool isUserValid)
        {
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User?
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
        /// Retrieves the clinic relationship mapping associated with the authenticated user account.
        /// </summary>
        /// <param name="userId">
        /// The identifier representing the current authenticated user.
        /// </param>
        /// <param name="isUserValid">
        /// Validation state indicating whether user identity resolution succeeded.
        /// </param>
        /// <returns>
        /// The corresponding staff-clinic relationship record if found; otherwise null.
        /// </returns>
        private async Task<StaffClinic?> RetrieveClinicData(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }

            return await _staffClinicRepository
                .FindByCondition(x => x.UserId == userId && x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Evaluates whether the authenticated user is associated with a valid clinic context.
        /// </summary>
        /// <param name="staffClinic">
        /// The resolved clinic relationship entity associated with the current user.
        /// </param>
        /// <param name="isUserValid">
        /// Indicates whether user authentication information was successfully resolved.
        /// </param>
        /// <param name="isClinicExist">
        /// Flag updated when no valid clinic association can be located.
        /// </param>
        private void ValidateClinicContext(StaffClinic? staffClinic, bool isUserValid, ref bool isClinicExist)
        {
            if (!isUserValid)
            {
                return;
            }

            if (staffClinic == null)
            {
                isClinicExist = false;
            }
        }

        /// <summary>
        /// Retrieves the targeted clinic entity from persistence storage using the staff clinic relationship.
        /// </summary>
        /// <param name="staffClinic">
        /// The staff clinic relationship entity.
        /// </param>
        /// <param name="isClinicExist">
        /// Validation flag indicating whether clinic context verification succeeded.
        /// </param>
        /// <returns>
        /// The matching clinic entity if found; otherwise null.
        /// </returns>
        private async Task<Clinic?> RetrieveClinic(StaffClinic? staffClinic, bool isClinicExist)
        {
            if (!isClinicExist || staffClinic == null)
            {
                return null;
            }

            return await _clinicRepository
                .FindByCondition(x => x.Id == staffClinic.ClinicId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Evaluates whether the targeted clinic record exists within the persistence layer.
        /// </summary>
        /// <param name="clinic">
        /// The clinic entity returned from the repository query operation.
        /// </param>
        /// <param name="isClinicExist">
        /// Flag updated when the requested clinic cannot be located.
        /// </param>
        private void ValidateClinicExistence(Clinic? clinic, ref bool isClinicExist)
        {
            if (clinic == null)
            {
                isClinicExist = false;
            }
        }

        /// <summary>
        /// Validates that the targeted clinic belongs to the authenticated staff member.
        /// </summary>
        /// <param name="clinic">
        /// The clinic entity selected for publication request processing.
        /// </param>
        /// <param name="staffClinic">
        /// The clinic relationship entity associated with the authenticated administrator.
        /// </param>
        /// <param name="isClinicExist">
        /// Indicates whether the clinic record was successfully located.
        /// </param>
        /// <param name="isAuthorizedClinic">
        /// Flag updated when ownership validation fails.
        /// </param>
        private void ValidateClinicOwnership(
            Clinic? clinic,
            StaffClinic? staffClinic,
            bool isClinicExist,
            ref bool isAuthorizedClinic)
        {
            if (!isClinicExist || clinic == null || staffClinic == null)
            {
                return;
            }

            if (clinic.Id != staffClinic.ClinicId)
            {
                isAuthorizedClinic = false;
            }
        }

        /// <summary>
        /// Validates the publication status of the clinic to prevent duplicate requests.
        /// </summary>
        /// <param name="clinic">
        /// The clinic entity selected for publication request processing.
        /// </param>
        /// <param name="isClinicExist">
        /// Indicates whether the clinic record was successfully located.
        /// </param>
        /// <param name="isAuthorizedClinic">
        /// Indicates whether ownership validation checks succeeded.
        /// </param>
        /// <param name="isAlreadyPublished">
        /// Flag updated when clinic is already published.
        /// </param>
        /// <param name="isAlreadyRequested">
        /// Flag updated when clinic already has a pending publication request.
        /// </param>
        private void ValidatePublicationStatus(
            Clinic? clinic,
            bool isClinicExist,
            bool isAuthorizedClinic,
            ref bool isAlreadyPublished,
            ref bool isAlreadyRequested)
        {
            if (!isClinicExist || !isAuthorizedClinic || clinic == null)
            {
                return;
            }

            if (clinic.IsPublished)
            {
                isAlreadyPublished = true;
            }

            if (clinic.IsPublicationRequested)
            {
                isAlreadyRequested = true;
            }
        }

        /// <summary>
        /// Applies publication request modifications by updating clinic status flags.
        /// </summary>
        /// <param name="clinic">
        /// The target clinic entity selected for update operations.
        /// </param>
        /// <param name="isClinicExist">
        /// Indicates whether clinic validation checks succeeded.
        /// </param>
        /// <param name="isAuthorizedClinic">
        /// Indicates whether ownership validation checks succeeded.
        /// </param>
        /// <param name="isAlreadyPublished">
        /// Indicates whether clinic is already published.
        /// </param>
        /// <param name="isAlreadyRequested">
        /// Indicates whether clinic already has a pending publication request.
        /// </param>
        private static void UpdateClinicPublicationRequest(
            Clinic? clinic,
            bool isClinicExist,
            bool isAuthorizedClinic,
            bool isAlreadyPublished,
            bool isAlreadyRequested)
        {
            if (!isClinicExist || !isAuthorizedClinic || clinic == null)
            {
                return;
            }

            if (isAlreadyPublished || isAlreadyRequested)
            {
                return;
            }

            clinic.IsPublicationRequested = true;
            clinic.PublicationRequestedAt = DateTime.Now;
        }

        /// <summary>
        /// Coordinates validation outcome evaluation, persists entity modifications,
        /// and packages standardized API responses.
        /// </summary>
        /// <param name="clinic">
        /// The clinic entity containing pending update state modifications.
        /// </param>
        /// <param name="isUserValid">
        /// Indicates whether user identity validation succeeded.
        /// </param>
        /// <param name="isClinicExist">
        /// Indicates whether clinic context validation succeeded.
        /// </param>
        /// <param name="isAuthorizedClinic">
        /// Indicates whether clinic ownership validation succeeded.
        /// </param>
        /// <param name="isAlreadyPublished">
        /// Indicates whether clinic is already published.
        /// </param>
        /// <param name="isAlreadyRequested">
        /// Indicates whether clinic already has a pending publication request.
        /// </param>
        /// <returns>
        /// A standardized API response describing processing results.
        /// </returns>
        private async Task<ApiResponse<bool>> CreateResponse(
            Clinic? clinic,
            bool isUserValid,
            bool isClinicExist,
            bool isAuthorizedClinic,
            bool isAlreadyPublished,
            bool isAlreadyRequested)
        {
            var errorResponse = CreateErrorResponse(
                isUserValid,
                isClinicExist,
                isAuthorizedClinic,
                isAlreadyPublished,
                isAlreadyRequested);

            if (errorResponse != null)
            {
                return errorResponse;
            }

            await _clinicRepository.UpdateAsync(clinic!);
            await _clinicRepository.SaveChangesAsync();

            return ApiResponse<bool>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                true);
        }

        /// <summary>
        /// Evaluates validation state variables and generates standardized error responses
        /// when business rule requirements are not satisfied.
        /// </summary>
        /// <param name="isUserValid">
        /// Indicates whether authentication information is valid.
        /// </param>
        /// <param name="isClinicExist">
        /// Indicates whether a clinic association exists for the authenticated user.
        /// </param>
        /// <param name="isAuthorizedClinic">
        /// Indicates whether the clinic belongs to the authenticated staff.
        /// </param>
        /// <param name="isAlreadyPublished">
        /// Indicates whether the clinic is already published.
        /// </param>
        /// <param name="isAlreadyRequested">
        /// Indicates whether a publication request is already pending.
        /// </param>
        /// <returns>
        /// A failed API response when validation rules are violated; otherwise null.
        /// </returns>
        private ApiResponse<bool>? CreateErrorResponse(
            bool isUserValid,
            bool isClinicExist,
            bool isAuthorizedClinic,
            bool isAlreadyPublished,
            bool isAlreadyRequested)
        {
            if (!isUserValid)
            {
                return ApiResponse<bool>.Fail(
                    GeneralCode.APP_MESSAGE_4033.ToString());
            }

            if (!isClinicExist)
            {
                return ApiResponse<bool>.Fail(
                    GeneralCode.APP_MESSAGE_4034.ToString());
            }

            if (!isAuthorizedClinic)
            {
                return ApiResponse<bool>.Fail(
                    GeneralCode.APP_MESSAGE_4058.ToString());
            }

            if (isAlreadyPublished)
            {
                return ApiResponse<bool>.Fail(
                    GeneralCode.APP_MESSAGE_4059.ToString());
            }

            if (isAlreadyRequested)
            {
                return ApiResponse<bool>.Fail(
                    GeneralCode.APP_MESSAGE_4000.ToString());
            }

            return null;
        }
    }
}