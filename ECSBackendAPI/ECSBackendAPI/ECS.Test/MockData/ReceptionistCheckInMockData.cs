using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class ReceptionistCheckInMockData
    {
        public static readonly Guid ValidAppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid NonExistentAppointmentId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        public static readonly Guid RoomId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid ClinicId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid DoctorId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        public static DateTime GetTodayVnDate()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
        }

        public static FacilityRoom GetFacilityRoom()
        {
            return new FacilityRoom
            {
                Id = RoomId,
                ClinicId = ClinicId,
                RoomName = "Room 101",
                IsActive = true
            };
        }

        public static DoctorSchedule GetDoctorSchedule(FacilityRoom? room = null, bool hasRoom = true)
        {
            return new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = DoctorId,
                WorkDate = GetTodayVnDate(),
                Room = hasRoom ? (room ?? GetFacilityRoom()) : null
            };
        }

        public static TimeSlot GetTimeSlot(DoctorSchedule? schedule = null)
        {
            return new TimeSlot
            {
                Id = Guid.NewGuid(),
                ScheduleId = schedule?.Id ?? Guid.NewGuid(),
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                Schedule = schedule
            };
        }

        public static DoctorProfile GetDoctorProfile()
        {
            return new DoctorProfile
            {
                Id = DoctorId,
                ClinicId = ClinicId,
                UserId = Guid.NewGuid()
            };
        }

        public static Appointment GetValidCheckInAppointment(
            AppointmentStatus status = AppointmentStatus.CONFIRMED,
            bool depositPaid = true,
            DateTime? appointmentDate = null,
            bool hasRoom = true,
            bool hasSlot = true,
            bool hasSchedule = true)
        {
            TimeSlot? slot = null;

            if (hasSlot)
            {
                DoctorSchedule? schedule = hasSchedule
                    ? GetDoctorSchedule(hasRoom ? GetFacilityRoom() : null, hasRoom)
                    : null;

                slot = GetTimeSlot(schedule);
            }

            return new Appointment
            {
                Id = ValidAppointmentId,
                DoctorId = DoctorId,
                SlotId = slot?.Id ?? Guid.NewGuid(),
                AppointmentDate = appointmentDate ?? GetTodayVnDate(),
                Status = status,
                DepositPaid = depositPaid,
                Slot = slot!,
                Doctor = GetDoctorProfile()
            };
        }
    }
}