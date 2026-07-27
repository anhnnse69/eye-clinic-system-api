using ECS.Application.Services.ClinicAdminManagementServices.RequestPublishClinicServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class RequestPublishClinicMockData
    {
        public static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ValidClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static RequestPublishClinicRequest GetValidRequest(string? clinicIdStr = null)
        {
            return new RequestPublishClinicRequest
            {
                ClinicId = clinicIdStr ?? ValidClinicId.ToString()
            };
        }

        public static StaffClinic GetValidStaffClinic(Guid? userId = null, Guid? clinicId = null)
        {
            return new StaffClinic
            {
                Id = Guid.NewGuid(),
                UserId = userId ?? ValidUserId,
                ClinicId = clinicId ?? ValidClinicId,
                Role = StaffRole.CLINIC_ADMIN,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static Clinic GetValidClinic(Guid? clinicId = null, bool isPublished = false, bool isPublicationRequested = false)
        {
            return new Clinic
            {
                Id = clinicId ?? ValidClinicId,
                Name = "Test Clinic",
                Address = "123 Test Street",
                Phone = "0987654321",
                Email = "clinic@test.com",
                IsActive = true,
                IsPublished = isPublished,
                IsPublicationRequested = isPublicationRequested,
                PublicationRequestedAt = isPublicationRequested ? DateTime.Now.AddDays(-1) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
