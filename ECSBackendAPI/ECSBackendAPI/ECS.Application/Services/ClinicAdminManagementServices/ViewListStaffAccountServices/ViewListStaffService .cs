using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices
{
    /// <summary>
    /// Handles clinic staff list retrieval operations by verifying administrator context.
    /// </summary>
    public class ViewListStaffService : IViewListStaffService
    {
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewListStaffService"/> class with required dependencies.
        /// </summary>
        /// <param name="staffClinicRepository">Repository for querying staff-to-clinic relationships.</param>
        /// <param name="httpContextAccessor">Accessor to retrieve authentication context from HTTP request.</param>
        public ViewListStaffService(
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes clinic staff list request by validating the authenticated user and fetching related clinic details.
        /// </summary>
        /// <param name="request">The view staff list request details.</param>
        /// <returns>An <see cref="ApiResponse{List{StaffAccountResponse}}"/> containing data on success, or an error code.</returns>
        public async Task<ApiResponse<List<StaffAccountResponse>>> Process(ViewListStaffRequest request)
        {
            // Step 1: Initialize sequential control status validation variables
            bool isUserValid = true;
            // Step 2: Extract identity information parameter metrics from the active security claim session context
            var userId = RetrieveUserId(out isUserValid);
            // Step 3: Search operational relational databases to identify the clinic bound tightly to the active account
            var clinicId = await RetrieveClinicId(userId, isUserValid);
            // Step 4: Pull physical storage staff collections mapping against the verified environment context
            var staffList = await RetrieveStaffData(clinicId);
            // Step 5: Evaluate processing status parameters to safely confirm tracking context existence
            bool isClinicExist = ValidateClinicExistence(clinicId);
            // Step 6: Package state evaluations dynamically to create descriptive payload data structures
            return CreateResponse(staffList, isUserValid, isClinicExist);
        }

        /// <summary>
        /// Resolves the logged-in user signature coordinates using synchronous token parsing streams.
        /// </summary>
        /// <param name="isUserValid">Output evaluation status updating to false if identification variables fail parsing matches.</param>
        /// <returns>The decoded user identity descriptor parameter identifier value.</returns>
        private Guid RetrieveUserId(out bool isUserValid)
        {
            isUserValid = true;
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }
            return userId;
        }

        /// <summary>
        /// Probes system allocation tables to isolate unique context configurations related directly onto the logged-in account.
        /// </summary>
        /// <param name="userId">The system identity identifier tracking target metrics variables.</param>
        /// <param name="isUserValid">Validation controller metric parameter guarding access execution states.</param>
        /// <returns>A tracking key token matching system database configurations if matched; otherwise null.</returns>
        private async Task<Guid?> RetrieveClinicId(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }
            // Wrapped clearly within EF Queryable Extensions to completely avert compiler method type inference ambiguities
            var staffClinic = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _staffClinicRepository.FindByCondition(x => x.UserId == userId && x.IsActive)
            );
            return staffClinic?.ClinicId;
        }

        /// <summary>
        /// Asynchronously tracks underlying domain allocation rows using custom eager data inclusion strategies.
        /// </summary>
        /// <param name="clinicId">The contextual domain identifier configuration mapping the filter logic matrix.</param>
        /// <returns>A safe transactional data item array list tracking matching profile associations.</returns>
        private async Task<List<StaffClinic>> RetrieveStaffData(Guid? clinicId)
        {
            if (!clinicId.HasValue)
            {
                return new List<StaffClinic>();
            }
            return await EntityFrameworkQueryableExtensions.ToListAsync(
                _staffClinicRepository.FindByCondition(x => x.ClinicId == clinicId.Value && x.IsActive)
                                      .Include(x => x.User)
            );
        }

        /// <summary>
        /// Synchronous structural validation converter assessing resource allocation status variables.
        /// </summary>
        /// <param name="clinicId">The evaluated contextual location identity identifier data state.</param>
        /// <returns>True if tracking records map safely onto active domain locations; otherwise false.</returns>
        private bool ValidateClinicExistence(Guid? clinicId)
        {
            return clinicId.HasValue;
        }

        /// <summary>
        /// Analyzes state logic monitoring variables to determine outcome layout packaging choices.
        /// </summary>
        /// <param name="staffList">The persistent entity rows returned directly out of structural database executions.</param>
        /// <param name="isUserValid">Indicates whether user token extraction verification matched expectations successfully.</param>
        /// <param name="isClinicExist">Indicates data layer tracking context presence attributes.</param>
        /// <returns>A standardized application payload container detailed for transport serialization layers.</returns>
        private ApiResponse<List<StaffAccountResponse>> CreateResponse(
            List<StaffClinic> staffList,
            bool isUserValid,
            bool isClinicExist)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isClinicExist);
            if (errorResponse != null)
            {
                return errorResponse;
            }
            return ApiResponse<List<StaffAccountResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                MapToResponse(staffList));
        }

        /// <summary>
        /// Evaluates structural tracking conditions matrix variables to issue systemized application error entries.
        /// </summary>
        /// <param name="isUserValid">The structural state value verifying the validation of incoming authentication parameters.</param>
        /// <param name="isClinicExist">The state checking indicator capturing contextual environment existence benchmarks.</param>
        /// <returns>A failed API standard metadata package capsule if an error rule trips; otherwise null properties.</returns>
        private ApiResponse<List<StaffAccountResponse>>? CreateErrorResponse(bool isUserValid, bool isClinicExist)
        {
            // Return 4001 if authentication tokens fail validation operations
            if (!isUserValid)
            {
                return ApiResponse<List<StaffAccountResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }
            // Return 4020 if the linked operations clinic setup drops out of active entity streams
            if (!isClinicExist)
            {
                return ApiResponse<List<StaffAccountResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4020.ToString());
            }
            return null;
        }

        /// <summary>
        /// Transforms internal persistent context database model list directly onto business serialization object schemas.
        /// </summary>
        /// <param name="staffList">The internal physical domain entity record source collection.</param>
        /// <returns>A structured data list projection representation instance context.</returns>
        private List<StaffAccountResponse> MapToResponse(List<StaffClinic> staffList)
        {
            return staffList.Select(item => new StaffAccountResponse
            {
                // Mapping physical core system user identifier mappings
                UserId = item.UserId,
                // Extracting demographic metadata strings securely bypassing null values
                FullName = item.User?.FullName ?? string.Empty,
                Email = item.User?.Email ?? string.Empty,
                Phone = item.User?.Phone ?? string.Empty,
                // Translating application systemic enum signatures into localized descriptive layout tokens
                Role = item.Role switch
                {
                    StaffRole.DOCTOR => "Bác sĩ",
                    StaffRole.RECEPTIONIST => "Tiếp tân",
                    StaffRole.CLINIC_ADMIN => "Quản lý phòng khám",
                    _ => item.Role.ToString()
                },
                // Mapping functional timeline recording configurations
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt
            }).ToList();
        }
    }
}