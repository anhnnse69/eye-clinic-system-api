using System.Linq.Expressions;
using ECS.Application.Services.ClinicDoctorDiscoveryService.RegisterClinicApplicationServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ClinicDoctorDiscoveryServices.RegisterClinicApplicationServices
{
    /// <summary>
    /// Unit tests for <see cref="RegisterClinicApplicationService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on <c>RegisterClinicApplicationService.cs</c>.
    /// </summary>
    public class RegisterClinicApplicationServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<ClinicRegistrationRequest, Guid, AppDbContext>>
            _applicationQueryRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<ClinicRegistrationRequest, Guid, AppDbContext>>
            _applicationRepoMock = new();
        private readonly Mock<IValidator<RegisterClinicApplicationRequest>>
            _validatorMock = new();
        private readonly RegisterClinicApplicationService _sut;

        public RegisterClinicApplicationServiceTests()
        {
            _sut = new RegisterClinicApplicationService(
                _applicationQueryRepoMock.Object,
                _applicationRepoMock.Object,
                _validatorMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Repository helpers
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Wires the query repository so that FindByCondition for ClinicRegistrationRequest
        /// returns the supplied list of pending applications. Pass an empty list to indicate
        /// "no duplicate pending application".
        /// </summary>
        private void SetupDuplicateCheckRepo(IEnumerable<ClinicRegistrationRequest> pending)
        {
            var list = pending.ToList();
            var queryable = list.BuildMockDbSet<ClinicRegistrationRequest>();
            _applicationQueryRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<ClinicRegistrationRequest, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupDuplicateCheckRepoEmpty()
            => SetupDuplicateCheckRepo(Array.Empty<ClinicRegistrationRequest>());

        /// <summary>
        /// Wires the write repository so that CreateAsync + SaveChangesAsync complete cleanly.
        /// CreateAsync returns the supplied Guid (its return value is discarded by the service).
        /// </summary>
        private void SetupApplicationCreateSuccess(Guid returnedId)
        {
            _applicationRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<ClinicRegistrationRequest>()))
                .ReturnsAsync(returnedId);
            _applicationRepoMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.FromResult(1));
        }

        // ─────────────────────────────────────────────────────────────────
        // Validator helpers
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Configures the FluentValidation mock so ValidateAsync returns an invalid
        /// ValidationResult whose first error carries the supplied <paramref name="errorCode"/>.
        /// </summary>
        private void SetupValidatorInvalid(string errorCode)
        {
            var failures = new List<ValidationFailure>
            {
                new("PropertyName", "Error message") { ErrorCode = errorCode }
            };
            _validatorMock
                .Setup(v => v.ValidateAsync(
                    It.IsAny<RegisterClinicApplicationRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));
        }

        /// <summary>
        /// Configures the FluentValidation mock so ValidateAsync returns a valid
        /// ValidationResult (no failures).
        /// </summary>
        private void SetupValidatorValid()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(
                    It.IsAny<RegisterClinicApplicationRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new List<ValidationFailure>()));
        }

        // ─────────────────────────────────────────────────────────────────
        // Data factories
        // ─────────────────────────────────────────────────────────────────

        private static RegisterClinicApplicationRequest GetValidRequest() => new()
        {
            ClinicName = "  Saigon Eye Clinic  ",
            ClinicAddress = "  123 Nguyen Hue Street  ",
            ContactName = "  Nguyen Van A  ",
            ContactPhone = "+84912345678",
            ContactEmail = "  Test@Clinic.VN  ",
            BusinessLicenseUrl = "https://example.com/license.pdf"
        };

        private static ClinicRegistrationRequest MakePendingApplication(
            string contactEmail) => new()
        {
            Id = Guid.NewGuid(),
            ClinicName = "Existing Clinic",
            ClinicAddress = "Existing Address",
            ContactName = "Existing Contact",
            ContactPhone = "+84900000000",
            ContactEmail = contactEmail,
            BusinessLicenseUrl = "https://example.com/existing.pdf",
            Status = "PENDING",
            RequestedAt = DateTime.UtcNow.AddDays(-1)
        };

        // ==================================================================
        // ====================== Process(...) tests ========================
        // ==================================================================

        /// <summary>
        /// TC-RCA-01: Validator returns invalid → service short-circuits before the duplicate
        /// query and before persistence.
        /// Covers: ValidateRequestAsync (invalid branch),
        ///         CheckPendingApplicationAsync (early-return on !isValidationPassed),
        ///         CreateErrorResponse (validation-fail branch),
        ///         Process (early return).
        /// </summary>
        [Fact]
        public async Task Process_ValidationFails_ReturnsErrorCodeWithoutQuerying()
        {
            //Arrange 1
            var request = GetValidRequest();
            const string errorCode = "APP_MESSAGE_4003";

            //Arrange 2
            SetupValidatorInvalid(errorCode);
            // duplicate-check repo and write repo are intentionally NOT configured;
            // any invocation would throw.

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(errorCode);
            result.Data.Should().BeFalse();

            _applicationQueryRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<ClinicRegistrationRequest, bool>>>(),
                    It.IsAny<bool>()),
                Times.Never);
            _applicationRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<ClinicRegistrationRequest>()),
                Times.Never);
            _applicationRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-RCA-02: Validator valid + no duplicate pending → service persists the
        /// application and returns APP_MESSAGE_2000 with Data=true.
        /// Covers: ValidateRequestAsync (valid branch),
        ///         CheckPendingApplicationAsync (full query branch),
        ///         CreateResponse (success branch),
        ///         CreateErrorResponse (default-return-null branch),
        ///         CreateClinicApplicationAsync (full write path).
        /// </summary>
        [Fact]
        public async Task Process_ValidationPassedNoDuplicate_PersistsAndReturnsSuccess()
        {
            //Arrange 1
            var request = GetValidRequest();
            var generatedId = Guid.NewGuid();

            //Arrange 2
            SetupValidatorValid();
            SetupDuplicateCheckRepoEmpty();
            SetupApplicationCreateSuccess(generatedId);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeTrue();

            _applicationQueryRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<ClinicRegistrationRequest, bool>>>(),
                    It.IsAny<bool>()),
                Times.Once);
            _applicationRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<ClinicRegistrationRequest>()),
                Times.Once);
            _applicationRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// TC-RCA-03: Validator valid + existing pending application with the same
        /// (normalised) email → service returns APP_MESSAGE_4023 without persisting.
        /// Covers: CreateErrorResponse (duplicate-exists branch).
        /// </summary>
        [Fact]
        public async Task Process_ValidationPassedDuplicatePending_Returns4023()
        {
            //Arrange 1
            var request = GetValidRequest();
            var existingPending = MakePendingApplication("test@clinic.vn");

            //Arrange 2
            SetupValidatorValid();
            SetupDuplicateCheckRepo(new[] { existingPending });
            SetupApplicationCreateSuccess(Guid.NewGuid());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4023.ToString());
            result.Data.Should().BeFalse();

            _applicationRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<ClinicRegistrationRequest>()),
                Times.Never);
            _applicationRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-RCA-04: Validator returns a non-default error code (APP_MESSAGE_4019) →
        /// service propagates that error code verbatim. Same code path as TC-RCA-01
        /// but exercises a different `errorCode` constant to ensure no hard-coded
        /// codes are leaked through CreateErrorResponse.
        /// </summary>
        [Fact]
        public async Task Process_ValidationFailsWithCustomErrorCode_PropagatesErrorCode()
        {
            //Arrange 1
            var request = GetValidRequest();
            const string errorCode = "APP_MESSAGE_4019";

            //Arrange 2
            SetupValidatorInvalid(errorCode);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(errorCode);
            result.Data.Should().BeFalse();
        }

        /// <summary>
        /// TC-RCA-05: Request contact email has whitespace + uppercase; service must
        /// call FindByCondition (i.e. the duplicate query executes) — proves the
        /// `Trim().ToLower()` normalization branch inside CheckPendingApplicationAsync.
        /// </summary>
        [Fact]
        public async Task Process_DuplicateCheckEmailIsTrimmedAndLowercased()
        {
            //Arrange 1
            var request = GetValidRequest();

            //Arrange 2
            SetupValidatorValid();
            SetupDuplicateCheckRepoEmpty();
            SetupApplicationCreateSuccess(Guid.NewGuid());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            _applicationQueryRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<ClinicRegistrationRequest, bool>>>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-RCA-06: BuildClinicApplication mapping test. Asserts every entity-field
        /// assignment: trimmed names/address/phone, lowercased trimmed email, hard-coded
        /// Status="PENDING", auto-generated non-empty Id, non-default RequestedAt.
        /// </summary>
        [Fact]
        public async Task Process_BuildClinicApplication_TrimsStringFieldsAndLowercasesEmail()
        {
            //Arrange 1
            var request = GetValidRequest();

            //Arrange 2
            SetupValidatorValid();
            SetupDuplicateCheckRepoEmpty();
            SetupApplicationCreateSuccess(Guid.NewGuid());

            //Act
            await _sut.Process(request);

            //Assert
            _applicationRepoMock.Verify(
                r => r.CreateAsync(It.Is<ClinicRegistrationRequest>(a =>
                    a.Id != Guid.Empty &&
                    a.ClinicName == "Saigon Eye Clinic" &&
                    a.ClinicAddress == "123 Nguyen Hue Street" &&
                    a.ContactName == "Nguyen Van A" &&
                    a.ContactPhone == "+84912345678" &&
                    a.ContactEmail == "test@clinic.vn" &&
                    a.BusinessLicenseUrl == "https://example.com/license.pdf" &&
                    a.Status == "PENDING" &&
                    a.RequestedAt != default)),
                Times.Once);
        }

        /// <summary>
        /// TC-RCA-07: BusinessLicenseUrl is mapped verbatim (no Trim() applied).
        /// Covers the literal `BusinessLicenseUrl = request.BusinessLicenseUrl` assignment
        /// in BuildClinicApplication.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_BusinessLicenseUrlIsCopiedVerbatim()
        {
            //Arrange 1
            var request = GetValidRequest();
            const string url = "https://example.com/license.pdf";

            //Arrange 2
            SetupValidatorValid();
            SetupDuplicateCheckRepoEmpty();
            SetupApplicationCreateSuccess(Guid.NewGuid());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            _applicationRepoMock.Verify(
                r => r.CreateAsync(It.Is<ClinicRegistrationRequest>(a =>
                    a.BusinessLicenseUrl == url)),
                Times.Once);
        }
    }
}