using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class ViewDoctorAppointmentsMockData
    {
        public static readonly Guid DefaultDoctorUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid DefaultDoctorProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static DoctorProfile GetDoctorProfile(Guid? id = null, Guid? userId = null, bool isActive = true)
        {
            return new DoctorProfile
            {
                Id = id ?? DefaultDoctorProfileId,
                UserId = userId ?? DefaultDoctorUserId,
                IsActive = isActive
            };
        }

        public static User GetUser(Guid? id = null, string? avatarUrl = "https://example.com/avatar.jpg")
        {
            return new User
            {
                Id = id ?? Guid.NewGuid(),
                AvatarUrl = avatarUrl
            };
        }

        public static PatientProfile GetPatientProfile(
            Guid? id = null,
            string fullName = "Nguyen Van A",
            string? phone = "0987654321",
            Gender gender = Gender.MALE,
            DateTime? dob = null,
            User? user = null)
        {
            var userEntity = user ?? GetUser();
            return new PatientProfile
            {
                Id = id ?? Guid.NewGuid(),
                UserId = userEntity.Id,
                User = userEntity,
                FullName = fullName,
                PhoneNumber = phone,
                Gender = gender,
                Dob = dob ?? new DateTime(1990, 1, 1)
            };
        }

        public static Service GetService(Guid? id = null, string serviceName = "Kham Mat Tong Quat", decimal price = 150000m)
        {
            return new Service
            {
                Id = id ?? Guid.NewGuid(),
                ServiceName = serviceName,
                Price = price
            };
        }

        public static TimeSlot GetSlot(Guid? id = null, DateTime? startTime = null, DateTime? endTime = null)
        {
            var start = startTime ?? DateTime.Today.AddHours(9);
            return new TimeSlot
            {
                Id = id ?? Guid.NewGuid(),
                StartTime = start,
                EndTime = endTime ?? start.AddMinutes(30)
            };
        }

        public static MedicalRecord GetMedicalRecord(Guid? id = null)
        {
            return new MedicalRecord
            {
                Id = id ?? Guid.NewGuid()
            };
        }

        public static Appointment GetAppointment(
            Guid? id = null,
            Guid? doctorId = null,
            PatientProfile? patient = null,
            Service? service = null,
            TimeSlot? slot = null,
            MedicalRecord? medicalRecord = null,
            DateTime? appointmentDate = null,
            AppointmentStatus status = AppointmentStatus.PENDING,
            string? symptoms = "Dau mat",
            string bookingSource = "ONLINE",
            decimal depositAmount = 50000m,
            bool depositPaid = true,
            DateTime? createdAt = null)
        {
            var patientProfile = patient ?? GetPatientProfile();
            var timeSlot = slot ?? GetSlot();
            return new Appointment
            {
                Id = id ?? Guid.NewGuid(),
                DoctorId = doctorId ?? DefaultDoctorProfileId,
                PatientId = patientProfile.Id,
                Patient = patientProfile,
                ServiceId = service?.Id,
                Service = service,
                SlotId = timeSlot.Id,
                Slot = timeSlot,
                MedicalRecord = medicalRecord,
                AppointmentDate = appointmentDate ?? DateTime.Today,
                Status = status,
                Symptoms = symptoms,
                BookingSource = bookingSource,
                DepositAmount = depositAmount,
                DepositPaid = depositPaid,
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
        }
    }
}
