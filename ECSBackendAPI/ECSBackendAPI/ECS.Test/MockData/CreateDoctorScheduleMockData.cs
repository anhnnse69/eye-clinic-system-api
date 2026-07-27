using ECS.Application.Services.DoctorScheduleManagementServices.CreateDoctorScheduleService;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class CreateDoctorScheduleMockData
    {
        public static readonly Guid ReceptionistUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid OtherClinicId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid DoctorProfileId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid DoctorUserId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        public static readonly Guid RoomId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        public static StaffClinic GetStaffClinic(
            Guid? id = null,
            Guid? userId = null,
            Guid? clinicId = null,
            StaffRole role = StaffRole.RECEPTIONIST,
            bool isActive = true) => new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? ReceptionistUserId,
            ClinicId = clinicId ?? ClinicId,
            Role = role,
            IsActive = isActive
        };

        public static Clinic GetClinic(
            Guid? id = null,
            string name = "Saigon Eye Clinic",
            TimeOnly? openTime = null,
            TimeOnly? closeTime = null,
            bool isActive = true) => new()
        {
            Id = id ?? ClinicId,
            Name = name,
            Address = "123 Le Loi, Quan 1, TP.HCM",
            Phone = "02812345678",
            Email = "info@saigoneye.vn",
            OpenTime = openTime ?? new TimeOnly(8, 0),
            CloseTime = closeTime ?? new TimeOnly(20, 0),
            IsActive = isActive
        };

        public static User GetDoctorUser(Guid? id = null, string fullName = "Doctor A") => new()
        {
            Id = id ?? DoctorUserId,
            Phone = "0901111111",
            Email = "doctor.a@example.com",
            PasswordHash = "x",
            FullName = fullName,
            Role = UserRole.DOCTOR,
            IsActive = true
        };

        public static DoctorProfile GetDoctorProfile(
            Guid? id = null,
            Guid? userId = null,
            Guid? clinicId = null,
            User? user = null,
            bool isActive = true)
        {
            var doctorUser = user ?? GetDoctorUser();
            return new DoctorProfile
            {
                Id = id ?? DoctorProfileId,
                UserId = userId ?? doctorUser.Id,
                ClinicId = clinicId ?? ClinicId,
                SpecialtyId = null,
                IsActive = isActive,
                User = doctorUser
            };
        }

        public static FacilityRoom GetFacilityRoom(
            Guid? id = null,
            Guid? clinicId = null,
            string roomName = "Room 101",
            bool isActive = true) => new()
        {
            Id = id ?? RoomId,
            ClinicId = clinicId ?? ClinicId,
            RoomName = roomName,
            RoomType = "Examination",
            IsActive = isActive
        };

        public static CreateDoctorScheduleRequest GetValidRequest(
            List<DateOnly>? workDates = null,
            List<string>? shiftTypes = null,
            Guid? roomId = null)
        {
            return new CreateDoctorScheduleRequest
            {
                WorkDates = workDates ?? new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = shiftTypes ?? new List<string> { "MORNING" },
                RoomId = roomId ?? RoomId
            };
        }
    }
}
