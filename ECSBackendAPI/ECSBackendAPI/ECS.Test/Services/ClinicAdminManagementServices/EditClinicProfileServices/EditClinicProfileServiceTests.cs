using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.EditClinicProfileServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace ECS.Test.Services.ClinicAdminManagementServices.EditClinicProfileServices
{
    public class EditClinicProfileServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<Clinic, Guid, AppDbContext>> _clinicRepositoryMock = new();
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepositoryMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly EditClinicProfileService _sut;

        public EditClinicProfileServiceTests()
        {
            _sut = new EditClinicProfileService(
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
        public async Task Process_NullHttpContext_ReturnsFail4001()
        {
            //Arrange 1
            var request = EditClinicProfileMockData.GetValidRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeFalse();
            _staffClinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _clinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_InvalidUserIdClaim_ReturnsFail4001()
        {
            //Arrange 1
            var request = EditClinicProfileMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextClaim("invalid-guid-format");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeFalse();
            _staffClinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _clinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_MissingNameIdentifierClaim_ReturnsFail4001()
        {
            //Arrange 1
            var request = EditClinicProfileMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextClaim(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeFalse();
            _staffClinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _clinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_StaffClinicNotFound_ReturnsFail4020()
        {
            //Arrange 1
            var request = EditClinicProfileMockData.GetValidRequest();
            var userId = EditClinicProfileMockData.ValidUserId;

            //Arrange 2
            SetupHttpContextClaim(userId.ToString());
            SetupStaffClinicRepository(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeFalse();
            _clinicRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _clinicRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Clinic>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_ClinicNotFound_ReturnsFail4020()
        {
            //Arrange 1
            var request = EditClinicProfileMockData.GetValidRequest();
            var userId = EditClinicProfileMockData.ValidUserId;
            var staffClinic = EditClinicProfileMockData.GetActiveStaffClinic();

            //Arrange 2
            SetupHttpContextClaim(userId.ToString());
            SetupStaffClinicRepository(staffClinic);
            SetupClinicRepository(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeFalse();
            _clinicRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Clinic>()),
                Times.Never);
            _clinicRepositoryMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task Process_ValidRequest_UpdatesClinicAndReturnsSuccess()
        {
            //Arrange 1
            var request = EditClinicProfileMockData.GetValidRequest();
            var userId = EditClinicProfileMockData.ValidUserId;
            var staffClinic = EditClinicProfileMockData.GetActiveStaffClinic();
            var clinic = EditClinicProfileMockData.GetActiveClinic();

            //Arrange 2
            SetupHttpContextClaim(userId.ToString());
            SetupStaffClinicRepository(staffClinic);
            SetupClinicRepository(clinic);
            _clinicRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Clinic>()))
                .Returns(Task.CompletedTask);
            _clinicRepositoryMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeTrue();
            clinic.Name.Should().Be(request.Name);
            clinic.Address.Should().Be(request.Address);
            clinic.Phone.Should().Be(request.Phone);
            clinic.Email.Should().Be(request.Email);
            clinic.LogoUrl.Should().Be(request.LogoUrl);
            clinic.Description.Should().Be(request.Description);
            clinic.OpenTime.Should().Be(request.OpenTime);
            clinic.CloseTime.Should().Be(request.CloseTime);
            _clinicRepositoryMock.Verify(x => x.UpdateAsync(clinic), Times.Once);
            _clinicRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }
    }
}
