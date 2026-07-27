using ECS.Application.Services.ClinicAdminManagementServices.ClinicViewListMedicineServices;
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

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicViewListMedicineServices
{
    /// <summary>
    /// Unit tests for <see cref="GetMedicineCatalogService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on GetMedicineCatalogService.cs.
    /// </summary>
    public class GetMedicineCatalogServiceTests
    {
        private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private readonly Mock<IRepositoryQueryBase<MedicineCatalog, Guid, AppDbContext>> _medicineRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly GetMedicineCatalogService _sut;

        public GetMedicineCatalogServiceTests()
        {
            _sut = new GetMedicineCatalogService(
                _medicineRepoMock.Object,
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

        private void SetupMedicineRepo(IEnumerable<MedicineCatalog> medicines)
        {
            var list = medicines.ToList();
            var mockQueryable = list.BuildMockDbSet<MedicineCatalog>();

            _medicineRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<MedicineCatalog, bool>>>(),
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

        private static MedicineCatalog Medicine(
            string name = "Paracetamol",
            string? genericName = null,
            string? manufacturer = null,
            bool isActive = true,
            DateTime? createdAt = null) => new()
        {
            Id = Guid.NewGuid(),
            ClinicId = ClinicId,
            MedicineName = name,
            GenericName = genericName,
            Unit = "Box",
            DosageForm = "Tablet",
            Concentration = "500mg",
            Manufacturer = manufacturer,
            Notes = "Sample",
            IsActive = isActive,
            CreatedAt = createdAt ?? new DateTime(2026, 7, 26, 10, 30, 0)
        };

        // ── Test Cases ───────────────────────────────────────────────────────────

        /// <summary>
        /// TC-GMC-01: HttpContext is null → first null-conditional short-circuits → userIdClaim is null
        /// → Guid.TryParse fails → isUserValid=false → APP_MESSAGE_4001.
        /// StaffClinic repo never reached. Medicine repo is still queried by ExecutePagedQuery in
        /// production code, so we must wire it up with an empty list to satisfy the async provider.
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            SetupMedicineRepo(Array.Empty<MedicineCatalog>());

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
            _medicineRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-GMC-02: HttpContext is non-null but HttpContext.User is null → second null-conditional
        /// short-circuits → userIdClaim is null → Guid.TryParse fails → APP_MESSAGE_4001.
        /// ExecutePagedQuery is still invoked in production (unconditional), so wire the medicine repo.
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContextUser_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest();

            //Arrange 2
            var httpContext = new DefaultHttpContext();
            httpContext.User = null;
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
            SetupMedicineRepo(Array.Empty<MedicineCatalog>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-GMC-03: HttpContext has a User but no NameIdentifier claim → FindFirst returns null
        /// → userIdClaim is null → Guid.TryParse fails → APP_MESSAGE_4001.
        /// ExecutePagedQuery is still invoked in production (unconditional), so wire the medicine repo.
        /// </summary>
        [Fact]
        public async Task Process_MissingNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest();

            //Arrange 2
            SetupHttpContextClaim(null);
            SetupMedicineRepo(Array.Empty<MedicineCatalog>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-GMC-04: Claim value is not a valid Guid → Guid.TryParse false branch → APP_MESSAGE_4001.
        /// ExecutePagedQuery is still invoked in production (unconditional), so wire the medicine repo.
        /// </summary>
        [Fact]
        public async Task Process_InvalidGuidClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");
            SetupMedicineRepo(Array.Empty<MedicineCatalog>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _medicineRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-GMC-05: Valid Guid claim but no active StaffClinic row → RetrieveClinicId returns null
        /// → APP_MESSAGE_4020. The medicine repo is still consulted by ExecutePagedQuery (unconditional
        /// in production), so it must be wired up with an empty list.
        /// </summary>
        [Fact]
        public async Task Process_ValidUserNoActiveClinic_Returns4020ClinicNotFound()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest();

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(null);
            SetupMedicineRepo(Array.Empty<MedicineCatalog>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            result.Meta.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _medicineRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-GMC-06: Happy path with empty medicine list → success with empty data, meta.Total=0.
        /// Covers ExecutePagedQuery zero-count path, MapToResponseDto empty-list path,
        /// BuildPaginationMeta with Total=0, CreateResponse success path.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_EmptyMedicineList_Returns2000WithEmptyData()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest();

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupMedicineRepo(Array.Empty<MedicineCatalog>());

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

            _medicineRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-GMC-07: SearchTerm is " PARA " → exercises Trim().ToLower() and the
        /// x.MedicineName.ToLower().Contains(searchTerm) arm of the SearchTerm OR.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermTrimsAndLowercases()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest
            {
                SearchTerm = " PARA "
            };
            var medicine = Medicine(name: "Paracetamol");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupMedicineRepo(new[] { medicine });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].MedicineName.Should().Be("Paracetamol");
            result.Meta!.Total.Should().Be(1);
        }

        /// <summary>
        /// TC-GMC-08: SearchTerm matches GenericName (not MedicineName) → exercises the
        /// x.GenericName != null && x.GenericName.ToLower().Contains(searchTerm) arm.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermMatchesGenericName()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest
            {
                SearchTerm = "acet"
            };
            var medicine = Medicine(name: "BrandX", genericName: "Acetaminophen");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupMedicineRepo(new[] { medicine });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].GenericName.Should().Be("Acetaminophen");
        }

        /// <summary>
        /// TC-GMC-09: SearchTerm matches Manufacturer (not MedicineName or GenericName) → exercises
        /// the x.Manufacturer != null && x.Manufacturer.ToLower().Contains(searchTerm) arm.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermMatchesManufacturer()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest
            {
                SearchTerm = "pharma"
            };
            var medicine = Medicine(name: "BrandY", manufacturer: "GlobalPharma Inc");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupMedicineRepo(new[] { medicine });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].Manufacturer.Should().Be("GlobalPharma Inc");
        }

        /// <summary>
        /// TC-GMC-10: SearchTerm is null → exercises request.SearchTerm?.Trim()?.ToLower() null-conditional
        /// and string.IsNullOrEmpty(searchTerm) true branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermNull_SkipsNameMatching()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest
            {
                SearchTerm = null
            };
            var medicine = Medicine(name: "Ibuprofen");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupMedicineRepo(new[] { medicine });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].MedicineName.Should().Be("Ibuprofen");
        }

        /// <summary>
        /// TC-GMC-11: IsActive = true → exercises request.IsActive.HasValue true branch and
        /// x.IsActive == request.IsActive.Value true branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_IsActiveTrueFilter()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest
            {
                IsActive = true
            };
            var medicine = Medicine(name: "Amoxicillin", isActive: true);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupMedicineRepo(new[] { medicine });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].IsActive.Should().BeTrue();
        }

        /// <summary>
        /// TC-GMC-12: IsActive = false → exercises request.IsActive.HasValue true branch and
        /// x.IsActive == request.IsActive.Value false branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_IsActiveFalseFilter()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest
            {
                IsActive = false
            };
            var medicine = Medicine(name: "Aspirin", isActive: false);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupMedicineRepo(new[] { medicine });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].IsActive.Should().BeFalse();
        }

        /// <summary>
        /// TC-GMC-13: IsActive = null → exercises request.IsActive.HasValue false branch (skip).
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_IsActiveNull_SkipsIsActiveFilter()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest
            {
                IsActive = null
            };
            var medicine = Medicine(name: "VitaminC", isActive: true);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupMedicineRepo(new[] { medicine });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
        }

        /// <summary>
        /// TC-GMC-14: Multi-row dataset with PageNumber=2 / PageSize=5 → exercises OrderBy + Skip + Take
        /// + CountAsync chain and BuildPaginationMeta values.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_PaginationMeta_PopulatedFromRequest()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest
            {
                PageNumber = 2,
                PageSize = 5
            };
            var rows = Enumerable.Range(0, 20)
                .Select(i => Medicine(name: $"Med{i:00}"))
                .ToList();

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupMedicineRepo(rows);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(5);
            result.Meta!.Page.Should().Be(2);
            result.Meta.Size.Should().Be(5);
            result.Meta.Total.Should().Be(20);
        }

        /// <summary>
        /// TC-GMC-15: Single medicine with all nullable fields populated → verifies every property
        /// mapping in MapToResponseDto and the CreatedAt dd/MM/yyyy HH:mm format.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MapToResponseDto_AllFieldsCopied()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest();
            var medicineId = Guid.NewGuid();
            var createdAt = new DateTime(2026, 7, 26, 10, 30, 0);
            var medicine = new MedicineCatalog
            {
                Id = medicineId,
                ClinicId = ClinicId,
                MedicineName = "Paracetamol",
                GenericName = "Acetaminophen",
                Unit = "Box",
                DosageForm = "Tablet",
                Concentration = "500mg",
                Manufacturer = "GlobalPharma",
                Notes = "Take after meal",
                IsActive = true,
                CreatedAt = createdAt
            };

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupMedicineRepo(new[] { medicine });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            var dto = result.Data![0];
            dto.Id.Should().Be(medicineId.ToString());
            dto.MedicineName.Should().Be("Paracetamol");
            dto.GenericName.Should().Be("Acetaminophen");
            dto.Unit.Should().Be("Box");
            dto.DosageForm.Should().Be("Tablet");
            dto.Concentration.Should().Be("500mg");
            dto.Manufacturer.Should().Be("GlobalPharma");
            dto.Notes.Should().Be("Take after meal");
            dto.IsActive.Should().BeTrue();
            dto.CreatedAt.Should().Be("26/07/2026 10:30");
        }

        /// <summary>
        /// TC-GMC-16: Single medicine with all nullable fields null → verifies nullable projection
        /// and IsActive=false is preserved.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MapToResponseDto_NullableFieldsAreNull()
        {
            //Arrange 1
            var request = new GetMedicineCatalogRequest();
            var medicine = new MedicineCatalog
            {
                Id = Guid.NewGuid(),
                ClinicId = ClinicId,
                MedicineName = "Paracetamol",
                GenericName = null,
                Unit = null,
                DosageForm = null,
                Concentration = null,
                Manufacturer = null,
                Notes = null,
                IsActive = false,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0)
            };

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupMedicineRepo(new[] { medicine });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            var dto = result.Data![0];
            dto.MedicineName.Should().Be("Paracetamol");
            dto.GenericName.Should().BeNull();
            dto.Unit.Should().BeNull();
            dto.DosageForm.Should().BeNull();
            dto.Concentration.Should().BeNull();
            dto.Manufacturer.Should().BeNull();
            dto.Notes.Should().BeNull();
            dto.IsActive.Should().BeFalse();
            dto.CreatedAt.Should().Be("01/01/2026 00:00");
        }
    }
}
