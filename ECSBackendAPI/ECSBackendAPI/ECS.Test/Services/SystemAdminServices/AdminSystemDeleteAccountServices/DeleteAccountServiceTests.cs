using System.Linq.Expressions;
using ECS.Application.Services.SystemAdminServices.AdminSystemDeleteAccountServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.SystemAdminServices.AdminSystemDeleteAccountServices
{
    /// <summary>
    /// Unit tests for <see cref="DeleteAccountService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class DeleteAccountServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>> _userRepoMock = new();
        private readonly DeleteAccountService _sut;

        public DeleteAccountServiceTests()
        {
            _sut = new DeleteAccountService(_userRepoMock.Object);
        }

        private void SetupUserQuery(User? user)
        {
            var rows = user == null ? new List<User>() : new List<User> { user };
            var dbSet = rows.BuildMockDbSet<User>();
            _userRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()))
                .Returns(dbSet.Object);
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
        public async Task Process_UserNotFound_Returns4020()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetDeactivateAccountRequest();

            //Arrange 2
            SetupUserQuery(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_DeactivateActiveUser_Returns2000WithInactiveStatus()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetDeactivateAccountRequest();
            var target = SystemAdminAccountMockData.GetTargetAccount();

            //Arrange 2
            SetupUserQuery(target);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.UserId.Should().Be(target.Id);
            result.Data.IsActive.Should().BeFalse();
            result.Data.UpdatedAt.Should().NotBeNullOrWhiteSpace();
            target.IsActive.Should().BeFalse();
            _userRepoMock.Verify(r => r.UpdateAsync(target), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_ActivateInactiveUser_Returns2000WithActiveStatus()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetActivateAccountRequest();
            var target = SystemAdminAccountMockData.GetInactiveTargetAccount();

            //Arrange 2
            SetupUserQuery(target);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.UserId.Should().Be(target.Id);
            result.Data.IsActive.Should().BeTrue();
            target.IsActive.Should().BeTrue();
            _userRepoMock.Verify(r => r.UpdateAsync(target), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_ValidRequest_UsesUserIdPredicate()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetDeactivateAccountRequest();
            var target = SystemAdminAccountMockData.GetTargetAccount();
            Expression<Func<User, bool>>? capturedPredicate = null;
            var dbSet = new List<User> { target }.BuildMockDbSet<User>();

            //Arrange 2
            _userRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()))
                .Callback<Expression<Func<User, bool>>, bool>((predicate, _) => capturedPredicate = predicate)
                .Returns(dbSet.Object);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            capturedPredicate.Should().NotBeNull();
            var predicate = capturedPredicate!.Compile();
            predicate(target).Should().BeTrue();
            predicate(new User
            {
                Id = Guid.NewGuid(),
                FullName = "Other",
                Phone = "0900000000",
                Role = ECS.Domain.Enums.UserRole.PATIENT,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }).Should().BeFalse();
        }
    }
}
