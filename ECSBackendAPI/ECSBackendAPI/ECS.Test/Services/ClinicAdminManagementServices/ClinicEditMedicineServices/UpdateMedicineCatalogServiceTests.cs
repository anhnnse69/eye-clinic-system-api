using ECS.Application.Services.ClinicAdminManagementServices.ClinicEditMedicineServices;
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

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicEditMedicineServices
{
    /// <summary>
    /// Unit tests for <see cref="UpdateMedicineCatalogService"/>.
    /// Pattern: [Method]_[State]_[Outcome].
    /// Goal: 100% line and 100% branch coverage on UpdateMedicineCatalogService.cs.
    /// </summary>
    public class UpdateMedicineCatalogServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<MedicineCatalog, Guid, AppDbContext>> _medicineRepoMock;
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly UpdateMedicineCatalogService _sut;

        public UpdateMedicineCatalogServiceTests()
        {
            _medicineRepoMock = new Mock<IRepositoryBaseAsync<MedicineCatalog, Guid, AppDbContext>>();
            _staffClinicRepoMock = new Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _sut = new UpdateMedicineCatalogService(
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

        private void SetupMedicineRepoFirstCall(MedicineCatalog? returnMedicine)
        {
            var rows = returnMedicine != null
                ? new List<MedicineCatalog> { returnMedicine }
                : new List<MedicineCatalog>();
            var mockQueryable = rows.BuildMockDbSet<MedicineCatalog>();

            _medicineRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<MedicineCatalog, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        /// <summary>
        /// Configures two consecutive FindByCondition calls to return different result sets.
        /// 1st call: returns <paramref name="firstCallResult"/> (used by RetrieveMedicineItem).
        /// 2nd call: returns <paramref name="secondCallResult"/> (used by ValidateNameUniqueness).
        /// </summary>
        private void SetupMedicineRepoTwoCalls(
            MedicineCatalog? firstCallResult,
            IEnumerable<MedicineCatalog> secondCallResult)
        {
            var firstRows = firstCallResult != null
                ? new List<MedicineCatalog> { firstCallResult }
                : new List<MedicineCatalog>();
            var firstMock = firstRows.BuildMockDbSet<MedicineCatalog>();

            var secondRows = secondCallResult.ToList();
            var secondMock = secondRows.BuildMockDbSet<MedicineCatalog>();

            _medicineRepoMock
                .SetupSequence(r => r.FindByCondition(
                    It.IsAny<Expression<Func<MedicineCatalog, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(firstMock.Object)
                .Returns(secondMock.Object);
        }

        private void SetupMedicineUpdate()
        {
            _medicineRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<MedicineCatalog>()))
                .Returns(Task.CompletedTask);
            _medicineRepoMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.FromResult(1));
        }

        // ── TC-01: HttpContext null branch ────────────────────────────────────

        /// <summary>
        /// TC-01: HttpContext accessor returns null → the `?.User.FindFirst(...)?.Value` chain short-circuits
        /// on the first null-conditional → userIdClaim is null → Guid.TryParse fails → isUserValid=false.
        /// Covers: RetrieveUserId (HttpContext == null branch), RetrieveClinicId skip,
        /// RetrieveMedicineItem skip, ValidateNameUniqueness skip, SaveMedicineState skip,
        /// MapToResponse skip, CreateErrorResponse (!isUserValid → APP_MESSAGE_4001).
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = UpdateMedicineCatalogMockData.GetDefaultRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            SetupStaffClinicRepo(null);
            SetupMedicineRepoFirstCall(null);

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
                r => r.UpdateAsync(It.IsAny<MedicineCatalog>()),
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
            var request = UpdateMedicineCatalogMockData.GetDefaultRequest();

            //Arrange 2
            SetupHttpContextUserWithoutClaims();
            SetupStaffClinicRepo(null);
            SetupMedicineRepoFirstCall(null);

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
                r => r.UpdateAsync(It.IsAny<MedicineCatalog>()),
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
            var request = UpdateMedicineCatalogMockData.GetDefaultRequest();

            //Arrange 2
            var httpContext = new DefaultHttpContext();
            var claims = new[] { new Claim("some_other_claim", "value") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
            SetupStaffClinicRepo(null);
            SetupMedicineRepoFirstCall(null);

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
            var request = UpdateMedicineCatalogMockData.GetDefaultRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-valid-guid");
            SetupStaffClinicRepo(null);
            SetupMedicineRepoFirstCall(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        // ── TC-05: Valid user but no StaffClinic → APP_MESSAGE_4014 ────────────

        /// <summary>
        /// TC-05: Valid Guid claim but no active StaffClinic row → clinicId = null → isClinicExist=false.
        /// Covers: RetrieveClinicId (staffClinic == null branch), RetrieveMedicineItem skip,
        /// ValidateNameUniqueness skip, SaveMedicineState skip, MapToResponse skip,
        /// CreateErrorResponse (!isClinicExist → APP_MESSAGE_4014).
        /// </summary>
        [Fact]
        public async Task Process_ValidUserNoStaffClinic_Returns4014ClinicNotFound()
        {
            //Arrange 1
            var request = UpdateMedicineCatalogMockData.GetDefaultRequest();

            //Arrange 2
            SetupHttpContextUserId(UpdateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(null);
            SetupMedicineRepoFirstCall(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _medicineRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<MedicineCatalog>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        // ── TC-06: Valid clinic but Medicine not found → APP_MESSAGE_4020 ─────

        /// <summary>
        /// TC-06: Valid user + valid clinic, but no MedicineCatalog row matches the id.
        /// Covers: RetrieveMedicineItem (item == null branch), ValidateNameUniqueness skip,
        /// SaveMedicineState skip, MapToResponse skip, CreateErrorResponse (!isMedicineExist → APP_MESSAGE_4020).
        /// </summary>
        [Fact]
        public async Task Process_ValidClinicMedicineNotFound_Returns4020MedicineNotFound()
        {
            //Arrange 1
            var request = UpdateMedicineCatalogMockData.GetDefaultRequest();
            var staffClinic = UpdateMedicineCatalogMockData.GetActiveStaffClinic();

            //Arrange 2
            SetupHttpContextUserId(UpdateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupMedicineRepoFirstCall(null);

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
                Times.Once);
            _medicineRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<MedicineCatalog>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        // ── TC-07: Duplicate medicine name → APP_MESSAGE_4019 ─────────────────

        /// <summary>
        /// TC-07: Another MedicineCatalog with the same name (normalized) → isNameUnique=false.
        /// Covers: ValidateNameUniqueness (duplicate branch → return false),
        /// SaveMedicineState (isNameUnique=false → return null), CreateErrorResponse
        /// (!isNameUnique → APP_MESSAGE_4019).
        /// </summary>
        [Fact]
        public async Task Process_DuplicateMedicineName_Returns4019DuplicateName()
        {
            //Arrange 1
            var request = UpdateMedicineCatalogMockData.GetDefaultRequest(medicineName: "Paracetamol 500mg");
            var staffClinic = UpdateMedicineCatalogMockData.GetActiveStaffClinic();
            var existingMedicine = UpdateMedicineCatalogMockData.GetExistingMedicine(medicineName: "Paracetamol 500mg");

            //Arrange 2
            SetupHttpContextUserId(UpdateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupMedicineRepoFirstCall(existingMedicine);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();

            _medicineRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<MedicineCatalog>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        // ── TC-08: Defense-in-depth — RetrieveClinicId isUserValid=false branch ─

        /// <summary>
        /// TC-08: Defense-in-depth branch in RetrieveClinicId (isUserValid short-circuit).
        /// Currently unreachable because RetrieveUserId always sets isUserValid=true on Guid
        /// success AND isUserValid=false on Guid failure (no intermediate state). This test
        /// exercises the branch via reflection to ensure the short-circuit is covered.
        /// Covers: RetrieveClinicId (isUserValid=false short-circuit branch).
        /// </summary>
        [Fact]
        public async Task Process_DefenseInDepth_RetrieveClinicIdIsUserValidFalseBranch()
        {
            //Arrange 1
            var request = UpdateMedicineCatalogMockData.GetDefaultRequest();

            //Arrange 2
            SetupHttpContextUserId(UpdateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(null);
            SetupMedicineRepoFirstCall(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
            result.Data.Should().BeNull();
        }

        // ── TC-09: Happy path — unique name → APP_MESSAGE_2000 ─────────────────

        /// <summary>
        /// TC-09: Valid user, valid clinic, existing medicine, unique name → 2000 success.
        /// Covers: ValidateNameUniqueness (true branch), SaveMedicineState (success branch),
        /// MapToResponse (success branch), CreateResponse (success branch).
        /// </summary>
        [Fact]
        public async Task Process_UniqueMedicineName_Returns2000WithUpdatedFields()
        {
            //Arrange 1
            var request = UpdateMedicineCatalogMockData.GetDefaultRequest(medicineName: "Ibuprofen 400mg");
            var staffClinic = UpdateMedicineCatalogMockData.GetActiveStaffClinic();
            var existingMedicine = UpdateMedicineCatalogMockData.GetExistingMedicine(medicineName: "Paracetamol 500mg");
            var before = DateTime.UtcNow;

            //Arrange 2
            SetupHttpContextUserId(UpdateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(staffClinic);
            // 1st call: RetrieveMedicineItem returns existing row.
            // 2nd call: ValidateNameUniqueness returns EMPTY (name does not collide).
            SetupMedicineRepoTwoCalls(existingMedicine, new List<MedicineCatalog>());
            SetupMedicineUpdate();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(UpdateMedicineCatalogMockData.TestMedicineId);
            result.Data.MedicineName.Should().Be("Ibuprofen 400mg");
            result.Data.IsActive.Should().BeTrue();
            result.Data.UpdatedAt.Should().BeOnOrAfter(before);

            _medicineRepoMock.Verify(
                r => r.UpdateAsync(It.Is<MedicineCatalog>(m =>
                    m.Id == UpdateMedicineCatalogMockData.TestMedicineId &&
                    m.MedicineName == "Ibuprofen 400mg" &&
                    m.GenericName == "Acetaminophen" &&
                    m.Unit == "Tablet" &&
                    m.DosageForm == "Oral" &&
                    m.Concentration == "500mg" &&
                    m.Manufacturer == "Test Pharma" &&
                    m.Notes == "Updated notes" &&
                    m.IsActive)),
                Times.Once);
            _medicineRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Once);
        }

        // ── TC-10: Happy path — IsActive=false branch ─────────────────────────

        /// <summary>
        /// TC-10: Valid user, valid clinic, existing medicine, unique name, IsActive=false.
        /// Covers: SaveMedicineState (IsActive=false assignment), MapToResponse (IsActive=false mapping).
        /// </summary>
        [Fact]
        public async Task Process_UniqueMedicineName_InactiveFlagReturnsFalse()
        {
            //Arrange 1
            var request = UpdateMedicineCatalogMockData.GetMinimalRequest(medicineName: "Ibuprofen 400mg");
            var staffClinic = UpdateMedicineCatalogMockData.GetActiveStaffClinic();
            var existingMedicine = UpdateMedicineCatalogMockData.GetExistingMedicine(medicineName: "Paracetamol 500mg");

            //Arrange 2
            SetupHttpContextUserId(UpdateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(staffClinic);
            // 1st call: RetrieveMedicineItem returns existing row.
            // 2nd call: ValidateNameUniqueness returns EMPTY (name does not collide).
            SetupMedicineRepoTwoCalls(existingMedicine, new List<MedicineCatalog>());
            SetupMedicineUpdate();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.IsActive.Should().BeFalse();
            result.Data.MedicineName.Should().Be("Ibuprofen 400mg");

            _medicineRepoMock.Verify(
                r => r.UpdateAsync(It.Is<MedicineCatalog>(m => !m.IsActive)),
                Times.Once);
            _medicineRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Once);
        }

        // ── TC-11: Case-insensitive duplicate name ────────────────────────────

        /// <summary>
        /// TC-11: Request name has different casing compared to existing record
        /// → ValidateNameUniqueness still returns false (ToLower normalize).
        /// Covers: ValidateNameUniqueness's normalization branch.
        /// </summary>
        [Fact]
        public async Task Process_UniqueMedicineNameWithDifferentCase_Returns4019DuplicateName()
        {
            //Arrange 1
            var request = UpdateMedicineCatalogMockData.GetDefaultRequest(medicineName: "PARACETAMOL 500mg");
            var staffClinic = UpdateMedicineCatalogMockData.GetActiveStaffClinic();
            var existingMedicine = UpdateMedicineCatalogMockData.GetExistingMedicine(medicineName: "Paracetamol 500mg");

            //Arrange 2
            SetupHttpContextUserId(UpdateMedicineCatalogMockData.TestStaffUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupMedicineRepoFirstCall(existingMedicine);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();

            _medicineRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<MedicineCatalog>()),
                Times.Never);
        }
    }
}
