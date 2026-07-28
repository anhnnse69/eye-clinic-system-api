using System.Linq.Expressions;
using ECS.Application.Services.SystemAdminServices.AdminSystemEditAccountServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.SystemAdminServices.AdminSystemEditAccountServices
{
    /// <summary>
    /// Unit tests for <see cref="EditAccountService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class EditAccountServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>> _userRepoMock = new();
        private readonly EditAccountService _sut;

        public EditAccountServiceTests()
        {
            _sut = new EditAccountService(_userRepoMock.Object);
        }

        private void SetupQueries(User? targetUser, User? phoneConflict = null, User? emailConflict = null)
        {
            var targetRows = targetUser == null ? new List<User>() : new List<User> { targetUser };
            var targetDbSet = targetRows.BuildMockDbSet<User>();
            var phoneRows = phoneConflict == null ? new List<User>() : new List<User> { phoneConflict };
            var phoneDbSet = phoneRows.BuildMockDbSet<User>();
            var emailRows = emailConflict == null ? new List<User>() : new List<User> { emailConflict };
            var emailDbSet = emailRows.BuildMockDbSet<User>();

            _userRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<User, bool>> predicate, bool _) =>
                {
                    var text = predicate.Body.ToString();
                    if (text.Contains("Phone")) return phoneDbSet.Object;
                    if (text.Contains("Email")) return emailDbSet.Object;
                    return targetDbSet.Object;
                });
        }

        private void SetupPersistenceSuccess()
        {
            _userRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);
            _userRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);
        }

        [Fact]
        public async Task Process_TargetUserNotFound_Returns4020()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidEditAccountRequest();

            //Arrange 2
            SetupQueries(targetUser: null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_DuplicatePhone_Returns4018()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidEditAccountRequest();
            var target = SystemAdminAccountMockData.GetTargetAccount();
            var phoneConflict = SystemAdminAccountMockData.GetUserWithPhone(request.Phone);

            //Arrange 2
            SetupQueries(target, phoneConflict: phoneConflict);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4018.ToString());
            result.Data.Should().BeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Process_DuplicateEmail_Returns4017()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidEditAccountRequest();
            var target = SystemAdminAccountMockData.GetTargetAccount();
            var emailConflict = SystemAdminAccountMockData.GetUserWithEmail(request.Email!);

            //Arrange 2
            SetupQueries(target, emailConflict: emailConflict);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4017.ToString());
            result.Data.Should().BeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Process_ValidRequest_Returns2000WithUpdatedFields()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidEditAccountRequest();
            var target = SystemAdminAccountMockData.GetTargetAccount();

            //Arrange 2
            SetupQueries(target);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(target.Id);
            result.Data.Phone.Should().Be(request.Phone);
            result.Data.Email.Should().Be(request.Email);
            result.Data.FullName.Should().Be(request.FullName);
            result.Data.Role.Should().Be(request.Role.ToString());
            result.Data.AvatarUrl.Should().Be(request.AvatarUrl);
            result.Data.UpdatedAt.Should().NotBeNullOrWhiteSpace();
            target.Phone.Should().Be(request.Phone);
            target.Email.Should().Be(request.Email);
            target.FullName.Should().Be(request.FullName);
            target.Role.Should().Be(request.Role);
            target.AvatarUrl.Should().Be(request.AvatarUrl);
            _userRepoMock.Verify(r => r.UpdateAsync(target), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_NullEmail_SkipsEmailUniquenessCheck()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidEditAccountRequest();
            request.Email = null;
            var target = SystemAdminAccountMockData.GetTargetAccount();

            //Arrange 2
            SetupQueries(target);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Email.Should().BeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(target), Times.Once);
        }
    }
}
