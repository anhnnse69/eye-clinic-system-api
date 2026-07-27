using System.Linq.Expressions;
using ECS.Domain.Entities.Auth;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using MockQueryable;
using Moq;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistSearchAccountServices.Tests
{
    public class ReceptionistSearchAccountServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<User, Guid, AppDbContext>> _userQueryRepoMock;
        private readonly ReceptionistSearchAccountService _sut;

        public ReceptionistSearchAccountServiceTests()
        {
            _userQueryRepoMock = new Mock<IRepositoryQueryBase<User, Guid, AppDbContext>>();
            _sut = new ReceptionistSearchAccountService(_userQueryRepoMock.Object);
        }

        private void SetupUserQueryRepo(List<User> databaseData)
        {
            var mockQueryable = databaseData.BuildMock();
            _userQueryRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<User, bool>> predicate, bool trackChanges) =>
                {
                    return mockQueryable.Where(predicate);
                });
        }

        [Fact]
        public async Task Process_NoFilters_ReturnsActivePatientUsersOnly()
        {
            // Arrange
            var users = ReceptionistSearchAccountMockData.GetUsersList();
            SetupUserQueryRepo(users);
            var request = ReceptionistSearchAccountMockData.GetDefaultRequest();

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("APP_MESSAGE_2000");
            response.Data.Should().NotBeNull();
            response.Data.Should().HaveCount(3);
            response.Data!.Select(x => x.Id).Should().BeEquivalentTo(new[]
            {
                ReceptionistSearchAccountMockData.User1Id.ToString(),
                ReceptionistSearchAccountMockData.User2Id.ToString(),
                ReceptionistSearchAccountMockData.User3Id.ToString()
            });
            response.Data.Select(x => x.Id).Should().NotContain(ReceptionistSearchAccountMockData.InactiveUserId.ToString());
            response.Data.Select(x => x.Id).Should().NotContain(ReceptionistSearchAccountMockData.DoctorUserId.ToString());
        }

        [Fact]
        public async Task Process_FilterByNameOnly_ReturnsMatchingUsers()
        {
            // Arrange
            var users = ReceptionistSearchAccountMockData.GetUsersList();
            SetupUserQueryRepo(users);
            var request = ReceptionistSearchAccountMockData.GetDefaultRequest();
            request.FullName = "  VAN  ";

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(2);
            response.Data!.Select(x => x.FullName).Should().BeEquivalentTo(new[] { "Nguyen Van A", "Le Van C" });
        }

        [Fact]
        public async Task Process_FilterByPhoneOnly_ReturnsMatchingUsers()
        {
            // Arrange
            var users = ReceptionistSearchAccountMockData.GetUsersList();
            SetupUserQueryRepo(users);
            var request = ReceptionistSearchAccountMockData.GetDefaultRequest();
            request.Phone = " 0912 "; // Test trim

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(1);
            response.Data![0].FullName.Should().Be("Tran Thi B");
            response.Data[0].Phone.Should().Be("0912345678");
        }

        [Fact]
        public async Task Process_FilterByEmailOnly_ReturnsMatchingUsersIgnoringNullEmail()
        {
            // Arrange
            var users = ReceptionistSearchAccountMockData.GetUsersList();
            SetupUserQueryRepo(users);
            var request = ReceptionistSearchAccountMockData.GetDefaultRequest();
            request.Email = "  GMAIL.COM  ";

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(1);
            response.Data![0].FullName.Should().Be("Nguyen Van A");
            response.Data[0].Email.Should().Be("nguyenvana@gmail.com");
        }

        [Fact]
        public async Task Process_AllFiltersCombined_ReturnsMatchingUsers()
        {
            // Arrange
            var users = ReceptionistSearchAccountMockData.GetUsersList();
            SetupUserQueryRepo(users);
            var request = new ReceptionistSearchAccountRequest
            {
                FullName = "Nguyen",
                Phone = "0901",
                Email = "nguyen"
            };

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(1);
            response.Data![0].FullName.Should().Be("Nguyen Van A");
            response.Data[0].Phone.Should().Be("0901234567");
            response.Data[0].Email.Should().Be("nguyenvana@gmail.com");
        }

        [Fact]
        public async Task Process_EmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            SetupUserQueryRepo(new List<User>());
            var request = ReceptionistSearchAccountMockData.GetDefaultRequest();

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("APP_MESSAGE_2000");
            response.Data.Should().NotBeNull();
            response.Data.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("", "   ", "")]
        public async Task Process_NullOrWhitespaceFilters_IgnoredAndReturnsAllActivePatients(string? name, string? phone, string? email)
        {
            // Arrange
            var users = ReceptionistSearchAccountMockData.GetUsersList();
            SetupUserQueryRepo(users);
            var request = new ReceptionistSearchAccountRequest
            {
                FullName = name,
                Phone = phone,
                Email = email
            };

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(3);
        }
    }
}