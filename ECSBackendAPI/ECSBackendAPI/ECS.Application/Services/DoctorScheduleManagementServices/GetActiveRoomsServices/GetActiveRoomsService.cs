using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorScheduleManagementServices.GetActiveRoomsServices
{
    /// <summary>
    /// Service responsible for retrieving a list of active facility rooms filtered strictly by the performing receptionist's clinic boundary.
    /// Primarily utilized to populate room selection pickers or dropdown components during schedule creation.
    /// </summary>
    public class GetActiveRoomsService : IGetActiveRoomsService
    {
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepo;
        private readonly IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> _roomRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetActiveRoomsService"/> class.
        /// </summary>
        /// <param name="staffClinicRepo">Repository interface to verify receptionist identities and clinic mapping boundaries.</param>
        /// <param name="roomRepo">Repository interface to query and filter active facility rooms.</param>
        public GetActiveRoomsService(
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepo,
            IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> roomRepo)
        {
            _staffClinicRepo = staffClinicRepo;
            _roomRepo = roomRepo;
        }

        /// <summary>
        /// Processes the request to look up all active facility rooms belonging to the receptionist's assigned clinic context.
        /// </summary>
        /// <param name="receptionistUserId">The unique identifier of the performing receptionist user.</param>
        /// <returns>An API standard template response wrapping the list of structured clinic room payloads.</returns>
        public async Task<ApiResponse<List<ClinicRoomResponse>>> Process(Guid receptionistUserId)
        {
            var clinicId = await ResolveReceptionistClinicIdAsync(receptionistUserId);
            var rooms = await QueryActiveRoomsByClinicAsync(clinicId);

            return CreateSuccessResponse(rooms);
        }

        /// <summary>
        /// Resolves the clinic identifier associated with the active receptionist user.
        /// </summary>
        /// <param name="receptionistUserId">The unique identifier of the receptionist user.</param>
        /// <returns>The clinic identifier mapped to the specified receptionist.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the receptionist is not found or is inactive.</exception>
        private async Task<Guid> ResolveReceptionistClinicIdAsync(Guid receptionistUserId)
        {
            var staffClinic = await _staffClinicRepo
                .FindByCondition(sc => sc.UserId == receptionistUserId && sc.IsActive)
                .FirstOrDefaultAsync();

            if (staffClinic is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4008.ToString());

            return staffClinic.ClinicId;
        }

        /// <summary>
        /// Queries and maps active facility rooms within a specific clinic, sorted alphabetically by room name.
        /// </summary>
        /// <param name="clinicId">The unique identifier of the clinic target context.</param>
        /// <returns>A read-only list of mapped <see cref="ClinicRoomResponse"/> objects.</returns>
        private async Task<List<ClinicRoomResponse>> QueryActiveRoomsByClinicAsync(Guid clinicId)
        {
            return await _roomRepo
                .FindByCondition(r => r.ClinicId == clinicId && r.IsActive)
                .Select(r => new ClinicRoomResponse
                {
                    RoomId = r.Id,
                    RoomName = r.RoomName,
                    RoomType = r.RoomType,
                    IsActive = r.IsActive
                })
                .OrderBy(r => r.RoomName)
                .ToListAsync();
        }

        /// <summary>
        /// Wraps the retrieved room list into a standardized API success template response wrapper.
        /// </summary>
        /// <param name="rooms">The list of mapped clinic room responses payload.</param>
        /// <returns>The API standard outcome object encapsulating the collection response context.</returns>
        private static ApiResponse<List<ClinicRoomResponse>> CreateSuccessResponse(List<ClinicRoomResponse> rooms)
        {
            return ApiResponse<List<ClinicRoomResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                rooms);
        }
    }
}