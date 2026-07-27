using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Services.PatientProfileManagementServices.GetPatientProfilesServices;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.PatientProfileManagementServices.GetPatientProfilesServices
{
    public class GetPatientProfilesServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>> _patientProfileRepositoryMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly AppDbContext _context;
        private readonly GetPatientProfilesService _sut;

        public GetPatientProfilesServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _sut = new GetPatientProfilesService(
                _patientProfileRepositoryMock.Object,
                _context,
                _httpContextAccessorMock.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SetupHttpContextClaim(string? claimValue)
        {
            var context = new DefaultHttpContext();
            if (claimValue != null)
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, claimValue) },
                    "TestAuth"));
            }

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
        }

        private void SetupPatientProfileRepository(List<PatientProfile> rows)
        {
            var queryable = rows.BuildMockDbSet();

            _patientProfileRepositoryMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<PatientProfile, bool>>>(),
                    It.IsAny<bool>()))
                .Returns((Expression<Func<PatientProfile, bool>> predicate, bool _) =>
                    queryable.Object.Where(predicate));
        }

        private void SeedUserPatients(params UserPatient[] links)
        {
            _context.Set<UserPatient>().AddRange(links);
            _context.SaveChanges();
        }

        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new GetPatientProfilesRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            SetupPatientProfileRepository(new List<PatientProfile>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            // isUserValid = false -> accessibleProfileIds rỗng -> filterExpression = x => false,
            // nhưng isDataScopeExist vẫn giữ true mặc định (ValidateDataContext return sớm) nên repo vẫn được gọi.
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Process_MissingNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new GetPatientProfilesRequest();

            //Arrange 2
            SetupHttpContextClaim(null);
            SetupPatientProfileRepository(new List<PatientProfile>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Process_MalformedNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new GetPatientProfilesRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");
            SetupPatientProfileRepository(new List<PatientProfile>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Process_ValidUserWithoutAnyProfile_Returns2000WithEmptyListAndDefaultMeta()
        {
            //Arrange 1
            var request = new GetPatientProfilesRequest();

            //Arrange 2
            SetupHttpContextClaim(PatientProfileMockData.UserId.ToString());
            SetupPatientProfileRepository(new List<PatientProfile>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.Meta!.Page.Should().Be(1);
            result.Meta.Size.Should().Be(10);
            result.Meta.Total.Should().Be(0);
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Once); // only the direct-profile-ids lookup, paged query short-circuited
        }

        [Fact]
        public async Task Process_ValidUserWithDirectProfileOnly_Returns2000WithMappedProfileAndDefaultRelationship()
        {
            //Arrange 1
            var request = new GetPatientProfilesRequest();
            var directProfile = PatientProfileMockData.GetDirectProfile();

            //Arrange 2
            SetupHttpContextClaim(PatientProfileMockData.UserId.ToString());
            SetupPatientProfileRepository(new List<PatientProfile> { directProfile });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().ContainSingle();
            var dto = result.Data![0];
            dto.Id_patientProfile.Should().Be(directProfile.Id.ToString());
            dto.FullName.Should().Be(directProfile.FullName);
            dto.Gender.Should().Be(directProfile.Gender.ToString());
            dto.Dob.Should().Be(directProfile.Dob.ToString("dd/MM/yyyy"));
            dto.IdentityNumber.Should().Be(directProfile.IdentityNumber);
            dto.PhoneNumber.Should().Be(directProfile.PhoneNumber);
            dto.Relationship.Should().Be("Bản thân");
            dto.CreatedAt.Should().Be(directProfile.CreatedAt.ToString("dd/MM/yyyy HH:mm"));
            result.Meta!.Total.Should().Be(1);
        }

        [Fact]
        public async Task Process_ValidUserWithLinkedProfile_ReturnsRelationshipFromUserPatientLink()
        {
            //Arrange 1
            var request = new GetPatientProfilesRequest();
            var linkedProfile = PatientProfileMockData.GetLinkedProfile();
            var link = PatientProfileMockData.GetUserPatientLink();

            //Arrange 2
            SetupHttpContextClaim(PatientProfileMockData.UserId.ToString());
            SeedUserPatients(link);
            SetupPatientProfileRepository(new List<PatientProfile> { linkedProfile });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            var dto = result.Data.Should().ContainSingle().Subject;
            dto.Id_patientProfile.Should().Be(linkedProfile.Id.ToString());
            dto.Relationship.Should().Be(link.Relationship);
            dto.IdentityNumber.Should().Be("N/A");
            dto.PhoneNumber.Should().Be("N/A");
        }

        [Fact]
        public async Task Process_SearchTermMatchesFullNameIdentityOrPhone_ReturnsOnlyMatchingProfiles()
        {
            //Arrange 1
            var matchByName = PatientProfileMockData.GetDirectProfile(); // "Nguyen Van A"
            var nonMatch = new PatientProfile
            {
                Id = Guid.NewGuid(),
                UserId = PatientProfileMockData.UserId,
                FullName = "Le Van C",
                Gender = Gender.MALE,
                Dob = new DateTime(1985, 1, 1),
                IdentityNumber = "999999999999",
                PhoneNumber = "0999999999",
                CreatedAt = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc)
            };
            var request = new GetPatientProfilesRequest { SearchTerm = "nguyen van a" };

            //Arrange 2
            SetupHttpContextClaim(PatientProfileMockData.UserId.ToString());
            SetupPatientProfileRepository(new List<PatientProfile> { matchByName, nonMatch });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle();
            result.Data![0].Id_patientProfile.Should().Be(matchByName.Id.ToString());
            result.Meta!.Total.Should().Be(1);
        }

        [Fact]
        public async Task Process_SearchTermNoMatch_ReturnsEmptyListWithZeroTotal()
        {
            //Arrange 1
            var profile = PatientProfileMockData.GetDirectProfile();
            var request = new GetPatientProfilesRequest { SearchTerm = "khong-ton-tai" };

            //Arrange 2
            SetupHttpContextClaim(PatientProfileMockData.UserId.ToString());
            SetupPatientProfileRepository(new List<PatientProfile> { profile });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeEmpty();
            result.Meta!.Total.Should().Be(0);
        }

        [Fact]
        public async Task Process_Paging_ReturnsCorrectPageOrderedByCreatedAtDescending()
        {
            //Arrange 1
            var older = PatientProfileMockData.GetDirectProfile(); // CreatedAt = 2026-01-10
            var newer = new PatientProfile
            {
                Id = Guid.NewGuid(),
                UserId = PatientProfileMockData.UserId,
                FullName = "Pham Thi D",
                Gender = Gender.FEMALE,
                Dob = new DateTime(1992, 6, 6),
                IdentityNumber = "111111111111",
                PhoneNumber = "0911111111",
                CreatedAt = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc)
            };
            var request = new GetPatientProfilesRequest { PageNumber = 1, PageSize = 1 };

            //Arrange 2
            SetupHttpContextClaim(PatientProfileMockData.UserId.ToString());
            SetupPatientProfileRepository(new List<PatientProfile> { older, newer });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle();
            result.Data![0].Id_patientProfile.Should().Be(newer.Id.ToString()); // most recent first
            result.Meta!.Total.Should().Be(2);
            result.Meta.Page.Should().Be(1);
            result.Meta.Size.Should().Be(1);
            result.Meta.TotalPages.Should().Be(2);
            result.Meta.HasNext.Should().BeTrue();
            result.Meta.HasPrevious.Should().BeFalse();
        }

        [Fact]
        public async Task Process_ValidUser_UsesAccessibleProfileIdsPredicate()
        {
            //Arrange 1
            var directProfile = PatientProfileMockData.GetDirectProfile();
            var linkedProfile = PatientProfileMockData.GetLinkedProfile();
            var link = PatientProfileMockData.GetUserPatientLink();
            var foreignProfile = new PatientProfile
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FullName = "Foreign User",
                Gender = Gender.OTHER,
                Dob = new DateTime(2000, 1, 1),
                CreatedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc)
            };
            var request = new GetPatientProfilesRequest();

            //Arrange 2
            SetupHttpContextClaim(PatientProfileMockData.UserId.ToString());
            SeedUserPatients(link);
            SetupPatientProfileRepository(new List<PatientProfile> { directProfile, linkedProfile, foreignProfile });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().HaveCount(2);
            result.Data!.Select(x => x.Id_patientProfile)
                .Should().BeEquivalentTo(new[] { directProfile.Id.ToString(), linkedProfile.Id.ToString() });
            result.Meta!.Total.Should().Be(2);
        }
    }
}