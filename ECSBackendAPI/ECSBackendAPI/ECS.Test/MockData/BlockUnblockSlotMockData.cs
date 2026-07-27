using ECS.Application.Services.DoctorScheduleManagementServices.BlockUnblockSlotServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class BlockUnblockSlotMockData
    {
        // Receptionist User ID
        public static readonly Guid ReceptionistUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Clinic IDs
        public static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid OtherClinicId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // Doctor IDs
        public static readonly Guid DoctorProfileId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid DoctorUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        // Slot ID
        public static readonly Guid SlotId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        // Schedule ID
        public static readonly Guid ScheduleId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        public static StaffClinic GetActiveStaffClinic(
            Guid? clinicId = null,
            Guid? userId = null,
            bool isActive = true) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? ReceptionistUserId,
            ClinicId = clinicId ?? ClinicId,
            Role = StaffRole.RECEPTIONIST,
            IsActive = isActive
        };

        public static DoctorProfile GetActiveDoctorProfile(
            Guid? clinicId = null,
            Guid? id = null,
            Guid? userId = null,
            bool isActive = true) => new()
        {
            Id = id ?? DoctorProfileId,
            UserId = userId ?? DoctorUserId,
            ClinicId = clinicId ?? ClinicId,
            IsActive = isActive,
            User = new User
            {
                Id = userId ?? DoctorUserId,
                FullName = "Dr. Smith",
                Email = "dr.smith@example.com",
                PasswordHash = "hash",
                Phone = "0901234567",
                Role = UserRole.DOCTOR,
                IsActive = true
            }
        };

        public static TimeSlot GetAvailableSlot(
            Guid? doctorId = null,
            Guid? scheduleId = null,
            Guid? slotId = null) => new()
        {
            Id = slotId ?? SlotId,
            ScheduleId = scheduleId ?? ScheduleId,
            Status = SlotStatus.AVAILABLE,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            MaxPatients = 1,
            CurrentPatients = 0,
            Schedule = new DoctorSchedule
            {
                Id = scheduleId ?? ScheduleId,
                DoctorId = doctorId ?? DoctorProfileId,
                WorkDate = DateTime.UtcNow.Date,
                ShiftType = ShiftType.MORNING,
                IsDeleted = false
            }
        };

        public static TimeSlot GetBlockedSlot(
            Guid? doctorId = null,
            Guid? scheduleId = null,
            Guid? slotId = null) => new()
        {
            Id = slotId ?? SlotId,
            ScheduleId = scheduleId ?? ScheduleId,
            Status = SlotStatus.BLOCKED,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            MaxPatients = 1,
            CurrentPatients = 0,
            Schedule = new DoctorSchedule
            {
                Id = scheduleId ?? ScheduleId,
                DoctorId = doctorId ?? DoctorProfileId,
                WorkDate = DateTime.UtcNow.Date,
                ShiftType = ShiftType.MORNING,
                IsDeleted = false
            }
        };

        public static TimeSlot GetBookedSlot(
            Guid? doctorId = null,
            Guid? scheduleId = null,
            Guid? slotId = null) => new()
        {
            Id = slotId ?? SlotId,
            ScheduleId = scheduleId ?? ScheduleId,
            Status = SlotStatus.BOOKED,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            MaxPatients = 1,
            CurrentPatients = 1,
            Schedule = new DoctorSchedule
            {
                Id = scheduleId ?? ScheduleId,
                DoctorId = doctorId ?? DoctorProfileId,
                WorkDate = DateTime.UtcNow.Date,
                ShiftType = ShiftType.MORNING,
                IsDeleted = false
            }
        };

        public static BlockUnblockSlotRequest GetBlockRequest() =>
            new() { Block = true };

        public static BlockUnblockSlotRequest GetUnblockRequest() =>
            new() { Block = false };
    }
}
