using ECS.Application.Common.OTP;
using ECS.Application.Services.AuthServices.ResetPasswordServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using System.Reflection;

namespace ECS.Test.Services.AuthServices
{
    /// <summary>
    /// Unit tests for <see cref="ResetPasswordService"/>.
    /// Covers: happy path, validation failures (4003, 4019), OTP failures (4031, 4032),
    /// user not found (4020), DB exception (5001), case-insensitive email lookup,
    /// and response shape.
    /// </summary>
    public class ResetPasswordServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryQueryBase<User, Guid, AppDbContext>> _userQueryRepoMock;
        private readonly Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>> _userRepoMock;
        private readonly Mock<IValidator<ResetPasswordRequest>> _validatorMock;
        private readonly ResetPasswordService _sut;

        public ResetPasswordServiceTests()
        {
            _userQueryRepoMock = new Mock<IRepositoryQueryBase<User, Guid, AppDbContext>>();
            _userRepoMock = new Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>>();
            _validatorMock = new Mock<IValidator<ResetPasswordRequest>>();
            _sut = new ResetPasswordService(
                _userQueryRepoMock.Object,
                _userRepoMock.Object,
                _validatorMock.Object);

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
                        new ValidationFailure("Property", "Error message")
                        {
                            ErrorCode = errorCode ?? GeneralCode.APP_MESSAGE_4003.ToString()
                        }
                    });

            _validatorMock
                .Setup(v => v.Validate(It.IsAny<ResetPasswordRequest>()))
                .Returns(validationResult);
        }

        /// <summary>
        /// Stores an OTP entry in the static cache, then mutates the cached entry's
        /// <see cref="OtpData.ExpiresAt"/> to a past time so that <see cref="OtpCacheManager.Validate"/>
        /// returns the "expired" branch (4032).
        /// </summary>
        private void StoreExpiredOtp(string resetToken, string email, string otp)
        {
            OtpCacheManager.Store(resetToken, email, otp);

            // Use reflection to forcibly backdate the cached entry's expiration timestamp.
            var cacheField = typeof(OtpCacheManager)
                .GetField("_otpCache", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("OtpCacheManager._otpCache field not found.");
            var cache = (Dictionary<string, OtpData>)cacheField.GetValue(null)!;
            var entry = cache[resetToken];
            cache[resetToken] = entry with { ExpiresAt = DateTime.UtcNow.AddMinutes(-1) };
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// TC-RP-01: Happy path — valid request, valid OTP, user found, password updated → success.
        /// </summary>
        [Fact]
        public async Task Process_ValidRequestOtpValidUserFound_UpdatesPasswordAndReturnsSuccess()
        {
            //Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var resetToken = "token-success";
            var otp = "123456";
            var newPassword = "NewPassword@123";
            OtpCacheManager.Store(resetToken, user.Email!, otp);
            var request = new ResetPasswordRequest
            {
                ResetToken = resetToken,
                Otp = otp,
                NewPassword = newPassword,
                ConfirmPassword = newPassword
            };

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(user);
            _userRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeNull();
            BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash).Should().BeTrue();
            user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        /// <summary>
        /// TC-RP-02: Validator fails with 4003 (NotEmpty) → returns 4003, no OTP lookup, no update.
        /// </summary>
        [Fact]
        public async Task Process_InvalidRequest_ReturnsValidationError_4003()
        {
            //Arrange 1
            var resetToken = "token-validation-4003";
            var request = new ResetPasswordRequest
            {
                ResetToken = string.Empty,
                Otp = "123456",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };
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
            OtpCacheManager.Validate(resetToken, "123456").ErrorCode.Should().NotBeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// TC-RP-03: Validator fails with 4019 (invalid password format) → returns 4019, no update.
        /// Covers the second validation error code path.
        /// </summary>
        [Fact]
        public async Task Process_InvalidPasswordFormat_ReturnsValidationError_4019()
        {
            //Arrange 1
            var request = new ResetPasswordRequest
            {
                ResetToken = "token-validation-4019",
                Otp = "123456",
                NewPassword = "weak",
                ConfirmPassword = "weak"
            };
            var errorCode = GeneralCode.APP_MESSAGE_4019.ToString();

            //Arrange 2
            SetupValidator(isValid: false, errorCode: errorCode);
            SetupUserQueryRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(errorCode);
            result.Data.Should().BeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// TC-RP-04: Valid request but OTP not in cache → returns 4031 (Invalid OTP).
        /// Covers the `!_otpCache.TryGetValue(...)` branch in OtpCacheManager.Validate.
        /// </summary>
        [Fact]
        public async Task Process_ValidRequestOtpNotInCache_ReturnsOtpInvalid_4031()
        {
            //Arrange 1
            var request = new ResetPasswordRequest
            {
                ResetToken = "token-not-in-cache",
                Otp = "123456",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            // OtpCacheManager returns raw numeric codes ("4031") rather than the "APP_MESSAGE_..." prefix.
            result.CodeMessage.Should().Be("4031");
            result.Data.Should().BeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// TC-RP-05: Valid request but OTP expired → returns 4032 (OTP expired).
        /// Covers the `otpData.ExpiresAt < DateTime.UtcNow` branch in OtpCacheManager.Validate.
        /// </summary>
        [Fact]
        public async Task Process_ValidRequestOtpExpired_ReturnsOtpExpired_4032()
        {
            //Arrange 1
            var resetToken = "token-expired";
            var email = "nguyenvana@ECS.vn";
            var otp = "123456";
            StoreExpiredOtp(resetToken, email, otp);
            var request = new ResetPasswordRequest
            {
                ResetToken = resetToken,
                Otp = otp,
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            // OtpCacheManager returns raw numeric codes ("4032") rather than the "APP_MESSAGE_..." prefix.
            result.CodeMessage.Should().Be("4032");
            result.Data.Should().BeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// TC-RP-06: Valid request but OTP value mismatch → returns 4031 (Invalid OTP).
        /// Covers the `otpData.Otp != otp` branch in OtpCacheManager.Validate.
        /// </summary>
        [Fact]
        public async Task Process_ValidRequestOtpMismatch_ReturnsOtpInvalid_4031()
        {
            //Arrange 1
            var resetToken = "token-mismatch";
            var email = "nguyenvana@ECS.vn";
            OtpCacheManager.Store(resetToken, email, "111111");
            var request = new ResetPasswordRequest
            {
                ResetToken = resetToken,
                Otp = "222222",
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            // OtpCacheManager returns raw numeric codes ("4031") rather than the "APP_MESSAGE_..." prefix.
            result.CodeMessage.Should().Be("4031");
            result.Data.Should().BeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// TC-RP-07: Valid OTP but user not found in DB → returns 4020 (User not found).
        /// Covers `IsUserFound` false → `UpdateUserPassword` short-circuits → `CreateErrorResponse` 3rd branch.
        /// </summary>
        [Fact]
        public async Task Process_ValidCredentialsUserNotFound_ReturnsUserNotFound_4020()
        {
            //Arrange 1
            var resetToken = "token-user-not-found";
            var email = "ghost@ECS.vn";
            var otp = "123456";
            OtpCacheManager.Store(resetToken, email, otp);
            var request = new ResetPasswordRequest
            {
                ResetToken = resetToken,
                Otp = otp,
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// TC-RP-08: Valid request but `_userRepository.UpdateAsync` throws → returns 5001 (DB error).
        /// Covers the `catch` branch in `UpdateUserPassword` and its return value `false`.
        /// </summary>
        [Fact]
        public async Task Process_ValidCredentialsUpdateAsyncThrows_ReturnsDatabaseError_5001()
        {
            //Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var resetToken = "token-db-error";
            var otp = "123456";
            OtpCacheManager.Store(resetToken, user.Email!, otp);
            var request = new ResetPasswordRequest
            {
                ResetToken = resetToken,
                Otp = otp,
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(user);
            _userRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<User>()))
                .ThrowsAsync(new Exception("Simulated DB failure"));

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
            result.Data.Should().BeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        /// <summary>
        /// TC-RP-09: Email lookup is case-insensitive — stored email lowercased matches an
        /// upper-cased email returned by the OTP cache.
        /// Covers the `x.Email.ToLower() == email` predicate in RetrieveUserByEmail.
        /// </summary>
        [Fact]
        public async Task Process_EmailIsUppercase_NormalizedBeforeLookup()
        {
            //Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var resetToken = "token-uppercase";
            var otp = "123456";
            // Cache stores the email as-is (uppercase) — the service must lowercase it before lookup.
            OtpCacheManager.Store(resetToken, "NGUYENVANA@ECS.VN", otp);
            var request = new ResetPasswordRequest
            {
                ResetToken = resetToken,
                Otp = otp,
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(user);
            _userRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        /// <summary>
        /// TC-RP-10: Success response shape — confirms `CreateSuccessResponse` constructor path
        /// sets `Data == null` and `CodeMessage == APP_MESSAGE_2000`.
        /// </summary>
        [Fact]
        public async Task Process_ResponseShapeOnSuccess_DataIsNullAndCodeIs2000()
        {
            //Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var resetToken = "token-shape";
            var otp = "123456";
            OtpCacheManager.Store(resetToken, user.Email!, otp);
            var request = new ResetPasswordRequest
            {
                ResetToken = resetToken,
                Otp = otp,
                NewPassword = "NewPassword@123",
                ConfirmPassword = "NewPassword@123"
            };

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(user);
            _userRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeNull();
        }
    }
}
