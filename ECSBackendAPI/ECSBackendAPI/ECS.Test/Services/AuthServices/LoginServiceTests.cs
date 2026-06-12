using FluentAssertions;
using ECS.Application.Services.AuthServices.LoginServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.ConfigService.JwtService;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using MockQueryable.Moq;
using ECS.Test.MockData;
using Moq;
using System.Linq.Expressions;

namespace ECS.Test.Services.AuthServices
{
    /// <summary>
    /// Unit tests for <see cref="LoginService"/>.
    /// Covers: happy path, wrong password, user not found, inactive user.
    /// </summary>
    public class LoginServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<User, Guid, AppDbContext>> _userRepoMock;
        private readonly Mock<IJwtTokenService> _jwtServiceMock;
        private readonly LoginService _loginService;

        public LoginServiceTests()
        {
            _userRepoMock = new Mock<IRepositoryQueryBase<User, Guid, AppDbContext>>();
            _jwtServiceMock = new Mock<IJwtTokenService>();
            var validator = new LoginRequestValidator();
            _loginService = new LoginService(_userRepoMock.Object, _jwtServiceMock.Object, validator);
        }

        // ── Helper ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Setups the user repository to return the given user when queried by email.
        /// </summary>
        /// <summary>
        /// IMPORTANT: Must use AsyncQueryable wrapping — plain List.AsQueryable() does NOT implement
        /// IAsyncQueryProvider, causing InvalidOperationException on EF Core's FirstOrDefaultAsync.
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

        // ── Test Cases ────────────────────────────────────────────────────────────

        /// <summary>
        /// TC-LOGIN-01: Valid credentials → success with JWT token.
        /// </summary>
        [Fact]
        public async Task Login_ValidCredentials_ReturnsSuccessWithToken()
        {
            // Arrange
            var user = UserMockData.GetValidActiveUser();
            var request = LoginRequestMockData.GetValidRequest();
            const string fakeToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.fake";
            SetupUserRepo(user);
            _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns(fakeToken);
            // Act
            var result = await _loginService.Proccess(request);
            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().Be(fakeToken);
        }

        /// <summary>
        /// TC-LOGIN-02: Wrong password → returns 4016 error code.
        /// </summary>
        [Fact]
        public async Task Login_WrongPassword_Returns4016()
        {
            // Arrange
            var user = UserMockData.GetValidActiveUser();
            var request = LoginRequestMockData.GetWrongPasswordRequest();
            SetupUserRepo(user);
            // Act
            var result = await _loginService.Proccess(request);
            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4016.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-LOGIN-03: Email not found in database → returns 4016 error code.
        /// </summary>
        [Fact]
        public async Task Login_UserNotFound_Returns4016()
        {
            // Arrange
            var request = LoginRequestMockData.GetNotFoundEmailRequest();
            SetupUserRepo(null); // No user found
            // Act
            var result = await _loginService.Proccess(request);
            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4016.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-LOGIN-04: Inactive user (IsActive = false) → repo returns null → returns 4016.
        /// Inactive users are filtered out at query level (x.IsActive condition in RetrieveUserData).
        /// </summary>
        [Fact]
        public async Task Login_InactiveUser_Returns4016()
        {
            // Arrange
            var request = LoginRequestMockData.GetInactiveUserRequest();
            // Inactive user is NOT returned by the repo because query filters IsActive = true
            SetupUserRepo(null);
            // Act
            var result = await _loginService.Proccess(request);
            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4016.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-LOGIN-05: Valid credentials → JWT service is called exactly once.
        /// </summary>
        [Fact]
        public async Task Login_ValidCredentials_CallsJwtServiceOnce()
        {
            // Arrange
            var user = UserMockData.GetValidActiveUser();
            var request = LoginRequestMockData.GetValidRequest();
            SetupUserRepo(user);
            _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("token");
            // Act
            await _loginService.Proccess(request);
            // Assert
            _jwtServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Once);
        }

        /// <summary>
        /// TC-LOGIN-06: Wrong password → JWT service is never called.
        /// </summary>
        [Fact]
        public async Task Login_WrongPassword_NeverCallsJwtService()
        {
            // Arrange
            var user = UserMockData.GetValidActiveUser();
            var request = LoginRequestMockData.GetWrongPasswordRequest();
            SetupUserRepo(user);
            // Act
            await _loginService.Proccess(request);
            // Assert
            _jwtServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// TC-LOGIN-07: Email is case-insensitive → "NGUYENVANA@ECS.VN" matches stored "nguyenvana@ECS.vn".
        /// </summary>
        [Fact]
        public async Task Login_UpperCaseEmail_NormalizedAndMatchesCorrectly()
        {
            // Arrange
            var user = UserMockData.GetValidActiveUser();
            var request = new ECS.Application.Services.AuthServices.LoginServices.LoginRequest
            {
                EmailAddress = "NGUYENVANA@ECS.VN", // uppercase
                Password = "Test@12345"
            };
            SetupUserRepo(user);
            _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("token");
            // Act
            var result = await _loginService.Proccess(request);
            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
        }

        /// <summary>
        /// TC-LOGIN-08: Valid login response contains non-null, non-empty token.
        /// </summary>
        [Fact]
        public async Task Login_ValidCredentials_TokenIsNotNullOrEmpty()
        {
            // Arrange
            var user = UserMockData.GetValidActiveUser();
            var request = LoginRequestMockData.GetValidRequest();
            SetupUserRepo(user);
            _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("some.jwt.token");
            // Act
            var result = await _loginService.Proccess(request);
            // Assert
            result.Data!.Token.Should().NotBeNullOrWhiteSpace();
        }
    }
}
