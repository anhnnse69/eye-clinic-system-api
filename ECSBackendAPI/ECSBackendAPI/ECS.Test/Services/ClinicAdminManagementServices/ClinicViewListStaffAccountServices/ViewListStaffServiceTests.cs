using ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices;
using ECS.Domain.Entities.Auth;
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

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicViewListStaffAccountServices
{
    /// <summary>
    /// Unit tests for <see cref="ViewListStaffService"/>.
    /// Pattern: [Method]_[State]_[Outcome].
    /// Goal: 100% line coverage on <c>ViewListStaffService .cs</c>.
    /// </summary>
    public class ViewListStaffServiceTests
    {
        private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly ViewListStaffService _sut;

        public ViewListStaffServiceTests()
        {
            _sut = new ViewListStaffService(
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

        /// <summary>
        /// Sets up the staff-clinic repository's no-arg / single-predicate overload to return the
        /// supplied entries (or empty when null). Used for the admin-context lookup in
        /// <c>RetrieveClinicId</c>.
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
        /// Sets up the staff-clinic repository's three-arg overload (predicate + trackChanges +
        /// includeProperties) to return the supplied staff rows. This is the overload invoked by
        /// <c>BuildStaffQuery</c>.
        /// </summary>
        private void SetupStaffWithIncludesRepo(IEnumerable<StaffClinic> rows)
        {
            var list = rows.ToList();
            var mockQueryable = list.BuildMockDbSet<StaffClinic>();

            _staffClinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<StaffClinic, object>>[]>()))
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

        private static StaffClinic Staff(
            StaffRole role = StaffRole.DOCTOR,
            bool isActive = true,
            string fullName = "Nguyen Van A",
            string email = "a@example.com",
            string phone = "0987654321",
            DateTime? createdAt = null,
            User? user = null) => new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ClinicId = ClinicId,
            Role = role,
            IsActive = isActive,
            CreatedAt = createdAt ?? new DateTime(2026, 7, 26, 10, 30, 0),
            UpdatedAt = DateTime.UtcNow,
            User = user ?? new User
            {
                Id = Guid.NewGuid(),
                Phone = phone,
                Email = email,
                FullName = fullName,
                PasswordHash = "x",
                Role = UserRole.DOCTOR,
                IsActive = true
            }
        };

        // ── Test Cases ───────────────────────────────────────────────────────────

        /// <summary>
        /// TC-VLS-01: HttpContext is null → first null-conditional short-circuits → userIdClaim is null
        /// → Guid.TryParse fails → isUserValid=false → APP_MESSAGE_4001.
        /// StaffClinic repo never reached.
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewListStaffRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);

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
        }

        /// <summary>
        /// TC-VLS-02: HttpContext is non-null but HttpContext.User is null → second null-conditional
        /// short-circuits → userIdClaim is null → Guid.TryParse fails → APP_MESSAGE_4001.
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContextUser_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewListStaffRequest();

            //Arrange 2
            var httpContext = new DefaultHttpContext { User = null };
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VLS-03: HttpContext has a User but no NameIdentifier claim → FindFirst returns null
        /// → userIdClaim is null → Guid.TryParse fails → APP_MESSAGE_4001.
        /// </summary>
        [Fact]
        public async Task Process_MissingNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewListStaffRequest();

            //Arrange 2
            SetupHttpContextClaim(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VLS-04: Claim value is not a valid Guid → Guid.TryParse false branch → APP_MESSAGE_4001.
        /// </summary>
        [Fact]
        public async Task Process_InvalidGuidClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewListStaffRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VLS-05: Valid Guid claim but no active StaffClinic row → RetrieveClinicId returns null
        /// → ValidateClinicExistence false → APP_MESSAGE_4020. The staff-with-includes repo is
        /// never reached because BuildStaffQuery short-circuits on `!isClinicExist`.
        /// </summary>
        [Fact]
        public async Task Process_ValidUserNoActiveClinic_Returns4020ClinicNotFound()
        {
            //Arrange 1
            var request = new ViewListStaffRequest();

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            result.Meta.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<StaffClinic, object>>[]>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VLS-06: Happy path with empty staff list → success with empty data, meta.Total=0.
        /// Covers BuildStaffQuery happy path, ComputeTotalCount zero-count path,
        /// FetchPagedStaffData empty-list path, MapToResponse empty-list path,
        /// CreateResponse success path, MetaResponse with Total=0.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_EmptyStaffList_Returns2000WithEmptyData()
        {
            //Arrange 1
            var request = new ViewListStaffRequest();

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(Array.Empty<StaffClinic>());

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

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<StaffClinic, object>>[]>()),
                Times.Once);
        }

        /// <summary>
        /// TC-VLS-07: SearchTerm = " nguyen " → exercises Trim().ToLower() and the
        /// x.User!.FullName.ToLower().Contains(cleanSearch) arm.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermTrimsAndLowercases()
        {
            //Arrange 1
            var request = new ViewListStaffRequest
            {
                SearchTerm = " nguyen "
            };
            var staff = Staff(fullName: "Nguyen Van A");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(new[] { staff });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].FullName.Should().Be("Nguyen Van A");
            result.Meta!.Total.Should().Be(1);
        }

        /// <summary>
        /// TC-VLS-08: SearchTerm matches Email (not FullName) → exercises the
        /// x.User!.Email.ToLower().Contains(cleanSearch) arm.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermMatchesEmail()
        {
            //Arrange 1
            var request = new ViewListStaffRequest
            {
                SearchTerm = "MAIL"
            };
            var staff = Staff(fullName: "Brand X", email: "ezmail@example.com");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(new[] { staff });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].Email.Should().Be("ezmail@example.com");
        }

        /// <summary>
        /// TC-VLS-09: SearchTerm matches Phone (not FullName or Email) → exercises the
        /// x.User!.Phone.Contains(cleanSearch) arm. Phone is matched WITHOUT ToLower() per
        /// production code.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermMatchesPhone()
        {
            //Arrange 1
            var request = new ViewListStaffRequest
            {
                SearchTerm = "555"
            };
            var staff = Staff(fullName: "Not Matching", email: "not@match.com", phone: "0987555123");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(new[] { staff });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].Phone.Should().Be("0987555123");
        }

        /// <summary>
        /// TC-VLS-10: SearchTerm = null → exercises IsNullOrWhiteSpace true branch (skip filter).
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermNull_SkipsNameMatching()
        {
            //Arrange 1
            var request = new ViewListStaffRequest
            {
                SearchTerm = null
            };
            var staff = Staff(fullName: "Anything");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(new[] { staff });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
        }

        /// <summary>
        /// TC-VLS-11: SearchTerm = "   " (whitespace only) → exercises IsNullOrWhiteSpace true branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermWhitespace_SkipsNameMatching()
        {
            //Arrange 1
            var request = new ViewListStaffRequest
            {
                SearchTerm = "   "
            };
            var staff = Staff(fullName: "Anything");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(new[] { staff });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
        }

        /// <summary>
        /// TC-VLS-12: IsActive = true → exercises request.IsActive.HasValue true branch and
        /// x.IsActive == request.IsActive.Value true branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_IsActiveTrueFilter()
        {
            //Arrange 1
            var request = new ViewListStaffRequest
            {
                IsActive = true
            };
            var staff = Staff(isActive: true);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(new[] { staff });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].IsActive.Should().BeTrue();
        }

        /// <summary>
        /// TC-VLS-13: IsActive = false → exercises request.IsActive.HasValue true branch and
        /// x.IsActive == request.IsActive.Value false branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_IsActiveFalseFilter()
        {
            //Arrange 1
            var request = new ViewListStaffRequest
            {
                IsActive = false
            };
            var staff = Staff(isActive: false);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(new[] { staff });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].IsActive.Should().BeFalse();
        }

        /// <summary>
        /// TC-VLS-14: IsActive = null → exercises request.IsActive.HasValue false branch (skip filter).
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_IsActiveNull_SkipsIsActiveFilter()
        {
            //Arrange 1
            var request = new ViewListStaffRequest
            {
                IsActive = null
            };
            var staff = Staff(isActive: true);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(new[] { staff });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
        }

        /// <summary>
        /// TC-VLS-15: Multi-row dataset with PageNumber=2 / PageSize=5 → exercises OrderByDescending
        /// + Skip + Take + CountAsync chain and BuildPaginationMeta values.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_PaginationMeta_PopulatedFromRequest()
        {
            //Arrange 1
            var request = new ViewListStaffRequest
            {
                PageNumber = 2,
                PageSize = 5
            };
            var rows = Enumerable.Range(0, 20)
                .Select(i => Staff(createdAt: new DateTime(2026, 1, 1).AddDays(i)))
                .ToList();

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(rows);

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
        /// TC-VLS-16: Role = DOCTOR → switch maps to "Bác sĩ".
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MapToResponse_DoctorRole()
        {
            //Arrange 1
            var request = new ViewListStaffRequest();
            var staff = Staff(role: StaffRole.DOCTOR);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(new[] { staff });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].Role.Should().Be("Bác sĩ");
        }

        /// <summary>
        /// TC-VLS-17: Role = RECEPTIONIST → switch maps to "Tiếp tân".
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MapToResponse_ReceptionistRole()
        {
            //Arrange 1
            var request = new ViewListStaffRequest();
            var staff = Staff(role: StaffRole.RECEPTIONIST);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(new[] { staff });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].Role.Should().Be("Tiếp tân");
        }

        /// <summary>
        /// TC-VLS-18: Role = CLINIC_ADMIN → switch maps to "Quản lý phòng khám".
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MapToResponse_ClinicAdminRole()
        {
            //Arrange 1
            var request = new ViewListStaffRequest();
            var staff = Staff(role: StaffRole.CLINIC_ADMIN);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(new[] { staff });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].Role.Should().Be("Quản lý phòng khám");
        }

        /// <summary>
        /// TC-VLS-19: Role uses an out-of-range enum value (cast from int) → exercises the
        /// default branch `_ => item.Role.ToString()` of the switch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MapToResponse_UnknownRoleFallback()
        {
            //Arrange 1
            var request = new ViewListStaffRequest();
            var staff = Staff();
            staff.Role = (StaffRole)999;

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(new[] { staff });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].Role.Should().Be(((StaffRole)999).ToString());
        }

        /// <summary>
        /// TC-VLS-20: Staff with User = null → exercises FullName/Email/Phone null-coalescing
        /// fallback to string.Empty.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MapToResponse_NullUserFieldsFallbackToEmpty()
        {
            //Arrange 1
            var request = new ViewListStaffRequest();
            var staff = Staff();
            staff.User = null!;

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupStaffWithIncludesRepo(new[] { staff });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].FullName.Should().Be(string.Empty);
            result.Data![0].Email.Should().Be(string.Empty);
            result.Data![0].Phone.Should().Be(string.Empty);
        }
    }
}
