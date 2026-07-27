using ECS.Domain.Entities.Clinics;

namespace ECS.Test.MockData
{
    public static class ClinicProfileMockData
    {
        public static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static StaffClinic GetActiveStaffClinic()
        {
            return new StaffClinic
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                UserId = UserId,
                ClinicId = ClinicId,
                IsActive = true
            };
        }

        public static Clinic GetActiveClinic()
        {
            return new Clinic
            {
                Id = ClinicId,
                Name = "ECS Eye Clinic",
                Address = "123 Nguyen Trai, Ha Noi",
                Phone = "02412345678",
                Email = "contact@ecs-clinic.vn",
                LogoUrl = "https://cdn.ecs-clinic.vn/logo.png",
                Description = "Comprehensive eye care clinic",
                IsActive = true,
                RatingAvg = 4.75m,
                ReviewCount = 128,
                OpenTime = new TimeOnly(7, 30),
                CloseTime = new TimeOnly(17, 30),
                IsPublished = true,
                IsPublicationRequested = true,
                PublicationRequestedAt = new DateTime(2026, 7, 20, 8, 15, 0, DateTimeKind.Utc)
            };
        }
    }
}
