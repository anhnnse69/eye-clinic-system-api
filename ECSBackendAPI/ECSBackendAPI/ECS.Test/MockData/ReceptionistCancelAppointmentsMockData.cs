using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class ReceptionistCancelAppointmentsMockData
    {
        public static readonly Guid ValidAppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid NonExistentAppointmentId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        public static readonly Guid SlotId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid PatientId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid DoctorId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        public static TimeSlot GetTimeSlot(int currentPatients = 1, SlotStatus status = SlotStatus.BOOKED)
        {
            return new TimeSlot
            {
                Id = SlotId,
                ScheduleId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(3),
                MaxPatients = 5,
                CurrentPatients = currentPatients,
                Status = status
            };
        }

        public static Appointment GetPendingAppointment(TimeSlot? slot = null, DateTime? appointmentDate = null)
        {
            // Mặc định lấy ngày tương lai (ngày mai theo giờ VN) để không vi phạm CANNOT_CANCEL_PAST_APPOINTMENT
            DateTime todayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;

            return new Appointment
            {
                Id = ValidAppointmentId,
                PatientId = PatientId,
                DoctorId = DoctorId,
                SlotId = SlotId,
                AppointmentDate = appointmentDate ?? todayVn.AddDays(1),
                Status = AppointmentStatus.PENDING,
                CreatedAt = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc),
                Slot = slot ?? GetTimeSlot()
            };
        }

        public static Appointment GetPastAppointment()
        {
            DateTime todayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
            return GetPendingAppointment(appointmentDate: todayVn.AddDays(-1));
        }

        public static Appointment GetCancelledAppointment()
        {
            var appointment = GetPendingAppointment();
            appointment.Status = AppointmentStatus.CANCELLED;
            return appointment;
        }

        public static Appointment GetCompletedAppointment()
        {
            var appointment = GetPendingAppointment();
            appointment.Status = AppointmentStatus.COMPLETED;
            return appointment;
        }

        public static Appointment GetInProgressAppointment()
        {
            var appointment = GetPendingAppointment();
            appointment.Status = AppointmentStatus.IN_PROGRESS;
            return appointment;
        }

        public static Appointment GetArrivedAppointment()
        {
            var appointment = GetPendingAppointment();
            appointment.Status = AppointmentStatus.ARRIVED;
            return appointment;
        }
    }
}