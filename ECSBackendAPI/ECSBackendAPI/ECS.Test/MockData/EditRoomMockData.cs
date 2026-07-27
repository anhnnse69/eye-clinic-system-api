using ECS.Application.Services.ClinicAdminManagementServices.ClinicEditRoomServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Provides reusable mock data for EditRoom unit tests.
    /// </summary>
    public static class EditRoomMockData
    {
        // ── Deterministic identifiers ───────────────────────────────────────────

        public static Guid TestUserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static Guid TestClinicId => Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static Guid TestRoomId => Guid.Parse("33333333-3333-3333-3333-333333333333");

        // ── Aggregate helpers ──────────────────────────────────────────────────

        public static StaffClinic GetActiveStaffClinic() => new StaffClinic
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            ClinicId = TestClinicId,
            Role = StaffRole.CLINIC_ADMIN,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };

        public static FacilityRoom GetFacilityRoom(
            string roomName = "Consultation Room A1",
            string? roomType = "Consultation",
            bool isActive = true,
            Guid? id = null,
            Guid? clinicId = null) => new FacilityRoom
        {
            Id = id ?? TestRoomId,
            ClinicId = clinicId ?? TestClinicId,
            RoomName = roomName,
            RoomType = roomType,
            IsActive = isActive
        };

        // ── Request factories ──────────────────────────────────────────────────

        public static EditRoomRequest GetValidRequest(string? roomName = null, string? roomType = null) => new EditRoomRequest
        {
            RoomId = TestRoomId,
            RoomName = roomName ?? "Consultation Room A2",
            RoomType = roomType ?? "Consultation"
        };

        public static EditRoomRequest GetRequestWithName(string roomName, string? roomType = "Consultation") => new EditRoomRequest
        {
            RoomId = TestRoomId,
            RoomName = roomName,
            RoomType = roomType
        };
    }
}