using ECS.Application.Services.AuthServices.ViewAccountInfoServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;

namespace ECS.Test.Services.AuthServices
{
    /// <summary>
    /// Unit tests for <see cref="ViewAccountInfoService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on ViewAccountInfoService.cs.
    /// </summary>
    public class ViewAccountInfoServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<User, Guid, AppDbContext>> _userRepoMock;
        private readonly ViewAccountInfoService _sut;

        public ViewAccountInfoServiceTests()
        {
            _userRepoMock = new Mock<IRepositoryQueryBase<User, Guid, AppDbContext>>();
            var validator = new ViewAccountInfoRequestValidator();
            _sut = new ViewAccountInfoService(_userRepoMock.Object, validator);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Setups the user repository to return the given user when queried by id and active flag.
        /// IMPORTANT: Must use AsyncQueryable wrapping so FirstOrDefaultAsync resolves an
        /// IAsyncQueryProvider; a plain List.AsQueryable() throws InvalidOperationException.
        /// </summary>
        private void SetupUserRepo(User? returnUser)
        {
            var users = returnUser != null ? new List<User> { returnUser } : new List<User>();
            var mockQueryable = users.BuildMockDbSet<User>();

            _userRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// TC-VAI-01: Empty UserId → validation failure → response code APP_MESSAGE_4003.
        /// Covers: failed validation state, ValidateUser validation-failure state,
        /// and validation-error response selection.
        /// </summary>
        [Fact]
        public async Task Process_EmptyUserId_Returns4003ValidationError()
        {
            //Arrange 1
            var request = ViewAccountInfoRequestMockData.GetEmptyUserIdRequest();

            //Arrange 2
            SetupUserRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4003.ToString());
            result.Data.Should().BeNull();
            _userRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-VAI-02: Valid request but user does not exist in the repository →
        /// response code APP_MESSAGE_4020.
        /// Covers: validation passed branch, repository returned null branch,
        /// user-not-found branch in Process.
        /// </summary>
        [Fact]
        public async Task Process_ValidUserNotFound_Returns4020Error()
        {
            //Arrange 1
            var request = ViewAccountInfoRequestMockData.GetUnmatchedUserIdRequest();

            //Arrange 2
            SetupUserRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            _userRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-VAI-03: Valid request and an active user exists → success response
        /// with every ViewAccountInfoResponse field mapped correctly.
        /// Covers: validation passed branch, repository returned user branch,
        /// success branch in Process including every ViewAccountInfoResponse assignment
        /// and ApiResponse.Success wrapping.
        /// </summary>
        [Fact]
        public async Task Process_ValidActiveUser_ReturnsMappedAccountInformation()
        {
            //Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var request = ViewAccountInfoRequestMockData.GetValidRequest();

            //Arrange 2
            SetupUserRepo(user);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(user.Id);
            result.Data.Email.Should().Be(user.Email);
            result.Data.Phone.Should().Be(user.Phone);
            result.Data.FullName.Should().Be(user.FullName);
            result.Data.Role.Should().Be(user.Role.ToString());
            result.Data.IsActive.Should().Be(user.IsActive);
            result.Data.AvatarUrl.Should().Be(user.AvatarUrl);
            result.Data.CreatedAt.Should().Be(user.CreatedAt);
            result.Data.UpdatedAt.Should().Be(user.UpdatedAt);

            _userRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }
    }
}
