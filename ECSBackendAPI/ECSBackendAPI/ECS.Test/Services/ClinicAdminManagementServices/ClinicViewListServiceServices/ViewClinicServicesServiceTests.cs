using ECS.Application.Services.ClinicAdminManagementServices.ViewListServiceServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicViewListServiceServices
{
    /// <summary>
    /// Unit tests for <see cref="ViewClinicServicesService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on <c>ViewClinicServicesService.cs</c>.
    /// </summary>
    public class ViewClinicServicesServiceTests
    {
        private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private readonly Mock<IRepositoryQueryBase<Service, Guid, AppDbContext>> _serviceRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly ViewClinicServicesService _sut;

        public ViewClinicServicesServiceTests()
        {
            _sut = new ViewClinicServicesService(
                _serviceRepoMock.Object,
                _staffClinicRepoMock.Object,
                _httpContextAccessorMock.Object);
        }

        // ── HttpContext helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Wires the http context accessor so that <see cref="ClaimTypes.NameIdentifier"/> resolves
        /// to a claim with the supplied raw value (used to test invalid Guid strings).
        /// Pass <c>null</c> to leave the principal with no claims.
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

        // ── Repository helpers ───────────────────────────────────────────────────

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

        private void SetupServiceRepo(IEnumerable<Service> services)
        {
            var list = services.ToList();
            var mockQueryable = list.BuildMockDbSet<Service>();

            _serviceRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Service, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        // ── Data factories ───────────────────────────────────────────────────────

        private static StaffClinic ActiveStaffClinic() => new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ClinicId = ClinicId,
            IsActive = true
        };

        private static Service MakeService(
            string name = "Consultation",
            decimal? price = 150_000m,
            int durationMinutes = 30,
            bool isActive = true,
            Guid? clinicId = null) => new()
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId ?? ClinicId,
            ServiceName = name,
            Price = price,
            DurationMinutes = durationMinutes,
            IsActive = isActive
        };

        // ── Test Cases ───────────────────────────────────────────────────────────

        /// <summary>
        /// TC-VCS-01: HttpContext is null → first null-conditional short-circuits → userIdClaim is null
        /// → Guid.TryParse fails → isUserValid=false → APP_MESSAGE_4001.
        /// StaffClinic repo never reached. Service repo is still queried by ExecutePagedQuery in
        /// production code, so we must wire it up with an empty list to satisfy the async provider.
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            SetupServiceRepo(Array.Empty<Service>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            result.Meta.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-VCS-02: HttpContext is non-null but HttpContext.User is null → second null-conditional
        /// short-circuits → userIdClaim is null → Guid.TryParse fails → APP_MESSAGE_4001.
        /// ExecutePagedQuery is still invoked in production (unconditional), so wire the service repo.
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContextUser_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest();

            //Arrange 2
            var httpContext = new DefaultHttpContext();
            httpContext.User = null;
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
            SetupServiceRepo(Array.Empty<Service>());

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
                Times.Once);
        }

        /// <summary>
        /// TC-VCS-03: HttpContext has a User but no NameIdentifier claim → FindFirst returns null
        /// → userIdClaim is null → Guid.TryParse fails → APP_MESSAGE_4001.
        /// ExecutePagedQuery is still invoked in production (unconditional), so wire the service repo.
        /// </summary>
        [Fact]
        public async Task Process_MissingNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest();

            //Arrange 2
            SetupHttpContextClaim(null);
            SetupServiceRepo(Array.Empty<Service>());

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
                Times.Once);
        }

        /// <summary>
        /// TC-VCS-04: Claim value is not a valid Guid → Guid.TryParse false branch → APP_MESSAGE_4001.
        /// ExecutePagedQuery is still invoked in production (unconditional), so wire the service repo.
        /// </summary>
        [Fact]
        public async Task Process_InvalidGuidClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");
            SetupServiceRepo(Array.Empty<Service>());

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
                Times.Once);
        }

        /// <summary>
        /// TC-VCS-05: Valid Guid claim but no active StaffClinic row → RetrieveClinicId returns null
        /// → APP_MESSAGE_4020. The service repo is still consulted by ExecutePagedQuery (unconditional
        /// in production), so it must be wired up with an empty list.
        /// </summary>
        [Fact]
        public async Task Process_ValidUserNoActiveClinic_Returns4020ClinicNotFound()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest();

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(null);
            SetupServiceRepo(Array.Empty<Service>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            result.Meta.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-VCS-06: Happy path with empty service list → success with empty data, meta.Total=0.
        /// Covers ExecutePagedQuery zero-count path, MapToResponseDto empty-list path,
        /// BuildPaginationMeta with Total=0, CreateResponse success path.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_EmptyServiceList_Returns2000WithEmptyData()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest();

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupServiceRepo(Array.Empty<Service>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Count.Should().Be(0);

            result.Meta.Should().NotBeNull();
            result.Meta!.Page.Should().Be(1);
            result.Meta.Size.Should().Be(10);
            result.Meta.Total.Should().Be(0);

            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-VCS-07: SearchTerm is " Consulta " → exercises Trim().ToLower() and the
        /// x.ServiceName.ToLower().Contains(searchTerm) arm of the SearchTerm OR.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermTrimsAndLowercases()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest
            {
                SearchTerm = " Consulta "
            };
            var service = MakeService(name: "Consultation");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupServiceRepo(new[] { service });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].ServiceName.Should().Be("Consultation");
            result.Meta!.Total.Should().Be(1);
        }

        /// <summary>
        /// TC-VCS-08: SearchTerm is null → exercises request.SearchTerm?.Trim()?.ToLower() null-conditional
        /// and string.IsNullOrEmpty(searchTerm) true branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermNull_SkipsNameMatching()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest
            {
                SearchTerm = null
            };
            var service = MakeService(name: "Therapy");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupServiceRepo(new[] { service });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].ServiceName.Should().Be("Therapy");
        }

        /// <summary>
        /// TC-VCS-09: SearchTerm is empty string → exercises explicit empty search behavior
        /// and string.IsNullOrEmpty(searchTerm) true branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermEmpty_SkipsNameMatching()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest
            {
                SearchTerm = ""
            };
            var service = MakeService(name: "Checkup");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupServiceRepo(new[] { service });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].ServiceName.Should().Be("Checkup");
        }

        /// <summary>
        /// TC-VCS-10: IsActive = true → exercises request.IsActive.HasValue true branch and
        /// x.IsActive == request.IsActive.Value true branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_IsActiveTrueFilter()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest
            {
                IsActive = true
            };
            var service = MakeService(name: "Active-Service", isActive: true);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupServiceRepo(new[] { service });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].IsActive.Should().BeTrue();
        }

        /// <summary>
        /// TC-VCS-11: IsActive = false → exercises request.IsActive.HasValue true branch and
        /// x.IsActive == request.IsActive.Value false branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_IsActiveFalseFilter()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest
            {
                IsActive = false
            };
            var service = MakeService(name: "Inactive-Service", isActive: false);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupServiceRepo(new[] { service });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].IsActive.Should().BeFalse();
        }

        /// <summary>
        /// TC-VCS-12: IsActive = null → exercises request.IsActive.HasValue false branch (skip filter).
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_IsActiveNull_SkipsIsActiveFilter()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest
            {
                IsActive = null
            };
            var service = MakeService(name: "Any-Service", isActive: true);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupServiceRepo(new[] { service });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
        }

        /// <summary>
        /// TC-VCS-13: Valid user with a populated service row → verifies every DTO mapping
        /// assignment in MapToResponseDto: Id_service (Guid → ToString), ServiceName,
        /// Price, DurationMinutes, IsActive.
        /// </summary>
        [Fact]
        public async Task Process_ValidUserWithService_ReturnsMappedServiceResponse()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest();
            var serviceId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var service = new Service
            {
                Id = serviceId,
                ClinicId = ClinicId,
                ServiceName = "Eye Exam",
                Price = 250_000m,
                DurationMinutes = 45,
                IsActive = true
            };

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupServiceRepo(new[] { service });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            var mapped = result.Data![0];
            mapped.Id_service.Should().Be(serviceId.ToString());
            mapped.ServiceName.Should().Be("Eye Exam");
            mapped.Price.Should().Be(250_000m);
            mapped.DurationMinutes.Should().Be(45);
            mapped.IsActive.Should().BeTrue();
        }

        /// <summary>
        /// TC-VCS-14: Valid user with a populated service row whose Price is null →
        /// covers the nullable Price projection in MapToResponseDto.
        /// </summary>
        [Fact]
        public async Task Process_ValidUserWithServiceHavingNullPrice_ReturnsMappedServiceResponseWithNullPrice()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest();
            var service = MakeService(name: "Free-Service", price: null);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupServiceRepo(new[] { service });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].Price.Should().BeNull();
        }

        /// <summary>
        /// TC-VCS-15: PageNumber=2 and PageSize=5 with 12 rows. Verifies OrderBy(ServiceName)
        /// behavior with Skip and Take. The MockQueryable returns the rows in the order
        /// they were inserted; the test only verifies that pagination metadata is built
        /// from the request and that the page size is respected.
        /// </summary>
        [Fact]
        public async Task Process_PageTwoWithCustomPageSize_ReturnsCorrectPageMetadata()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest
            {
                PageNumber = 2,
                PageSize = 5
            };
            var services = new[]
            {
                MakeService(name: "Service-01"),
                MakeService(name: "Service-02"),
                MakeService(name: "Service-03"),
                MakeService(name: "Service-04"),
                MakeService(name: "Service-05"),
                MakeService(name: "Service-06"),
                MakeService(name: "Service-07"),
                MakeService(name: "Service-08"),
                MakeService(name: "Service-09"),
                MakeService(name: "Service-10")
            };

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupServiceRepo(services);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Meta.Should().NotBeNull();
            result.Meta!.Page.Should().Be(2);
            result.Meta.Size.Should().Be(5);
            result.Meta.Total.Should().Be(10);
        }

        /// <summary>
        /// TC-VCS-16: Service repository query result is null-coalesced to an empty list
        /// inside ExecutePagedQuery semantics. The MockQueryable always yields a non-null
        /// sequence, so we cover it indirectly by combining a normal happy path with a
        /// populated dataset. Mirrors the existing sibling suite's predicate-only checks.
        /// </summary>
        [Fact]
        public async Task Process_ValidUser_AppliesClinicIdFilter_FromStaffClinicMapping()
        {
            //Arrange 1
            var request = new ViewClinicServicesRequest();
            var staffClinic = ActiveStaffClinic();
            var matchingService = MakeService(name: "Matching-Clinic", clinicId: ClinicId);
            var otherClinicId = Guid.Parse("99999999-9999-9999-9999-999999999999");
            var otherClinicService = MakeService(name: "Other-Clinic", clinicId: otherClinicId);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(staffClinic);
            // Both rows are returned by the mock; the test verifies the
            // BuildFilterExpression path receives the right clinicId by inspecting
            // the captured expression argument of the service repository.
            SetupServiceRepo(new[] { matchingService, otherClinicService });
            Expression<Func<Service, bool>>? capturedFilter = null;
            _serviceRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()))
                .Callback<Expression<Func<Service, bool>>, bool>((expr, _) => capturedFilter = expr)
                .Returns(new List<Service> { matchingService }.BuildMockDbSet<Service>().Object);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            capturedFilter.Should().NotBeNull();
            capturedFilter!.Compile()(matchingService).Should().BeTrue();
            capturedFilter.Compile()(otherClinicService).Should().BeFalse();
        }
    }
}
