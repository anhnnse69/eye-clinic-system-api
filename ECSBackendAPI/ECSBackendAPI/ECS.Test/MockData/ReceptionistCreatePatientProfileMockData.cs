using ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreatePatientProfileServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class ReceptionistCreatePatientProfileMockData
    {
        public static readonly Guid ExistingUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid NonExistentUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        public static User GetExistingUser()
        {
            return new User
            {
                Id = ExistingUserId,
                FullName = "Nguyen Van A",
                Phone = "0901234567",
                Email = "nguyenvana@example.com",
                Role = UserRole.PATIENT,
                IsActive = true
            };
        }

        public static ReceptionistCreatePatientProfileRequest GetRequestWithExistingUser()
        {
            return new ReceptionistCreatePatientProfileRequest
            {
                IsHasAccount = true,
                SelectedUserId = ExistingUserId,
                FullName = "Nguyen Van A",
                Gender = "MALE",
                Dob = "1990-01-01",
                PhoneNumber = "0901234567",
                Email = "nguyenvana@example.com",
                Address = "123 Street",
                IdentityNumber = "123456789",
                BhytNumber = "HS123456789"
            };
        }

        public static ReceptionistCreatePatientProfileRequest GetRequestWithAutoAccountCreation(string? email = "newpatient@example.com")
        {
            return new ReceptionistCreatePatientProfileRequest
            {
                IsHasAccount = false,
                SelectedUserId = null,
                FullName = "Tran Thi B",
                Gender = "FEMALE",
                Dob = "1995-05-15",
                PhoneNumber = "0987654321",
                Email = email,
                Address = "456 Avenue",
                IdentityNumber = "987654321",
                BhytNumber = "DN987654321"
            };
        }
    }
}