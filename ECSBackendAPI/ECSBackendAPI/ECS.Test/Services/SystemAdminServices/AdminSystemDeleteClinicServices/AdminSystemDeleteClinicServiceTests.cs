using ECS.Application.Services.SystemAdminServices.AdminSystemDeleteClinicServices;
using ECS.Domain.Entities.Clinics;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Moq;

namespace ECS.Test.Services.SystemAdminServices.AdminSystemDeleteClinicServices
{
    /// <summary>
    /// Unit tests for <see cref="AdminSystemDeleteClinicService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class AdminSystemDeleteClinicServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<Clinic, Guid, AppDbContext>> _clinicRepoMock = new();
        private readonly AdminSystemDeleteClinicService _sut;

        public AdminSystemDeleteClinicServiceTests()
        {
            _sut = new AdminSystemDeleteClinicService(_clinicRepoMock.Object);
        }

        private void SetupGetById(Clinic? clinic)
        {
            _clinicRepoMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(clinic);
        }

        private void SetupPersistenceSuccess()
        {
            _clinicRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<Clinic>()))
                .Returns(Task.CompletedTask);
            _clinicRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);
        }

        [Fact]
        public async Task Process_ClinicNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var clinicId = SystemAdminClinicMockData.ClinicId;

            //Arrange 2
            SetupGetById(null);

            //Act
            var act = () => _sut.Process(clinicId);

            //Assert
            var exception = await act.Should().ThrowAsync<KeyNotFoundException>();
            exception.Which.Message.Should().Be($"Clinic with ID {clinicId} not found.");
            _clinicRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Clinic>()), Times.Never);
            _clinicRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ValidClinic_Returns2000AndSetsInactive()
        {
            //Arrange 1
            var clinic = SystemAdminClinicMockData.GetClinicWithPublicationRequest();

            //Arrange 2
            SetupGetById(clinic);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(clinic.Id);

            //Assert
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data.Should().NotBeNull();
            result.Data!.Id_clinic.Should().Be(clinic.Id.ToString());
            result.Data.ClinicName.Should().Be(clinic.Name);
            result.Data.Status.Should().Be("INACTIVE");
            result.Data.UpdatedAt.Should().NotBeNullOrWhiteSpace();
            clinic.IsActive.Should().BeFalse();
            _clinicRepoMock.Verify(r => r.UpdateAsync(clinic), Times.Once);
            _clinicRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_ValidClinic_UsesProvidedClinicId()
        {
            //Arrange 1
            var clinic = SystemAdminClinicMockData.GetClinicWithPublicationRequest();
            var requestedId = clinic.Id;

            //Arrange 2
            SetupGetById(clinic);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(requestedId);

            //Assert
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            _clinicRepoMock.Verify(r => r.GetByIdAsync(requestedId), Times.Once);
        }
    }
}
