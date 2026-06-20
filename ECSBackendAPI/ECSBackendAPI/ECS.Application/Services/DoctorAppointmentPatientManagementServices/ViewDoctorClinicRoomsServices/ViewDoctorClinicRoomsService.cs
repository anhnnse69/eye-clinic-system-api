using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorClinicRoomsServices
{
    /// <summary>
    /// Retrieves the list of active facility rooms belonging to the clinic
    /// of the currently authenticated doctor.
    /// </summary>
    public class ViewDoctorClinicRoomsService : IViewDoctorClinicRoomsService
    {
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>
            _doctorRepo;
        private readonly IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext>
            _roomRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="ViewDoctorClinicRoomsService"/>.
        /// </summary>
        public ViewDoctorClinicRoomsService(
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo,
            IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> roomRepo)
        {
            _doctorRepo = doctorRepo;
            _roomRepo = roomRepo;
        }

        /// <summary>
        /// Retrieves the active rooms belonging to the doctor's clinic.
        /// </summary>
        /// <param name="userId">
        /// Identifier of the user account linked to the doctor profile.
        /// </param>
        /// <returns>
        /// A successful response containing the list of active clinic rooms.
        /// </returns>
        public async Task<ApiResponse<List<ClinicRoomResponse>>> Process(Guid userId)
        {
            var doctorProfile = await ResolveActiveDoctorProfileAsync(userId);
            var rooms = await FetchActiveRoomsAsync(doctorProfile.ClinicId);
            var result = BuildResponse(rooms);
            return CreateSuccessResponse(result);
        }

        /// <summary>
        /// Resolves the active doctor profile for the specified user.
        /// Throws when not found.
        /// </summary>
        /// <param name="userId">
        /// Identifier of the user account.
        /// </param>
        /// <returns>
        /// The resolved <see cref="DoctorProfile"/>.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the doctor cannot be found.
        /// </exception>
        private async Task<DoctorProfile> ResolveActiveDoctorProfileAsync(
            Guid userId)
        {
            var doctorProfile = await _doctorRepo
                .FindByCondition(d =>
                    d.UserId == userId &&
                    d.IsActive)
                .FirstOrDefaultAsync();
            if (doctorProfile is null)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4008.ToString());

            return doctorProfile;
        }

        /// <summary>
        /// Fetches all active rooms belonging to the given clinic.
        /// </summary>
        private async Task<List<FacilityRoom>> FetchActiveRoomsAsync(
            Guid clinicId)
        {
            return await _roomRepo
                .FindByCondition(r =>
                    r.ClinicId == clinicId &&
                    r.IsActive)
                .OrderBy(r => r.RoomName)
                .ToListAsync();
        }

        /// <summary>
        /// Maps facility room entities to response DTOs.
        /// </summary>
        private static List<ClinicRoomResponse> BuildResponse(
            List<FacilityRoom> rooms)
        {
            return rooms.Select(room => new ClinicRoomResponse
            {
                RoomId = room.Id,
                RoomName = room.RoomName,
                RoomType = string.IsNullOrWhiteSpace(room.RoomType)
                    ? "General"
                    : room.RoomType,
                IsActive = room.IsActive,
            }).ToList();
        }

        /// <summary>
        /// Creates a successful API response.
        /// </summary>
        private static ApiResponse<List<ClinicRoomResponse>> CreateSuccessResponse(
            List<ClinicRoomResponse> result)
        {
            return ApiResponse<List<ClinicRoomResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result);
        }
    }
}