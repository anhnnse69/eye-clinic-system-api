using System;
using ECS.Domain.Entities.Clinics;

namespace ECS.Test.MockData
{
    public static class GetClinicApplicationDetailMockData
    {
        public static readonly Guid ValidApplicationId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        public static readonly Guid NotFoundApplicationId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        public static ClinicRegistrationRequest GetValidApplication(Guid? id = null) => new()
        {
            Id = id ?? ValidApplicationId,
            ClinicName = "Phong Kham Da Khoa HealthCare",
            ClinicAddress = "123 Duong Le Loi, Quan 1, TP.HCM",
            ContactName = "Nguyen Van A",
            ContactPhone = "0987654321",
            ContactEmail = "nguyenvana@example.com",
            BusinessLicenseUrl = "https://example.com/license.pdf",
            Status = "PENDING", // Thay Enum bằng chuỗi string
            ReviewNote = "Pending verification",
            RequestedAt = new DateTime(2025, 5, 20, 14, 30, 0, DateTimeKind.Utc)
        };
    }
}