using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetDetailPatientDemographicsServices;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.DoctorAppointmentPatientManagementServices.GetDetailPatientDemographicsServices
{
    public class GetDetailPatientDemographicsServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>> _patientProfileRepositoryMock;
        private readonly AppDbContext _context;
        private readonly Mock<IValidator<GetDetailPatientDemographicsRequest>> _validatorMock;
        private readonly GetDetailPatientDemographicsService _service;

        public GetDetailPatientDemographicsServiceTests()
        {
            _patientProfileRepositoryMock = new Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>>();
            _validatorMock = new Mock<IValidator<GetDetailPatientDemographicsRequest>>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _service = new GetDetailPatientDemographicsService(
                _patientProfileRepositoryMock.Object,
                _context,
                _validatorMock.Object);

            SetupPatientProfileRepo(null);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SetupValidator(bool isValid)
        {
            if (isValid)
            {
                _validatorMock.Setup(v => v.Validate(It.IsAny<GetDetailPatientDemographicsRequest>()))
                    .Returns(new ValidationResult());
            }
            else
            {
                var failure = new ValidationFailure("PatientId", "Patient ID is required")
                {
                    ErrorCode = GeneralCode.APP_MESSAGE_4003.ToString()
                };
                _validatorMock.Setup(v => v.Validate(It.IsAny<GetDetailPatientDemographicsRequest>()))
                    .Returns(new ValidationResult(new[] { failure }));
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
        }

        // ── Test Cases ────────────────────────────────────────────────────────────

        [Fact]
        public async Task Process_RequestValidationFailed_ReturnsBadRequest4003()
        {
            //Arrange 1
            var request = GetDetailPatientDemographicsMockData.GetInvalidRequest();

            //Arrange 2
            SetupValidator(isValid: false);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4003.ToString());
            _patientProfileRepositoryMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task Process_PatientProfileNotFound_ReturnsNotFound4010()
        {
            //Arrange 1
            var request = GetDetailPatientDemographicsMockData.GetValidRequest();

            //Arrange 2
            SetupValidator(isValid: true);
            SetupPatientProfileRepo(null);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4010.ToString());
            _patientProfileRepositoryMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task Process_PatientProfileExists_ReturnsSuccess2000WithDemographicsData()
        {
            //Arrange 1
            var request = GetDetailPatientDemographicsMockData.GetValidRequest();
            var patientProfile = GetDetailPatientDemographicsMockData.GetPatientProfile();

            //Arrange 2
            SetupValidator(isValid: true);
            SetupPatientProfileRepo(patientProfile);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.FullName.Should().Be("Nguyen Van B");
            result.Data.Dob.Should().Be("1990-05-20");
            result.Data.Gender.Should().Be(Gender.MALE.ToString());
            result.Data.PhoneNumber.Should().Be("0987654321");
            result.Data.IdentityNumber.Should().Be("012345678901");
            result.Data.BhytNumber.Should().Be("DN4010123456789");
            result.Data.Address.Should().Be("123 Le Loi, Quan 1, TP.HCM");
            result.Data.BloodType.Should().Be("O+");
            result.Data.Allergies.Should().Be("Peanuts, Penicillin");
            result.Data.MedicalHistory.Should().Be("Hypertension diagnosed 2021");

            _patientProfileRepositoryMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()), Times.Once);
        }
    }
}
