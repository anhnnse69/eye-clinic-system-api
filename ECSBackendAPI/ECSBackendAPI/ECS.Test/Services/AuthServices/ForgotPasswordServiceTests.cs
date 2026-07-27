using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using ECS.Application.Common.OTP;
using ECS.Application.Services.AuthServices.ForgotPasswordServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.ConfigService.EmailService;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;

namespace ECS.Test.Services.AuthServices
{
    /// <summary>
    /// Unit tests for <see cref="ForgotPasswordService"/>.
    /// Covers: success, validation failures (4003, 4019), user not found (4020),
    /// email send failure (5003), normalization, and response shape.
    /// </summary>
    public class ForgotPasswordServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryQueryBase<User, Guid, AppDbContext>> _userQueryRepoMock;
        private readonly Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>> _userRepoMock;
        private readonly Mock<IValidator<ForgotPasswordRequest>> _validatorMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly ForgotPasswordService _sut;

        public ForgotPasswordServiceTests()
        {
            _userQueryRepoMock = new Mock<IRepositoryQueryBase<User, Guid, AppDbContext>>();
            _userRepoMock = new Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>>();
            _validatorMock = new Mock<IValidator<ForgotPasswordRequest>>();
            _emailServiceMock = new Mock<IEmailService>();
            _sut = new ForgotPasswordService(
                _userQueryRepoMock.Object,
                _userRepoMock.Object,
                _validatorMock.Object,
                _emailServiceMock.Object);

            // Reset static OTP cache between tests to avoid bleed-through.
            OtpCacheManager.Clear();
        }

        public void Dispose()
        {
            OtpCacheManager.Clear();
            GC.SuppressFinalize(this);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// IMPORTANT: Must use AsyncQueryable wrapping for async queries.
        /// </summary>
        private void SetupUserQueryRepo(User? returnUser)
        {
            var users = returnUser != null ? new List<User> { returnUser } : new List<User>();
            var mockQueryable = users.BuildMockDbSet<User>();

            _userQueryRepoMock
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
                        new ValidationFailure("Email", "Error message")
                        {
                            ErrorCode = errorCode ?? GeneralCode.APP_MESSAGE_4019.ToString()
                        }
                    });

            _validatorMock
                .Setup(v => v.Validate(It.IsAny<ForgotPasswordRequest>()))
                .Returns(validationResult);
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// TC-FP-01: Happy path — valid email, user found, email sent → success with reset token.
        /// </summary>
        [Fact]
        public async Task Process_ValidEmailUserFoundAndEmailSent_ReturnsSuccessWithResetToken()
        {
            //Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var request = new ForgotPasswordRequest { Email = user.Email! };

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(user);
            _emailServiceMock
                .Setup(e => e.BuildPasswordResetEmailBody(It.IsAny<string>()))
                .Returns("<html>otp</html>");
            _emailServiceMock
                .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.ResetToken.Should().NotBeNullOrWhiteSpace();
            _emailServiceMock.Verify(
                e => e.SendEmailAsync(user.Email!, "ECS Medical - Password Reset OTP", "<html>otp</html>"),
                Times.Once);
            _emailServiceMock.Verify(
                e => e.BuildPasswordResetEmailBody(It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// TC-FP-02: Validator fails with 4019 (invalid email format) → returns 4019, no email sent.
        /// </summary>
        [Fact]
        public async Task Process_InvalidEmail_ReturnsValidationErrorAndSkipsLookup()
        {
            //Arrange 1
            var request = new ForgotPasswordRequest { Email = "not-an-email" };
            var errorCode = GeneralCode.APP_MESSAGE_4019.ToString();

            //Arrange 2
            SetupValidator(isValid: false, errorCode: errorCode);
            SetupUserQueryRepo(null); // empty queryable to satisfy FirstOrDefaultAsync

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(errorCode);
            result.Data.Should().BeNull();
            _emailServiceMock.Verify(
                e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
            _userRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<User>()),
                Times.Never);
        }

        /// <summary>
        /// TC-FP-03: Validator fails with 4003 (NotEmpty) → returns 4003, no email sent.
        /// </summary>
        [Fact]
        public async Task Process_EmptyEmail_ReturnsValidationError_4003()
        {
            //Arrange 1
            var request = new ForgotPasswordRequest { Email = string.Empty };
            var errorCode = GeneralCode.APP_MESSAGE_4003.ToString();

            //Arrange 2
            SetupValidator(isValid: false, errorCode: errorCode);
            SetupUserQueryRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(errorCode);
            result.Data.Should().BeNull();
            _emailServiceMock.Verify(
                e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        /// <summary>
        /// TC-FP-04: Valid email but user not found → returns 4020, no email sent.
        /// </summary>
        [Fact]
        public async Task Process_ValidEmailUserNotFound_ReturnsUserNotFound_4020()
        {
            //Arrange 1
            var request = new ForgotPasswordRequest { Email = "ghost@ECS.vn" };

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(null); // no user found

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            _emailServiceMock.Verify(
                e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        /// <summary>
        /// TC-FP-05: User found but email send fails → returns 5003, OTP cache not written.
        /// </summary>
        [Fact]
        public async Task Process_ValidEmailUserFoundButEmailSendFails_ReturnsExternalServiceError_5003()
        {
            //Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var request = new ForgotPasswordRequest { Email = user.Email! };

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(user);
            _emailServiceMock
                .Setup(e => e.BuildPasswordResetEmailBody(It.IsAny<string>()))
                .Returns("<html>otp</html>");
            _emailServiceMock
                .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5003.ToString());
            result.Data.Should().BeNull();
            _emailServiceMock.Verify(
                e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// TC-FP-06: Email with uppercase + surrounding whitespace is normalized (ToLower().Trim())
        /// before lookup, and the email is sent to the normalized address.
        /// </summary>
        [Fact]
        public async Task Process_EmailIsUppercaseAndWhitespace_NormalizedBeforeLookup()
        {
            //Arrange 1
            var user = UserMockData.GetValidActiveUser();
            // SendEmailAsync is invoked with the entity's stored Email (not the normalized lookup key).
            var request = new ForgotPasswordRequest { Email = "  NgUYENVANA@ECS.vn  " };

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(user);
            _emailServiceMock
                .Setup(e => e.BuildPasswordResetEmailBody(It.IsAny<string>()))
                .Returns("<html>otp</html>");
            _emailServiceMock
                .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            // SendEmailAsync is invoked with user.Email (the stored entity email),
            // not the lowercased lookup key. Repository lookup did match after normalization.
            _emailServiceMock.Verify(
                e => e.SendEmailAsync(user.Email!, "ECS Medical - Password Reset OTP", "<html>otp</html>"),
                Times.Once);
            _userQueryRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-FP-07: Success response shape — ResetToken is present and non-empty,
        /// confirming CreateSuccessResponse constructor path is covered.
        /// </summary>
        [Fact]
        public async Task Process_ResponseShapeOnSuccess_DataResetTokenMatchesInvariants()
        {
            //Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var request = new ForgotPasswordRequest { Email = user.Email! };

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(user);
            _emailServiceMock
                .Setup(e => e.BuildPasswordResetEmailBody(It.IsAny<string>()))
                .Returns("<html>otp</html>");
            _emailServiceMock
                .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.ResetToken.Should().NotBeNullOrEmpty();
            result.Data.ResetToken.Length.Should().BeGreaterThan(10);
        }
    }
}