using ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentByClinicServices;
using ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Test.MockData
{
    public static class CreateAppointmentByClinicMockData
    {
        public static readonly Guid ValidClinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ValidDoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid ValidPatientId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid ValidSlotId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid ValidScheduleId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid ValidAppointmentId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        public static CreateAppointmentByClinicRequest GetValidRequest(
            Guid? clinicId = null,
            Guid? patientId = null,
            string? slotId = null)
        {
            var cid = clinicId ?? ValidClinicId;
            return new CreateAppointmentByClinicRequest
            {
                ClinicId = cid,
                PatientId = patientId ?? ValidPatientId,
                SlotId = slotId ?? $"clinic_{cid}_2026-08-10_08:00",
                ServiceId = Guid.NewGuid(),
                Symptoms = "Đau mắt đỏ"
            };
        }

        public static Clinic GetClinic(
            Guid? id = null,
            bool isActive = true,
            TimeOnly? openTime = null,
            TimeOnly? closeTime = null)
        {
            return new Clinic
            {
                Id = id ?? ValidClinicId,
                Name = "Phòng Khám Mắt EyeCare",
                Address = "123 Cần Thơ",
                Phone = "0901234567",
                IsActive = isActive,
                OpenTime = openTime ?? new TimeOnly(8, 0),  // 08:00
                CloseTime = closeTime ?? new TimeOnly(17, 0), // 17:00
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
            SlotStatus status = SlotStatus.AVAILABLE)
        {
            var docId = doctorId ?? ValidDoctorId;
            return new TimeSlot
            {
                Id = id ?? ValidSlotId,
                ScheduleId = ValidScheduleId,
                StartTime = startTime ?? new DateTime(2026, 8, 10, 8, 0, 0),
                EndTime = (startTime ?? new DateTime(2026, 8, 10, 8, 0, 0)).AddMinutes(30),
                MaxPatients = 2,
                CurrentPatients = 0,
                Status = status,
                Schedule = new DoctorSchedule
                {
                    Id = ValidScheduleId,
                    DoctorId = docId
                }
            };
        }

        public static CreateAppointmentResponse GetCreateAppointmentResponse()
        {
            return new CreateAppointmentResponse
            {
                Id_appointment = ValidAppointmentId,
                DoctorName = "BS. Nguyễn Văn A",
                ClinicName = "Phòng Khám Mắt EyeCare",
                ServiceName = "Khám Tổng Quát",
                AppointmentDate = "2026-08-10",
                TimeSlot = "08:00 - 08:30",
                Status = "PENDING",
                DepositAmount = 100000,
                DepositPaid = false,
                BookingSource = "MOBILE"
            };
        }
    }
}