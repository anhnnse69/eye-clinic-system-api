using ECS.Application.Services.DoctorScheduleManagementServices.EditDoctorScheduleServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class EditDoctorScheduleMockData
    {
        public static readonly Guid ReceptionistUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid OtherClinicId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid DoctorProfileId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid DoctorUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid ScheduleId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        public static readonly Guid RoomId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        public static readonly Guid NewRoomId = Guid.Parse("88888888-8888-8888-8888-888888888888");

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

        public static DoctorProfile GetActiveDoctorProfile(
            Guid? id = null,
            Guid? userId = null,
            Guid? clinicId = null) => new()
        {
            Id = id ?? DoctorProfileId,
            UserId = userId ?? DoctorUserId,
            ClinicId = clinicId ?? ClinicId,
            SpecialtyId = null,
            IsActive = true
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
            IsActive = false
        };

        public static FacilityRoom GetActiveRoom(
            Guid? id = null,
            Guid? clinicId = null,
            string roomName = "Room 101") => new()
        {
            Id = id ?? RoomId,
            ClinicId = clinicId ?? ClinicId,
            RoomName = roomName,
            RoomType = "Examination",
            IsActive = true
        };

        public static FacilityRoom GetInactiveRoom(
            Guid? id = null,
            Guid? clinicId = null) => new()
        {
            Id = id ?? RoomId,
            ClinicId = clinicId ?? ClinicId,
            RoomName = "Inactive Room",
            RoomType = "Examination",
            IsActive = false
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
            Guid? doctorId = null,
            DateTime? workDate = null,
            ShiftType shiftType = ShiftType.MORNING,
            Guid? roomId = null,
            List<TimeSlot>? timeSlots = null) => new()
        {
            Id = id ?? ScheduleId,
            DoctorId = doctorId ?? DoctorProfileId,
            WorkDate = workDate ?? DateTime.UtcNow.Date.AddDays(1),
            ShiftType = shiftType,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TimeSlots = timeSlots ?? new List<TimeSlot> { GetAvailableTimeSlot(), GetBlockedTimeSlot() }
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

        public static DoctorSchedule GetScheduleOwnedByDifferentDoctor(
            Guid? id = null,
            Guid? doctorId = null) => new()
        {
            Id = id ?? ScheduleId,
            DoctorId = doctorId ?? Guid.Parse("99999999-9999-9999-9999-999999999999"),
            WorkDate = DateTime.UtcNow.Date.AddDays(1),
            ShiftType = ShiftType.MORNING,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TimeSlots = new List<TimeSlot> { GetAvailableTimeSlot() }
        };

        public static EditDoctorScheduleRequest GetValidRequest_ChangeRoomOnly(
            Guid? roomId = null) => new()
        {
            WorkDate = null,
            RoomId = roomId ?? NewRoomId
        };

        public static EditDoctorScheduleRequest GetValidRequest_ChangeDateOnly(
            DateOnly? newDate = null) => new()
        {
            WorkDate = newDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            RoomId = null
        };

        public static EditDoctorScheduleRequest GetValidRequest_ChangeBoth(
            DateOnly? newDate = null,
            Guid? newRoomId = null) => new()
        {
            WorkDate = newDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            RoomId = newRoomId ?? NewRoomId
        };
    }
}
