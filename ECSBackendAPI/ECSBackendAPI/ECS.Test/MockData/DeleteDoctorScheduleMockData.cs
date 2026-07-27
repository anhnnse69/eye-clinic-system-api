using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class DeleteDoctorScheduleMockData
    {
        public static readonly Guid ReceptionistUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid OtherClinicId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid DoctorProfileId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid DoctorUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid ScheduleId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        public static readonly Guid OtherDoctorProfileId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        public static readonly Guid OtherScheduleId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        public static StaffClinic GetActiveStaffClinic(
            Guid? userId = null,
            Guid? clinicId = null) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? ReceptionistUserId,
            ClinicId = clinicId ?? ClinicId,
            Role = StaffRole.RECEPTIONIST,
            IsActive = true
        };

        public static StaffClinic GetInactiveStaffClinic(
            Guid? userId = null,
            Guid? clinicId = null) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? ReceptionistUserId,
            ClinicId = clinicId ?? ClinicId,
            Role = StaffRole.RECEPTIONIST,
            IsActive = false
        };

        public static User GetDoctorUser(
            Guid? id = null,
            string fullName = "BS. Le Van C") => new()
        {
            Id = id ?? DoctorUserId,
            Phone = "0909123456",
            Email = "levanc@clinic.vn",
            PasswordHash = "hash",
            FullName = fullName,
            Role = UserRole.DOCTOR,
            IsActive = true
        };

        public static DoctorProfile GetActiveDoctorProfile(
            Guid? id = null,
            Guid? userId = null,
            Guid? clinicId = null) => new()
        {
            Id = id ?? DoctorProfileId,
            UserId = userId ?? DoctorUserId,
            ClinicId = clinicId ?? ClinicId,
            SpecialtyId = null,
            IsActive = true,
            User = GetDoctorUser()
        };

        public static DoctorProfile GetInactiveDoctorProfile(
            Guid? id = null,
            Guid? userId = null,
            Guid? clinicId = null) => new()
        {
            Id = id ?? DoctorProfileId,
            UserId = userId ?? DoctorUserId,
            ClinicId = clinicId ?? ClinicId,
            SpecialtyId = null,
            IsActive = false,
            User = GetDoctorUser()
        };

        public static TimeSlot GetAvailableTimeSlot(
            Guid? scheduleId = null) => new()
        {
            Id = Guid.NewGuid(),
            ScheduleId = scheduleId ?? ScheduleId,
            StartTime = DateTime.UtcNow.Date.AddHours(8),
            EndTime = DateTime.UtcNow.Date.AddHours(9),
            Status = SlotStatus.AVAILABLE,
            MaxPatients = 1,
            CurrentPatients = 0
        };

        public static TimeSlot GetBookedTimeSlot(
            Guid? scheduleId = null) => new()
        {
            Id = Guid.NewGuid(),
            ScheduleId = scheduleId ?? ScheduleId,
            StartTime = DateTime.UtcNow.Date.AddHours(9),
            EndTime = DateTime.UtcNow.Date.AddHours(10),
            Status = SlotStatus.BOOKED,
            MaxPatients = 1,
            CurrentPatients = 1
        };

        public static TimeSlot GetBlockedTimeSlot(
            Guid? scheduleId = null) => new()
        {
            Id = Guid.NewGuid(),
            ScheduleId = scheduleId ?? ScheduleId,
            StartTime = DateTime.UtcNow.Date.AddHours(10),
            EndTime = DateTime.UtcNow.Date.AddHours(11),
            Status = SlotStatus.BLOCKED,
            MaxPatients = 1,
            CurrentPatients = 0
        };

        public static DoctorSchedule GetScheduleWithNoBookedSlots(
            Guid? id = null,
            Guid? doctorId = null) => new()
        {
            Id = id ?? ScheduleId,
            DoctorId = doctorId ?? DoctorProfileId,
            WorkDate = DateTime.UtcNow.Date.AddDays(1),
            ShiftType = ShiftType.MORNING,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TimeSlots = new List<TimeSlot>
            {
                GetAvailableTimeSlot(),
                GetBlockedTimeSlot()
            }
        };

        public static DoctorSchedule GetScheduleWithBookedSlots(
            Guid? id = null,
            Guid? doctorId = null) => new()
        {
            Id = id ?? ScheduleId,
            DoctorId = doctorId ?? DoctorProfileId,
            WorkDate = DateTime.UtcNow.Date.AddDays(1),
            ShiftType = ShiftType.MORNING,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TimeSlots = new List<TimeSlot>
            {
                GetAvailableTimeSlot(),
                GetBookedTimeSlot(),
                GetBlockedTimeSlot()
            }
        };

        public static DoctorSchedule GetScheduleWithNullTimeSlots(
            Guid? id = null,
            Guid? doctorId = null) => new()
        {
            Id = id ?? ScheduleId,
            DoctorId = doctorId ?? DoctorProfileId,
            WorkDate = DateTime.UtcNow.Date.AddDays(1),
            ShiftType = ShiftType.MORNING,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TimeSlots = null!
        };

        public static DoctorSchedule GetDeletedSchedule(
            Guid? id = null,
            Guid? doctorId = null) => new()
        {
            Id = id ?? ScheduleId,
            DoctorId = doctorId ?? DoctorProfileId,
            WorkDate = DateTime.UtcNow.Date.AddDays(1),
            ShiftType = ShiftType.MORNING,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            TimeSlots = null
        };
    }
}
