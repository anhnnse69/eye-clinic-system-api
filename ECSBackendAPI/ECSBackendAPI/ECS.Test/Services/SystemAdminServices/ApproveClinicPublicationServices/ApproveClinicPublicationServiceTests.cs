using System.Linq.Expressions;
using ECS.Application.Services.SystemAdminServices.ApproveClinicPublicationServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.SystemAdminServices.ApproveClinicPublicationServices
{
    /// <summary>
    /// Unit tests for <see cref="ApproveClinicPublicationService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class ApproveClinicPublicationServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _queryRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<Clinic, Guid, AppDbContext>> _commandRepoMock = new();
        private readonly ApproveClinicPublicationService _sut;

        public ApproveClinicPublicationServiceTests()
        {
            _sut = new ApproveClinicPublicationService(
                _queryRepoMock.Object,
                _commandRepoMock.Object);
        }

        private void SetupClinicQuery(Clinic? clinic)
        {
            var rows = clinic == null ? new List<Clinic>() : new List<Clinic> { clinic };
            var dbSet = rows.BuildMockDbSet<Clinic>();
            _queryRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(dbSet.Object);
        }

        private void SetupPersistenceSuccess()
        {
            _commandRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);
        }

        [Fact]
        public async Task Process_ClinicNotFound_Throws4004()
        {
            //Arrange 1
            var clinicId = SystemAdminClinicMockData.ClinicId;
            var adminId = SystemAdminClinicMockData.AdminUserId;

            //Arrange 2
            SetupClinicQuery(null);

            //Act
            var act = () => _sut.Process(clinicId, adminId);

            //Assert
            var exception = await act.Should().ThrowAsync<KeyNotFoundException>();
            exception.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4004.ToString());
            _commandRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ClinicWithoutPublicationRequest_Throws4000()
        {
            //Arrange 1
            var clinicId = SystemAdminClinicMockData.ClinicId;
            var adminId = SystemAdminClinicMockData.AdminUserId;
            var clinic = SystemAdminClinicMockData.GetClinicWithoutPublicationRequest();

            //Arrange 2
            SetupClinicQuery(clinic);

            //Act
            var act = () => _sut.Process(clinicId, adminId);

            //Assert
            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4000.ToString());
            _commandRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_AlreadyPublishedClinicWithoutRequest_Throws4000()
        {
            //Arrange 1
            var clinicId = SystemAdminClinicMockData.ClinicId;
            var adminId = SystemAdminClinicMockData.AdminUserId;
            var clinic = SystemAdminClinicMockData.GetPublishedClinic();

            //Arrange 2
            SetupClinicQuery(clinic);

            //Act
            var act = () => _sut.Process(clinicId, adminId);

            //Assert
            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4000.ToString());
        }

        [Fact]
        public async Task Process_ValidPublicationRequest_Returns2000AndPublishesClinic()
        {
            //Arrange 1
            var clinicId = SystemAdminClinicMockData.ClinicId;
            var adminId = SystemAdminClinicMockData.AdminUserId;
            var clinic = SystemAdminClinicMockData.GetClinicWithPublicationRequest();

            //Arrange 2
            SetupClinicQuery(clinic);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(clinicId, adminId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeTrue();
            clinic.IsPublished.Should().BeTrue();
            clinic.IsPublicationRequested.Should().BeFalse();
            clinic.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
            _commandRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_ValidPublicationRequest_UsesClinicIdPredicate()
        {
            //Arrange 1
            var clinicId = SystemAdminClinicMockData.ClinicId;
            var adminId = SystemAdminClinicMockData.AdminUserId;
            var clinic = SystemAdminClinicMockData.GetClinicWithPublicationRequest();
            Expression<Func<Clinic, bool>>? capturedPredicate = null;
            var dbSet = new List<Clinic> { clinic }.BuildMockDbSet<Clinic>();

            //Arrange 2
            _queryRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()))
                .Callback<Expression<Func<Clinic, bool>>, bool>((predicate, _) =>
                    capturedPredicate = predicate)
                .Returns(dbSet.Object);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(clinicId, adminId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            capturedPredicate.Should().NotBeNull();
            var predicate = capturedPredicate!.Compile();
            predicate(clinic).Should().BeTrue();
            predicate(new Clinic
            {
                Id = Guid.NewGuid(),
                Name = "Other Clinic",
                Address = "Other Address",
                Phone = "000",
                IsActive = true,
                IsPublicationRequested = true
            }).Should().BeFalse();
        }
    }
}
