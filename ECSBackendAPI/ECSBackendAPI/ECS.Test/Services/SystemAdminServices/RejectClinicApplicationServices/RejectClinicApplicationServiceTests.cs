using System.Linq.Expressions;
using ECS.Application.Services.SystemAdminServices.RejectClinicApplicationServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.SystemAdminServices.RejectClinicApplicationServices
{
    /// <summary>
    /// Unit tests for <see cref="RejectClinicApplicationService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class RejectClinicApplicationServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<ClinicRegistrationRequest, Guid, AppDbContext>> _repositoryMock = new();
        private readonly RejectClinicApplicationService _sut;

        public RejectClinicApplicationServiceTests()
        {
            _sut = new RejectClinicApplicationService(_repositoryMock.Object);
        }

        private void SetupQuery(ClinicRegistrationRequest? application)
        {
            var rows = application == null
                ? new List<ClinicRegistrationRequest>()
                : new List<ClinicRegistrationRequest> { application };

            var mockDbSet = rows.BuildMockDbSet();

            _repositoryMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<ClinicRegistrationRequest, bool>>>(), It.IsAny<bool>()))
                .Returns(mockDbSet.Object);
        }

        private void SetupPersistenceSuccess()
        {
            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<ClinicRegistrationRequest>()))
                .Returns(Task.CompletedTask);

            _repositoryMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Process_InvalidReviewNote_ThrowsArgumentException(string? invalidNote)
        {
            // Arrange
            var id = RejectClinicApplicationMockData.ApplicationId;
            var adminId = RejectClinicApplicationMockData.AdminId;
            var request = new RejectClinicApplicationRequest { ReviewNote = invalidNote! };

            // Act
            var act = () => _sut.Process(id, request, adminId);

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4003.ToString());

            _repositoryMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<ClinicRegistrationRequest, bool>>>(), It.IsAny<bool>()), Times.Never);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ClinicRegistrationRequest>()), Times.Never);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ApplicationNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id = RejectClinicApplicationMockData.ApplicationId;
            var adminId = RejectClinicApplicationMockData.AdminId;
            var request = RejectClinicApplicationMockData.GetValidRequest();

            SetupQuery(application: null);

            // Act
            var act = () => _sut.Process(id, request, adminId);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4029.ToString());

            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ClinicRegistrationRequest>()), Times.Never);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Theory]
        [InlineData("APPROVED")]
        [InlineData("REJECTED")]
        public async Task Process_ApplicationStatusNotPending_ThrowsInvalidOperationException(string nonPendingStatus)
        {
            // Arrange
            var id = RejectClinicApplicationMockData.ApplicationId;
            var adminId = RejectClinicApplicationMockData.AdminId;
            var request = RejectClinicApplicationMockData.GetValidRequest();
            var application = RejectClinicApplicationMockData.GetNonPendingApplication(id, nonPendingStatus);

            SetupQuery(application);

            // Act
            var act = () => _sut.Process(id, request, adminId);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4030.ToString());

            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ClinicRegistrationRequest>()), Times.Never);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ValidRequest_UpdatesStateAndReturnsSuccess()
        {
            // Arrange
            var id = RejectClinicApplicationMockData.ApplicationId;
            var adminId = RejectClinicApplicationMockData.AdminId;
            var request = RejectClinicApplicationMockData.GetValidRequest();
            var application = RejectClinicApplicationMockData.GetPendingApplication(id);

            SetupQuery(application);
            SetupPersistenceSuccess();

            // Act
            var result = await _sut.Process(id, request, adminId);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeTrue();

            application.Status.Should().Be("REJECTED");
            application.ReviewNote.Should().Be(request.ReviewNote);
            application.ReviewedBy.Should().Be(adminId);
            application.ReviewedAt.Should().NotBeNull();

            _repositoryMock.Verify(r => r.UpdateAsync(application), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}