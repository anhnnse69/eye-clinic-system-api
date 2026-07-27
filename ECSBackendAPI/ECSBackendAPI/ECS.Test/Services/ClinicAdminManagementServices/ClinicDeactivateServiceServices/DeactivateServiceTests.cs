using ECS.Application.Services.ClinicAdminManagementServices.DeactivateServiceServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicDeactivateServiceServices
{
    /// <summary>
    /// Unit tests for <see cref="DeactivateService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// Goal: 100% line coverage on DeactivateService.cs.
    /// </summary>
    public class DeactivateServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<Service, Guid, AppDbContext>> _serviceRepoMock;
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly DeactivateService _sut;

        public DeactivateServiceTests()
        {
            _serviceRepoMock = new Mock<IRepositoryBaseAsync<Service, Guid, AppDbContext>>();
            _staffClinicRepoMock = new Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _sut = new DeactivateService(
                _serviceRepoMock.Object,
                _staffClinicRepoMock.Object,
                _httpContextAccessorMock.Object);
        }

        // ── HttpContext helpers ────────────────────────────────────────────────

        /// <summary>
        /// Wires the http context accessor so that <see cref="ClaimTypes.NameIdentifier"/>
        /// resolves to a claim with the supplied raw value. Pass <c>null</c> to leave
        /// the principal with no claims.
        /// </summary>
        private void SetupHttpContextClaim(string? rawClaimValue)
        {
            var httpContext = new DefaultHttpContext();
            if (rawClaimValue != null)
            {
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, rawClaimValue) };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                httpContext.User = new ClaimsPrincipal(identity);
            }
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        }

        private void SetupHttpContextUserId(Guid userId)
            => SetupHttpContextClaim(userId.ToString());

        // ── Repository helpers ────────────────────────────────────────────────

        /// <summary>
        /// Sets up the staff-clinic query repository. Pass <c>null</c> for an empty result set.
        /// </summary>
        private void SetupStaffClinicRepo(StaffClinic? returnStaffClinic)
        {
            var rows = returnStaffClinic != null
                ? new List<StaffClinic> { returnStaffClinic }
                : new List<StaffClinic>();
            var mockQueryable = rows.BuildMockDbSet<StaffClinic>();

            _staffClinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        /// <summary>
        /// Sets up the service repository. Pass <c>null</c> for an empty result set.
        /// </summary>
        private void SetupServiceRepo(Service? returnService)
        {
            var rows = returnService != null
                ? new List<Service> { returnService }
                : new List<Service>();
            var mockQueryable = rows.BuildMockDbSet<Service>();

            _serviceRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Service, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        // ── Test Cases ─────────────────────────────────────────────────────────

        /// <summary>
        /// TC-DS-01: HttpContext accessor returns null → the
        /// `_httpContextAccessor.HttpContext?.User.FindFirst(...)?.Value` chain short-circuits
        /// on the very first null-conditional → userIdClaim is null → Guid.TryParse fails
        /// → isUserValid=false.
        /// Covers: RetrieveUserId (HttpContext == null branch),
        /// RetrieveClinicId (isUserValid=false short-circuit),
        /// RetrieveServiceData (clinicId == null short-circuit),
        /// ValidateServicePresence (service == null → isServiceExist=false),
        /// ApplyStateDeactivation (isServiceExist=false → early return),
        /// MapToResponseDto (isServiceExist=false OR service==null → empty DTO),
        /// CreateErrorResponse (!isUserValid → APP_MESSAGE_4001).
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = DeactivateServiceMockData.GetValidRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            SetupStaffClinicRepo(null);
            SetupServiceRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            // The error path uses ApiResponse.Fail which sets Data to default (null).
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<Service>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-DS-02: HttpContext has no NameIdentifier claim at all (User is present but
        /// FindFirst returns null) → userIdClaim is null → Guid.TryParse fails → isUserValid=false.
        /// Covers: RetrieveUserId (User != null branch where FindFirst returns null).
        /// </summary>
        [Fact]
        public async Task Process_HttpContextHasNoNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = DeactivateServiceMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextClaim(null);
            SetupStaffClinicRepo(null);
            SetupServiceRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<Service>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-DS-03: NameIdentifier claim is present but its value is not a valid Guid string
        /// → Guid.TryParse returns false → isUserValid=false.
        /// Covers: RetrieveUserId (TryParse-fails branch on the raw claim value).
        /// </summary>
        [Fact]
        public async Task Process_NameIdentifierClaimIsNotAValidGuid_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = DeactivateServiceMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-valid-guid");
            SetupStaffClinicRepo(null);
            SetupServiceRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<Service>()),
                Times.Never);
        }

        /// <summary>
        /// TC-DS-04: Happy path where the target service is currently IsActive=true.
        /// After the service toggles, IsActive becomes false. The response reports the
        /// deactivated state with APP_MESSAGE_2000.
        /// Covers: full Process flow including RetrieveClinicId success,
        /// RetrieveServiceData success, ValidateServicePresence (service != null),
        /// ApplyStateDeactivation (toggle + UpdateAsync + SaveChangesAsync),
        /// MapToResponseDto full mapping, CreateResponse success branch.
        /// </summary>
        [Fact]
        public async Task Process_ValidActiveService_DeactivatesAndReturnsSuccessWithIsActiveFalse()
        {
            //Arrange 1
            var request = DeactivateServiceMockData.GetValidRequest();
            var activeStaffClinic = DeactivateServiceMockData.GetActiveStaffClinic();
            var activeService = DeactivateServiceMockData.GetActiveService();

            //Arrange 2
            SetupHttpContextUserId(DeactivateServiceMockData.TestUserId);
            SetupStaffClinicRepo(activeStaffClinic);
            SetupServiceRepo(activeService);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.ServiceId.Should().Be(DeactivateServiceMockData.TestServiceId);
            result.Data.IsActive.Should().BeFalse();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _serviceRepoMock.Verify(
                r => r.UpdateAsync(It.Is<Service>(s =>
                    s.Id == DeactivateServiceMockData.TestServiceId &&
                    s.IsActive == false)),
                Times.Once);
            _serviceRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// TC-DS-05: Happy path where the target service is currently IsActive=false.
        /// After the service toggles, IsActive becomes true. This covers the
        /// reverse-direction toggle branch of `service.IsActive = !service.IsActive`.
        /// </summary>
        [Fact]
        public async Task Process_ValidInactiveService_ActivatesAndReturnsSuccessWithIsActiveTrue()
        {
            //Arrange 1
            var request = DeactivateServiceMockData.GetValidRequest();
            var activeStaffClinic = DeactivateServiceMockData.GetActiveStaffClinic();
            var inactiveService = DeactivateServiceMockData.GetInactiveService();

            //Arrange 2
            SetupHttpContextUserId(DeactivateServiceMockData.TestUserId);
            SetupStaffClinicRepo(activeStaffClinic);
            SetupServiceRepo(inactiveService);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.ServiceId.Should().Be(DeactivateServiceMockData.TestServiceId);
            result.Data.IsActive.Should().BeTrue();

            _serviceRepoMock.Verify(
                r => r.UpdateAsync(It.Is<Service>(s =>
                    s.Id == DeactivateServiceMockData.TestServiceId &&
                    s.IsActive == true)),
                Times.Once);
            _serviceRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// TC-DS-06: User is valid and has an active StaffClinic, but the requested
        /// ServiceId does not match any service in the user's clinic.
        /// Covers: RetrieveClinicId success (returns Guid), RetrieveServiceData miss
        /// (returns null), ValidateServicePresence (service == null → isServiceExist=false),
        /// ApplyStateDeactivation (isServiceExist=false → early return),
        /// MapToResponseDto (isServiceExist=false OR service==null → empty DTO),
        /// CreateErrorResponse (!isServiceExist → APP_MESSAGE_4020).
        /// </summary>
        [Fact]
        public async Task Process_NoMatchingService_Returns4020ServiceNotFoundError()
        {
            //Arrange 1
            var request = DeactivateServiceMockData.GetValidRequest();
            var activeStaffClinic = DeactivateServiceMockData.GetActiveStaffClinic();

            //Arrange 2
            SetupHttpContextUserId(DeactivateServiceMockData.TestUserId);
            SetupStaffClinicRepo(activeStaffClinic);
            SetupServiceRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            // The error path uses ApiResponse.Fail which sets Data to default (null).
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _serviceRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<Service>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-DS-07: User is valid but has NO active StaffClinic row. This drives the
        /// `staffClinic?.ClinicId` null-conditional `staffClinic == null` branch inside
        /// <see cref="RetrieveClinicId"/> (line 100, the second of the two branches on
        /// that line). After this branch is hit, the result is `null` clinicId, which
        /// then makes <see cref="RetrieveServiceData"/> return null, eventually driving
        /// the same APP_MESSAGE_4020 error path as TC-DS-06.
        /// </summary>
        [Fact]
        public async Task Process_ValidUserButNoActiveStaffClinic_Returns4020ServiceNotFoundError()
        {
            //Arrange 1
            var request = DeactivateServiceMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextUserId(DeactivateServiceMockData.TestUserId);
            // The staff-clinic query is issued but the mock returns an empty set, so
            // the `staffClinic?.ClinicId` chain on line 100 returns null.
            SetupStaffClinicRepo(null);
            SetupServiceRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();

            // The staff-clinic query IS issued (line 97) because isUserValid is still
            // true at that point. The chain then resolves to null, but the call to
            // FindByCondition itself is observed.
            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<Service>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }
    }
}
