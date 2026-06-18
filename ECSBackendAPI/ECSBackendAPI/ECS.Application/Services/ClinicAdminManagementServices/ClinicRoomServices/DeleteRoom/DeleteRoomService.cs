using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.DeleteRoom
{
    /// <summary>
    /// Handles physical room status transformations by verifying authentication alignments and soft-deleting or toggling block targets.
    /// </summary>
    public class DeleteRoomService : IDeleteRoomService
    {
        private readonly IRepositoryBaseAsync<FacilityRoom, Guid, AppDbContext> _facilityRoomRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="DeleteRoomService"/> with required database layers and HTTP context handlers.
        /// </summary>
        /// <param name="facilityRoomRepository">Repository handling data persistence and status mutations for facility rooms.</param>
        /// <param name="staffClinicRepository">Repository managing contextual relationship mapping administrators to active clinics.</param>
        /// <param name="httpContextAccessor">Accessor extracting claims verification variables out of active HTTP context pipes.</param>
        public DeleteRoomService(
            IRepositoryBaseAsync<FacilityRoom, Guid, AppDbContext> facilityRoomRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _facilityRoomRepository = facilityRoomRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes room soft deletion modifications via strict linear checkpoint assignments without branching if blocks.
        /// </summary>
        /// <param name="request">The operational criteria packet containing status modification request parameters.</param>
        /// <returns>An envelope payload indicating execution success metrics or error tracking entries.</returns>
        public async Task<ApiResponse<DeleteRoomResponse>> Process(DeleteRoomRequest request)
        {
            // Step 1: Initialize sequential control tracking validation state flags
            bool isUserValid = true;
            bool isRoomExist = true;

            // Step 2: Extract identity parameters from the current security claim context
            var userId = RetrieveUserId(out isUserValid);

            // Step 3: Search operational relational structures to isolate the clinic linked directly onto the administrator account
            var clinicId = await RetrieveClinicId(userId, isUserValid);

            // Step 4: Access underlying storage spaces to locate the physical room record matching the route indices
            var originalRoom = await FetchRoomData(request.RoomId, clinicId);

            // Step 5: Screen targeting availability matrices and record system state confirmation metrics flags
            ValidateRoomExistence(originalRoom, ref isRoomExist);

            // Step 6: Apply the soft deletion or toggled visibility parameter adjustments onto the database target node
            var updatedRoom = await ApplyStatusMutation(originalRoom, request.IsActive, isRoomExist);

            // Step 7: Assemble final system transport models to deliver the processing execution response envelope
            return CreateResponse(updatedRoom, isUserValid, isRoomExist);
        }

        /// <summary>
        /// Resolves the logged-in user signature coordinates using token claim parsing streams.
        /// </summary>
        /// <param name="isUserValid">Output evaluation status updating to false if identification variables fail parsing matches.</param>
        /// <returns>The decoded user identity descriptor parameter identifier value on success; otherwise Guid.Empty.</returns>
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
        /// Probes system allocation maps to extract the unique clinic identity attached to the active user profile.
        /// </summary>
        /// <param name="userId">The verified system user tracker key configuration value.</param>
        /// <param name="isUserValid">Validation controller metric parameter guarding access execution states.</param>
        /// <returns>A tracking token matching clinic configurations if isolated successfully; otherwise null.</returns>
        private async Task<Guid?> RetrieveClinicId(Guid userId, bool isUserValid)
        {
            if (!isUserValid) return null;

            var staffClinic = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _staffClinicRepository.FindByCondition(x => x.UserId == userId && x.IsActive)
            );

            return staffClinic?.ClinicId;
        }

        /// <summary>
        /// Queries the data store for an active or blocked facility room record matching specified identifiers and clinic bindings.
        /// </summary>
        /// <param name="roomId">The targeted physical tracking element primary database key index.</param>
        /// <param name="clinicId">The parent clinic allocation boundary parameter tracking context.</param>
        /// <returns>The physical model context tracking instance properties if found; otherwise null.</returns>
        private async Task<FacilityRoom?> FetchRoomData(Guid roomId, Guid? clinicId)
        {
            if (!clinicId.HasValue) return null;

            return await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _facilityRoomRepository.FindByCondition(x => x.Id == roomId && x.ClinicId == clinicId.Value)
            );
        }

        /// <summary>
        /// Evaluates the fetched facility room presence and flags missing data discrepancies.
        /// </summary>
        /// <param name="originalRoom">The resolved physical model database record data entry, or null.</param>
        /// <param name="isRoomExist">Flag updated to false if data validation checks determine entity absence.</param>
        private void ValidateRoomExistence(FacilityRoom? originalRoom, ref bool isRoomExist)
        {
            if (originalRoom == null)
            {
                isRoomExist = false;
            }
        }

        /// <summary>
        /// mutates the target entity model active indicator flag inside tracking contexts and commits transaction parameters.
        /// </summary>
        /// <param name="room">The mutable facility room domain entity tracker block instance context reference.</param>
        /// <param name="isActiveTarget">The targeted structural status representation intended for application updates.</param>
        /// <param name="isRoomExist">Pre-condition status flag evaluating resource visualization benchmarks.</param>
        /// <returns>The modified and persisted entity tracking reference model; otherwise null.</returns>
        private async Task<FacilityRoom?> ApplyStatusMutation(FacilityRoom? room, bool isActiveTarget, bool isRoomExist)
        {
            if (!isRoomExist || room == null) return null;

            // Apply soft-delete (false) or unlock/reactivate (true) status based on request payload parameters
            room.IsActive = isActiveTarget;

            await _facilityRoomRepository.UpdateAsync(room);
            await _facilityRoomRepository.SaveChangesAsync();

            return room;
        }

        /// <summary>
        /// Transforms persistent core domain layouts directly into decoupled presentation data schemas.
        /// </summary>
        /// <param name="room">The internal physical data model entry source block reference.</param>
        /// <returns>A structured presentation target data payload instance setup configuration.</returns>
        private DeleteRoomResponse MapToResponseDto(FacilityRoom room)
        {
            return new DeleteRoomResponse
            {
                // Mapping physical core system database record keys onto DTO fields
                Id = room.Id,
                // Passing the registered parent clinic contextual location reference link
                ClinicId = room.ClinicId,
                // Assigning the string literal text containing the current room designation
                RoomName = room.RoomName,
                // Relaying functional architectural classification strings mapping configuration specs
                RoomType = room.RoomType,
                // Exporting operational business status verification switches tracking soft deletion states
                IsActive = room.IsActive
            };
        }

        /// <summary>
        /// Evaluates processing outcome metrics variables to deliver standard successful envelopes or error schemas.
        /// </summary>
        /// <param name="updatedRoom">The persisted domain structural tracking element containing updated execution metrics variables.</param>
        /// <param name="isUserValid">Flag parameter verifying active session profile validation states.</param>
        /// <param name="isRoomExist">Flag parameter tracking domain data resource discovery benchmarks.</param>
        /// <returns>An encapsulated application serialization envelope payload configured for external streams.</returns>
        private ApiResponse<DeleteRoomResponse> CreateResponse(
            FacilityRoom? updatedRoom,
            bool isUserValid,
            bool isRoomExist)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isRoomExist);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<DeleteRoomResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                MapToResponseDto(updatedRoom!));
        }

        /// <summary>
        /// Evaluates checkpoint indicators matrix values to formulate structural application error entries.
        /// </summary>
        /// <param name="isUserValid">Indicates whether session access context metrics parsed matching variables safely.</param>
        /// <param name="isRoomExist">Indicates whether database records were mapped inside boundaries successfully.</param>
        /// <returns>A failed variant payload package context if errors triggered; otherwise null properties.</returns>
        private ApiResponse<DeleteRoomResponse>? CreateErrorResponse(bool isUserValid, bool isRoomExist)
        {
            // Return 4001 if the user session claims context is corrupted or missing
            if (!isUserValid)
            {
                return ApiResponse<DeleteRoomResponse>.Fail(GeneralCode.APP_MESSAGE_4001.ToString());
            }
            // Return 4020 if the facility room node target resource is absent from the clinic context
            if (!isRoomExist)
            {
                return ApiResponse<DeleteRoomResponse>.Fail(GeneralCode.APP_MESSAGE_4020.ToString());
            }

            return null;
        }
    }
}