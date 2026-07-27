using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistUpdatePatientProfileServices.Tests
{
    public static class ReceptionistUpdatePatientProfileMockData
    {
        public static readonly Guid PatientId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
        public static readonly Guid OtherPatientId = Guid.Parse("a9999999-9999-9999-9999-999999999999");

        public static PatientProfile GetExistingPatient()
        {
            return new PatientProfile
            {
                Id = PatientId,
                FullName = "Old Name",
                Gender = Gender.OTHER,
                Dob = new DateTime(1980, 1, 1),
                IdentityNumber = "000000000000",
                Address = "Old Address",
                PhoneNumber = "0900000000",
                BhytNumber = "OLDBHYT",
                BloodType = "A+",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
        }

        public static List<PatientProfile> GetPatientsList()
        {
            return new List<PatientProfile>
            {
                GetExistingPatient(),
                new PatientProfile
                {
                    Id = OtherPatientId,
                    FullName = "Other Patient",
                    Gender = Gender.MALE,
                    Dob = new DateTime(1990, 5, 5),
                    PhoneNumber = "0911111111",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
        }

        public static ReceptionistUpdatePatientProfileRequest GetValidRequest()
        {
            return new ReceptionistUpdatePatientProfileRequest
            {
                FullName = "  Nguyen Van A  ",
                Gender = "male",
                Dob = "1990-01-01",
                PhoneNumber = " 0901234567 ",
                Address = " 123 Main St ",
                IdentityNumber = " 123456789012 ",
                BhytNumber = " bhyt001 ",
                Email = "nguyenvana@gmail.com"
            };
        }
    }
}