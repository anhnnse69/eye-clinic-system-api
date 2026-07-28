using ECS.Application.Services.SystemAdminServices.AdminSystemUpdateClinicServices;
using ECS.Domain.Entities.Clinics;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Moq;
using Xunit;

namespace ECS.Test.Services.SystemAdminServices.AdminSystemUpdateClinicServices
{
    /// <summary>
    /// Unit tests for <see cref="UpdateClinicService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult]
    /// Target: 100% Line & Branch Coverage
    /// </summary>
    public class AdminSystemUpdateClinicServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<Clinic, Guid, AppDbContext>> _repositoryMock = new();
        private readonly UpdateClinicService _sut;

        public AdminSystemUpdateClinicServiceTests()
        {
            _sut = new UpdateClinicService(_repositoryMock.Object);
        }

        [Fact]
        public async Task Process_ClinicNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var clinicId = SystemAdminClinicMockData.NotFoundClinicId;
            var request = SystemAdminClinicMockData.GetValidUpdateClinicRequest();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(clinicId))
                .ReturnsAsync((Clinic?)null);

            // Act
            Func<Task> act = async () => await _sut.Process(clinicId, request);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Clinic with ID {clinicId} not found.");

            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Clinic>()), Times.Never);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ActiveClinicWithFullDetails_ReturnsSuccessResponseAndActiveStatus()
        {
            // Arrange
            var clinicId = SystemAdminClinicMockData.ClinicId;
            var existingClinic = SystemAdminClinicMockData.GetClinicWithFullDetails();
            var request = SystemAdminClinicMockData.GetValidUpdateClinicRequest();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(clinicId))
                .ReturnsAsync(existingClinic);

            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Clinic>()))
                .Returns(Task.CompletedTask);

            _repositoryMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _sut.Process(clinicId, request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data.Should().NotBeNull();
            result.Data!.Id_clinic.Should().Be(clinicId.ToString());
            result.Data.ClinicName.Should().Be("Updated Eye Clinic Name"); // Verifies Trim()
            result.Data.Status.Should().Be("ACTIVE");
            result.Data.UpdatedAt.Should().NotBeNullOrWhiteSpace();

            // Verify entity mutation in detail
            existingClinic.Name.Should().Be("Updated Eye Clinic Name");
            existingClinic.Address.Should().Be("999 New Street, District 1, HCMC");
            existingClinic.Phone.Should().Be("0988776655");
            existingClinic.Email.Should().Be("updated@saigoneye.vn");
            existingClinic.LogoUrl.Should().Be("https://example.com/new-logo.png");
            existingClinic.Description.Should().Be("Updated description for eye care center.");

            _repositoryMock.Verify(r => r.UpdateAsync(existingClinic), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_InactiveClinicWithNullOptionalFields_ReturnsInactiveStatusAndNullFields()
        {
            // Arrange
            var clinicId = SystemAdminClinicMockData.SecondaryClinicId;
            var existingClinic = SystemAdminClinicMockData.GetInactiveClinic();
            var request = SystemAdminClinicMockData.GetUpdateClinicRequestWithNulls();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(clinicId))
                .ReturnsAsync(existingClinic);

            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Clinic>()))
                .Returns(Task.CompletedTask);

            _repositoryMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _sut.Process(clinicId, request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data.Should().NotBeNull();
            result.Data!.Status.Should().Be("INACTIVE");

            // Verifies null-conditional trimming (Email?.Trim() & Description?.Trim())
            existingClinic.Email.Should().BeNull();
            existingClinic.Description.Should().BeNull();

            _repositoryMock.Verify(r => r.UpdateAsync(existingClinic), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}