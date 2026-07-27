using ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Test.MockData
{
    public static class CreateAppointmentMockData
    {
        public static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ValidDoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid ValidPatientId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid ValidSlotId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid ValidServiceId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid ValidClinicId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        public static CreateAppointmentRequest GetValidRequest() => new()
        {
            PatientId = ValidPatientId,
            DoctorId = ValidDoctorId,
            SlotId = ValidSlotId,
            ServiceId = ValidServiceId,
            Symptoms = "Đau mắt nhẹ"
        };

        public static DoctorProfile GetDoctorProfile(bool isDoctorActive = true, bool isClinicActive = true)
        {
            return new DoctorProfile
            {
                Id = ValidDoctorId,
                UserId = ValidUserId,
                ClinicId = ValidClinicId,
                IsActive = isDoctorActive,
                User = new() { FullName = "Bác sĩ Nguyễn Văn A" },
                Clinic = new Clinic
                {
                    Id = ValidClinicId,
                    Name = "Phòng khám Đa khoa A",
                    IsActive = isClinicActive
                }
            };
        }

        public static Service GetService(bool isActive = true) => new()
        {
            Id = ValidServiceId,
            ClinicId = ValidClinicId,
            ServiceName = "Khám mắt tổng quát",
            IsActive = isActive
        };

        public static TimeSlot GetTimeSlot(
            Guid? doctorId = null,
            DateTime? startTime = null,
            SlotStatus status = SlotStatus.AVAILABLE,
            int currentPatients = 0,
            int maxPatients = 5)
        {
            var targetDoctorId = doctorId ?? ValidDoctorId;
            var start = startTime ?? DateTime.Now.AddDays(1);

            return new TimeSlot
            {
                Id = ValidSlotId,
                StartTime = start,
                EndTime = start.AddMinutes(30),
                Status = status,
                CurrentPatients = currentPatients,
                MaxPatients = maxPatients,
                Schedule = new DoctorSchedule
                {
                    Id = Guid.NewGuid(),
                    DoctorId = targetDoctorId
                }
            };
        }
    }
}