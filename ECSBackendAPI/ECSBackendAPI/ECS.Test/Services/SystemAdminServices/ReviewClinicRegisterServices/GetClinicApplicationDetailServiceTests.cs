using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECS.Application.Services.SystemAdminServices.ReviewClinicRegisterServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Moq;
using Xunit;

namespace ECS.Test.Services.SystemAdminServices.ReviewClinicRegisterServices
{
    /// <summary>
    /// Unit tests for <see cref="GetClinicApplicationDetailService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class GetClinicApplicationDetailServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<ClinicRegistrationRequest, Guid, AppDbContext>> _queryRepoMock = new();
        private readonly GetClinicApplicationDetailService _sut;

        public GetClinicApplicationDetailServiceTests()
        {
            _sut = new GetClinicApplicationDetailService(_queryRepoMock.Object);
        }

        [Fact]
        public async Task Process_ApplicationNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var applicationId = GetClinicApplicationDetailMockData.NotFoundApplicationId;

            _queryRepoMock
                .Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync((ClinicRegistrationRequest?)null);

            // Act
            Func<Task> act = async () => await _sut.Process(applicationId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4029.ToString());

            _queryRepoMock.Verify(r => r.GetByIdAsync(applicationId), Times.Once);
        }

        [Fact]
        public async Task Process_ValidApplicationId_ReturnsSuccessWithMappedData()
        {
            // Arrange
            var applicationId = GetClinicApplicationDetailMockData.ValidApplicationId;
            var application = GetClinicApplicationDetailMockData.GetValidApplication(applicationId);

            _queryRepoMock
                .Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);

            // Act
            var result = await _sut.Process(applicationId);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());

            result.Data.Should().NotBeNull();
            result.Data!.Id_clinic_registration.Should().Be(application.Id.ToString());
            result.Data.ClinicName.Should().Be(application.ClinicName);
            result.Data.ClinicAddress.Should().Be(application.ClinicAddress);
            result.Data.ContactName.Should().Be(application.ContactName);
            result.Data.ContactPhone.Should().Be(application.ContactPhone);
            result.Data.ContactEmail.Should().Be(application.ContactEmail);
            result.Data.BusinessLicenseUrl.Should().Be(application.BusinessLicenseUrl);
            result.Data.Status.Should().Be(application.Status);
            result.Data.ReviewNote.Should().Be(application.ReviewNote);
            result.Data.RequestedAt.Should().Be("20/05/2025 14:30");

            _queryRepoMock.Verify(r => r.GetByIdAsync(applicationId), Times.Once);
        }
    }
}