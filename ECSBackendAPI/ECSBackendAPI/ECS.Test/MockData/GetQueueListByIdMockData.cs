using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class GetQueueListByIdMockData
    {
        public static readonly Guid DefaultDoctorUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid DefaultDoctorProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static DoctorProfile GetDoctorProfile(Guid? id = null, bool isActive = true)
        {
            return new DoctorProfile
            {
                Id = id ?? DefaultDoctorProfileId,
                UserId = DefaultDoctorUserId,
                IsActive = isActive
            };
        }

        public static PatientProfile GetPatientProfile(Guid? id = null, string fullName = "Nguyen Van B", string? phone = "0912345678")
        {
            return new PatientProfile
            {
                Id = id ?? Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FullName = fullName,
                PhoneNumber = phone,
                Gender = Gender.FEMALE,
                Dob = new DateTime(1995, 5, 20)
            };
        }

        public static FacilityRoom GetRoom(Guid? id = null, string roomName = "Phong 101")
        {
            return new FacilityRoom
            {
                Id = id ?? Guid.NewGuid(),
                RoomName = roomName
            };
        }

        public static Service GetService(Guid? id = null, string serviceName = "Kham mat tong quat")
        {
            return new Service
            {
                Id = id ?? Guid.NewGuid(),
                ServiceName = serviceName
            };
        }

        public static TimeSlot GetSlot(Guid? id = null)
        {
            return new TimeSlot
            {
                Id = id ?? Guid.NewGuid(),
                StartTime = DateTime.Today.AddHours(8),
                EndTime = DateTime.Today.AddHours(8).AddMinutes(30)
            };
        }

        public static Queue GetQueueItem(
            Guid doctorProfileId,
            DateOnly date,
            int queueNumber,
            QueueStatus status,
            PatientProfile patient,
            FacilityRoom? room,
            Service? service,
            TimeSlot? slot = null,
            bool includeMedicalRecord = false)
        {
            var apptId = Guid.NewGuid();
            var timeSlot = slot ?? GetSlot();
            var appt = new Appointment
            {
                Id = apptId,
                DoctorId = doctorProfileId,
                PatientId = patient.Id,
                Patient = patient,
                ServiceId = service?.Id,
                Service = service,
                SlotId = timeSlot.Id,
                Slot = timeSlot,
                AppointmentDate = date.ToDateTime(TimeOnly.MinValue),
                BookingSource = "ONLINE",
                Symptoms = "Dau mat"
            };

            if (includeMedicalRecord)
            {
                appt.MedicalRecord = new MedicalRecord
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = apptId,
                    PatientId = patient.Id,
                    DoctorId = doctorProfileId
                };
            }

            return new Queue
            {
                Id = Guid.NewGuid(),
                QueueNumber = queueNumber,
                AppointmentId = apptId,
                Appointment = appt,
                ClinicId = Guid.NewGuid(),
                RoomId = room?.Id,
                Room = room,
                Status = status,
                CalledAt = status == QueueStatus.CALLING || status == QueueStatus.IN_PROGRESS
                    ? DateTime.UtcNow
                    : null,
                CompletedAt = status == QueueStatus.COMPLETED ? DateTime.UtcNow : null
            };
        }
    }
}
