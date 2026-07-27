using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Test.MockData
{
    public static class ClinicSlotMockData
    {
        public static readonly Guid ValidClinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ValidDoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid ValidScheduleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid ValidSlotId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        public static Clinic GetClinic(
            Guid? id = null,
            bool isActive = true,
            TimeOnly? openTime = null,
            TimeOnly? closeTime = null)
        {
            return new Clinic
            {
                Id = id ?? ValidClinicId,
                Name = "Phòng khám Mắt EyeCare",
                Address = "123 Nguyễn Văn Cừ",
                Phone = "0901234567",
                IsActive = isActive,
                OpenTime = openTime ?? new TimeOnly(8, 0), // 08:00
                CloseTime = closeTime ?? new TimeOnly(9, 0), // 09:00
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static DoctorProfile GetDoctor(
            Guid? id = null,
            Guid? clinicId = null,
            bool isActive = true)
        {
            return new DoctorProfile
            {
                Id = id ?? ValidDoctorId,
                ClinicId = clinicId ?? ValidClinicId,
                UserId = Guid.NewGuid(),
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static TimeSlot GetTimeSlot(
            Guid? id = null,
            Guid? doctorId = null,
            DateTime? startTime = null,
            SlotStatus status = SlotStatus.AVAILABLE,
            int maxPatients = 2,
            int currentPatients = 0)
        {
            var docId = doctorId ?? ValidDoctorId;
            return new TimeSlot
            {
                Id = id ?? ValidSlotId,
                ScheduleId = ValidScheduleId,
                StartTime = startTime ?? new DateTime(2026, 8, 10, 8, 0, 0),
                EndTime = (startTime ?? new DateTime(2026, 8, 10, 8, 0, 0)).AddMinutes(30),
                MaxPatients = maxPatients,
                CurrentPatients = currentPatients,
                Status = status,
                Schedule = new DoctorSchedule
                {
                    Id = ValidScheduleId,
                    DoctorId = docId
                }
            };
        }
    }
}