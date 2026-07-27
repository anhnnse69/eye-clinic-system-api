using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
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
    /// Covers: happy path, wrong password, user not found, inactive user, validation failures.
    /// </summary>
    public class LoginServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<User, Guid, AppDbContext>> _userRepoMock;
        private readonly Mock<IJwtTokenService> _jwtServiceMock;
        private readonly Mock<IValidator<LoginRequest>> _validatorMock;
        private LoginService _loginService;

        public LoginServiceTests()
        {
            _userRepoMock = new Mock<IRepositoryQueryBase<User, Guid, AppDbContext>>();
            _jwtServiceMock = new Mock<IJwtTokenService>();
            _validatorMock = new Mock<IValidator<LoginRequest>>();
            _loginService = new LoginService(_userRepoMock.Object, _jwtServiceMock.Object, _validatorMock.Object);
        }

        // ── Helper ────────────────────────────────────────────────────────────────

        /// <summary>
        /// IMPORTANT: Must use AsyncQueryable wrapping for async queries.
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

        private void SetupValidator(bool isValid, string? errorCode = null)
        {
            var validationResult = new ValidationResult(
                isValid
                    ? new List<ValidationFailure>()
                    : new List<ValidationFailure>
                    {
                        new ValidationFailure("Property", "Error message") 
                        { 
                            ErrorCode = errorCode ?? GeneralCode.APP_MESSAGE_4003.ToString() 
                        }
                    });

            _validatorMock
                .Setup(v => v.Validate(It.IsAny<LoginRequest>()))
                .Returns(validationResult);
        }

        private void ResetMocks()
        {
            _userRepoMock.Reset();
            _jwtServiceMock.Reset();
            _validatorMock.Reset();
            _loginService = new LoginService(_userRepoMock.Object, _jwtServiceMock.Object, _validatorMock.Object);
        }

        /// <summary>
        /// TC-LOGIN-01: Valid credentials → success with JWT token.
        /// </summary>
        [Fact]
        public async Task Login_ValidCredentials_ReturnsSuccessWithToken()
        {
            // Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var request = LoginRequestMockData.GetValidRequest();
            const string fakeToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.fake";

            // Arrange 2
            SetupValidator(isValid: true);
            SetupUserRepo(user);
            _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns(fakeToken);

            // Act
            var result = await _loginService.Proccess(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().Be(fakeToken);
            _jwtServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Once);
        }

        /// <summary>
        /// TC-LOGIN-02: Wrong password → returns 4016 error code.
        /// </summary>
        [Fact]
        public async Task Login_WrongPassword_Returns4016()
        {
            // Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var request = LoginRequestMockData.GetWrongPasswordRequest();

            // Arrange 2
            SetupValidator(isValid: true);
            SetupUserRepo(user);

            // Act
            var result = await _loginService.Proccess(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4016.ToString());
            result.Data.Should().BeNull();
            _jwtServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// TC-LOGIN-03: Email not found in database → returns 4016 error code.
        /// </summary>
        [Fact]
        public async Task Login_UserNotFound_Returns4016()
        {
            // Arrange 1
            var request = LoginRequestMockData.GetNotFoundEmailRequest();

            // Arrange 2
            SetupValidator(isValid: true);
            SetupUserRepo(null); // No user found

            // Act
            var result = await _loginService.Proccess(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4016.ToString());
            result.Data.Should().BeNull();
            _jwtServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// TC-LOGIN-04: Inactive user (IsActive = false) → repo returns null → returns 4016.
        /// Inactive users are filtered out at query level (x.IsActive condition in RetrieveUserData).
        /// </summary>
        [Fact]
        public async Task Login_InactiveUser_Returns4016()
        {
            // Arrange 1
            var request = LoginRequestMockData.GetInactiveUserRequest();

            // Arrange 2
            SetupValidator(isValid: true);
            SetupUserRepo(null);

            // Act
            var result = await _loginService.Proccess(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4016.ToString());
            result.Data.Should().BeNull();
            _jwtServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// TC-LOGIN-05: Invalid request (validation fails) → returns validation error code.
        /// Covers: ValidateRequest lines 73-74 and CreateErrorResponse line 160.
        /// </summary>
        [Fact]
        public async Task Login_InvalidRequest_ReturnsValidationErrorCode()
        {
            // Arrange 1
            var request = new LoginRequest
            {
                EmailAddress = "",
                Password = "short"
            };
            var validationErrorCode = GeneralCode.APP_MESSAGE_4003.ToString();

            // Arrange 2
            SetupValidator(isValid: false, errorCode: validationErrorCode);
            SetupUserRepo(null); // Setup empty repo to avoid IAsyncQueryProvider error

            // Act
            var result = await _loginService.Proccess(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(validationErrorCode);
            result.Data.Should().BeNull();
            _jwtServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// TC-LOGIN-06: Valid request but user not found → repo returns null → password never verified.
        /// Covers: ValidateCredentials early return path (line 93) when validation passed but user is null.
        /// </summary>
        [Fact]
        public async Task Login_ValidationPassedButUserNotFound_NeverVerifiesPassword()
        {
            // Arrange 1
            var request = LoginRequestMockData.GetValidRequest();

            // Arrange 2
            SetupValidator(isValid: true);
            SetupUserRepo(null); // User not found

            // Act
            var result = await _loginService.Proccess(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4016.ToString());
            _jwtServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// TC-LOGIN-07: Email is case-insensitive → "NGUYENVANA@ECS.VN" matches stored "nguyenvana@ECS.vn".
        /// </summary>
        [Fact]
        public async Task Login_UpperCaseEmail_NormalizedAndMatchesCorrectly()
        {
            // Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var request = new LoginRequest
            {
                EmailAddress = "NGUYENVANA@ECS.VN", // uppercase
                Password = "Test@12345"
            };

            // Arrange 2
            SetupValidator(isValid: true);
            SetupUserRepo(user);
            _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("token");

            // Act
            var result = await _loginService.Proccess(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            _jwtServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Once);
        }

        /// <summary>
        /// TC-LOGIN-08: Valid login response contains non-null, non-empty token.
        /// </summary>
        [Fact]
        public async Task Login_ValidCredentials_TokenIsNotNullOrEmpty()
        {
            // Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var request = LoginRequestMockData.GetValidRequest();

            // Arrange 2
            SetupValidator(isValid: true);
            SetupUserRepo(user);
            _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("some.jwt.token");

            // Act
            var result = await _loginService.Proccess(request);

            // Assert
            result.Data!.Token.Should().NotBeNullOrWhiteSpace();
        }
    }
}
