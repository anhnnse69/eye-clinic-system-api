using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Services.PatientProfileManagementServices.ViewPatientProfileDetailServices;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.PatientProfileManagementServices.ViewPatientProfileDetailServices
{
    public class ViewPatientProfileDetailServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>> _patientProfileRepositoryMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly AppDbContext _context;
        private readonly ViewPatientProfileDetailService _sut;

        public ViewPatientProfileDetailServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _sut = new ViewPatientProfileDetailService(
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

        private void SetupPatientProfileRepository(PatientProfile? profile)
        {
            var rows = profile == null ? new List<PatientProfile>() : new List<PatientProfile> { profile };
            var queryable = rows.BuildMockDbSet();

            _patientProfileRepositoryMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<PatientProfile, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SeedUserPatients(params UserPatient[] links)
        {
            _context.Set<UserPatient>().AddRange(links);
            _context.SaveChanges();
        }

        private static PatientProfile BuildProfile(Guid profileId, Guid? ownerUserId = null)
        {
            return new PatientProfile
            {
                Id = profileId,
                UserId = ownerUserId,
                FullName = "Nguyen Van A",
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 5, 20),
                IdentityNumber = "001090012345",
                Address = "123 Le Loi",
                PhoneNumber = "0912345678",
                BhytNumber = "BH123",
                BloodType = "O+",
                Allergies = "Penicillin",
                MedicalHistory = "None",
                CreatedAt = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc)
            };
        }

        [Fact]
        public async Task Process_NullHttpContext_Returns4033AuthenticationError()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();

            //Arrange 2
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

            //Act
            var result = await _sut.Process(profileId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_MissingNameIdentifierClaim_Returns4033AuthenticationError()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();

            //Arrange 2
            SetupHttpContextClaim(null);

            //Act
            var result = await _sut.Process(profileId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_MalformedNameIdentifierClaim_Returns4033AuthenticationError()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");

            //Act
            var result = await _sut.Process(profileId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_ProfileNotFound_Returns4010NotFoundError()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();

            //Arrange 2
            SetupHttpContextClaim(Guid.NewGuid().ToString());
            SetupPatientProfileRepository(null);

            //Act
            var result = await _sut.Process(profileId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4010.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_UserNeitherOwnerNorLinked_Returns4014AuthorizationError()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var profile = BuildProfile(profileId, ownerUserId);

            //Arrange 2
            SetupHttpContextClaim(otherUserId.ToString());
            SetupPatientProfileRepository(profile);

            //Act
            var result = await _sut.Process(profileId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_UserIsOwner_Returns2000WithDefaultRelationshipWhenNoLink()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var profile = BuildProfile(profileId, ownerUserId);

            //Arrange 2
            SetupHttpContextClaim(ownerUserId.ToString());
            SetupPatientProfileRepository(profile);

            //Act
            var result = await _sut.Process(profileId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.PatientProfileId.Should().Be(profile.Id);
            result.Data.FullName.Should().Be(profile.FullName);
            result.Data.Gender.Should().Be(profile.Gender);
            result.Data.Dob.Should().Be(profile.Dob);
            result.Data.IdentityNumber.Should().Be(profile.IdentityNumber);
            result.Data.Address.Should().Be(profile.Address);
            result.Data.PhoneNumber.Should().Be(profile.PhoneNumber);
            result.Data.BhytNumber.Should().Be(profile.BhytNumber);
            result.Data.BloodType.Should().Be(profile.BloodType);
            result.Data.Allergies.Should().Be(profile.Allergies);
            result.Data.MedicalHistory.Should().Be(profile.MedicalHistory);
            result.Data.CreatedAt.Should().Be(profile.CreatedAt);
            result.Data.UpdatedAt.Should().Be(profile.UpdatedAt);
            result.Data.Relationship.Should().Be("Bản thân"); // không có UserPatient link tương ứng
        }

        [Fact]
        public async Task Process_UserIsNotOwnerButLinked_Returns2000WithRelationshipFromLink()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var linkedUserId = Guid.NewGuid();
            var profile = BuildProfile(profileId, ownerUserId);
            var link = new UserPatient
            {
                UserId = linkedUserId,
                PatientId = profileId,
                Relationship = "Con"
            };

            //Arrange 2
            SetupHttpContextClaim(linkedUserId.ToString());
            SetupPatientProfileRepository(profile);
            SeedUserPatients(link);

            //Act
            var result = await _sut.Process(profileId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Relationship.Should().Be("Con");
        }

        [Fact]
        public async Task Process_LinkExistsWithWhitespaceRelationship_ReturnsDefaultRelationship()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var profile = BuildProfile(profileId, ownerUserId);
            var link = new UserPatient
            {
                UserId = ownerUserId,
                PatientId = profileId,
                Relationship = "   "
            };

            //Arrange 2
            SetupHttpContextClaim(ownerUserId.ToString());
            SetupPatientProfileRepository(profile);
            SeedUserPatients(link);

            //Act
            var result = await _sut.Process(profileId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Relationship.Should().Be("Bản thân");
        }

        [Fact]
        public async Task Process_OwnerWithExistingLink_UsesRelationshipFromLinkNotDefault()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var profile = BuildProfile(profileId, ownerUserId);
            var link = new UserPatient
            {
                UserId = ownerUserId,
                PatientId = profileId,
                Relationship = "Chủ tài khoản"
            };

            //Arrange 2
            SetupHttpContextClaim(ownerUserId.ToString());
            SetupPatientProfileRepository(profile);
            SeedUserPatients(link);

            //Act
            var result = await _sut.Process(profileId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Relationship.Should().Be("Chủ tài khoản");
        }
    }
}