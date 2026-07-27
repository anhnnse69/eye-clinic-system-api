using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientDetailsServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.Services.ReceptionistManagementServices.ReceptionistGetPatientDetailsServices
{
    public static class ReceptionistGetPatientDetailsMockData
    {
        public static readonly Guid ReceptionistUserId = Guid.NewGuid();
        public static readonly Guid TargetClinicId = Guid.NewGuid();
        public static readonly Guid OtherClinicId = Guid.NewGuid();
        public static readonly Guid PatientId = Guid.NewGuid();

        public static List<StaffClinic> GetStaffClinics(Guid staffUserId)
        {
            return new List<StaffClinic>
            {
                new StaffClinic
                {
                    Id = Guid.NewGuid(),
                    UserId = staffUserId,
                    ClinicId = TargetClinicId,
                    Role = (StaffRole)3,
                    IsActive = true
                }
            };
        }

        public static PatientProfile GetPatientProfile(Guid patientId)
        {
            return new PatientProfile
            {
                Id = patientId,
                FullName = "Alice Smith",
                Gender = Gender.FEMALE,
                Dob = new DateTime(1995, 5, 5),
                IdentityNumber = "123456789012",
                Address = "123 Main Street",
                PhoneNumber = "0988123456",
                BhytNumber = "BHYT12345",
                BloodType = "A+",
                Allergies = "Peanuts",
                MedicalHistory = "Asthma",
                CreatedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                User = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "alice@example.com",
                    AvatarUrl = "https://example.com/avatar.jpg"
                }
            };
        }
        public static PatientProfile GetPatientProfileWithNullUser(Guid patientId)
        {
            return new PatientProfile
            {
                Id = patientId,
                FullName = "Bob Marley",
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 1, 1),
                IdentityNumber = "987654321098",
                Address = "456 Side Street",
                PhoneNumber = "0912345678",
                BhytNumber = "BHYT99999",
                BloodType = "O+",
                Allergies = "None",
                MedicalHistory = "None",
                CreatedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                User = null
            };
        }

        public static List<Appointment> GetAppointments(Guid patientId)
        {
            var doctor1 = new DoctorProfile
            {
                Id = Guid.NewGuid(),
                ClinicId = TargetClinicId,
                User = new User { FullName = "Dr. Gregory House" },
                Specialty = new Specialty { Name = "Cardiology" }
            };

            var doctor2 = new DoctorProfile
            {
                Id = Guid.NewGuid(),
                ClinicId = TargetClinicId,
                User = new User { FullName = "Dr. John Watson" },
                Specialty = null
            };

            var doctorOtherClinic = new DoctorProfile
            {
                Id = Guid.NewGuid(),
                ClinicId = OtherClinicId,
                User = new User { FullName = "Dr. Strange" },
                Specialty = new Specialty { Name = "Neurology" }
            };

            return new List<Appointment>
            {
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    DoctorId = doctor1.Id,
                    Doctor = doctor1,
                    AppointmentDate = new DateTime(2026, 7, 27, 8, 0, 0),
                    Status = AppointmentStatus.COMPLETED,
                    BookingSource = "ONLINE",
                    Symptoms = "Chest pain"
                },
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    DoctorId = doctor2.Id,
                    Doctor = doctor2,
                    AppointmentDate = new DateTime(2026, 7, 26, 9, 0, 0),
                    Status = AppointmentStatus.ARRIVED,
                    BookingSource = "WALKIN",
                    Symptoms = "Fever"
                },
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    DoctorId = doctorOtherClinic.Id,
                    Doctor = doctorOtherClinic,
                    AppointmentDate = new DateTime(2026, 7, 25, 10, 0, 0),
                    Status = AppointmentStatus.PENDING,
                    BookingSource = "ONLINE",
                    Symptoms = "Headache"
                }
            };
        }

        public static ReceptionistGetPatientDetailsRequest GetValidRequest()
        {
            return new ReceptionistGetPatientDetailsRequest
            {
                CurrentUserId = ReceptionistUserId,
                PatientId = PatientId
            };
        }
    }
}