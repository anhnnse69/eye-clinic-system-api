using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicProfileServices
{
    /// <summary>
    /// Handles clinic profile retrieval operations by verifying administrator context.
    /// </summary>
    public class ViewClinicService : IViewClinicService
    {
        private readonly IRepositoryQueryBase<Clinic, Guid, AppDbContext> _clinicRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="ViewClinicService"/> with required dependencies.
        /// </summary>
        /// <param name="clinicRepository">Repository for querying clinic master data.</param>
        /// <param name="staffClinicRepository">Repository for querying staff-to-clinic relationships.</param>
        /// <param name="httpContextAccessor">Accessor to retrieve authentication context from HTTP request.</param>
        public ViewClinicService(
            IRepositoryQueryBase<Clinic, Guid, AppDbContext> clinicRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _clinicRepository = clinicRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes clinic profile request by validating the authenticated user and fetching related clinic details.
        /// </summary>
        /// <param name="request">The view clinic request details.</param>
        /// <returns>An <see cref="ApiResponse{ViewClinicResponse}"/> containing data on success, or an error code.</returns>
        public async Task<ApiResponse<ViewClinicResponse>> Process(ViewClinicRequest request)
        {
            // Initialize status tracking flags
            bool isUserValid = true;
            bool isClinicExist = true;
            // Extract User ID from current token context
            var userId = RetrieveUserId(ref isUserValid);
            // Fetch linked Clinic ID based on user relationship mapping
            var clinicId = await RetrieveClinicId(userId, isUserValid);
            // Fetch core clinic entity attributes
            var retrievedClinic = await RetrieveClinicData(clinicId);
            // Validate that the targeted clinic record exists and is active
            ValidateRetrievedData(retrievedClinic, ref isClinicExist);
            // Assemble API payload or generate contextual workflow error response
            return CreateResponse(retrievedClinic, isUserValid, isClinicExist);
        }

        /// <summary>
        /// Retrieves the current user's unique identifier from HTTP context JWT identity claims.
        /// </summary>
        /// <param name="isUserValid">Flag updated to <c>false</c> if the identity claim is missing or malformed.</param>
        /// <returns>The extracted <see cref="Guid"/> on success; otherwise <see cref="Guid.Empty"/>.</returns>
        private Guid RetrieveUserId(ref bool isUserValid)
        {
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)
                ?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }
            return userId;
        }

        /// <summary>
        /// Resolves the associated Clinic ID for the specified staff/user account.
        /// </summary>
        /// <param name="userId">The verified user identifier.</param>
        /// <param name="isUserValid">Pre-condition check status indicating if user resolution is skipped.</param>
        /// <returns>The mapped <see cref="Guid"/> of the target clinic, or <c>null</c> if skipped/not found.</returns>
        private async Task<Guid?> RetrieveClinicId(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }
            var staffClinic = await _staffClinicRepository
                .FindByCondition(x =>
                    x.UserId == userId &&
                    x.IsActive)
                .FirstOrDefaultAsync();
            return staffClinic?.ClinicId;
        }

        /// <summary>
        /// Queries the data store for an active clinic record matching the specified unique identifier.
        /// </summary>
        /// <param name="clinicId">The targeted clinic identifier identifier token.</param>
        /// <returns>The matching active <see cref="Clinic"/> entity if found; otherwise <c>null</c>.</returns>
        private async Task<Clinic?> RetrieveClinicData(Guid? clinicId)
        {
            if (!clinicId.HasValue)
            {
                return null;
            }
            return await _clinicRepository
                .FindByCondition(x =>
                    x.Id == clinicId.Value &&
                    x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Evaluates the fetched clinic entity presence and flags missing data discrepancies.
        /// </summary>
        /// <param name="retrievedClinic">The resolved entity, or <c>null</c> if the look-up yielded no record.</param>
        /// <param name="isClinicExist">Flag updated to <c>false</c> if data validation checks fail.</param>
        private void ValidateRetrievedData(Clinic? retrievedClinic, ref bool isClinicExist)
        {
            if (retrievedClinic == null)
            {
                isClinicExist = false;
            }
        }

        /// <summary>
        /// Generates an encapsulation framework structure response payload containing entity details or validation failures.
        /// </summary>
        /// <param name="retrievedClinic">The queried clinic model entity instance.</param>
        /// <param name="isUserValid">Flag state parameter mapping token context validity.</param>
        /// <param name="isClinicExist">Flag state parameter mapping entity persistence verification.</param>
        /// <returns>A configured <see cref="ApiResponse{ViewClinicResponse}"/>.</returns>
        private ApiResponse<ViewClinicResponse> CreateResponse(
            Clinic? retrievedClinic,
            bool isUserValid,
            bool isClinicExist)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isClinicExist);

            if (errorResponse != null)
            {
                return errorResponse;
            }
            return ApiResponse<ViewClinicResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                MapToResponse(retrievedClinic!));
        }

        /// <summary>
        /// Screens runtime process execution flag indicators to return standardized systemic application error schemas.
        /// </summary>
        /// <param name="isUserValid">Indicates whether user resolution parameter parsing was successful.</param>
        /// <param name="isClinicExist">Indicates whether a clinic instance entity was successfully retrieved.</param>
        /// <returns>A failed <see cref="ApiResponse{ViewClinicResponse}"/> variant if errors are found; otherwise <c>null</c>.</returns>
        private ApiResponse<ViewClinicResponse>? CreateErrorResponse(bool isUserValid, bool isClinicExist)
        {
            // Return 4001 if user session context is corrupted
            if (!isUserValid)
            {
                return ApiResponse<ViewClinicResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }
            // Return 4020 if the clinic data instance does not exist
            if (!isClinicExist)
            {
                return ApiResponse<ViewClinicResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4020.ToString());
            }
            return null;
        }

        /// <summary>
        /// Transforms internal persistent context database model data directly onto business serialization object schemas.
        /// </summary>
        /// <param name="clinic">The internal physical domain entity record source object.</param>
        /// <returns>A structural domain projection instance representation object.</returns>
        private ViewClinicResponse MapToResponse(Clinic clinic)
        {
            return new ViewClinicResponse
            {
                Id = clinic.Id,
                Name = clinic.Name,
                Address = clinic.Address,
                Phone = clinic.Phone,
                Email = clinic.Email,
                LogoUrl = clinic.LogoUrl,
                Description = clinic.Description,
                IsActive = clinic.IsActive,
                RatingAvg = clinic.RatingAvg,
                ReviewCount = clinic.ReviewCount
            };
        }
    }
}