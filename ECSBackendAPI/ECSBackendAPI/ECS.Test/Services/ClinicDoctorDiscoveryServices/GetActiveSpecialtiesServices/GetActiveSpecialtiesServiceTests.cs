using System.Linq.Expressions;
using System.Reflection;
using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicDoctorDiscoveryService.GetActiveSpecialtiesServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ClinicDoctorDiscoveryServices.GetActiveSpecialtiesServices
{
    /// <summary>
    /// Unit tests for <see cref="GetActiveSpecialtiesService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on <c>GetActiveSpecialtiesService.cs</c>.
    /// </summary>
    public class GetActiveSpecialtiesServiceTests
    {
        private static readonly Guid SpecialtyAlphaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid SpecialtyMuId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid SpecialtyZetaId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        private readonly Mock<IRepositoryQueryBase<Specialty, Guid, AppDbContext>> _specialtyRepoMock = new();
        private readonly GetActiveSpecialtiesService _sut;

        public GetActiveSpecialtiesServiceTests()
        {
            _sut = new GetActiveSpecialtiesService(_specialtyRepoMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Reflection helpers for private methods
        // ─────────────────────────────────────────────────────────────────

        private static object? InvokePrivate(object target, string methodName, params object[] args)
        {
            var mi = target.GetType().GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            mi.Should().NotBeNull($"method '{methodName}' must exist on {target.GetType().Name}");
            return mi!.Invoke(target, args);
        }

        private static async Task<T> InvokePrivateAsync<T>(object target, string methodName, params object[] args)
        {
            var raw = InvokePrivate(target, methodName, args);
            raw.Should().NotBeNull();
            var task = (Task<T>)raw!;
            return await task;
        }

        // ─────────────────────────────────────────────────────────────────
        // Repository setup helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupSpecialtyRepo(IEnumerable<Specialty> specialties)
        {
            var list = specialties.ToList();
            var queryable = list.BuildMockDbSet<Specialty>();
            _specialtyRepoMock
                .Setup(r => r.FindAll(false))
                .Returns(queryable.Object);
        }

        private void SetupEmptySpecialtyRepo()
            => SetupSpecialtyRepo(Array.Empty<Specialty>());

        // ─────────────────────────────────────────────────────────────────
        // Data factories
        // ─────────────────────────────────────────────────────────────────

        private static Specialty MakeSpecialty(
            Guid id,
            string name,
            string? description,
            bool isActive = true) => new()
        {
            Id = id,
            Name = name,
            Description = description,
            IsActive = isActive
        };

        // ==================================================================
        // ====================== Process() tests ===========================
        // ==================================================================

        /// <summary>
        /// TC-GAS-01: Repo returns an empty list → Process returns APP_MESSAGE_2000
        /// with empty Data (no items, but list is non-null). Also covers the
        /// empty branch of MapToSpecialtyListResponse (Select on empty source).
        /// </summary>
        [Fact]
        public async Task Process_EmptySpecialtyList_Returns2000WithEmptyData()
        {
            //Arrange 1

            //Arrange 2
            SetupEmptySpecialtyRepo();

            //Act
            var result = await _sut.Process();

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
            result.Meta.Should().BeNull();

            _specialtyRepoMock.Verify(r => r.FindAll(false), Times.Once);
        }

        /// <summary>
        /// TC-GAS-02: Happy path with a single active specialty → Process returns APP_MESSAGE_2000
        /// and the mapper produces a single DTO with correct Id (Guid.ToString), Name, Description.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SingleSpecialty_MapsAllFields()
        {
            //Arrange 1
            var specialty = MakeSpecialty(
                id: SpecialtyAlphaId,
                name: "Cardiology",
                description: "Heart care");

            //Arrange 2
            SetupSpecialtyRepo(new[] { specialty });

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);

            var dto = result.Data![0];
            dto.Id.Should().Be(SpecialtyAlphaId.ToString());
            dto.Name.Should().Be("Cardiology");
            dto.Description.Should().Be("Heart care");

            _specialtyRepoMock.Verify(r => r.FindAll(false), Times.Once);
        }

        /// <summary>
        /// TC-GAS-03: Happy path with multiple specialties seeded OUT-OF-ORDER →
        /// FetchActiveSpecialtiesFromRepo's OrderBy(s => s.Name) reorders them
        /// alphabetically before mapping. Asserts the order in the response.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MultipleSpecialties_OrderedByNameAscending()
        {
            //Arrange 1
            var zeta = MakeSpecialty(SpecialtyZetaId, "Zeta", "Z description");
            var alpha = MakeSpecialty(SpecialtyAlphaId, "Alpha", "A description");
            var mu = MakeSpecialty(SpecialtyMuId, "Mu", "M description");

            //Arrange 2
            SetupSpecialtyRepo(new[] { zeta, alpha, mu });

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Should().HaveCount(3);

            // OrderBy(s => s.Name) must produce Alpha → Mu → Zeta
            result.Data![0].Id.Should().Be(SpecialtyAlphaId.ToString());
            result.Data![0].Name.Should().Be("Alpha");
            result.Data![1].Id.Should().Be(SpecialtyMuId.ToString());
            result.Data![1].Name.Should().Be("Mu");
            result.Data![2].Id.Should().Be(SpecialtyZetaId.ToString());
            result.Data![2].Name.Should().Be("Zeta");
        }

        /// <summary>
        /// TC-GAS-04: Specialty has Description = null → mapper must propagate null
        /// to the response DTO (Description is `string?`).
        /// </summary>
        [Fact]
        public async Task Process_NullableDescription_MapsToNullDtoField()
        {
            //Arrange 1
            var specialty = MakeSpecialty(
                id: SpecialtyAlphaId,
                name: "Cardiology",
                description: null);

            //Arrange 2
            SetupSpecialtyRepo(new[] { specialty });

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Should().HaveCount(1);
            result.Data![0].Id.Should().Be(SpecialtyAlphaId.ToString());
            result.Data![0].Name.Should().Be("Cardiology");
            result.Data![0].Description.Should().BeNull();
        }

        /// <summary>
        /// TC-GAS-05: Meta must be null on the success response because
        /// CreateApiResponse uses the 2-arg Success(codeMessage, data) overload
        /// (no meta parameter).
        /// </summary>
        [Fact]
        public async Task Process_ResponseShape_HasNoMetaOnSuccess()
        {
            //Arrange 1

            //Arrange 2
            SetupEmptySpecialtyRepo();

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Meta.Should().BeNull();
        }

        /// <summary>
        /// TC-GAS-06: Independent branch coverage for the OrderBy-Name semantics.
        /// Uses a larger non-alphabetical set so the test exercises the OrderBy
        /// chain end-to-end with a varied cardinality.
        /// </summary>
        [Fact]
        public async Task Process_RepoReturnsList_UsesOrderByNameSemantics()
        {
            //Arrange 1
            var rows = new[]
            {
                MakeSpecialty(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Dermatology", "Skin care"),
                MakeSpecialty(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Allergology", "Allergy care"),
                MakeSpecialty(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Neurology", "Nerve care")
            };

            //Arrange 2
            SetupSpecialtyRepo(rows);

            //Act
            var result = await _sut.Process();

            //Assert
            result.Data!.Select(d => d.Name).Should().ContainInOrder(
                "Allergology", "Dermatology", "Neurology");
        }

        // ==================================================================
        // ===== FetchActiveSpecialtiesFromRepo() — private helper ===========
        // ==================================================================

        /// <summary>
        /// TC-GAS-07: Direct reflection call to verify the private repo-helper
        /// returns an empty list when the repo returns no rows. Confirms it
        /// does not mutate behaviour beyond delegating to the queryable.
        /// </summary>
        [Fact]
        public async Task FetchActiveSpecialtiesFromRepo_EmptyRepo_ReturnsEmptyList()
        {
            //Arrange 1

            //Arrange 2
            SetupEmptySpecialtyRepo();

            //Act
            var result = await InvokePrivateAsync<List<Specialty>>(_sut, "FetchActiveSpecialtiesFromRepo");

            //Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            _specialtyRepoMock.Verify(r => r.FindAll(false), Times.Once);
        }

        /// <summary>
        /// TC-GAS-08: Direct reflection call with multiple rows. MockQueryable's
        /// in-memory provider supports OrderBy. This test validates the helper's
        /// `OrderBy(s => s.Name)` execution path (and the ToListAsync terminal).
        /// </summary>
        [Fact]
        public async Task FetchActiveSpecialtiesFromRepo_MultipleRows_ReturnsOrderedList()
        {
            //Arrange 1
            var rows = new[]
            {
                MakeSpecialty(SpecialtyZetaId, "Zeta", "Z desc"),
                MakeSpecialty(SpecialtyAlphaId, "Alpha", "A desc"),
                MakeSpecialty(SpecialtyMuId, "Mu", "M desc")
            };

            //Arrange 2
            SetupSpecialtyRepo(rows);

            //Act
            var result = await InvokePrivateAsync<List<Specialty>>(_sut, "FetchActiveSpecialtiesFromRepo");

            //Assert
            result.Should().HaveCount(3);
            result[0].Name.Should().Be("Alpha");
            result[1].Name.Should().Be("Mu");
            result[2].Name.Should().Be("Zeta");
        }

        // ==================================================================
        // =========== CreateApiResponse() — private helper ==================
        // ==================================================================

        /// <summary>
        /// TC-GAS-09: Direct reflection call to CreateApiResponse. Verifies the
        /// Data payload is propagated, CodeMessage is APP_MESSAGE_2000, and Meta
        /// stays null (2-arg Success overload is used).
        /// </summary>
        [Fact]
        public void CreateApiResponse_WrapsDataWithCodeMessage2000()
        {
            //Arrange 1
            var input = new List<GetActiveSpecialtiesResponse>
            {
                new() { Id = Guid.NewGuid().ToString(), Name = "Test", Description = "Test desc" }
            };

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CreateApiResponse", input);
            var result = raw.Should().BeAssignableTo<ApiResponse<List<GetActiveSpecialtiesResponse>>>().Subject;

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeSameAs(input);
            result.Data!.Should().HaveCount(1);
            result.Data![0].Name.Should().Be("Test");
            result.Meta.Should().BeNull();
        }

        // ==================================================================
        // ===== Process() invocation-count verification ====================
        // ==================================================================

        /// <summary>
        /// TC-GAS-10: Guards against accidental multiple repo fetches. Only one
        /// call to FindAll(false) should occur per Process invocation.
        /// </summary>
        [Fact]
        public async Task Process_InvokesSpecialtyRepoFindAllExactlyOnce()
        {
            //Arrange 1

            //Arrange 2
            SetupEmptySpecialtyRepo();

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            _specialtyRepoMock.Verify(r => r.FindAll(false), Times.Once);
            _specialtyRepoMock.Verify(r => r.FindAll(It.IsAny<bool>()), Times.Once);
        }
    }
}
