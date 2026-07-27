using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class ViewPatientDemographicsMockData
    {
        public static readonly Guid DefaultDoctorUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid DefaultDoctorProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid DefaultPatientUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid DefaultPatientProfileId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        public static DoctorProfile GetDoctorProfile(Guid doctorUserId, Guid doctorProfileId)
        {
            return new DoctorProfile
            {
                Id = doctorProfileId,
                UserId = doctorUserId,
                IsActive = true,
                User = new User
                {
                    Id = doctorUserId,
                    FullName = "Bac Si A",
                    PasswordHash = "hash123",
                    Phone = "0987654321"
                }
            };
        }

        public static PatientProfile GetPatientProfile(Guid patientId, Guid? userId = null)
        {
            return new PatientProfile
            {
                Id = patientId,
                UserId = userId ?? DefaultPatientUserId,
                FullName = "Nguyen Van A",
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 1, 1),
                IdentityNumber = "0123456789",
                PhoneNumber = "0987654321",
                Address = "123 Le Loi",
                BhytNumber = "BHYT123456",
                BloodType = "O+",
                Allergies = "Khong",
                MedicalHistory = "Khong",
                HasMedicalDemographics = true
            };
        }

        public static MedicalRecord GetMedicalRecord(
            Guid id,
            PatientProfile patient,
            DoctorProfile doctor,
            RecordType recordType = RecordType.MS21_TRAUMA,
            string summary = "Chan thuong mat",
            string complaint = "Dau mat")
        {
            var apptId = Guid.NewGuid();
            var appt = new Appointment
            {
                Id = apptId,
                DoctorId = doctor.Id,
                PatientId = patient.Id,
                AppointmentDate = new DateTime(2025, 5, 10),
                BookingSource = "ONLINE"
            };

            return new MedicalRecord
            {
                Id = id,
                PatientId = patient.Id,
                Patient = patient,
                DoctorId = doctor.Id,
                Doctor = doctor,
                AppointmentId = apptId,
                Appointment = appt,
                RecordType = recordType,
                Summary = summary,
                ChiefComplaint = complaint,
                IsLocked = false,
                CreatedAt = new DateTime(2025, 5, 10, 9, 0, 0)
            };
        }
    }
}
