using ECS.Application.Services.AuthServices.RegisterServices;
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

namespace ECS.Test.Services.AuthServices
{
    /// <summary>
    /// Unit tests for <see cref="RegisterService"/>.
    /// Covers: validation failure (skips uniqueness checks), email duplicate (4017),
    /// phone duplicate (4018), happy path, normalization, and User entity shape.
    /// Goal: 100% line coverage on RegisterService.cs.
    /// </summary>
    public class RegisterServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<User, Guid, AppDbContext>> _userQueryRepoMock;
        private readonly Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>> _userRepoMock;
        private readonly Mock<IValidator<RegisterRequest>> _validatorMock;
        private readonly RegisterService _sut;

        public RegisterServiceTests()
        {
            _userQueryRepoMock = new Mock<IRepositoryQueryBase<User, Guid, AppDbContext>>();
            _userRepoMock = new Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>>();
            _validatorMock = new Mock<IValidator<RegisterRequest>>();
            _sut = new RegisterService(
                _userQueryRepoMock.Object,
                _userRepoMock.Object,
                _validatorMock.Object);
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
                .Setup(v => v.Validate(It.IsAny<RegisterRequest>()))
                .Returns(validationResult);
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// TC-REG-01: Validation fails → returns validation error code,
        /// Skips uniqueness checks (FindByCondition never invoked).
        /// Covers: ValidateRequest (result.IsValid == false branch),
        /// CheckEmailUniqueness + CheckPhoneUniqueness (isValidationPassed == false early-return),
        /// CreateErrorResponse (!validationResult.IsPassed branch).
        /// </summary>
        [Fact]
        public async Task Process_InvalidRequest_ReturnsValidationErrorAndSkipsUniquenessChecks()
        {
            //Arrange 1
            var request = RegisterRequestMockData.GetInvalidRequest();
            var expectedCode = GeneralCode.APP_MESSAGE_4003.ToString();

            //Arrange 2
            SetupValidator(isValid: false, errorCode: expectedCode);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(expectedCode);
            result.Data.Should().BeNull();
            _userQueryRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _userRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<User>()),
                Times.Never);
            _userRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-REG-02: Validation passes but email already exists → returns 4017,
        /// Phone check is skipped (short-circuit).
        /// Covers: CheckEmailUniqueness (isValidationPassed == true → query → exists),
        /// CreateErrorResponse (emailResult.Exists branch).
        /// </summary>
        [Fact]
        public async Task Process_ValidRequestEmailAlreadyExists_Returns4017AndSkipsPhoneCheck()
        {
            //Arrange 1
            var request = RegisterRequestMockData.GetExistingEmailRequest();
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "existing@ECS.vn",
                Phone = "0988888888",
                IsActive = true
            };

            //Arrange 2
            SetupValidator(isValid: true);
            // Build a queryable with one user → AnyAsync() returns true.
            SetupUserQueryRepo(existingUser);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4017.ToString());
            result.Data.Should().BeNull();
            _userRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<User>()),
                Times.Never);
            _userRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-REG-03: Validation passes, email unique, but phone already exists → returns 4018.
        /// Covers: CheckEmailUniqueness (exists == false),
        /// CheckPhoneUniqueness (isValidationPassed == true → query → exists),
        /// CreateErrorResponse (phoneResult.Exists branch).
        /// </summary>
        [Fact]
        public async Task Process_ValidRequestEmailUniquePhoneAlreadyExists_Returns4018()
        {
            //Arrange 1
            var request = RegisterRequestMockData.GetExistingPhoneRequest();
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "someoneelse@ECS.vn",
                Phone = "0909999999",
                IsActive = true
            };

            //Arrange 2
            SetupValidator(isValid: true);
            // First call (email check) → empty queryable, AnyAsync returns false.
            // Second call (phone check) → user with matching phone, AnyAsync returns true.
            var emptyQueryable = new List<User>().BuildMockDbSet<User>();
            var userQueryable = new List<User> { existingUser }.BuildMockDbSet<User>();
            _userQueryRepoMock
                .SetupSequence(r => r.FindByCondition(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(emptyQueryable.Object)
                .Returns(userQueryable.Object);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4018.ToString());
            result.Data.Should().BeNull();
            _userRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<User>()),
                Times.Never);
            _userRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-REG-04: Happy path — validation passes, neither email nor phone exists → user created.
        /// Covers: CheckEmailUniqueness (exists == false),
        /// CheckPhoneUniqueness (exists == false),
        /// CreateErrorResponse (returns null),
        /// CreateUserAsync (CreateAsync + SaveChangesAsync + Success response),
        /// BuildUser (full User entity construction).
        /// </summary>
        [Fact]
        public async Task Process_ValidRequestNoDuplicates_CreatesUserAndReturnsSuccess()
        {
            //Arrange 1
            var request = RegisterRequestMockData.GetValidRequest();

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(null); // No users found → both uniqueness checks pass
            _userRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(Guid.NewGuid());
            _userRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().Be(true);
            _userRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<User>()),
                Times.Once);
            _userRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// TC-REG-05: Happy path — verify the User entity passed to CreateAsync has
        /// correctly normalised fields (trimmed, lowercased email) and proper defaults.
        /// Covers: BuildUser (returns a User with normalized fields, hashed password,
        /// UserRole.PATIENT, IsActive = true, UTC timestamps).
        /// </summary>
        [Fact]
        public async Task Process_ValidRequest_BuildsUserWithCorrectFields()
        {
            //Arrange 1
            var request = RegisterRequestMockData.GetValidRequest();
            User? capturedUser = null;

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(null);
            _userRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync(Guid.NewGuid());
            _userRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            capturedUser.Should().NotBeNull();
            capturedUser!.FullName.Should().Be("Nguyen Van A");
            capturedUser.Email.Should().Be("nguyenvana@ecs.vn"); // lowercased (full email is .ToLower()'d)
            capturedUser.Phone.Should().Be("0901234567");
            capturedUser.Role.Should().Be(UserRole.PATIENT);
            capturedUser.IsActive.Should().BeTrue();
            capturedUser.Id.Should().NotBe(Guid.Empty);
            BCrypt.Net.BCrypt.Verify("Test@12345", capturedUser.PasswordHash).Should().BeTrue();
            capturedUser.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            capturedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// TC-REG-06: Validation passes, email with uppercase + whitespace is normalized
        /// (Trim + ToLower) before lookup, and user is still created successfully.
        /// Covers: CheckEmailUniqueness normalization path (Trim + ToLower on email).
        /// </summary>
        [Fact]
        public async Task Process_ValidRequestEmailUppercaseAndWhitespace_NormalizedBeforeLookup()
        {
            //Arrange 1
            var request = RegisterRequestMockData.GetUpperCaseEmailRequest();

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(null); // No matching user after normalization
            _userRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(Guid.NewGuid());
            _userRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            _userRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<User>()),
                Times.Once);
        }

        /// <summary>
        /// TC-REG-07: Validation passes with empty DB (no users). Both uniqueness checks
        /// run against an empty queryable → AnyAsync returns false in both.
        /// Covers: ValidateRequest (result.IsValid == true branch),
        /// CheckEmailUniqueness (returns UniquenessResult(false)),
        /// CheckPhoneUniqueness (returns UniquenessResult(false)),
        /// CreateErrorResponse (returns null).
        /// </summary>
        [Fact]
        public async Task Process_ValidRequestEmptyDatabase_ReachesUserCreationPath()
        {
            //Arrange 1
            var request = RegisterRequestMockData.GetValidRequest();

            //Arrange 2
            SetupValidator(isValid: true);
            SetupUserQueryRepo(null); // empty DB
            _userRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(Guid.NewGuid());
            _userRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            _userQueryRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()),
                Times.Exactly(2)); // email check + phone check
        }
    }
}
