using ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Provides reusable mock data for CreateStaff unit tests.
    /// </summary>
    public static class CreateStaffMockData
    {
        // ── Deterministic identifiers ───────────────────────────────────────────

        public static Guid TestAdminUserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static Guid TestClinicId => Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static Guid TestStaffClinicId => Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static Guid TestGeneratedUserId => Guid.Parse("44444444-4444-4444-4444-444444444444");

        // ── Aggregate helpers ──────────────────────────────────────────────────

        public static StaffClinic GetActiveStaffClinic() => new StaffClinic
        {
            Id = TestStaffClinicId,
            UserId = TestAdminUserId,
            ClinicId = TestClinicId,
            Role = StaffRole.CLINIC_ADMIN,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            UpdatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        public static User GetUserWithPhone(string phone) => new User
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Phone = phone,
            Email = "different@ECS.vn",
            FullName = "Existing Phone Owner",
            PasswordHash = "x",
            Role = UserRole.PATIENT,
            IsActive = true
        };

        public static User GetUserWithEmail(string email) => new User
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            Phone = "0999999999",
            Email = email,
            FullName = "Existing Email Owner",
            PasswordHash = "x",
            Role = UserRole.PATIENT,
            IsActive = true
        };

        // ── Request factories ──────────────────────────────────────────────────

        public static CreateStaffRequest GetValidReceptionistRequest() => new CreateStaffRequest
        {
            Phone = "0987654321",
            Email = "new-staff@ECS.vn",
            FullName = "Nguyen Van NewStaff",
            Password = "Staff@12345",
            StaffRole = StaffRole.RECEPTIONIST
        };

        public static CreateStaffRequest GetValidDoctorRequest() => new CreateStaffRequest
        {
            Phone = "0987654322",
            Email = "new-doctor@ECS.vn",
            FullName = "BS. Nguyen Van Doctor",
            Password = "Doctor@12345",
            StaffRole = StaffRole.DOCTOR
        };

        public static CreateStaffRequest GetValidClinicAdminRequest() => new CreateStaffRequest
        {
            Phone = "0987654323",
            Email = "new-admin@ECS.vn",
            FullName = "Nguyen Van Admin",
            Password = "Admin@12345",
            StaffRole = StaffRole.CLINIC_ADMIN
        };
    }
}
