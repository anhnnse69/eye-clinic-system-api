using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class PatientProfileMockData
    {
        public static readonly Guid UserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        public static readonly Guid DirectProfileId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid LinkedProfileId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        public static PatientProfile GetDirectProfile()
        {
            return new PatientProfile
            {
                Id = DirectProfileId,
                UserId = UserId,
                FullName = "Nguyen Van A",
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 5, 20),
                IdentityNumber = "001090012345",
                PhoneNumber = "0912345678",
                CreatedAt = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc)
            };
        }

        public static PatientProfile GetLinkedProfile()
        {
            return new PatientProfile
            {
                Id = LinkedProfileId,
                UserId = null,
                FullName = "Tran Thi B",
                Gender = Gender.FEMALE,
                Dob = new DateTime(2015, 3, 12),
                IdentityNumber = null,
                PhoneNumber = null,
                CreatedAt = new DateTime(2026, 2, 15, 9, 0, 0, DateTimeKind.Utc)
            };
        }

        public static UserPatient GetUserPatientLink()
        {
            return new UserPatient
            {
                UserId = UserId,
                PatientId = LinkedProfileId,
                Relationship = "Con"
            };
        }
    }
}