using System.Linq.Expressions;
using ECS.Application.Services.SystemAdminServices.AdminSystemCreateClinicAdminServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.SystemAdminServices.AdminSystemCreateClinicAdminServices
{
    /// <summary>
    /// Unit tests for <see cref="CreateClinicAdminService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class CreateClinicAdminServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>> _userRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<Clinic, Guid, AppDbContext>> _clinicRepoMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly CreateClinicAdminService _sut;

        public CreateClinicAdminServiceTests()
        {
            _sut = new CreateClinicAdminService(
                _userRepoMock.Object,
                _staffClinicRepoMock.Object,
                _clinicRepoMock.Object,
                _emailServiceMock.Object);
        }

        private void SetupClinicQuery(Clinic? clinic)
        {
            var rows = clinic == null ? new List<Clinic>() : new List<Clinic> { clinic };
            var dbSet = rows.BuildMockDbSet<Clinic>();
            _clinicRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()))
                .Returns(dbSet.Object);
        }

        private void SetupIdentityConflictQuery(bool hasConflict)
        {
            var rows = hasConflict
                ? new List<User> { SystemAdminAccountMockData.GetUserWithEmail("existing@ECS.vn") }
                : new List<User>();
            var dbSet = rows.BuildMockDbSet<User>();
            _userRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()))
                .Returns(dbSet.Object);
        }

        private void SetupPersistenceSuccess()
        {
            _userRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(Guid.NewGuid());
            _staffClinicRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<StaffClinic>()))
                .ReturnsAsync(Guid.NewGuid());
            _userRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);
            _emailServiceMock
                .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task Process_ClinicNotFound_Throws4008()
        {
            //Arrange 1
            var request = SystemAdminClinicMockData.GetValidCreateClinicAdminRequest();

            //Arrange 2
            SetupClinicQuery(null);

            //Act
            var act = () => _sut.Process(request);

            //Assert
            var exception = await act.Should().ThrowAsync<KeyNotFoundException>();
            exception.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4008.ToString());
            _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
            _staffClinicRepoMock.Verify(r => r.CreateAsync(It.IsAny<StaffClinic>()), Times.Never);
            _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Process_DuplicateIdentity_Throws4017()
        {
            //Arrange 1
            var request = SystemAdminClinicMockData.GetValidCreateClinicAdminRequest();
            var clinic = SystemAdminClinicMockData.GetClinicWithPublicationRequest();

            //Arrange 2
            SetupClinicQuery(clinic);
            SetupIdentityConflictQuery(hasConflict: true);

            //Act
            var act = () => _sut.Process(request);

            //Assert
            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4017.ToString());
            _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
            _staffClinicRepoMock.Verify(r => r.CreateAsync(It.IsAny<StaffClinic>()), Times.Never);
        }

        [Fact]
        public async Task Process_ValidRequest_Returns2000AndProvisionsClinicAdmin()
        {
            //Arrange 1
            var request = SystemAdminClinicMockData.GetValidCreateClinicAdminRequest();
            var clinic = SystemAdminClinicMockData.GetClinicWithPublicationRequest();

            //Arrange 2
            SetupClinicQuery(clinic);
            SetupIdentityConflictQuery(hasConflict: false);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.ClinicId.Should().Be(request.ClinicId);
            result.Data.Email.Should().Be(request.Email);
            result.Data.Role.Should().Be(UserRole.CLINIC_ADMIN.ToString());
            result.Data.UserId.Should().NotBe(Guid.Empty);

            _userRepoMock.Verify(r => r.CreateAsync(It.Is<User>(u =>
                u.Phone == request.Phone &&
                u.Email == request.Email &&
                u.FullName == request.FullName &&
                u.Role == UserRole.CLINIC_ADMIN &&
                u.IsActive)), Times.Once);
            _staffClinicRepoMock.Verify(r => r.CreateAsync(It.Is<StaffClinic>(sc =>
                sc.ClinicId == request.ClinicId &&
                sc.Role == StaffRole.CLINIC_ADMIN &&
                sc.IsActive)), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_ValidRequest_SendsWelcomeEmailWithCredentials()
        {
            //Arrange 1
            var request = SystemAdminClinicMockData.GetValidCreateClinicAdminRequest();
            var clinic = SystemAdminClinicMockData.GetClinicWithPublicationRequest();

            //Arrange 2
            SetupClinicQuery(clinic);
            SetupIdentityConflictQuery(hasConflict: false);
            SetupPersistenceSuccess();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            _emailServiceMock.Verify(e => e.SendEmailAsync(
                request.Email,
                "[ECS System] Welcome! Your Clinic Administrator Account is Ready",
                It.Is<string>(body =>
                    body.Contains(request.Email) &&
                    body.Contains(clinic.Name) &&
                    body.Contains(request.FullName))), Times.Once);
        }
    }
}
