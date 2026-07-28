using System.Linq.Expressions;
using ECS.Application.Services.SystemAdminServices.AdminSystemCreateAccountServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.SystemAdminServices.AdminSystemCreateAccountServices
{
    /// <summary>
    /// Unit tests for <see cref="CreateAccountService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class CreateAccountServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>> _userRepoMock = new();
        private readonly CreateAccountService _sut;

        public CreateAccountServiceTests()
        {
            _sut = new CreateAccountService(_userRepoMock.Object);
        }

        private void SetupUniquenessRepo(User? phoneConflict, User? emailConflict)
        {
            var phoneRows = phoneConflict != null ? new List<User> { phoneConflict } : new List<User>();
            var phoneDbSet = phoneRows.BuildMockDbSet<User>();
            var emailRows = emailConflict != null ? new List<User> { emailConflict } : new List<User>();
            var emailDbSet = emailRows.BuildMockDbSet<User>();

            _userRepoMock
                .Setup(r => r.FindByCondition(
                    It.Is<Expression<Func<User, bool>>>(e => e.Body.ToString().Contains("Phone")),
                    It.IsAny<bool>()))
                .Returns(phoneDbSet.Object);

            _userRepoMock
                .Setup(r => r.FindByCondition(
                    It.Is<Expression<Func<User, bool>>>(e => e.Body.ToString().Contains("Email")),
                    It.IsAny<bool>()))
                .Returns(emailDbSet.Object);
        }

        private void SetupPersistenceSuccess()
        {
            _userRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(Guid.NewGuid());
            _userRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);
        }

        [Fact]
        public async Task Process_DuplicatePhone_Returns4018AndSkipsPersistence()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidCreateAccountRequest();
            var existing = SystemAdminAccountMockData.GetUserWithPhone(request.Phone);

            //Arrange 2
            SetupUniquenessRepo(phoneConflict: existing, emailConflict: null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4018.ToString());
            result.Data.Should().BeNull();
            _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_DuplicateEmail_Returns4017AndSkipsPersistence()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidCreateAccountRequest();
            var existing = SystemAdminAccountMockData.GetUserWithEmail(request.Email!);

            //Arrange 2
            SetupUniquenessRepo(phoneConflict: null, emailConflict: existing);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4017.ToString());
            result.Data.Should().BeNull();
            _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_NullEmail_SkipsEmailCheckAndReturns2000()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidCreateAccountRequest();
            request.Email = null;

            //Arrange 2
            SetupUniquenessRepo(phoneConflict: null, emailConflict: null);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Email.Should().BeNull();
            _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_ValidRequest_Returns2000WithMappedResponse()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidCreateAccountRequest();

            //Arrange 2
            SetupUniquenessRepo(phoneConflict: null, emailConflict: null);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Phone.Should().Be(request.Phone.Trim());
            result.Data.Email.Should().Be(request.Email!.Trim().ToLower());
            result.Data.FullName.Should().Be(request.FullName.Trim());
            result.Data.Role.Should().Be(request.Role);
            result.Data.IsActive.Should().BeTrue();
            result.Data.AvatarUrl.Should().Be(request.AvatarUrl);
            result.Data.CreatedAt.Should().NotBeNullOrWhiteSpace();
            _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_ValidRequest_TrimsAndNormalizesInputFields()
        {
            //Arrange 1
            var request = new CreateAccountRequest
            {
                Phone = "  0911222333  ",
                Email = "  NEW@ECS.vn  ",
                Password = "Secure@12345",
                FullName = "  Trimmed Name  ",
                Role = UserRole.PATIENT
            };

            //Arrange 2
            SetupUniquenessRepo(phoneConflict: null, emailConflict: null);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Phone.Should().Be("0911222333");
            result.Data.Email.Should().Be("new@ecs.vn");
            result.Data.FullName.Should().Be("Trimmed Name");
        }
    }
}
