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

        private async Task<Guid?> RetrieveClinicId(Guid userId, bool isUserValid)
        {
            if (!isUserValid) return null;

            var staffClinic = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _staffClinicRepository.FindByCondition(x => x.UserId == userId && x.IsActive)
            );

            return staffClinic?.ClinicId;
        }

        private async Task<FacilityRoom?> FetchRoomData(Guid roomId, Guid? clinicId)
        {
            if (!clinicId.HasValue) return null;

            return await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _facilityRoomRepository.FindByCondition(x => x.Id == roomId && x.ClinicId == clinicId.Value)
            );
        }

        private void ValidateRoomExistence(FacilityRoom? originalRoom, ref bool isRoomExist)
        {
            if (originalRoom == null)
            {
                isRoomExist = false;
            }
        }

        private async Task<bool> VerifyUniqueRoomName(string roomName, Guid roomId, Guid? clinicId, bool isRoomExist)
        {
            if (!isRoomExist || !clinicId.HasValue) return false;

            var normalizedName = roomName.Trim().ToLower();

            var isDuplicate = await EntityFrameworkQueryableExtensions.AnyAsync(
                _facilityRoomRepository.FindByCondition(x => x.ClinicId == clinicId.Value &&
                                                            x.Id != roomId &&
                                                            x.RoomName.Trim().ToLower() == normalizedName)
            );

            return !isDuplicate;
        }

        private async Task<FacilityRoom?> ApplyRoomUpdates(FacilityRoom? room, EditRoomRequest request, bool isRoomExist, bool isNameUnique)
        {
            if (!isRoomExist || !isNameUnique || room == null) return null;

            room.RoomName = request.RoomName.Trim();
            room.RoomType = request.RoomType;

            await _facilityRoomRepository.UpdateAsync(room);
            await _facilityRoomRepository.SaveChangesAsync();

            return room;
        }

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