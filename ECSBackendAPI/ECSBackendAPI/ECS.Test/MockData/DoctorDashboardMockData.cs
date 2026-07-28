using ECS.Application.Services.DoctorScheduleManagementServices.DoctorDashboardServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class DoctorDashboardMockData
    {
        public static readonly Guid DoctorUserId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        public static readonly Guid DoctorProfileId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        public static readonly Guid ClinicId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        public static DoctorProfile GetActiveDoctorProfile(
            Guid? id = null,
            Guid? userId = null,
            bool isActive = true)
        {
            return new DoctorProfile
            {
                Id = id ?? DoctorProfileId,
                UserId = userId ?? DoctorUserId,
                ClinicId = ClinicId,
                IsActive = isActive,
                Title = "Dr.",
                ExperienceYears = 5
            };
        }

        public static DoctorSchedule GetDoctorSchedule(
            Guid? doctorId = null,
            DateTime? workDate = null,
            ShiftType shiftType = ShiftType.MORNING)
        {
            return new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId ?? DoctorProfileId,
                WorkDate = workDate ?? DateTime.UtcNow.Date,
                ShiftType = shiftType,
                IsDeleted = false,
                TimeSlots = new List<TimeSlot>()
            };
        }

        public static TimeSlot GetTimeSlot(
            Guid? scheduleId = null,
            DateTime? startTime = null,
            SlotStatus status = SlotStatus.AVAILABLE)
        {
            return new TimeSlot
            {
                Id = Guid.NewGuid(),
                ScheduleId = scheduleId ?? Guid.NewGuid(),
                StartTime = startTime ?? DateTime.UtcNow.AddHours(1),
                EndTime = startTime?.AddMinutes(30) ?? DateTime.UtcNow.AddHours(1).AddMinutes(30),
                Status = status,
                MaxPatients = 1,
                CurrentPatients = 0
            };
        }

        public static Appointment GetAppointment(
            Guid? patientId = null,
            Guid? doctorId = null,
            DateTime? appointmentDate = null,
            AppointmentStatus status = AppointmentStatus.PENDING)
        {
            return new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patientId ?? Guid.NewGuid(),
                DoctorId = doctorId ?? DoctorProfileId,
                SlotId = Guid.NewGuid(),
                AppointmentDate = appointmentDate ?? DateTime.UtcNow,
                Status = status
            };
        }

        public static DoctorDashboardRequest GetValidRequest(
            DateOnly? startDate = null,
            DateOnly? endDate = null)
        {
            var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var start = startDate ?? end.AddDays(-6);
            return new DoctorDashboardRequest
            {
                StartDate = startDate,
                EndDate = endDate
            };
        }

        public static DoctorDashboardRequest GetRequestWithBothDates(DateOnly start, DateOnly end)
        {
            return new DoctorDashboardRequest
            {
                StartDate = start,
                EndDate = end
            };
        }

        public static DoctorDashboardRequest GetRequestWithOnlyEndDate(DateOnly endDate)
        {
            return new DoctorDashboardRequest
            {
                StartDate = null,
                EndDate = endDate
            };
        }

        public static DoctorDashboardRequest GetRequestWithBothNull()
        {
            return new DoctorDashboardRequest
            {
                StartDate = null,
                EndDate = null
            };
        }
    }
}
