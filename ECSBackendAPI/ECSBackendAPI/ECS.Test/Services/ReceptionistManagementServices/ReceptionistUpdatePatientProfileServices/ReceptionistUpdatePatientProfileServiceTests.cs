using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using MockQueryable;
using Moq;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistUpdatePatientProfileServices.Tests
{
    public class ReceptionistUpdatePatientProfileServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext>> _patientRepoMock;
        private readonly ReceptionistUpdatePatientProfileService _sut;

        public ReceptionistUpdatePatientProfileServiceTests()
        {
            _patientRepoMock = new Mock<IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext>>();
            _sut = new ReceptionistUpdatePatientProfileService(_patientRepoMock.Object);
        }

        private void SetupPatientRepo(List<PatientProfile> patients)
        {
            var mockQueryable = patients.BuildMock();
            _patientRepoMock
                .Setup(r => r.FindAll(true))
                .Returns(mockQueryable);
        }

        [Fact]
        public async Task Process_PatientExists_ReturnsSuccessWithUpdatedData()
        {
            // Arrange
            var patients = ReceptionistUpdatePatientProfileMockData.GetPatientsList();
            SetupPatientRepo(patients);
            var request = ReceptionistUpdatePatientProfileMockData.GetValidRequest();

            // Act
            var response = await _sut.Process(ReceptionistUpdatePatientProfileMockData.PatientId, request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("APP_MESSAGE_2000");
            response.Data.Should().NotBeNull();
            response.Data!.Id.Should().Be(ReceptionistUpdatePatientProfileMockData.PatientId.ToString());
            response.Data.FullName.Should().Be("Nguyen Van A");
        }

        [Fact]
        public async Task Process_TrimsWhitespaceFieldsCorrectly()
        {
            // Arrange
            var patients = ReceptionistUpdatePatientProfileMockData.GetPatientsList();
            SetupPatientRepo(patients);
            var request = ReceptionistUpdatePatientProfileMockData.GetValidRequest();

            // Act
            await _sut.Process(ReceptionistUpdatePatientProfileMockData.PatientId, request);

            // Assert
            var updatedEntity = patients.First(p => p.Id == ReceptionistUpdatePatientProfileMockData.PatientId);
            updatedEntity.FullName.Should().Be("Nguyen Van A");
            updatedEntity.PhoneNumber.Should().Be("0901234567");
            updatedEntity.Address.Should().Be("123 Main St");
            updatedEntity.IdentityNumber.Should().Be("123456789012");
        }

        [Fact]
        public async Task Process_BhytNumber_TrimmedAndUpperCased()
        {
            // Arrange
            var patients = ReceptionistUpdatePatientProfileMockData.GetPatientsList();
            SetupPatientRepo(patients);
            var request = ReceptionistUpdatePatientProfileMockData.GetValidRequest();
            request.BhytNumber = "  bhyt999  ";

            // Act
            await _sut.Process(ReceptionistUpdatePatientProfileMockData.PatientId, request);

            // Assert
            var updatedEntity = patients.First(p => p.Id == ReceptionistUpdatePatientProfileMockData.PatientId);
            updatedEntity.BhytNumber.Should().Be("BHYT999");
        }

        [Theory]
        [InlineData("male", Gender.MALE)]
        [InlineData("FEMALE", Gender.FEMALE)]
        [InlineData("Other", Gender.OTHER)]
        public async Task Process_GenderInput_ParsedCaseInsensitively(string genderInput, Gender expected)
        {
            // Arrange
            var patients = ReceptionistUpdatePatientProfileMockData.GetPatientsList();
            SetupPatientRepo(patients);
            var request = ReceptionistUpdatePatientProfileMockData.GetValidRequest();
            request.Gender = genderInput;

            // Act
            await _sut.Process(ReceptionistUpdatePatientProfileMockData.PatientId, request);

            // Assert
            var updatedEntity = patients.First(p => p.Id == ReceptionistUpdatePatientProfileMockData.PatientId);
            updatedEntity.Gender.Should().Be(expected);
        }

        [Fact]
        public async Task Process_InvalidGender_ThrowsArgumentException()
        {
            // Arrange
            var patients = ReceptionistUpdatePatientProfileMockData.GetPatientsList();
            SetupPatientRepo(patients);
            var request = ReceptionistUpdatePatientProfileMockData.GetValidRequest();
            request.Gender = "UNKNOWN";

            // Act
            var act = () => _sut.Process(ReceptionistUpdatePatientProfileMockData.PatientId, request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task Process_DobParsedCorrectly()
        {
            // Arrange
            var patients = ReceptionistUpdatePatientProfileMockData.GetPatientsList();
            SetupPatientRepo(patients);
            var request = ReceptionistUpdatePatientProfileMockData.GetValidRequest();
            request.Dob = "1999-12-31";

            // Act
            await _sut.Process(ReceptionistUpdatePatientProfileMockData.PatientId, request);

            // Assert
            var updatedEntity = patients.First(p => p.Id == ReceptionistUpdatePatientProfileMockData.PatientId);
            updatedEntity.Dob.Should().Be(new DateTime(1999, 12, 31));
        }

        [Fact]
        public async Task Process_InvalidDobFormat_ThrowsFormatException()
        {
            // Arrange
            var patients = ReceptionistUpdatePatientProfileMockData.GetPatientsList();
            SetupPatientRepo(patients);
            var request = ReceptionistUpdatePatientProfileMockData.GetValidRequest();
            request.Dob = "31/12/1999";

            // Act
            var act = () => _sut.Process(ReceptionistUpdatePatientProfileMockData.PatientId, request);

            // Assert
            await act.Should().ThrowAsync<FormatException>();
        }

        [Fact]
        public async Task Process_NullOptionalFields_MapsToNull()
        {
            // Arrange
            var patients = ReceptionistUpdatePatientProfileMockData.GetPatientsList();
            SetupPatientRepo(patients);
            var request = ReceptionistUpdatePatientProfileMockData.GetValidRequest();
            request.PhoneNumber = null;
            request.Address = null;
            request.IdentityNumber = null;
            request.BhytNumber = null;

            // Act
            await _sut.Process(ReceptionistUpdatePatientProfileMockData.PatientId, request);

            // Assert
            var updatedEntity = patients.First(p => p.Id == ReceptionistUpdatePatientProfileMockData.PatientId);
            updatedEntity.PhoneNumber.Should().BeNull();
            updatedEntity.Address.Should().BeNull();
            updatedEntity.IdentityNumber.Should().BeNull();
            updatedEntity.BhytNumber.Should().BeNull();
        }

        [Fact]
        public async Task Process_SetsUpdatedAtToCurrentUtcTime()
        {
            // Arrange
            var patients = ReceptionistUpdatePatientProfileMockData.GetPatientsList();
            SetupPatientRepo(patients);
            var request = ReceptionistUpdatePatientProfileMockData.GetValidRequest();
            var before = DateTime.UtcNow;

            // Act
            var response = await _sut.Process(ReceptionistUpdatePatientProfileMockData.PatientId, request);
            var after = DateTime.UtcNow;

            // Assert
            var updatedEntity = patients.First(p => p.Id == ReceptionistUpdatePatientProfileMockData.PatientId);
            updatedEntity.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
            response.Data!.UpdatedAt.Should().Be(updatedEntity.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        [Fact]
        public async Task Process_CallsUpdateAsyncAndSaveChangesAsyncExactlyOnce()
        {
            // Arrange
            var patients = ReceptionistUpdatePatientProfileMockData.GetPatientsList();
            SetupPatientRepo(patients);
            var request = ReceptionistUpdatePatientProfileMockData.GetValidRequest();

            // Act
            await _sut.Process(ReceptionistUpdatePatientProfileMockData.PatientId, request);

            // Assert
            _patientRepoMock.Verify(
                r => r.UpdateAsync(It.Is<PatientProfile>(p => p.Id == ReceptionistUpdatePatientProfileMockData.PatientId)),
                Times.Once);
            _patientRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_PatientNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            SetupPatientRepo(new List<PatientProfile>());
            var request = ReceptionistUpdatePatientProfileMockData.GetValidRequest();

            // Act
            var act = () => _sut.Process(Guid.NewGuid(), request);

            // Assert
            var exception = await act.Should().ThrowAsync<KeyNotFoundException>();
            exception.Which.Message.Should().Be("APP_MESSAGE_4004");

            _patientRepoMock.Verify(r => r.UpdateAsync(It.IsAny<PatientProfile>()), Times.Never);
            _patientRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_DoesNotAffectOtherPatients()
        {
            // Arrange
            var patients = ReceptionistUpdatePatientProfileMockData.GetPatientsList();
            SetupPatientRepo(patients);
            var request = ReceptionistUpdatePatientProfileMockData.GetValidRequest();

            // Act
            await _sut.Process(ReceptionistUpdatePatientProfileMockData.PatientId, request);

            // Assert
            var otherEntity = patients.First(p => p.Id == ReceptionistUpdatePatientProfileMockData.OtherPatientId);
            otherEntity.FullName.Should().Be("Other Patient");
        }
    }
}