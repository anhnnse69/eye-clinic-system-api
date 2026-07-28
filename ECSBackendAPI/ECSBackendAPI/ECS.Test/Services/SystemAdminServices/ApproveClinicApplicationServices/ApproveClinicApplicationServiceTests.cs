using System.Linq.Expressions;
using ECS.Application.Services.SystemAdminServices.ApproveClinicApplicationServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.SystemAdminServices.ApproveClinicApplicationServices
{
    /// <summary>
    /// Unit tests for <see cref="ApproveClinicApplicationService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class ApproveClinicApplicationServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<ClinicRegistrationRequest, Guid, AppDbContext>> _requestRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<Clinic, Guid, AppDbContext>> _clinicRepoMock = new();
        private readonly ApproveClinicApplicationService _sut;

        public ApproveClinicApplicationServiceTests()
        {
            _sut = new ApproveClinicApplicationService(
                _requestRepoMock.Object,
                _clinicRepoMock.Object);
        }

        private void SetupApplicationQuery(ClinicRegistrationRequest? application)
        {
            var rows = application == null
                ? new List<ClinicRegistrationRequest>()
                : new List<ClinicRegistrationRequest> { application };
            var dbSet = rows.BuildMockDbSet<ClinicRegistrationRequest>();
            _requestRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<ClinicRegistrationRequest, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(dbSet.Object);
        }

        private void SetupPersistenceSuccess()
        {
            _requestRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<ClinicRegistrationRequest>()))
                .Returns(Task.CompletedTask);
            _clinicRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<Clinic>()))
                .ReturnsAsync(Guid.NewGuid());
            _requestRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);
        }

        [Fact]
        public async Task Process_ApplicationNotFound_Throws4029()
        {
            //Arrange 1
            var applicationId = SystemAdminClinicMockData.ApplicationId;
            var adminId = SystemAdminClinicMockData.AdminUserId;

            //Arrange 2
            SetupApplicationQuery(null);

            //Act
            var act = () => _sut.Process(applicationId, adminId);

            //Assert
            var exception = await act.Should().ThrowAsync<KeyNotFoundException>();
            exception.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4029.ToString());
            _clinicRepoMock.Verify(r => r.CreateAsync(It.IsAny<Clinic>()), Times.Never);
            _requestRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ApplicationAlreadyApproved_Throws4030()
        {
            //Arrange 1
            var applicationId = SystemAdminClinicMockData.ApplicationId;
            var adminId = SystemAdminClinicMockData.AdminUserId;
            var application = SystemAdminClinicMockData.GetApprovedApplication();

            //Arrange 2
            SetupApplicationQuery(application);

            //Act
            var act = () => _sut.Process(applicationId, adminId);

            //Assert
            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4030.ToString());
            _clinicRepoMock.Verify(r => r.CreateAsync(It.IsAny<Clinic>()), Times.Never);
            _requestRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ApplicationAlreadyRejected_Throws4030()
        {
            //Arrange 1
            var applicationId = SystemAdminClinicMockData.ApplicationId;
            var adminId = SystemAdminClinicMockData.AdminUserId;
            var application = SystemAdminClinicMockData.GetRejectedApplication();

            //Arrange 2
            SetupApplicationQuery(application);

            //Act
            var act = () => _sut.Process(applicationId, adminId);

            //Assert
            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4030.ToString());
        }

        [Fact]
        public async Task Process_ValidPendingApplication_Returns2000AndProvisionsClinic()
        {
            //Arrange 1
            var applicationId = SystemAdminClinicMockData.ApplicationId;
            var adminId = SystemAdminClinicMockData.AdminUserId;
            var application = SystemAdminClinicMockData.GetPendingApplication();

            //Arrange 2
            SetupApplicationQuery(application);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(applicationId, adminId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeTrue();
            application.Status.Should().Be("APPROVED");
            application.ReviewedBy.Should().Be(adminId);
            application.ReviewedAt.Should().NotBeNull();
            application.ReviewNote.Should().BeNull();
            _requestRepoMock.Verify(r => r.UpdateAsync(application), Times.Once);
            _clinicRepoMock.Verify(r => r.CreateAsync(It.Is<Clinic>(c =>
                c.Name == application.ClinicName &&
                c.Address == application.ClinicAddress &&
                c.IsActive)), Times.Once);
            _requestRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_ValidPendingApplication_UsesApplicationIdPredicate()
        {
            //Arrange 1
            var applicationId = SystemAdminClinicMockData.ApplicationId;
            var adminId = SystemAdminClinicMockData.AdminUserId;
            var application = SystemAdminClinicMockData.GetPendingApplication();
            Expression<Func<ClinicRegistrationRequest, bool>>? capturedPredicate = null;
            var dbSet = new List<ClinicRegistrationRequest> { application }.BuildMockDbSet<ClinicRegistrationRequest>();

            //Arrange 2
            _requestRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<ClinicRegistrationRequest, bool>>>(),
                    It.IsAny<bool>()))
                .Callback<Expression<Func<ClinicRegistrationRequest, bool>>, bool>((predicate, _) =>
                    capturedPredicate = predicate)
                .Returns(dbSet.Object);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(applicationId, adminId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            capturedPredicate.Should().NotBeNull();
            var predicate = capturedPredicate!.Compile();
            predicate(application).Should().BeTrue();
            predicate(new ClinicRegistrationRequest
            {
                Id = Guid.NewGuid(),
                ClinicName = "Other",
                ClinicAddress = "Other",
                ContactName = "Other",
                ContactPhone = "0900000000",
                ContactEmail = "other@example.com",
                Status = "PENDING"
            }).Should().BeFalse();
        }
    }
}
