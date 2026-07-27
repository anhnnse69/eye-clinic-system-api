using System.Linq.Expressions;
using ECS.Application.Services.ClinicDoctorDiscoveryService.SearchClinicDoctorServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ClinicDoctorDiscoveryServices.SearchClinicDoctorServices
{
    /// <summary>
    /// Unit tests for <see cref="SearchClinicDoctorService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on <c>SearchClinicDoctorService.cs</c>.
    /// </summary>
    /// <remarks>
    /// MockQueryable's in-memory provider does NOT honour <c>.Include()</c>; the
    /// <c>.Include()</c> calls in <see cref="SearchClinicDoctorService"/> are treated as
    /// no-ops. Projections still work because we pre-populate the navigation properties
    /// (User, Clinic, Specialty) on each seed <see cref="DoctorProfile"/>.
    /// </remarks>
    public class SearchClinicDoctorServiceTests
    {
        private static readonly Guid ClinicAlphaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ClinicZetaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid DoctorAlphaId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid DoctorZetaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid SpecialtyId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        private static readonly Guid DoctorUserAlphaId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        private static readonly Guid DoctorUserZetaId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _clinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly SearchClinicDoctorService _sut;

        public SearchClinicDoctorServiceTests()
        {
            _sut = new SearchClinicDoctorService(
                _clinicRepoMock.Object,
                _doctorRepoMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Repository helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupClinicRepo(IEnumerable<Clinic> clinics)
        {
            var list = clinics.ToList();
            var queryable = list.BuildMockDbSet<Clinic>();
            _clinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupDoctorRepo(IEnumerable<DoctorProfile> doctors)
        {
            var list = doctors.ToList();
            var queryable = list.BuildMockDbSet<DoctorProfile>();
            _doctorRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorProfile, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupEmptyRepos()
        {
            SetupClinicRepo(Array.Empty<Clinic>());
            SetupDoctorRepo(Array.Empty<DoctorProfile>());
        }

        // ─────────────────────────────────────────────────────────────────
        // Data factories
        // ─────────────────────────────────────────────────────────────────

        private static Clinic MakeClinic(
            Guid id,
            string name,
            string address = "123 Street",
            string phone = "0900000000",
            string? email = "clinic@example.com",
            string? logoUrl = "https://example.com/logo.png",
            string? description = "Clinic description",
            decimal? ratingAvg = 4.5m,
            int? reviewCount = 10,
            bool isActive = true) => new()
        {
            Id = id,
            Name = name,
            Address = address,
            Phone = phone,
            Email = email,
            LogoUrl = logoUrl,
            Description = description,
            RatingAvg = ratingAvg,
            ReviewCount = reviewCount,
            IsActive = isActive
        };

        private static DoctorProfile MakeDoctor(
            Guid id,
            Guid clinicId,
            User user,
            string? specialtyName = "Cardiology",
            Guid? specialtyId = null,
            string? title = "Senior Doctor",
            int experienceYears = 10,
            string? bio = "Bio text",
            decimal? ratingAvg = 4.8m,
            int? reviewCount = 20,
            bool isActive = true)
        {
            Specialty? specialty = null;
            if (specialtyName != null || specialtyId.HasValue)
            {
                specialty = new Specialty
                {
                    Id = specialtyId ?? SpecialtyId,
                    Name = specialtyName ?? string.Empty,
                    IsActive = true
                };
            }
            return new DoctorProfile
            {
                Id = id,
                UserId = user.Id,
                ClinicId = clinicId,
                SpecialtyId = specialtyId,
                Title = title,
                ExperienceYears = experienceYears,
                Bio = bio,
                RatingAvg = ratingAvg,
                ReviewCount = reviewCount,
                IsActive = isActive,
                User = user,
                Clinic = MakeClinic(clinicId, "Host Clinic"),
                Specialty = specialty
            };
        }

        private static User MakeDoctorUser(Guid id, string fullName, string? avatarUrl = null) => new()
        {
            Id = id,
            Phone = "0987654321",
            Email = $"{fullName.Replace(' ', '.').ToLower()}@example.com",
            PasswordHash = "x",
            FullName = fullName,
            Role = UserRole.DOCTOR,
            IsActive = true,
            AvatarUrl = avatarUrl
        };

        // ==================================================================
        // ====================== Process(...) tests ========================
        // ==================================================================

        /// <summary>
        /// TC-SCD-01: Keyword = null → NormalizeKeyword returns "" → both queries take the
        /// `keyword == string.Empty` branch and return all seeded clinics and doctors.
        /// Covers: NormalizeKeyword (null branch),
        ///         SearchClinicsAsync empty-keyword branch,
        ///         SearchDoctorsAsync empty-keyword branch.
        /// </summary>
        [Fact]
        public async Task Process_NullKeyword_ReturnsAllActiveClinicsAndDoctors()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest { Keyword = null };
            var clinic = MakeClinic(ClinicAlphaId, "Saigon Eye Clinic");
            var doctor = MakeDoctor(
                DoctorAlphaId,
                ClinicAlphaId,
                MakeDoctorUser(DoctorUserAlphaId, "Nguyen Van A"));

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupDoctorRepo(new[] { doctor });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Clinics.Should().HaveCount(1);
            result.Data!.Doctors.Should().HaveCount(1);
        }

        /// <summary>
        /// TC-SCD-02: Keyword = "" → NormalizeKeyword returns "" (empty branch).
        /// Same observable behaviour as TC-SCD-01 but exercises the explicit empty branch.
        /// </summary>
        [Fact]
        public async Task Process_EmptyKeyword_ReturnsAllActiveClinicsAndDoctors()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest { Keyword = "" };
            var clinic = MakeClinic(ClinicAlphaId, "Saigon Eye Clinic");

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupDoctorRepo(Array.Empty<DoctorProfile>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Clinics.Should().HaveCount(1);
        }

        /// <summary>
        /// TC-SCD-03: Keyword = "   " (whitespace) → NormalizeKeyword returns ""
        /// (IsNullOrWhiteSpace branch).
        /// </summary>
        [Fact]
        public async Task Process_WhitespaceKeyword_ReturnsAllActiveClinicsAndDoctors()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest { Keyword = "   " };

            //Arrange 2
            SetupClinicRepo(new[] { MakeClinic(ClinicAlphaId, "Any") });
            SetupDoctorRepo(Array.Empty<DoctorProfile>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Clinics.Should().HaveCount(1);
        }

        /// <summary>
        /// TC-SCD-04: Keyword = "  SAIGON EYE  " → NormalizeKeyword applies Trim().ToLower(),
        /// producing "saigon eye". The captured predicate is verified directly because
        /// MockQueryable's in-memory provider does not apply the Where() predicate when
        /// chained with `.Contains(...)` on a string column.
        /// Covers: NormalizeKeyword non-empty branch (Trim + ToLower),
        ///         SearchClinicsAsync Contains branch.
        /// </summary>
        [Fact]
        public async Task Process_KeywordTrimsAndLowercases()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest { Keyword = "  SAIGON EYE  " };
            var matching = MakeClinic(ClinicAlphaId, "Saigon Eye Clinic");
            var nonMatching = MakeClinic(ClinicZetaId, "HA NOI Clinic");

            //Arrange 2
            SetupClinicRepo(new[] { matching, nonMatching });
            SetupDoctorRepo(Array.Empty<DoctorProfile>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());

            // MockQueryable does not apply Where predicates; verify the captured predicate instead.
            Expression<Func<Clinic, bool>>? capturedClinicPredicate = null;
            _clinicRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()),
                Times.Once);
            // Re-wire to capture the predicate and assert it directly via .Compile().
            _clinicRepoMock.Reset();
            SetupClinicRepo(new[] { matching, nonMatching });
            _clinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()))
                .Callback<Expression<Func<Clinic, bool>>, bool>((expr, _) => capturedClinicPredicate = expr)
                .Returns(new[] { matching }.BuildMockDbSet<Clinic>().Object);
            _ = _sut.Process(request).GetAwaiter().GetResult();
            capturedClinicPredicate.Should().NotBeNull();
            var compiled = capturedClinicPredicate!.Compile();
            compiled(matching).Should().BeTrue();
            compiled(nonMatching).Should().BeFalse();
        }

        /// <summary>
        /// TC-SCD-05: Both repos return empty lists → response contains empty Clinics and
        /// empty Doctors.
        /// Covers: SearchClinicsAsync empty branch (Count = 0),
        ///         SearchDoctorsAsync empty branch.
        /// </summary>
        [Fact]
        public async Task Process_EmptySeedData_ReturnsSuccessWithEmptyLists()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest { Keyword = "anything" };

            //Arrange 2
            SetupEmptyRepos();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Clinics.Should().BeEmpty();
            result.Data!.Doctors.Should().BeEmpty();
        }

        /// <summary>
        /// TC-SCD-06: Single clinic with all fields populated → asserts every ClinicSearchItem
        /// projection field is correctly copied.
        /// Covers: every assignment in SearchClinicsAsync's projection.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MapsClinicFieldsCorrectly()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest();
            var clinic = MakeClinic(
                id: ClinicAlphaId,
                name: "Saigon Eye Clinic",
                address: "123 Nguyen Hue",
                phone: "02812345678",
                email: "info@saigoneye.vn",
                logoUrl: "https://example.com/logo.png",
                description: "Specialised eye clinic",
                ratingAvg: 4.7m,
                reviewCount: 42);

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupDoctorRepo(Array.Empty<DoctorProfile>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.Clinics.Should().HaveCount(1);
            var item = result.Data!.Clinics[0];
            item.Id.Should().Be(ClinicAlphaId);
            item.Name.Should().Be("Saigon Eye Clinic");
            item.Address.Should().Be("123 Nguyen Hue");
            item.Phone.Should().Be("02812345678");
            item.Email.Should().Be("info@saigoneye.vn");
            item.LogoUrl.Should().Be("https://example.com/logo.png");
            item.Description.Should().Be("Specialised eye clinic");
            item.RatingAvg.Should().Be(4.7m);
            item.ReviewCount.Should().Be(42);
        }

        /// <summary>
        /// TC-SCD-07: Single doctor with all fields populated → asserts every DoctorSearchItem
        /// projection field is correctly copied.
        /// Covers: every assignment in SearchDoctorsAsync's projection.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MapsDoctorFieldsCorrectly()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest();
            var doctorUser = MakeDoctorUser(
                DoctorUserAlphaId,
                "Nguyen Van A",
                avatarUrl: "https://example.com/avatar.png");
            var doctor = MakeDoctor(
                id: DoctorAlphaId,
                clinicId: ClinicAlphaId,
                user: doctorUser,
                specialtyName: "Cardiology",
                title: "Senior Cardiologist",
                experienceYears: 15,
                bio: "Expert in cardiology",
                ratingAvg: 4.9m,
                reviewCount: 100);

            //Arrange 2
            SetupClinicRepo(Array.Empty<Clinic>());
            SetupDoctorRepo(new[] { doctor });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.Doctors.Should().HaveCount(1);
            var item = result.Data!.Doctors[0];
            item.Id.Should().Be(DoctorAlphaId);
            item.FullName.Should().Be("Nguyen Van A");
            item.AvatarUrl.Should().Be("https://example.com/avatar.png");
            item.Title.Should().Be("Senior Cardiologist");
            item.Specialty.Should().Be("Cardiology");
            item.ClinicName.Should().Be("Host Clinic");
            item.ExperienceYears.Should().Be(15);
            item.Bio.Should().Be("Expert in cardiology");
            item.RatingAvg.Should().Be(4.9m);
            item.ReviewCount.Should().Be(100);
        }

        /// <summary>
        /// TC-SCD-08: Doctor has Specialty = null → projection ternary picks the `null` arm.
        /// Covers: SearchDoctorsAsync `Specialty != null ? ... : null` false branch.
        /// </summary>
        [Fact]
        public async Task Process_DoctorWithNullSpecialty_MapSpecialtyAsNull()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest();
            var doctor = MakeDoctor(
                id: DoctorAlphaId,
                clinicId: ClinicAlphaId,
                user: MakeDoctorUser(DoctorUserAlphaId, "Nguyen Van A"),
                specialtyName: null,
                specialtyId: null);

            //Arrange 2
            SetupClinicRepo(Array.Empty<Clinic>());
            SetupDoctorRepo(new[] { doctor });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.Doctors.Should().HaveCount(1);
            result.Data!.Doctors[0].Specialty.Should().BeNull();
        }

        /// <summary>
        /// TC-SCD-09: Clinic has ReviewCount = null → projection's `?? 0` fallback kicks in.
        /// Covers: SearchClinicsAsync ReviewCount null-coalesce branch.
        /// </summary>
        [Fact]
        public async Task Process_ClinicWithNullReviewCount_DefaultsToZero()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest();
            var clinic = MakeClinic(ClinicAlphaId, "Saigon Eye Clinic", reviewCount: null);

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupDoctorRepo(Array.Empty<DoctorProfile>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.Clinics.Should().HaveCount(1);
            result.Data!.Clinics[0].ReviewCount.Should().Be(0);
        }

        /// <summary>
        /// TC-SCD-10: Doctor has ReviewCount = null → projection's `?? 0` fallback kicks in.
        /// Covers: SearchDoctorsAsync ReviewCount null-coalesce branch.
        /// </summary>
        [Fact]
        public async Task Process_DoctorWithNullReviewCount_DefaultsToZero()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest();
            var doctor = MakeDoctor(
                id: DoctorAlphaId,
                clinicId: ClinicAlphaId,
                user: MakeDoctorUser(DoctorUserAlphaId, "Nguyen Van A"),
                reviewCount: null);

            //Arrange 2
            SetupClinicRepo(Array.Empty<Clinic>());
            SetupDoctorRepo(new[] { doctor });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.Doctors.Should().HaveCount(1);
            result.Data!.Doctors[0].ReviewCount.Should().Be(0);
        }

        /// <summary>
        /// TC-SCD-11: Multiple clinics and doctors seeded out-of-order → both queries
        /// apply OrderBy ascending. Asserts the response lists are alphabetically ordered.
        /// Covers: OrderBy(c => c.Name) and OrderBy(d => d.User.FullName) chains.
        /// </summary>
        [Fact]
        public async Task Process_OrdersClinicsAndDoctorsByNameAscending()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest();
            var zetaClinic = MakeClinic(ClinicZetaId, "Zeta Clinic");
            var alphaClinic = MakeClinic(ClinicAlphaId, "Alpha Clinic");
            var doctorZeta = MakeDoctor(
                DoctorZetaId,
                ClinicAlphaId,
                MakeDoctorUser(DoctorUserZetaId, "Zeta Doctor"));
            var doctorAlpha = MakeDoctor(
                DoctorAlphaId,
                ClinicAlphaId,
                MakeDoctorUser(DoctorUserAlphaId, "Alpha Doctor"));

            //Arrange 2
            SetupClinicRepo(new[] { zetaClinic, alphaClinic });
            SetupDoctorRepo(new[] { doctorZeta, doctorAlpha });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());

            result.Data!.Clinics.Select(c => c.Name).Should().ContainInOrder(
                "Alpha Clinic", "Zeta Clinic");
            result.Data!.Doctors.Select(d => d.FullName).Should().ContainInOrder(
                "Alpha Doctor", "Zeta Doctor");
        }

        /// <summary>
        /// TC-SCD-12: Meta must be null on the success response because
        /// CreateSuccessResponse uses the 2-arg Success(codeMessage, data) overload
        /// (no meta parameter).
        /// </summary>
        [Fact]
        public async Task Process_ResponseShape_NoMetaOnSuccess()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest();

            //Arrange 2
            SetupEmptyRepos();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Meta.Should().BeNull();
        }

        /// <summary>
        /// TC-SCD-13: Both repositories' FindByCondition are called exactly once per
        /// Process invocation.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_BothReposInvokedExactlyOnce()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest();

            //Arrange 2
            SetupEmptyRepos();

            //Act
            await _sut.Process(request);

            //Assert
            _clinicRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()),
                Times.Once);
            _doctorRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorProfile, bool>>>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-SCD-14: Keyword "saigon eye" (lowercase) matches clinic "Saigon Eye Clinic" —
        /// proves the `c.Name.ToLower().Contains(keyword)` arm of SearchClinicsAsync.
        /// As with TC-SCD-04, MockQueryable does not apply Where predicates that use
        /// `.Contains()` on a string, so we capture the predicate and verify directly.
        /// </summary>
        [Fact]
        public async Task Process_KeywordMatchesCaseInsensitiveOnClinicName()
        {
            //Arrange 1
            var request = new SearchClinicDoctorRequest { Keyword = "saigon eye" };
            var matching = MakeClinic(ClinicAlphaId, "Saigon Eye Clinic");
            var nonMatching = MakeClinic(ClinicZetaId, "HA NOI Clinic");

            //Arrange 2
            Expression<Func<Clinic, bool>>? capturedPredicate = null;
            var singleMatch = new[] { matching }.BuildMockDbSet<Clinic>().Object;
            _clinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()))
                .Callback<Expression<Func<Clinic, bool>>, bool>((expr, _) => capturedPredicate = expr)
                .Returns(singleMatch);
            SetupDoctorRepo(Array.Empty<DoctorProfile>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            capturedPredicate.Should().NotBeNull();
            var compiled = capturedPredicate!.Compile();
            compiled(matching).Should().BeTrue();
            compiled(nonMatching).Should().BeFalse();
        }
    }
}