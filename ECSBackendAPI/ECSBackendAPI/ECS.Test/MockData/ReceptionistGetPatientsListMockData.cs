using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientsListServices.Tests
{
    public static class ReceptionistGetPatientsListMockData
    {
        public static readonly Guid ReceptionistUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public static ReceptionistGetPatientsListRequest GetDefaultRequest()
        {
            return new ReceptionistGetPatientsListRequest
            {
                CurrentUserId = ReceptionistUserId,
                PageNumber = 1,
                PageSize = 5
            };
        }

        public static List<PatientProfile> GetPatientProfilesList()
        {
            return new List<PatientProfile>
            {
                new PatientProfile
                {
                    Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                    FullName = "Nguyen Van A",
                    Gender = Gender.MALE,
                    Dob = new DateTime(1990, 1, 1),
                    IdentityNumber = "123456789012",
                    Address = "123 Main St",
                    PhoneNumber = "0901234567",
                    BhytNumber = "BHYT001",
                    BloodType = "A+",
                    CreatedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc)
                },
                new PatientProfile
                {
                    Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"),
                    FullName = "Tran Thi B",
                    Gender = Gender.FEMALE,
                    Dob = new DateTime(1995, 5, 5),
                    IdentityNumber = "098765432109",
                    Address = "456 High St",
                    PhoneNumber = "0912345678",
                    BhytNumber = "BHYT002",
                    BloodType = "O+",
                    CreatedAt = new DateTime(2026, 1, 2, 11, 0, 0, DateTimeKind.Utc)
                },
                new PatientProfile
                {
                    Id = Guid.Parse("a3333333-3333-3333-3333-333333333333"),
                    FullName = "Le Van C",
                    Gender = Gender.OTHER,
                    Dob = new DateTime(2000, 10, 10),
                    IdentityNumber = null,
                    Address = null,
                    PhoneNumber = null,
                    BhytNumber = null,
                    BloodType = null,
                    CreatedAt = new DateTime(2026, 1, 3, 12, 0, 0, DateTimeKind.Utc)
                }
            };
        }
    }
}