using ECS.Application.Services.ClinicAdminManagementServices.DeactivateServiceServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Provides reusable mock data for DeactivateService unit tests.
    /// </summary>
    public static class DeactivateServiceMockData
    {
        // ── Deterministic identifiers ───────────────────────────────────────────

        public static Guid TestUserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static Guid TestClinicId => Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static Guid TestServiceId => Guid.Parse("33333333-3333-3333-3333-333333333333");

        // ── Aggregate helpers ──────────────────────────────────────────────────

        public static StaffClinic GetActiveStaffClinic() => new StaffClinic
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            UserId = TestUserId,
            ClinicId = TestClinicId,
            Role = StaffRole.CLINIC_ADMIN,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            UpdatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        public static Service GetActiveService() => new Service
        {
            Id = TestServiceId,
            ClinicId = TestClinicId,
            ServiceName = "General Consultation",
            Price = 100_000m,
            DurationMinutes = 30,
            IsActive = true
        };

        public static Service GetInactiveService() => new Service
        {
            Id = TestServiceId,
            ClinicId = TestClinicId,
            ServiceName = "General Consultation",
            Price = 100_000m,
            DurationMinutes = 30,
            IsActive = false
        };

        // ── Request factories ──────────────────────────────────────────────────

        public static DeactivateServiceRequest GetValidRequest() => new DeactivateServiceRequest
        {
            ServiceId = TestServiceId
        };
    }
}
