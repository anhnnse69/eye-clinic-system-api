using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicProfileServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicProfileServices
{
    public class ViewClinicServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _clinicRepositoryMock = new();
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepositoryMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly ViewClinicService _sut;

        public ViewClinicServiceTests()
        {
            _sut = new ViewClinicService(
                _clinicRepositoryMock.Object,
                _staffClinicRepositoryMock.Object,
                _httpContextAccessorMock.Object);
        }

        private void SetupHttpContextClaim(string? claimValue)
        {
            var context = new DefaultHttpContext();
            if (claimValue != null)
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, claimValue) },
                    "TestAuth"));
            }

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
        }

        private void SetupStaffClinicRepository(StaffClinic? staffClinic)
        {
            var rows = staffClinic == null
                ? new List<StaffClinic>()
                : new List<StaffClinic> { staffClinic };
            var queryable = rows.BuildMockDbSet<StaffClinic>();

            _staffClinicRepositoryMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupClinicRepository(Clinic? clinic)
        {
            var rows = clinic == null
                ? new List<Clinic>()
                : new List<Clinic> { clinic };
            var queryable = rows.BuildMockDbSet<Clinic>();

            _clinicRepositoryMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewClinicRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _clinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_MissingNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewClinicRequest();

            //Arrange 2
            SetupHttpContextClaim(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _clinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_MalformedNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewClinicRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _clinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_ValidUserWithoutStaffClinic_Returns4020ClinicNotFoundError()
        {
            //Arrange 1
            var request = new ViewClinicRequest();

            //Arrange 2
            SetupHttpContextClaim(ClinicProfileMockData.UserId.ToString());
            SetupStaffClinicRepository(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _clinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_ValidUserWithMissingClinic_Returns4020ClinicNotFoundError()
        {
            //Arrange 1
            var request = new ViewClinicRequest();
            var staffClinic = ClinicProfileMockData.GetActiveStaffClinic();

            //Arrange 2
            SetupHttpContextClaim(ClinicProfileMockData.UserId.ToString());
            SetupStaffClinicRepository(staffClinic);
            SetupClinicRepository(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _clinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Process_ValidUserWithActiveClinic_Returns2000WithMappedClinicProfile()
        {
            //Arrange 1
            var request = new ViewClinicRequest();
            var staffClinic = ClinicProfileMockData.GetActiveStaffClinic();
            var clinic = ClinicProfileMockData.GetActiveClinic();

            //Arrange 2
            SetupHttpContextClaim(ClinicProfileMockData.UserId.ToString());
            SetupStaffClinicRepository(staffClinic);
            SetupClinicRepository(clinic);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(new ViewClinicResponse
            {
                Id = clinic.Id,
                Name = clinic.Name,
                Address = clinic.Address,
                Phone = clinic.Phone,
                Email = clinic.Email,
                LogoUrl = clinic.LogoUrl,
                Description = clinic.Description,
                IsActive = clinic.IsActive,
                RatingAvg = clinic.RatingAvg,
                ReviewCount = clinic.ReviewCount,
                OpenTime = clinic.OpenTime,
                CloseTime = clinic.CloseTime,
                IsPublished = clinic.IsPublished,
                IsPublicationRequested = clinic.IsPublicationRequested,
                PublicationRequestedAt = clinic.PublicationRequestedAt
            });
            _staffClinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _clinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Process_ValidUser_UsesActiveStaffClinicPredicate()
        {
            //Arrange 1
            var request = new ViewClinicRequest();
            var staffClinic = ClinicProfileMockData.GetActiveStaffClinic();
            var clinic = ClinicProfileMockData.GetActiveClinic();
            Expression<Func<StaffClinic, bool>>? capturedPredicate = null;
            var staffRows = new List<StaffClinic> { staffClinic }.BuildMockDbSet<StaffClinic>();

            //Arrange 2
            SetupHttpContextClaim(ClinicProfileMockData.UserId.ToString());
            _staffClinicRepositoryMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>()))
                .Callback<Expression<Func<StaffClinic, bool>>, bool>((predicate, _) => capturedPredicate = predicate)
                .Returns(staffRows.Object);
            SetupClinicRepository(clinic);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            capturedPredicate.Should().NotBeNull();
            var predicate = capturedPredicate!.Compile();
            predicate(staffClinic).Should().BeTrue();
            predicate(new StaffClinic
            {
                UserId = Guid.NewGuid(),
                ClinicId = staffClinic.ClinicId,
                IsActive = true
            }).Should().BeFalse();
            predicate(new StaffClinic
            {
                UserId = staffClinic.UserId,
                ClinicId = staffClinic.ClinicId,
                IsActive = false
            }).Should().BeFalse();
            _staffClinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Process_ValidClinicId_UsesActiveClinicPredicate()
        {
            //Arrange 1
            var request = new ViewClinicRequest();
            var staffClinic = ClinicProfileMockData.GetActiveStaffClinic();
            var clinic = ClinicProfileMockData.GetActiveClinic();
            Expression<Func<Clinic, bool>>? capturedPredicate = null;
            var clinicRows = new List<Clinic> { clinic }.BuildMockDbSet<Clinic>();

            //Arrange 2
            SetupHttpContextClaim(ClinicProfileMockData.UserId.ToString());
            SetupStaffClinicRepository(staffClinic);
            _clinicRepositoryMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()))
                .Callback<Expression<Func<Clinic, bool>>, bool>((predicate, _) => capturedPredicate = predicate)
                .Returns(clinicRows.Object);

            //Act
            var result = await _sut.Process(request);

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
                IsActive = true
            }).Should().BeFalse();
            predicate(new Clinic
            {
                Id = clinic.Id,
                Name = clinic.Name,
                Address = clinic.Address,
                Phone = clinic.Phone,
                IsActive = false
            }).Should().BeFalse();
            _clinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }
    }
}
