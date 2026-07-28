using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;

namespace ECS.Test.MockData
{
    public static class GetActiveRoomsMockData
    {
        public static readonly Guid ReceptionistUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static Clinic GetClinic(Guid? id = null) => new()
        {
            Id = id ?? ClinicId,
            Name = "Test Clinic",
            Address = "123 Test Street",
            Phone = "02812345678",
            Email = "test@clinic.vn",
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(20, 0),
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            UpdatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        public static StaffClinic GetActiveStaffClinic(
            Guid? userId = null,
            Guid? clinicId = null) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? ReceptionistUserId,
            ClinicId = clinicId ?? ClinicId,
            Clinic = GetClinic(clinicId ?? ClinicId),
            Role = Domain.Enums.StaffRole.RECEPTIONIST,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            UpdatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        public static FacilityRoom GetActiveFacilityRoom(
            Guid? id = null,
            Guid? clinicId = null,
            string roomName = "Phong Kham A",
            string? roomType = "Phong Kham",
            bool isActive = true) => new()
        {
            Id = id ?? Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ClinicId = clinicId ?? ClinicId,
            Clinic = GetClinic(clinicId ?? ClinicId),
            RoomName = roomName,
            RoomType = roomType,
            IsActive = isActive
        };

        public static FacilityRoom GetInactiveFacilityRoom(
            Guid? id = null,
            Guid? clinicId = null,
            string roomName = "Phong Khong Hoat Dong") => new()
        {
            Id = id ?? Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ClinicId = clinicId ?? ClinicId,
            Clinic = GetClinic(clinicId ?? ClinicId),
            RoomName = roomName,
            RoomType = null,
            IsActive = false
        };
    }
}
