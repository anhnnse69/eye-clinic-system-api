using ECS.Application.Services.ClinicAdminManagementServices.ClinicCreateRoomSevices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Provides reusable mock data for CreateRoom unit tests.
    /// </summary>
    public static class CreateRoomMockData
    {
        // ── Deterministic identifiers ───────────────────────────────────────────

        public static Guid TestUserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static Guid TestClinicId => Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static Guid TestRoomId => Guid.Parse("33333333-3333-3333-3333-333333333333");

        // ── Aggregate helpers ──────────────────────────────────────────────────

        public static User GetTestAdminUser() => new User
        {
            Id = TestUserId,
            FullName = "Nguyen Van Admin",
            Phone = "0900000001",
            Email = "admin@ECS.vn",
            PasswordHash = "x",
            Role = UserRole.CLINIC_ADMIN,
            IsActive = true
        };

        public static StaffClinic GetActiveStaffClinic() => new StaffClinic
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            ClinicId = TestClinicId,
            Role = StaffRole.CLINIC_ADMIN,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };

        public static FacilityRoom GetExistingFacilityRoom(string roomName = "Existing Room") => new FacilityRoom
        {
            Id = Guid.NewGuid(),
            ClinicId = TestClinicId,
            RoomName = roomName,
            RoomType = "Consultation",
            IsActive = true
        };

        // ── Request factories ──────────────────────────────────────────────────

        public static CreateRoomRequest GetValidRequest() => new CreateRoomRequest
        {
            RoomName = "  Consultation Room A1 ",
            RoomType = " Consultation "
        };
    }
}
