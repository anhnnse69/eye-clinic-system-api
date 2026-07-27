using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class GetMyQueueListMockData
    {
        public static readonly Guid DefaultDoctorUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid DefaultDoctorProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid DefaultPatientId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid DefaultRoomId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        public static DoctorProfile GetActiveDoctorProfile() => new()
        {
            Id = DefaultDoctorProfileId,
            UserId = DefaultDoctorUserId,
            IsActive = true
        };

        public static PatientProfile GetPatientProfile() => new()
        {
            Id = DefaultPatientId,
            FullName = "Tran Van C",
            PhoneNumber = "0912345678",
            Dob = new DateTime(1985, 10, 15),
            Gender = Gender.FEMALE
        };

        public static FacilityRoom GetRoom() => new()
        {
            Id = DefaultRoomId,
            RoomName = "Phong Kham 101"
        };

        public static Service GetService() => new()
        {
            Id = Guid.NewGuid(),
            ServiceName = "Kham Mat Tong Quat"
        };

        public static TimeSlot GetTimeSlot() => new()
        {
            Id = Guid.NewGuid(),
            StartTime = new DateTime(2025, 1, 1, 8, 30, 0)
        };

        public static Appointment GetAppointment(
            Guid doctorId,
            DateTime appointmentDate,
            PatientProfile? patient = null,
            Service? service = null,
            TimeSlot? slot = null,
            bool includeMedicalRecord = true,
            bool includePreliminaryDiagnosis = true)
        {
            var appointmentId = Guid.NewGuid();
            return new Appointment
            {
                Id = appointmentId,
                DoctorId = doctorId,
                PatientId = patient?.Id ?? DefaultPatientId,
                Patient = patient,
                Service = service,
                Slot = slot,
                AppointmentDate = appointmentDate,
                Symptoms = "Dau mat, do mat",
                BookingSource = "ONLINE",
                MedicalRecord = includeMedicalRecord ? new MedicalRecord { Id = Guid.NewGuid(), AppointmentId = appointmentId } : null,
                PreliminaryDiagnosis = includePreliminaryDiagnosis ? new PreliminaryDiagnosis { Id = Guid.NewGuid(), AppointmentId = appointmentId } : null
            };
        }

        public static Queue GetQueueItem(
            Appointment appointment,
            FacilityRoom? room,
            int queueNumber,
            QueueStatus status) => new()
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointment.Id,
            Appointment = appointment,
            RoomId = room?.Id,
            Room = room,
            QueueNumber = queueNumber,
            Status = status,
            CalledAt = status == QueueStatus.CALLING || status == QueueStatus.COMPLETED ? DateTime.UtcNow.AddMinutes(-20) : null,
            CompletedAt = status == QueueStatus.COMPLETED ? DateTime.UtcNow.AddMinutes(-5) : null
        };
    }
}
