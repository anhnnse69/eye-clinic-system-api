using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.RequestPublishClinicServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ClinicAdminManagementServices.RequestPublishClinicServices
{
    /// <summary>
    /// Unit tests for <see cref="RequestPublishClinicService"/>.
    /// Pattern: [Method]_[State]_[Outcome]
    /// </summary>
    public class RequestPublishClinicServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<Clinic, Guid, AppDbContext>> _clinicRepositoryMock;
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepositoryMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly RequestPublishClinicService _service;

        public RequestPublishClinicServiceTests()
        {
            _clinicRepositoryMock = new Mock<IRepositoryBaseAsync<Clinic, Guid, AppDbContext>>();
            _staffClinicRepositoryMock = new Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            _service = new RequestPublishClinicService(
                _clinicRepositoryMock.Object,
                _staffClinicRepositoryMock.Object,
                _httpContextAccessorMock.Object);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SetupHttpContext(string? userIdClaim = null, bool setNullContext = false)
        {
            if (setNullContext)
            {
                _httpContextAccessorMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);
                return;
            }

            var claims = new List<Claim>();
            if (userIdClaim != null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdClaim));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(httpContext);
        }

        private void SetupStaffClinicRepo(StaffClinic? staffClinic)
        {
            var list = staffClinic != null ? new List<StaffClinic> { staffClinic } : new List<StaffClinic>();
            var mockQueryable = list.BuildMockDbSet<StaffClinic>();

            _staffClinicRepositoryMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        private void SetupClinicRepo(Clinic? clinic)
        {
            var list = clinic != null ? new List<Clinic> { clinic } : new List<Clinic>();
            var mockQueryable = list.BuildMockDbSet<Clinic>();

            _clinicRepositoryMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        // ── Test Cases ────────────────────────────────────────────────────────────

        /// <summary>
        /// TC-01: HttpContext is null -> returns 4033 error.
        /// </summary>
        [Fact]
        public async Task Process_HttpContextNull_ReturnsFailForbidden4033()
        {
            //Arrange 1
            var request = RequestPublishClinicMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContext(setNullContext: true);
            SetupStaffClinicRepo(null);
            SetupClinicRepo(null);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeFalse();
            _clinicRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Clinic>()), Times.Never);
            _clinicRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        /// <summary>
        /// TC-01b: HttpContext.User is null -> returns 4033 error.
        /// </summary>
        [Fact]
        public async Task Process_HttpContextUserNull_ReturnsFailForbidden4033()
        {
            //Arrange 1
            var request = RequestPublishClinicMockData.GetValidRequest();

            //Arrange 2
            var httpContext = new DefaultHttpContext { User = null! };
            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(httpContext);
            SetupStaffClinicRepo(null);
            SetupClinicRepo(null);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeFalse();
            _clinicRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Clinic>()), Times.Never);
        }

        /// <summary>
        /// TC-02: ClaimTypes.NameIdentifier is missing -> returns 4033 error.
        /// </summary>
        [Fact]
        public async Task Process_UserClaimMissing_ReturnsFailForbidden4033()
        {
            //Arrange 1
            var request = RequestPublishClinicMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContext(userIdClaim: null);
            SetupStaffClinicRepo(null);
            SetupClinicRepo(null);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeFalse();
            _clinicRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Clinic>()), Times.Never);
        }

        /// <summary>
        /// TC-03: ClaimTypes.NameIdentifier is not a valid Guid -> returns 4033 error.
        /// </summary>
        [Fact]
        public async Task Process_UserClaimInvalidGuid_ReturnsFailForbidden4033()
        {
            //Arrange 1
            var request = RequestPublishClinicMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContext(userIdClaim: "not-a-valid-guid");
            SetupStaffClinicRepo(null);
            SetupClinicRepo(null);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeFalse();
            _clinicRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Clinic>()), Times.Never);
        }

        /// <summary>
        /// TC-04: Authenticated user has no StaffClinic record -> returns 4034 error.
        /// </summary>
        [Fact]
        public async Task Process_StaffClinicNotFound_ReturnsFailNotFound4034()
        {
            //Arrange 1
            var request = RequestPublishClinicMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContext(userIdClaim: RequestPublishClinicMockData.ValidUserId.ToString());
            SetupStaffClinicRepo(null);
            SetupClinicRepo(null);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4034.ToString());
            result.Data.Should().BeFalse();
            _clinicRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Clinic>()), Times.Never);
        }

        /// <summary>
        /// TC-05: StaffClinic exists, but Clinic record is not found -> returns 4034 error.
        /// </summary>
        [Fact]
        public async Task Process_ClinicNotFound_ReturnsFailNotFound4034()
        {
            //Arrange 1
            var request = RequestPublishClinicMockData.GetValidRequest();
            var staffClinic = RequestPublishClinicMockData.GetValidStaffClinic();

            //Arrange 2
            SetupHttpContext(userIdClaim: RequestPublishClinicMockData.ValidUserId.ToString());
            SetupStaffClinicRepo(staffClinic);
            SetupClinicRepo(null);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4034.ToString());
            result.Data.Should().BeFalse();
            _clinicRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Clinic>()), Times.Never);
        }

        /// <summary>
        /// TC-06: Clinic ID does not match StaffClinic.ClinicId -> returns 4058 error.
        /// </summary>
        [Fact]
        public async Task Process_UnauthorizedClinic_ReturnsFailForbidden4058()
        {
            //Arrange 1
            var request = RequestPublishClinicMockData.GetValidRequest();
            var mismatchedClinicId = Guid.NewGuid();
            var staffClinic = RequestPublishClinicMockData.GetValidStaffClinic(clinicId: mismatchedClinicId);
            var clinic = RequestPublishClinicMockData.GetValidClinic(clinicId: RequestPublishClinicMockData.ValidClinicId);

            //Arrange 2
            SetupHttpContext(userIdClaim: RequestPublishClinicMockData.ValidUserId.ToString());
            SetupStaffClinicRepo(staffClinic);
            SetupClinicRepo(clinic);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4058.ToString());
            result.Data.Should().BeFalse();
            _clinicRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Clinic>()), Times.Never);
        }

        /// <summary>
        /// TC-07: Clinic is already published -> returns 4059 error.
        /// </summary>
        [Fact]
        public async Task Process_ClinicAlreadyPublished_ReturnsFailConflict4059()
        {
            //Arrange 1
            var request = RequestPublishClinicMockData.GetValidRequest();
            var staffClinic = RequestPublishClinicMockData.GetValidStaffClinic();
            var clinic = RequestPublishClinicMockData.GetValidClinic(isPublished: true, isPublicationRequested: false);

            //Arrange 2
            SetupHttpContext(userIdClaim: RequestPublishClinicMockData.ValidUserId.ToString());
            SetupStaffClinicRepo(staffClinic);
            SetupClinicRepo(clinic);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4059.ToString());
            result.Data.Should().BeFalse();
            _clinicRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Clinic>()), Times.Never);
        }

        /// <summary>
        /// TC-08: Clinic publication is already requested -> returns 4000 error.
        /// </summary>
        [Fact]
        public async Task Process_ClinicAlreadyRequested_ReturnsFailBadRequest4000()
        {
            //Arrange 1
            var request = RequestPublishClinicMockData.GetValidRequest();
            var staffClinic = RequestPublishClinicMockData.GetValidStaffClinic();
            var clinic = RequestPublishClinicMockData.GetValidClinic(isPublished: false, isPublicationRequested: true);

            //Arrange 2
            SetupHttpContext(userIdClaim: RequestPublishClinicMockData.ValidUserId.ToString());
            SetupStaffClinicRepo(staffClinic);
            SetupClinicRepo(clinic);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4000.ToString());
            result.Data.Should().BeFalse();
            _clinicRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Clinic>()), Times.Never);
        }

        /// <summary>
        /// TC-09: Valid request -> updates Clinic.IsPublicationRequested to true and returns 2000 success.
        /// </summary>
        [Fact]
        public async Task Process_ValidRequest_UpdatesClinicPublicationRequestAndReturnsSuccess()
        {
            //Arrange 1
            var request = RequestPublishClinicMockData.GetValidRequest();
            var staffClinic = RequestPublishClinicMockData.GetValidStaffClinic();
            var clinic = RequestPublishClinicMockData.GetValidClinic(isPublished: false, isPublicationRequested: false);

            //Arrange 2
            SetupHttpContext(userIdClaim: RequestPublishClinicMockData.ValidUserId.ToString());
            SetupStaffClinicRepo(staffClinic);
            SetupClinicRepo(clinic);
            _clinicRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Clinic>())).Returns(Task.CompletedTask);
            _clinicRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeTrue();
            clinic.IsPublicationRequested.Should().BeTrue();
            clinic.PublicationRequestedAt.Should().NotBeNull();
            _clinicRepositoryMock.Verify(x => x.UpdateAsync(clinic), Times.Once);
            _clinicRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }
    }
}
