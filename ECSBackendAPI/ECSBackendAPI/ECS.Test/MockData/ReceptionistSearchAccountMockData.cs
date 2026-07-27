using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistSearchAccountServices.Tests
{
    public static class ReceptionistSearchAccountMockData
    {
        public static readonly Guid User1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid User2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid User3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid InactiveUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid DoctorUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        public static ReceptionistSearchAccountRequest GetDefaultRequest()
        {
            return new ReceptionistSearchAccountRequest
            {
                FullName = null,
                Phone = null,
                Email = null
            };
        }

        public static List<User> GetUsersList()
        {
            return new List<User>
            {
                new User
                {
                    Id = User1Id,
                    FullName = "Nguyen Van A",
                    Phone = "0901234567",
                    Email = "nguyenvana@gmail.com",
                    Role = UserRole.PATIENT,
                    IsActive = true,
                    PasswordHash = "hash1"
                },
                new User
                {
                    Id = User2Id,
                    FullName = "Tran Thi B",
                    Phone = "0912345678",
                    Email = "tranthib@yahoo.com",
                    Role = UserRole.PATIENT,
                    IsActive = true,
                    PasswordHash = "hash2"
                },
                new User
                {
                    Id = User3Id,
                    FullName = "Le Van C",
                    Phone = "0987654321",
                    Email = null,
                    Role = UserRole.PATIENT,
                    IsActive = true,
                    PasswordHash = "hash3"
                },
                new User
                {
                    Id = InactiveUserId,
                    FullName = "Pham Inactive",
                    Phone = "0900000000",
                    Email = "inactive@gmail.com",
                    Role = UserRole.PATIENT,
                    IsActive = false,
                    PasswordHash = "hash4"
                },
                new User
                {
                    Id = DoctorUserId,
                    FullName = "Doctor Strange",
                    Phone = "0999999999",
                    Email = "doctor@hospital.com",
                    Role = UserRole.DOCTOR,
                    IsActive = true,
                    PasswordHash = "hash5"
                }
            };
        }
    }
}