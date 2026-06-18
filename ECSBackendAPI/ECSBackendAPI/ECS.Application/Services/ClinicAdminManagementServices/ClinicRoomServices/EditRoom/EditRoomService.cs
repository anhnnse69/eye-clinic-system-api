using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.EditRoom
{
    /// <summary>
    /// Handles physical room structural manipulation requests by verifying authentication alignment and avoiding duplicates.
    /// </summary>
    public class EditRoomService : IEditRoomService
    {
        private readonly IRepositoryBaseAsync<FacilityRoom, Guid, AppDbContext> _facilityRoomRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="EditRoomService"/> with required ecosystem boundary interfaces.
        /// </summary>
        /// <param name="facilityRoomRepository">Repository mapping handling data updates for facility rooms.</param>
        /// <param name="staffClinicRepository">Repository managing relationships mapping administrators to specific clinics.</param>
        /// <param name="httpContextAccessor">Accessor fetching claim verification variables from active active HTTP context pipes.</param>
        public EditRoomService(
            IRepositoryBaseAsync<FacilityRoom, Guid, AppDbContext> facilityRoomRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _facilityRoomRepository = facilityRoomRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes core business data modifications via strict linear checkpoint assignments without branching if blocks.
        /// </summary>
        /// <param name="request">The operational criteria packet containing modification request data values.</param>
        /// <returns>An envelope payload indicating success metrics or tracking code indices.</returns>
        public async Task<ApiResponse<EditRoomResponse>> Process(EditRoomRequest request)
        {
            // Step 1: Initialize transaction status layout monitoring parameter flags
            bool isUserValid = true;
            bool isRoomExist = true;
            bool isNameUnique = true;

            // Step 2: Extract administrator claim session verification parameters from request identity context
            var userId = RetrieveUserId(out isUserValid);

            // Step 3: Identify the clinic context boundary matched directly against the acting administrator account
            var clinicId = await RetrieveClinicId(userId, isUserValid);

            // Step 4: Access underlying storage nodes to locate the targeted physical room entry context matching the criteria
            var originalRoom = await FetchRoomData(request.RoomId, clinicId);

            // Step 5: Screen targeting availability matrices and record system state confirmation metrics flags
            ValidateRoomExistence(originalRoom, ref isRoomExist);

            // Step 6: Query data vectors to screen against unexpected identity name collisions inside the same clinic block boundary
            isNameUnique = await VerifyUniqueRoomName(request.RoomName, request.RoomId, clinicId, isRoomExist);

            // Step 7: Apply the modified payload parameters directly onto underlying system database states
            var updatedRoom = await ApplyRoomUpdates(originalRoom, request, isRoomExist, isNameUnique);

            // Step 8: Assemble final system transport models to deliver the processing execution response envelope
            return CreateResponse(updatedRoom, isUserValid, isRoomExist, isNameUnique);
        }

        /// <summary>
        /// Extracts the user identifier tracking parameters from the current HTTP session state identity context.
        /// </summary>
        /// <param name="isUserValid">Output execution validation flag updated to false if token reading fails.</param>
        /// <returns>The decoded unique system identifier index signature value of the user.</returns>
        private Guid RetrieveUserId(out bool isUserValid)
        {
            isUserValid = true;
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }
            return userId;
        }

        /// <summary>
        /// Queries allocation profiles asynchronously to extract the matching clinic identifier linked onto the user.
        /// </summary>
        /// <param name="userId">The parsed user tracker identifier code block value.</param>
        /// <param name="isUserValid">The structural tracking gate validating session operations integrity metrics.</param>
        /// <returns>The assigned clinic identification key reference on successful tracking execution matches; otherwise null.</returns>
        private async Task<Guid?> RetrieveClinicId(Guid userId, bool isUserValid)
        {
            if (!isUserValid) return null;

            var staffClinic = await _staffClinicRepository
                .FindByCondition(x => x.UserId == userId && x.IsActive)
                .FirstOrDefaultAsync();

            return staffClinic?.ClinicId;
        }

        /// <summary>
        /// Queries data stores securely to access persistent facility room nodes assigned inside boundaries.
        /// </summary>
        /// <param name="roomId">The targeted physical structural element data primary key.</param>
        /// <param name="clinicId">The contextual domain boundary anchor matching the user tracking envelope.</param>
        /// <returns>The located internal domain structure tracking reference properties mapping entries, or null.</returns>
        private async Task<FacilityRoom?> FetchRoomData(Guid roomId, Guid? clinicId)
        {
            if (!clinicId.HasValue) return null;

            return await _facilityRoomRepository
                .FindByCondition(x => x.Id == roomId && x.ClinicId == clinicId.Value)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Verifies structural element availability checkpoints across mapping execution vectors.
        /// </summary>
        /// <param name="originalRoom">The located persistent target entity object reference returned by storage nodes, or null.</param>
        /// <param name="isRoomExist">Reference control status parameter updating to false if context validation fails.</param>
        private void ValidateRoomExistence(FacilityRoom? originalRoom, ref bool isRoomExist)
        {
            if (originalRoom == null)
            {
                isRoomExist = false;
            }
        }

        /// <summary>
        /// Evaluates naming designation strings inside matching clinic scopes to protect database uniqueness metrics bounds.
        /// </summary>
        /// <param name="roomName">The input string data literal text containing the prospective new name entry.</param>
        /// <param name="roomId">The core room primary tracker identity key to bypass collisions with self records.</param>
        /// <param name="clinicId">The contextual parent environment anchor reference matching scope layers.</param>
        /// <param name="isRoomExist">Verification precondition parameter regulating execution streams states.</param>
        /// <returns>True if the designated title avoids all matching duplicates in database vectors; otherwise false.</returns>
        private async Task<bool> VerifyUniqueRoomName(string roomName, Guid roomId, Guid? clinicId, bool isRoomExist)
        {
            if (!isRoomExist || !clinicId.HasValue) return false;

            var normalizedName = roomName.Trim().ToLower();

            var isDuplicate = await _facilityRoomRepository.FindByCondition(x =>
                x.ClinicId == clinicId.Value &&
                x.Id != roomId &&
                x.RoomName.Trim().ToLower() == normalizedName
            ).AnyAsync();

            return !isDuplicate;
        }

        /// <summary>
        /// Commits operational modification inputs directly onto mapped persistent storage configurations tracking streams.
        /// </summary>
        /// <param name="room">The internal data model entity layout target block reference.</param>
        /// <param name="request">The input packet container conveying updated functional specification fields.</param>
        /// <param name="isRoomExist">Checkpoint status flag evaluating historical component tracking visibility benchmarks.</param>
        /// <param name="isNameUnique">Checkpoint status flag capturing literal tracking validation metrics parameters.</param>
        /// <returns>The modified and persisted tracking context state entity instance configuration metadata, or null.</returns>
        private async Task<FacilityRoom?> ApplyRoomUpdates(FacilityRoom? room, EditRoomRequest request, bool isRoomExist, bool isNameUnique)
        {
            if (!isRoomExist || !isNameUnique || room == null) return null;

            room.RoomName = request.RoomName.Trim();
            room.RoomType = request.RoomType;

            await _facilityRoomRepository.UpdateAsync(room);
            await _facilityRoomRepository.SaveChangesAsync();

            return room;
        }

        /// <summary>
        /// Maps an internal data model layout onto an explicitly decoupled serializable application payload block.
        /// </summary>
        /// <param name="room">The updated backend physical resource tracking entity context instance reference.</param>
        /// <returns>A clean target structural DTO package model initialized for external presentation channels.</returns>
        private EditRoomResponse MapToResponseDto(FacilityRoom room)
        {
            return new EditRoomResponse
            {
                // Mapping entity system identifiers directly onto explicit tracking keys
                Id = room.Id,
                // Passing the registered clinic contextual boundary link reference
                ClinicId = room.ClinicId,
                // Assigning the safe string literal containing the room designation
                RoomName = room.RoomName,
                // Relaying functional classifications mapping system types
                RoomType = room.RoomType,
                // Exporting operational business verification criteria switches
                IsActive = room.IsActive
            };
        }

        /// <summary>
        /// Generates the standard processing response envelope wrapping the successfully generated outcomes payload blocks.
        /// </summary>
        /// <param name="updatedRoom">The persisted domain structural element context holding completed execution metrics variables.</param>
        /// <param name="isUserValid">The identity parameter condition verifying token security integrity.</param>
        /// <param name="isRoomExist">The resource tracking validation checker measuring targeting successes.</param>
        /// <param name="isNameUnique">The naming consistency confirmation control flag evaluating overlap statuses.</param>
        /// <returns>A unified data wrapper matching serialization structures with final success or error status metrics.</returns>
        private ApiResponse<EditRoomResponse> CreateResponse(
            FacilityRoom? updatedRoom,
            bool isUserValid,
            bool isRoomExist,
            bool isNameUnique)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isRoomExist, isNameUnique);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<EditRoomResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                MapToResponseDto(updatedRoom!));
        }

        /// <summary>
        /// Builds standard error messaging payloads capturing broken transactional rule markers triggered inside workflows.
        /// </summary>
        /// <param name="isUserValid">Indicates whether session access data matches user profiles safely.</param>
        /// <param name="isRoomExist">Indicates whether database entities were mapped inside boundaries successfully.</param>
        /// <param name="isNameUnique">Indicates whether prospective titles avoided collision anomalies in active scopes.</param>
        /// <returns>A customized application response serialization context filled with fail parameters, or null.</returns>
        private ApiResponse<EditRoomResponse>? CreateErrorResponse(bool isUserValid, bool isRoomExist, bool isNameUnique)
        {
            if (!isUserValid)
            {
                return ApiResponse<EditRoomResponse>.Fail(GeneralCode.APP_MESSAGE_4001.ToString());
            }
            if (!isRoomExist)
            {
                return ApiResponse<EditRoomResponse>.Fail(GeneralCode.APP_MESSAGE_4020.ToString());
            }
            if (!isNameUnique)
            {
                return ApiResponse<EditRoomResponse>.Fail(GeneralCode.APP_MESSAGE_4019.ToString());
            }

            return null;
        }
    }
}