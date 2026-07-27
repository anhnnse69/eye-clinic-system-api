using ECS.Application.Services.ClinicAdminManagementServices.ClinicCreateMedicineServices;
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

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicCreateMedicineServices
{
    /// <summary>
    /// Unit tests for <see cref="CreateMedicineCatalogService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// Goal: 100% line and 100% branch coverage on CreateMedicineCatalogService.cs.
    /// </summary>
    public class CreateMedicineCatalogServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<MedicineCatalog, Guid, AppDbContext>> _medicineRepoMock;
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly CreateMedicineCatalogService _sut;

        public CreateMedicineCatalogServiceTests()
        {
            _medicineRepoMock = new Mock<IRepositoryBaseAsync<MedicineCatalog, Guid, AppDbContext>>();
            _staffClinicRepoMock = new Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _sut = new CreateMedicineCatalogService(
                _medicineRepoMock.Object,
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

        private void SetupHttpContextUserWithoutClaims()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
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

        private void SetupMedicineRepo(MedicineCatalog? existingMedicine)
        {
            var rows = existingMedicine != null
                ? new List<MedicineCatalog> { existingMedicine }
                : new List<MedicineCatalog>();
            var mockQueryable = rows.BuildMockDbSet<MedicineCatalog>();

            _medicineRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<MedicineCatalog, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        private void SetupMedicineCreate(Guid returnedId)
        {
            _medicineRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<MedicineCatalog>()))
                .ReturnsAsync(returnedId);
            _medicineRepoMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.FromResult(1));
        }

        // ── TC-01: HttpContext null branch ────────────────────────────────────

        /// <summary>
        /// TC-01: HttpContext accessor returns null → the `?.User.FindFirst(...)?.Value` chain short-circuits
        /// on the first null-conditional → userIdClaim is null → Guid.TryParse fails → isUserValid=false.
        /// Covers: RetrieveUserId (HttpContext == null branch), RetrieveClinicId skip,
        /// IsMedicineNameUnique skip, PersistMedicineEntity early return,
        /// CreateErrorResponse (!isUserValid → APP_MESSAGE_4001).
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = CreateMedicineCatalogMockData.GetDefaultRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            SetupStaffClinicRepo(null);
            SetupMedicineRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<MedicineCatalog>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        // ── TC-02: User == null branch ────────────────────────────────────────

        /// <summary>
        /// TC-02: HttpContext != null but User is null/empty → `?.FindFirst(...)?.Value` short-circuits
        /// → userIdClaim null → Guid.TryParse fails → isUserValid=false.
        /// Covers: RetrieveUserId (User null branch).
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContextUser_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = CreateMedicineCatalogMockData.GetDefaultRequest();

            //Arrange 2
            SetupHttpContextUserWithoutClaims();
            SetupStaffClinicRepo(null);
            SetupMedicineRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<MedicineCatalog>()),
                Times.Never);
        }

        // ── TC-03: No NameIdentifier claim branch ─────────────────────────────

        /// <summary>
        /// TC-03: User has claims but no NameIdentifier one → FindFirst returns null → userIdClaim null → fail.
        /// Covers: RetrieveUserId (FindFirst returns null branch).
        /// </summary>
        [Fact]
        public async Task Process_NoNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = CreateMedicineCatalogMockData.GetDefaultRequest();

            //Arrange 2
            var httpContext = new DefaultHttpContext();
            var claims = new[] { new Claim("some_other_claim", "value") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
            SetupStaffClinicRepo(null);
            SetupMedicineRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        // ── TC-04: Invalid Guid claim branch ──────────────────────────────────

        /// <summary>
        /// TC-04: Claim value is non-Guid string → Guid.TryParse false → isUserValid=false.
        /// Covers: RetrieveUserId (Guid.TryParse false branch).
        /// </summary>
        [Fact]
        public async Task Process_InvalidGuidClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = CreateMedicineCatalogMockData.GetDefaultRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-valid-guid");
            SetupStaffClinicRepo(null);
            SetupMedicineRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        // ── TC-05: Valid user but no StaffClinic → clinicId null → 4020 ───────

        /// <summary>
        /// TC-05: Valid Guid claim but no active StaffClinic row → clinicId = null → isClinicExist=false.
        /// Covers: RetrieveClinicId (staffClinic == null branch), IsMedicineNameUnique skip,
        /// PersistMedicineEntity early return, CreateErrorResponse (!isClinicExist → APP_MESSAGE_4020).
        /// </summary>
        [Fact]
        public async Task Process_ValidUserNoStaffClinic_Returns4020ClinicNotFound()
        {
            //Arrange 1
            var request = CreateMedicineCatalogMockData.GetDefaultRequest();

            //Arrange 2
            SetupHttpContextUserId(CreateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(null);
            SetupMedicineRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _medicineRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<MedicineCatalog>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        // ── TC-06: Duplicate medicine name → 4015 ─────────────────────────────

        /// <summary>
        /// TC-06: Existing medicine with the same name (normalized) → isMedicineUnique=false.
        /// Covers: IsMedicineNameUnique (false branch → FindByCondition returns non-null),
        /// PersistMedicineEntity early return, CreateErrorResponse (!isMedicineUnique → APP_MESSAGE_4015).
        /// </summary>
        [Fact]
        public async Task Process_DuplicateMedicineName_Returns4015Duplicate()
        {
            //Arrange 1
            var request = CreateMedicineCatalogMockData.GetDefaultRequest("Paracetamol 500mg");
            var staffClinic = CreateMedicineCatalogMockData.GetActiveStaffClinic();
            var existingMedicine = CreateMedicineCatalogMockData.GetExistingMedicine("Paracetamol 500mg");

            //Arrange 2
            SetupHttpContextUserId(CreateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupMedicineRepo(existingMedicine);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4015.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _medicineRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _medicineRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<MedicineCatalog>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        // ── TC-07: Unique medicine name → 2000 with created Id ────────────────

        /// <summary>
        /// TC-07: Valid user, valid clinic, unique medicine name → success → medicine is created and saved.
        /// Covers: IsMedicineNameUnique (true branch), PersistMedicineEntity success path,
        /// MapToEntity, MapToResponseDto, CreateResponse success branch.
        /// </summary>
        [Fact]
        public async Task Process_UniqueMedicineName_Returns2000WithCreatedId()
        {
            //Arrange 1
            var request = CreateMedicineCatalogMockData.GetDefaultRequest("Paracetamol 500mg");
            var staffClinic = CreateMedicineCatalogMockData.GetActiveStaffClinic();
            var generatedId = CreateMedicineCatalogMockData.TestMedicineId;

            //Arrange 2
            SetupHttpContextUserId(CreateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupMedicineRepo(null);
            SetupMedicineCreate(generatedId);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(generatedId.ToString());

            _medicineRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<MedicineCatalog>()),
                Times.Once);
            _medicineRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Once);
        }

        // ── TC-08: Whitespace-around / case-insensitive duplicate detection ───

        /// <summary>
        /// TC-08: Request medicine name has leading/trailing whitespace and different casing compared
        /// to the existing DB record → IsMedicineNameUnique still returns false (Trim + ToLower normalize).
        /// Covers: IsMedicineNameUnique's normalization branch (medicineName.Trim().ToLower()).
        /// </summary>
        [Fact]
        public async Task Process_UniqueMedicineNameWithWhitespace_CaseInsensitiveMatch()
        {
            //Arrange 1
            var request = CreateMedicineCatalogMockData.GetDefaultRequest("  PARACETAMOL 500mg  ");
            var staffClinic = CreateMedicineCatalogMockData.GetActiveStaffClinic();
            var existingMedicine = CreateMedicineCatalogMockData.GetExistingMedicine("Paracetamol 500mg");

            //Arrange 2
            SetupHttpContextUserId(CreateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupMedicineRepo(existingMedicine);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4015.ToString());
            result.Data.Should().BeNull();

            _medicineRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _medicineRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<MedicineCatalog>()),
                Times.Never);
        }

        // ── TC-09: Blank medicine name bypasses uniqueness check ───────────────

        /// <summary>
        /// TC-09: medicineName is whitespace → IsMedicineNameUnique short-circuits to true
        /// (IsNullOrWhiteSpace branch) → caller proceeds to create the record.
        /// Covers: IsMedicineNameUnique (string.IsNullOrWhiteSpace branch).
        /// Note: Real production pipeline runs the validator first, but the service itself
        /// still treats blank names as unique and would persist them.
        /// </summary>
        [Fact]
        public async Task Process_BlankMedicineName_BypassesUniquenessCheck()
        {
            //Arrange 1
            var request = CreateMedicineCatalogMockData.GetBlankMedicineNameRequest();
            var staffClinic = CreateMedicineCatalogMockData.GetActiveStaffClinic();
            var generatedId = CreateMedicineCatalogMockData.TestMedicineId;

            //Arrange 2
            SetupHttpContextUserId(CreateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupMedicineRepo(null);
            SetupMedicineCreate(generatedId);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(generatedId.ToString());

            // The medicine repo's FindByCondition is skipped because IsNullOrWhiteSpace short-circuits.
            _medicineRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<MedicineCatalog>()),
                Times.Once);
            _medicineRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Once);
        }

        // ── TC-10: Happy path with all fields → MapToEntity + MapToResponseDto ─

        /// <summary>
        /// TC-10: Happy path with all request fields populated → MapToEntity executes all assignments,
        /// MapToResponseDto maps the generated Id to a string, and the response data is fully populated.
        /// Covers: MapToEntity (all field assignments), MapToResponseDto, CreateResponse success.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_CreatesMedicineWithAllFields()
        {
            //Arrange 1
            var request = new CreateMedicineCatalogRequest
            {
                MedicineName = "Ibuprofen 400mg",
                GenericName = "Ibuprofen",
                Unit = "Tablet",
                DosageForm = "Oral",
                Concentration = "400mg",
                Manufacturer = "Pharma Corp",
                Notes = "Anti-inflammatory"
            };
            var staffClinic = CreateMedicineCatalogMockData.GetActiveStaffClinic();
            var generatedId = CreateMedicineCatalogMockData.TestMedicineId;

            //Arrange 2
            SetupHttpContextUserId(CreateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupMedicineRepo(null);
            SetupMedicineCreate(generatedId);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(generatedId.ToString());

            _medicineRepoMock.Verify(
                r => r.CreateAsync(It.Is<MedicineCatalog>(m =>
                    m.ClinicId == CreateMedicineCatalogMockData.TestClinicId &&
                    m.MedicineName == "Ibuprofen 400mg" &&
                    m.GenericName == "Ibuprofen" &&
                    m.Unit == "Tablet" &&
                    m.DosageForm == "Oral" &&
                    m.Concentration == "400mg" &&
                    m.Manufacturer == "Pharma Corp" &&
                    m.Notes == "Anti-inflammatory" &&
                    m.IsActive)),
                Times.Once);
            _medicineRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        // ── TC-11: minimal request (only MedicineName) → all other fields null ─

        /// <summary>
        /// TC-11: Minimal request (only MedicineName set, everything else null) still gets persisted
        /// because the entity simply copies the null values from the request.
        /// Covers: MapToEntity assignment path with all nullable fields null.
        /// </summary>
        [Fact]
        public async Task Process_MinimalRequest_PersistsMedicineWithNullOptionalFields()
        {
            //Arrange 1
            var request = CreateMedicineCatalogMockData.GetMinimalRequest("Amoxicillin 250mg");
            var staffClinic = CreateMedicineCatalogMockData.GetActiveStaffClinic();
            var generatedId = CreateMedicineCatalogMockData.TestMedicineId;

            //Arrange 2
            SetupHttpContextUserId(CreateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupMedicineRepo(null);
            SetupMedicineCreate(generatedId);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(generatedId.ToString());

            _medicineRepoMock.Verify(
                r => r.CreateAsync(It.Is<MedicineCatalog>(m =>
                    m.ClinicId == CreateMedicineCatalogMockData.TestClinicId &&
                    m.MedicineName == "Amoxicillin 250mg" &&
                    m.GenericName == null &&
                    m.Unit == null &&
                    m.DosageForm == null &&
                    m.Concentration == null &&
                    m.Manufacturer == null &&
                    m.Notes == null &&
                    m.IsActive)),
                Times.Once);
        }
    }
}
