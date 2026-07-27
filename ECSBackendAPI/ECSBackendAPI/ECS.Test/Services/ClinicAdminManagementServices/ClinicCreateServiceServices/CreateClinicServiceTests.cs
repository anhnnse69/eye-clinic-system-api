using ECS.Application.Services.ClinicAdminManagementServices.CreateServiceServices;
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

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicCreateServiceServices
{
    /// <summary>
    /// Unit tests for <see cref="CreateService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// Goal: 100% line coverage on CreateClinicService.cs.
    /// </summary>
    public class CreateClinicServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<Service, Guid, AppDbContext>> _serviceRepoMock;
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly CreateService _sut;

        public CreateClinicServiceTests()
        {
            _serviceRepoMock = new Mock<IRepositoryBaseAsync<Service, Guid, AppDbContext>>();
            _staffClinicRepoMock = new Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _sut = new CreateService(
                _serviceRepoMock.Object,
                _staffClinicRepoMock.Object,
                _httpContextAccessorMock.Object);
        }

        // ── HttpContext helpers ────────────────────────────────────────────────

        /// <summary>
        /// Wires the http context accessor so that <see cref="ClaimTypes.NameIdentifier"/>
        /// resolves to a claim with the supplied raw value (used to exercise invalid Guid strings).
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

        // ── Repository helpers ────────────────────────────────────────────────

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

        private void SetupServiceRepo(Service? existingService)
        {
            var rows = existingService != null
                ? new List<Service> { existingService }
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
        /// TC-CC-01: HttpContext has no NameIdentifier claim → Guid.TryParse fails → isUserValid=false.
        /// Covers: RetrieveUserId (User != null but FindFirst returns null → Value is null → parse fails),
        /// RetrieveClinicId (isUserValid=false short-circuit),
        /// CheckDuplicateServiceName (isClinicValid=false short-circuit),
        /// SaveNewService (early return), CreateResponse → CreateErrorResponse (!isUserValid → 4001).
        /// </summary>
        [Fact]
        public async Task Process_NullUserClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = CreateClinicServiceMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextClaim(null);
            SetupStaffClinicRepo(null);
            SetupServiceRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<Service>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-CC-02: HttpContext accessor returns null → the `?.User.FindFirst(...)?.Value` chain
        /// short-circuits on the very first null-conditional → userIdClaim is null → Guid.TryParse fails.
        /// Covers: RetrieveUserId (HttpContext == null branch of the `?.User` chain).
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = CreateClinicServiceMockData.GetValidRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            SetupStaffClinicRepo(null);
            SetupServiceRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<Service>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-CC-03: HttpContext has a non-Guid claim value → Guid.TryParse returns false → isUserValid=false.
        /// Covers: RetrieveUserId (Guid.TryParse fail on non-empty invalid string branch).
        /// </summary>
        [Fact]
        public async Task Process_InvalidGuidClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = CreateClinicServiceMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-valid-guid");
            SetupStaffClinicRepo(null);
            SetupServiceRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<Service>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-CC-04: Valid Guid claim but no active StaffClinic row → clinicId = Guid.Empty → isClinicValid=false.
        /// Covers: RetrieveClinicId (staffClinic == null branch),
        /// CheckDuplicateServiceName (isClinicValid=false short-circuit),
        /// SaveNewService (early return), CreateErrorResponse (!isClinicValid → APP_MESSAGE_4020).
        /// </summary>
        [Fact]
        public async Task Process_NoActiveStaffClinic_Returns4020ClinicError()
        {
            //Arrange 1
            var request = CreateClinicServiceMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextUserId(CreateClinicServiceMockData.TestUserId);
            SetupStaffClinicRepo(null);
            SetupServiceRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<Service>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-CC-05: Existing service with the same normalized name → isDuplicate=true.
        /// Covers: CheckDuplicateServiceName (full path → AnyAsync returns true → returns true),
        /// SaveNewService (early return on isDuplicate),
        /// CreateErrorResponse (isDuplicate → APP_MESSAGE_4041).
        /// </summary>
        [Fact]
        public async Task Process_DuplicateServiceName_Returns4041DuplicateError()
        {
            //Arrange 1
            var request = CreateClinicServiceMockData.GetValidRequest();
            var staffClinic = CreateClinicServiceMockData.GetActiveStaffClinic();
            var existingService = CreateClinicServiceMockData.GetExistingService("General Consultation");

            //Arrange 2
            SetupHttpContextUserId(CreateClinicServiceMockData.TestUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupServiceRepo(existingService);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4041.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _serviceRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<Service>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-CC-06: Happy path with unique non-blank service name → success and persistence.
        /// Covers: full Process flow including CheckDuplicateServiceName (AnyAsync returns false),
        /// SaveNewService (CreateAsync + SaveChangesAsync), MapToEntity (Trim),
        /// MapToResponseDto (all field mappings), CreateResponse success branch.
        /// Note: <see cref="CreateService"/> generates the new entity Id via <c>Guid.NewGuid()</c> in
        /// MapToEntity and reuses that Id for the response payload (CreateAsync's return value is ignored).
        /// Therefore the assertion captures the entity passed to <c>CreateAsync</c> rather than the
        /// pre-arranged Guid in MockData.
        /// </summary>
        [Fact]
        public async Task Process_ValidUniqueService_ReturnsSuccessAndPersistsTrimmedService()
        {
            //Arrange 1
            var request = CreateClinicServiceMockData.GetValidRequest();
            var staffClinic = CreateClinicServiceMockData.GetActiveStaffClinic();
            var capturedEntities = new List<Service>();

            //Arrange 2
            SetupHttpContextUserId(CreateClinicServiceMockData.TestUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupServiceRepo(null);
            _serviceRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<Service>()))
                .Callback<Service>(s => capturedEntities.Add(s))
                .ReturnsAsync((Service s) => s.Id);
            _serviceRepoMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.FromResult(1));

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.ClinicId.Should().Be(CreateClinicServiceMockData.TestClinicId);
            result.Data.ServiceName.Should().Be("General Consultation");
            result.Data.Price.Should().Be(200000m);
            result.Data.DurationMinutes.Should().Be(30);
            result.Data.IsActive.Should().BeTrue();
            result.Data.Id.Should().NotBe(Guid.Empty);

            capturedEntities.Should().HaveCount(1);
            var persisted = capturedEntities[0];
            persisted.ClinicId.Should().Be(CreateClinicServiceMockData.TestClinicId);
            persisted.ServiceName.Should().Be("General Consultation");
            persisted.Price.Should().Be(200000m);
            persisted.DurationMinutes.Should().Be(30);
            persisted.IsActive.Should().BeTrue();
            // Response ID must match the entity ID that was persisted
            result.Data.Id.Should().Be(persisted.Id);

            _serviceRepoMock.Verify(r => r.CreateAsync(It.IsAny<Service>()), Times.Once);
            _serviceRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// TC-CC-07: Blank/whitespace service name → CheckDuplicateServiceName treats it as non-duplicate
        /// (string.IsNullOrWhiteSpace branch) → isDuplicate=false → still reaches happy-path persist.
        /// Covers: CheckDuplicateServiceName (string.IsNullOrWhiteSpace short-circuit branch),
        /// SaveNewService (persist path with empty trimmed name),
        /// MapToEntity (request.ServiceName.Trim() = ""), MapToResponseDto, CreateResponse success.
        /// Note: the response Id is generated by the service's MapToEntity (Guid.NewGuid), NOT by
        /// CreateAsync's return value — the assertion captures the persisted entity to verify
        /// the trimmed empty ServiceName.
        /// </summary>
        [Fact]
        public async Task Process_BlankServiceName_TreatedAsNonDuplicateAndPersistsTrimmed()
        {
            //Arrange 1
            var request = new CreateServiceRequest
            {
                ServiceName = "   ",
                Price = 50000m,
                DurationMinutes = 15
            };
            var staffClinic = CreateClinicServiceMockData.GetActiveStaffClinic();
            var capturedEntities = new List<Service>();

            //Arrange 2
            SetupHttpContextUserId(CreateClinicServiceMockData.TestUserId);
            SetupStaffClinicRepo(staffClinic);
            // Service repo duplicate check: NOT called because string.IsNullOrWhiteSpace short-circuits
            // before the FindByCondition query is issued.
            SetupServiceRepo(null);
            _serviceRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<Service>()))
                .Callback<Service>(s => capturedEntities.Add(s))
                .ReturnsAsync((Service s) => s.Id);
            _serviceRepoMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.FromResult(1));

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.ServiceName.Should().Be(string.Empty);
            result.Data.Price.Should().Be(50000m);
            result.Data.DurationMinutes.Should().Be(15);
            result.Data.Id.Should().NotBe(Guid.Empty);

            // Service duplicate check is NEVER called due to IsNullOrWhiteSpace short-circuit
            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Never);

            capturedEntities.Should().HaveCount(1);
            var persisted = capturedEntities[0];
            persisted.ClinicId.Should().Be(CreateClinicServiceMockData.TestClinicId);
            persisted.ServiceName.Should().Be(string.Empty);
            persisted.IsActive.Should().BeTrue();
            result.Data.Id.Should().Be(persisted.Id);

            _serviceRepoMock.Verify(r => r.CreateAsync(It.IsAny<Service>()), Times.Once);
            _serviceRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
