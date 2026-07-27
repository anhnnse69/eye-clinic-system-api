using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreatePatientDemographicsServices;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.DoctorAppointmentPatientManagementServices.CreatePatientDemographicsServices
{
    public class CreatePatientDemographicsServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext>> _patientProfileRepositoryMock;
        private readonly Mock<IValidator<CreatePatientDemographicsRequest>> _validatorMock;
        private readonly AppDbContext _context;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly CreatePatientDemographicsService _service;

        public CreatePatientDemographicsServiceTests()
        {
            _patientProfileRepositoryMock = new Mock<IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext>>();
            _validatorMock = new Mock<IValidator<CreatePatientDemographicsRequest>>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            _service = new CreatePatientDemographicsService(
                _patientProfileRepositoryMock.Object,
                _validatorMock.Object,
                _context,
                _httpContextAccessorMock.Object);

            SetupPatientProfileRepo(null);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SetupHttpContext(string? userIdClaim = null, bool setNullContext = false, bool setNullClaim = false)
        {
            if (setNullContext)
            {
                _httpContextAccessorMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);
                return;
            }

            var claims = new List<Claim>();
            if (userIdClaim != null && !setNullClaim)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdClaim));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(httpContext);
        }

        private void SetupValidator(bool isValid)
        {
            if (isValid)
            {
                _validatorMock.Setup(v => v.Validate(It.IsAny<CreatePatientDemographicsRequest>()))
                    .Returns(new ValidationResult());
            }
            else
            {
                _validatorMock.Setup(v => v.Validate(It.IsAny<CreatePatientDemographicsRequest>()))
                    .Returns(new ValidationResult(new[] { new ValidationFailure("PatientProfileId", "Required") }));
            }
        }

        private void SetupPatientProfileRepo(PatientProfile? patientProfile)
        {
            var list = patientProfile != null ? new List<PatientProfile> { patientProfile } : new List<PatientProfile>();
            var mockQueryable = list.BuildMockDbSet<PatientProfile>();

            _patientProfileRepositoryMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<PatientProfile, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);

            if (patientProfile != null && !_context.PatientProfiles.Any(p => p.Id == patientProfile.Id))
            {
                _context.PatientProfiles.Add(patientProfile);
                _context.SaveChanges();
            }
        }

        private Mock<IDbContextTransaction> SetupTransaction()
        {
            var transactionMock = new Mock<IDbContextTransaction>();
            transactionMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            transactionMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _patientProfileRepositoryMock
                .Setup(r => r.BeginTransactionAsync())
                .ReturnsAsync(transactionMock.Object);
            return transactionMock;
        }

        // ── Test Cases ────────────────────────────────────────────────────────────

        [Fact]
        public async Task Process_RequestValidationFailed_ReturnsBadRequest4003()
        {
            //Arrange 1
            var request = CreatePatientDemographicsMockData.GetValidRequest();

            //Arrange 2
            SetupValidator(isValid: false);
            SetupHttpContext(CreatePatientDemographicsMockData.DefaultDoctorUserId.ToString());

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4003.ToString());
            _patientProfileRepositoryMock.Verify(r => r.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_HttpContextNull_ReturnsForbidden4033()
        {
            //Arrange 1
            var request = CreatePatientDemographicsMockData.GetValidRequest();

            //Arrange 2
            SetupValidator(isValid: true);
            SetupHttpContext(setNullContext: true);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            _patientProfileRepositoryMock.Verify(r => r.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_UserClaimMissingOrInvalid_ReturnsForbidden4033()
        {
            //Arrange 1
            var request = CreatePatientDemographicsMockData.GetValidRequest();

            //Arrange 2
            SetupValidator(isValid: true);
            SetupHttpContext(userIdClaim: "invalid-guid-string");

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            _patientProfileRepositoryMock.Verify(r => r.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_UserClaimNull_ReturnsForbidden4033()
        {
            //Arrange 1
            var request = CreatePatientDemographicsMockData.GetValidRequest();

            //Arrange 2
            SetupValidator(isValid: true);
            SetupHttpContext(setNullClaim: true);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            _patientProfileRepositoryMock.Verify(r => r.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_PatientProfileIdInvalidGuid_ReturnsBadRequest4019()
        {
            //Arrange 1
            var request = CreatePatientDemographicsMockData.GetValidRequest();
            request.PatientProfileId = "not-a-valid-guid";

            //Arrange 2
            SetupValidator(isValid: true);
            SetupHttpContext(CreatePatientDemographicsMockData.DefaultDoctorUserId.ToString());

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            _patientProfileRepositoryMock.Verify(r => r.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_PatientProfileNotFound_ReturnsNotFound4010()
        {
            //Arrange 1
            var request = CreatePatientDemographicsMockData.GetValidRequest();

            //Arrange 2
            SetupValidator(isValid: true);
            SetupHttpContext(CreatePatientDemographicsMockData.DefaultDoctorUserId.ToString());
            SetupPatientProfileRepo(null);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4010.ToString());
            _patientProfileRepositoryMock.Verify(r => r.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_MedicalDemographicsAlreadyExists_ReturnsConflict4099()
        {
            //Arrange 1
            var request = CreatePatientDemographicsMockData.GetValidRequest();
            var existingProfile = CreatePatientDemographicsMockData.GetPatientProfile(hasMedicalDemographics: true);

            //Arrange 2
            SetupValidator(isValid: true);
            SetupHttpContext(CreatePatientDemographicsMockData.DefaultDoctorUserId.ToString());
            SetupPatientProfileRepo(existingProfile);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4099.ToString());
            _patientProfileRepositoryMock.Verify(r => r.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_DatabaseExceptionOnSave_RollsBackAndReturnsInternalServerError5001()
        {
            //Arrange 1
            var request = CreatePatientDemographicsMockData.GetValidRequest();
            var patientProfile = CreatePatientDemographicsMockData.GetPatientProfile(hasMedicalDemographics: false);

            //Arrange 2
            SetupValidator(isValid: true);
            SetupHttpContext(CreatePatientDemographicsMockData.DefaultDoctorUserId.ToString());
            SetupPatientProfileRepo(patientProfile);

            var transactionMock = SetupTransaction();
            transactionMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("DB write failure"));

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
            transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Process_ValidRequestWithAllFieldsProvided_UpdatesProfileAndReturnsSuccess2005()
        {
            //Arrange 1
            var request = CreatePatientDemographicsMockData.GetValidRequest();
            var patientProfile = CreatePatientDemographicsMockData.GetPatientProfile(hasMedicalDemographics: false);

            //Arrange 2
            SetupValidator(isValid: true);
            SetupHttpContext(CreatePatientDemographicsMockData.DefaultDoctorUserId.ToString());
            SetupPatientProfileRepo(patientProfile);
            var transactionMock = SetupTransaction();

            //Act
            var result = await _service.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.PatientProfileId.Should().Be(CreatePatientDemographicsMockData.DefaultPatientProfileId);
            result.Data.PatientName.Should().Be("Nguyen Van A");
            result.Data.BloodType.Should().Be("O+");
            result.Data.Allergies.Should().Be("Dust, Pollen");
            result.Data.MedicalHistory.Should().Be("No chronic diseases");
            result.Data.FamilyHistory.Should().Be("No genetic conditions");
            result.Data.LifestyleFactors.Should().Be("Non-smoker");
            result.Data.CurrentEyeMedications.Should().Be("Eye drops twice daily");
            result.Data.PreviousEyeSurgery.Should().Be("LASIK in 2020");
            result.Data.EyeVisionHistory.Should().Be("Mild myopia");
            result.Data.IsSuccess.Should().BeTrue();

            patientProfile.HasMedicalDemographics.Should().BeTrue();
            transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Process_ValidRequestWithMinimalFieldsAndInvalidFormats_IgnoresInvalidOptionalFormatsAndReturnsSuccess2005()
        {
            //Arrange 1
            var request = new CreatePatientDemographicsRequest
            {
                PatientProfileId = CreatePatientDemographicsMockData.DefaultPatientProfileId.ToString(),
                DateOfBirth = "invalid-dob-format",
                Gender = "INVALID_GENDER_ENUM",
                FullName = "  ", // Whitespace ignored
                PhoneNumber = null,
                IdentityNumber = "",
                BhytNumber = " ",
                Address = null,
                BloodType = "",
                Allergies = null,
                MedicalHistory = " ",
                FamilyHistory = null,
                LifestyleFactors = "",
                CurrentEyeMedications = null,
                PreviousEyeSurgery = " ",
                EyeVisionHistory = null
            };
            var patientProfile = CreatePatientDemographicsMockData.GetPatientProfile(hasMedicalDemographics: false);
            var originalDob = patientProfile.Dob;
            var originalGender = patientProfile.Gender;
            var originalName = patientProfile.FullName;

            //Arrange 2
            SetupValidator(isValid: true);
            SetupHttpContext(CreatePatientDemographicsMockData.DefaultDoctorUserId.ToString());
            SetupPatientProfileRepo(patientProfile);
            var transactionMock = SetupTransaction();

            //Act
            var result = await _service.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            patientProfile.Dob.Should().Be(originalDob);
            patientProfile.Gender.Should().Be(originalGender);
            patientProfile.FullName.Should().Be(originalName);
            patientProfile.HasMedicalDemographics.Should().BeTrue();
            transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
