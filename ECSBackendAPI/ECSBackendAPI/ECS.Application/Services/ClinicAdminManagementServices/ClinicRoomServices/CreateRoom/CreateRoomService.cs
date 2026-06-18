using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.CreateRoom
{
    /// <summary>
    /// Executes logical workflows required to parse authorizations, map parameters, validate unique naming rules, and persist new facility rooms.
    /// </summary>
    public class CreateRoomService : ICreateRoomService
    {
        private readonly IRepositoryBaseAsync<FacilityRoom, Guid, AppDbContext> _facilityRoomRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateRoomService"/> class with storage and identity infrastructure components.
        /// </summary>
        /// <param name="facilityRoomRepository">Write-capable data context abstraction layer managing rooms entity lifecycle tables.</param>
        /// <param name="staffClinicRepository">Read-only lookup context processing administrative employee workplace mapping layouts.</param>
        /// <param name="httpContextAccessor">Accessor pipeline capturing ambient user state profiles claims structures.</param>
        public CreateRoomService(
            IRepositoryBaseAsync<FacilityRoom, Guid, AppDbContext> facilityRoomRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _facilityRoomRepository = facilityRoomRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Runs through structural check validations and data insertion transactions sequentially without interrupting if control sequences.
        /// </summary>
        /// <param name="request">The filtered data carrier framework parameters package.</param>
        /// <returns>A populated transport data entity standard response block tracking execution indicators.</returns>
        public async Task<ApiResponse<CreateRoomResponse>> Process(CreateRoomRequest request)
        {
            // Step 1: Initialize transactional status verification markers
            bool isUserValid = true;
            bool isClinicValid = true;
            bool isNameUnique = true;

            // Step 2: Extract identity markers using contextual claim processing loops
            var userId = RetrieveUserId(ref isUserValid);

            // Step 3: Interrogate organizational configuration bounds to extract the clinic context identity code
            var clinicId = await RetrieveClinicIdAsync(userId, isUserValid);
            isClinicValid = EvaluateClinicValidity(clinicId, isUserValid);

            // Step 4: Validate room name uniqueness inside the boundary context of the determined clinic environment
            isNameUnique = await ValidateRoomNameUniquenessAsync(request.RoomName, clinicId, isClinicValid);

            // Step 5: Map incoming model attributes into clean persistent model entities
            var roomEntity = InitializeRoomEntity(request, clinicId);

            // Step 6: Save transformed entities through infrastructure storage interfaces onto persistent contexts
            var executionResultId = await SaveFacilityRoomRecordAsync(roomEntity, isUserValid, isClinicValid, isNameUnique);

            // Step 7: Map updated repository tracking models into serialization transport boundaries
            var finalResponsePayload = MapToResponseDto(roomEntity, executionResultId);

            // Step 8: Synthesize execution tracking metrics to generate target application payload containers
            return CreateResponse(finalResponsePayload, isUserValid, isClinicValid, isNameUnique);
        }

        /// <summary>
        /// Pulls primary security signature references matching identity tokens out of ambient HTTP headers context.
        /// </summary>
        /// <param name="isUserValid">Operational verification status flag updating to false upon tracking mismatch errors.</param>
        /// <returns>The decoded user identity coordinate reference code on success; otherwise Guid.Empty.</returns>
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
        /// Locates administrative operational boundaries associated explicitly onto the current identity token.
        /// </summary>
        /// <param name="userId">The parsed user descriptor lookup parameter matrix values.</param>
        /// <param name="isUserValid">Guard condition preventing calculation errors when preceding sequences drop validation.</param>
        /// <returns>A valid organizational reference token code identifier; otherwise Guid.Empty.</returns>
        private async Task<Guid> RetrieveClinicIdAsync(Guid userId, bool isUserValid)
        {
            if (!isUserValid)
            {
                return Guid.Empty;
            }

            var staffClinic = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _staffClinicRepository.FindByCondition(x => x.UserId == userId && x.IsActive)
            );

            if (staffClinic == null)
            {
                return Guid.Empty;
            }

            return staffClinic.ClinicId;
        }

        /// <summary>
        /// Evaluates whether the extracted clinic identifiers fulfill active system integrity validation boundaries.
        /// </summary>
        /// <param name="clinicId">The evaluated clinic identification token data.</param>
        /// <param name="isUserValid">The status tracking state of the primary user validation workflow step.</param>
        /// <returns>True if the clinic metadata token resolves to an operational state; otherwise false.</returns>
        private bool EvaluateClinicValidity(Guid clinicId, bool isUserValid)
        {
            return isUserValid && clinicId != Guid.Empty;
        }

        /// <summary>
        /// Performs checks inside the infrastructure repository layers to guarantee room names remain unique inside the scope of the clinic.
        /// </summary>
        /// <param name="roomName">The string text identifying the target clinical room nomenclature.</param>
        /// <param name="clinicId">The scoped enterprise identification token guiding the system verification filters.</param>
        /// <param name="isClinicValid">Guard status tracking variable ensuring previous lookups resolved properly.</param>
        /// <returns>True if the name is completely unique within the targeted operational clinic context; otherwise false.</returns>
        private async Task<bool> ValidateRoomNameUniquenessAsync(string roomName, Guid clinicId, bool isClinicValid)
        {
            if (!isClinicValid || string.IsNullOrWhiteSpace(roomName))
            {
                return true;
            }

            var standardizedName = roomName.Trim().ToLower();

            var doesExist = await EntityFrameworkQueryableExtensions.AnyAsync(
                _facilityRoomRepository.FindByCondition(x => x.ClinicId == clinicId && x.RoomName.Trim().ToLower() == standardizedName)
            );

            return !doesExist;
        }

        /// <summary>
        /// Creates a configured tracking model entity initialized with active state values.
        /// </summary>
        /// <param name="request">The raw presentation parameters model container object.</param>
        /// <param name="clinicId">The contextual tracking reference token location id.</param>
        /// <returns>A ready-to-persist <see cref="FacilityRoom"/> object model structure.</returns>
        private FacilityRoom InitializeRoomEntity(CreateRoomRequest request, Guid clinicId)
        {
            return new FacilityRoom
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                RoomName = request.RoomName.Trim(),
                RoomType = request.RoomType?.Trim(),
                IsActive = true
            };
        }

        /// <summary>
        /// Dispatches persistent creation commands across operational repository abstract structures.
        /// </summary>
        /// <param name="room">The configured persistent record model parameters target instance.</param>
        /// <param name="isUserValid">The current structural verification context value tracking parameter metrics.</param>
        /// <param name="isClinicValid">Context validity flag parameter indicating if storage insertion commands execute.</param>
        /// <param name="isNameUnique">Operational integrity flag tracking duplicate name conflicts.</param>
        /// <returns>The assigned unique database registration primary identification identifier tracker key code.</returns>
        private async Task<Guid> SaveFacilityRoomRecordAsync(FacilityRoom room, bool isUserValid, bool isClinicValid, bool isNameUnique)
        {
            if (!isUserValid || !isClinicValid || !isNameUnique)
            {
                return Guid.Empty;
            }

            var id = await _facilityRoomRepository.CreateAsync(room);
            await _facilityRoomRepository.SaveChangesAsync();
            return id;
        }

        /// <summary>
        /// Transforms backend relational structural objects directly onto presentation serialization entities.
        /// </summary>
        /// <param name="room">The physical domain record context data model layout properties parameters source.</param>
        /// <param name="generatedId">The database entity identity key tracker verification value token.</param>
        /// <returns>A mapped, serialized, presentation-safe result properties data payload object layout.</returns>
        private CreateRoomResponse MapToResponseDto(FacilityRoom room, Guid generatedId)
        {
            return new CreateRoomResponse
            {
                // Mapping the database tracking key token reference id
                Id = generatedId,
                // Assigning corporate facility location group linkage identity parameters
                ClinicId = room.ClinicId,
                // Mapping verified unique organizational naming records strings
                RoomName = room.RoomName,
                // Mapping specialized clinical operation taxonomy category strings
                RoomType = room.RoomType,
                // Passing default environment operational statuses indicators
                IsActive = room.IsActive
            };
        }

        /// <summary>
        /// Composes standardized success responses or delegates failure handling to error isolation branches.
        /// </summary>
        /// <param name="result">The compiled presentation object data package mapping target logic outcomes.</param>
        /// <param name="isUserValid">Indicates authorization check processing output metrics indicators matches.</param>
        /// <param name="isClinicValid">Indicates context state structural integrity layout validation boundaries indicators.</param>
        /// <param name="isNameUnique">Indicates the result metrics mapping the business validation constraints rules.</param>
        /// <returns>A structured unified transmission envelope packaging tracking outcomes data results.</returns>
        private ApiResponse<CreateRoomResponse> CreateResponse(CreateRoomResponse result, bool isUserValid, bool isClinicValid, bool isNameUnique)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isClinicValid, isNameUnique);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<CreateRoomResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result);
        }

        /// <summary>
        /// Screens runtime context trace markers to format standard application client exceptions schemas.
        /// </summary>
        /// <param name="isUserValid">True if token context variables passed decryption verification schemas.</param>
        /// <param name="isClinicValid">True if administrative user profile associations are attached securely.</param>
        /// <param name="isNameUnique">True if no other room inside the facility tracks the identical nomenclatures.</param>
        /// <returns>A failed variant envelope pattern container if rules trigger; otherwise null indicators.</returns>
        private ApiResponse<CreateRoomResponse>? CreateErrorResponse(bool isUserValid, bool isClinicValid, bool isNameUnique)
        {
            // Return 4001 if authentication tokens fail validation operations
            if (!isUserValid)
            {
                return ApiResponse<CreateRoomResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }

            // Return 4020 if the linked operations clinic setup drops out of active entity streams
            if (!isClinicValid)
            {
                return ApiResponse<CreateRoomResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4020.ToString());
            }

            // Return 4021 if a naming collision occurs inside the same facility context boundary
            if (!isNameUnique)
            {
                return ApiResponse<CreateRoomResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4021.ToString());
            }

            return null;
        }
    }
}