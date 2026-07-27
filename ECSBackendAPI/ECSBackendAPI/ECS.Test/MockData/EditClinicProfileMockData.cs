using System;
using ECS.Application.Services.ClinicAdminManagementServices.EditClinicProfileServices;
using ECS.Domain.Entities.Clinics;

namespace ECS.Test.MockData
{
    public static class EditClinicProfileMockData
    {
        public static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ValidClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static EditClinicProfileRequest GetValidRequest()
        {
            return new EditClinicProfileRequest
            {
                Name = "Updated ECS Eye Clinic",
                Address = "456 Le Loi, District 1, HCMC",
                Phone = "0987654321",
                Email = "updated@ecs-clinic.vn",
                LogoUrl = "https://cdn.ecs-clinic.vn/updated-logo.png",
                Description = "Updated description for ECS Eye Clinic",
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(18, 0)
            };
        }

        public static StaffClinic GetActiveStaffClinic()
        {
            return new StaffClinic
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                UserId = ValidUserId,
                ClinicId = ValidClinicId,
                IsActive = true
            };
        }

        public static Clinic GetActiveClinic()
        {
            return new Clinic
            {
                Id = ValidClinicId,
                Name = "Original ECS Eye Clinic",
                Address = "123 Nguyen Trai, Ha Noi",
                Phone = "02412345678",
                Email = "contact@ecs-clinic.vn",
                LogoUrl = "https://cdn.ecs-clinic.vn/logo.png",
                Description = "Original description",
                IsActive = true,
                OpenTime = new TimeOnly(7, 30),
                CloseTime = new TimeOnly(17, 30)
            };
        }
    }
}
