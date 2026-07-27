using ECS.Application.Services.ClinicAdminManagementServices.ClinicDeleteRoomServices;
using ECS.Domain.Entities.Clinics;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Provides reusable mock data for DeleteRoom unit tests.
    /// </summary>
    public static class DeleteRoomMockData
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
            IsActive = true
        };

        public static FacilityRoom GetFacilityRoom(bool isActive = true, string roomName = "Consultation Room A1", string? roomType = "Consultation") => new FacilityRoom
        {
            Id = TestRoomId,
            ClinicId = TestClinicId,
            RoomName = roomName,
            RoomType = roomType,
            IsActive = isActive
        };

        // ── Request factories ──────────────────────────────────────────────────

        public static DeleteRoomRequest GetDeactivateRequest() => new DeleteRoomRequest
        {
            RoomId = TestRoomId,
            IsActive = false
        };

        public static DeleteRoomRequest GetReactivateRequest() => new DeleteRoomRequest
        {
            RoomId = TestRoomId,
            IsActive = true
        };
    }
}