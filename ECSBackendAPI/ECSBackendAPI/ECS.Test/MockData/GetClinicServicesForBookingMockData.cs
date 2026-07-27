using ECS.Domain.Entities.Clinics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Test.MockData
{
    public static class GetClinicServicesForBookingMockData
    {
        public static readonly Guid ValidClinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ValidServiceId1 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid ValidServiceId2 = Guid.Parse("33333333-3333-3333-3333-333333333333");

        public static Clinic GetClinic(Guid? id = null, bool isActive = true)
        {
            return new Clinic
            {
                Id = id ?? ValidClinicId,
                Name = "Phòng khám Mắt Sài Gòn",
                Address = "123 Nguyễn Thị Minh Khai, Q.3",
                Phone = "0909123456",
                IsActive = isActive,
                IsPublished = true
            };
        }

        public static Service GetService(
            Guid? id = null,
            Guid? clinicId = null,
            string? serviceName = null,
            decimal? price = 200000m,
            int durationMinutes = 30,
            bool isActive = true)
        {
            return new Service
            {
                Id = id ?? ValidServiceId1,
                ClinicId = clinicId ?? ValidClinicId,
                ServiceName = serviceName ?? "Khám mắt tổng quát",
                Price = price,
                DurationMinutes = durationMinutes,
                IsActive = isActive
            };
        }
    }
}
