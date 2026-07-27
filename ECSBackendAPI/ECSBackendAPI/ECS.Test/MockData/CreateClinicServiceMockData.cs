using ECS.Application.Services.ClinicAdminManagementServices.CreateServiceServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Provides reusable mock data for CreateService unit tests.
    /// </summary>
    public static class CreateClinicServiceMockData
    {
        // ── Deterministic identifiers ───────────────────────────────────────────

        public static Guid TestUserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static Guid TestClinicId => Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static Guid TestServiceId => Guid.Parse("33333333-3333-3333-3333-333333333333");

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

        public static Service GetExistingService(string serviceName = "Existing Service") => new Service
        {
            Id = Guid.NewGuid(),
            ClinicId = TestClinicId,
            ServiceName = serviceName,
            Price = 100000m,
            DurationMinutes = 30,
            IsActive = true
        };

        // ── Request factories ──────────────────────────────────────────────────

        public static CreateServiceRequest GetValidRequest() => new CreateServiceRequest
        {
            ServiceName = "  General Consultation  ",
            Price = 200000m,
            DurationMinutes = 30
        };
    }
}
