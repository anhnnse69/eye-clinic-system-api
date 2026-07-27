using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using ECS.Application.Services.PatientProfileManagementServices.UpdatePatientProfileServices;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.PatientProfileManagementServices.UpdatePatientProfileServices
{
    public class UpdatePatientProfileServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext>> _patientProfileRepositoryMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly Mock<IDbContextTransaction> _transactionMock = new();
        private readonly AppDbContext _context;
        private readonly UpdatePatientProfileService _sut;

        public UpdatePatientProfileServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _sut = new UpdatePatientProfileService(
                _patientProfileRepositoryMock.Object,
                _httpContextAccessorMock.Object,
                _context);

            _patientProfileRepositoryMock
                .Setup(x => x.BeginTransactionAsync())
                .ReturnsAsync(_transactionMock.Object);
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

        private void SetupGetById(PatientProfile? profile)
        {
            _patientProfileRepositoryMock
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Expression<Func<PatientProfile, object>>>()))
                .ReturnsAsync(profile);
        }

        private void SetupConflictingIdentityNumbers(List<PatientProfile> rows)
        {
            var queryable = rows.BuildMockDbSet();

            _patientProfileRepositoryMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<PatientProfile, bool>>>(),
                    It.IsAny<bool>()))
                .Returns((Expression<Func<PatientProfile, bool>> predicate, bool _) =>
                    queryable.Object.Where(predicate));
        }

        private static PatientProfile BuildExistingProfile(Guid profileId, Guid ownerUserId, UserPatient? link = null)
        {
            var profile = new PatientProfile
            {
                Id = profileId,
                UserId = ownerUserId,
                FullName = "Nguyen Van A",
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 5, 20),
                IdentityNumber = "001090012345",
                PhoneNumber = "0912345678",
                CreatedAt = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc)
            };

            var actualLink = link ?? new UserPatient
            {
                UserId = ownerUserId,
                PatientId = profileId,
                Relationship = "Bản thân",
                CreatedAt = profile.CreatedAt
            };

            profile.UserPatients = new List<UserPatient> { actualLink };
            return profile;
        }

        private static UpdatePatientProfileRequest BuildValidRequest(
            string relationship = "Bản thân",
            string? identityNumber = "001090099999")
        {
            return new UpdatePatientProfileRequest
            {
                FullName = "  Nguyen Van A Updated  ",
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 5, 20),
                IdentityNumber = identityNumber,
                Address = "  456 Tran Phu  ",
                PhoneNumber = "  0987654321  ",
                BhytNumber = "  BH999  ",
                BloodType = "  A+  ",
                Allergies = "  None  ",
                MedicalHistory = "  Updated  ",
                Relationship = relationship
            };
        }

        [Fact]
        public async Task Process_NullHttpContext_Returns4033AuthenticationError()
        {
            //Arrange 1
            var request = BuildValidRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

            //Act
            var result = await _sut.Process(Guid.NewGuid(), request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(
                x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Expression<Func<PatientProfile, object>>>()),
                Times.Never);
            _patientProfileRepositoryMock.Verify(x => x.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_MissingNameIdentifierClaim_Returns4033AuthenticationError()
        {
            //Arrange 1
            var request = BuildValidRequest();

            //Arrange 2
            SetupHttpContextClaim(null);

            //Act
            var result = await _sut.Process(Guid.NewGuid(), request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_MalformedNameIdentifierClaim_Returns4033AuthenticationError()
        {
            //Arrange 1
            var request = BuildValidRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");

            //Act
            var result = await _sut.Process(Guid.NewGuid(), request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_ProfileNotFound_Returns4010NotFoundError()
        {
            //Arrange 1
            var request = BuildValidRequest();

            //Arrange 2
            SetupHttpContextClaim(Guid.NewGuid().ToString());
            SetupGetById(null);

            //Act
            var result = await _sut.Process(Guid.NewGuid(), request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4010.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(x => x.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_UserNotOwnerOfProfile_Returns4014AuthorizationError()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var profile = BuildExistingProfile(profileId, ownerUserId);
            var request = BuildValidRequest();

            //Arrange 2
            SetupHttpContextClaim(otherUserId.ToString());
            SetupGetById(profile);

            //Act
            var result = await _sut.Process(profileId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(x => x.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_IdentityNumberConflictsWithAnotherProfile_Returns4043ConflictError()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var profile = BuildExistingProfile(profileId, ownerUserId);
            var conflictingProfile = new PatientProfile
            {
                Id = Guid.NewGuid(),
                FullName = "Other",
                Gender = Gender.MALE,
                Dob = new DateTime(1980, 1, 1),
                IdentityNumber = "001090099999",
                CreatedAt = DateTime.UtcNow
            };
            var request = BuildValidRequest(identityNumber: "001090099999");

            //Arrange 2
            SetupHttpContextClaim(ownerUserId.ToString());
            SetupGetById(profile);
            SetupConflictingIdentityNumbers(new List<PatientProfile> { conflictingProfile });

            //Act
            var result = await _sut.Process(profileId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4043.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(x => x.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_IdentityNumberNull_SkipsUniquenessCheckAndReturns2006()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var profile = BuildExistingProfile(profileId, ownerUserId);
            var request = BuildValidRequest(identityNumber: null);

            //Arrange 2
            SetupHttpContextClaim(ownerUserId.ToString());
            SetupGetById(profile);
            _patientProfileRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<PatientProfile>())).Returns(Task.CompletedTask);
            _patientProfileRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            //Act
            var result = await _sut.Process(profileId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            result.Data.Should().NotBeNull();
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_ValidUpdate_Returns2006AndAppliesMutations()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var profile = BuildExistingProfile(profileId, ownerUserId);
            var request = BuildValidRequest(relationship: "Bản thân", identityNumber: "001090099999");

            //Arrange 2
            SetupHttpContextClaim(ownerUserId.ToString());
            SetupGetById(profile);
            SetupConflictingIdentityNumbers(new List<PatientProfile>());
            _patientProfileRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<PatientProfile>())).Returns(Task.CompletedTask);
            _patientProfileRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            //Act
            var result = await _sut.Process(profileId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            result.Data.Should().NotBeNull();
            profile.FullName.Should().Be("Nguyen Van A Updated"); // trimmed
            profile.IdentityNumber.Should().Be("001090099999");
            profile.Address.Should().Be("456 Tran Phu");
            profile.PhoneNumber.Should().Be("0987654321");
            profile.BhytNumber.Should().Be("BH999");
            profile.BloodType.Should().Be("A+");
            profile.Allergies.Should().Be("None");
            profile.MedicalHistory.Should().Be("Updated");
            profile.UserId.Should().Be(ownerUserId); // relationship "Bản thân"
            profile.UserPatients!.First().Relationship.Should().Be("Bản thân");

            result.Data!.PatientProfileId.Should().Be(profile.Id);
            result.Data.FullName.Should().Be(profile.FullName);
            result.Data.Relationship.Should().Be("Bản thân");
            result.Data.UpdatedAt.Should().Be(profile.UpdatedAt.ToString("dd/MM/yyyy HH:mm"));

            _patientProfileRepositoryMock.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _patientProfileRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<PatientProfile>()), Times.Once);
            _patientProfileRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
            _transactionMock.Verify(x => x.CommitAsync(default), Times.Once);
            _transactionMock.Verify(x => x.RollbackAsync(default), Times.Never);
        }

        [Fact]
        public async Task Process_RelationshipIsNotBanThan_SetsUserIdNullOnProfile()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var profile = BuildExistingProfile(profileId, ownerUserId);
            var request = BuildValidRequest(relationship: "Con", identityNumber: null);

            //Arrange 2
            SetupHttpContextClaim(ownerUserId.ToString());
            SetupGetById(profile);
            _patientProfileRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<PatientProfile>())).Returns(Task.CompletedTask);
            _patientProfileRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            //Act
            var result = await _sut.Process(profileId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            profile.UserId.Should().BeNull();
            result.Data!.Relationship.Should().Be("Con");
        }

        [Fact]
        public async Task Process_AllOptionalFieldsBlank_LeavesCorrespondingProfileFieldsNull()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var profile = BuildExistingProfile(profileId, ownerUserId);
            var request = new UpdatePatientProfileRequest
            {
                FullName = "Nguyen Van A",
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 5, 20),
                IdentityNumber = "   ",
                Address = "   ",
                PhoneNumber = "   ",
                BhytNumber = "   ",
                BloodType = "   ",
                Allergies = "   ",
                MedicalHistory = "   ",
                Relationship = "Bản thân"
            };

            //Arrange 2
            SetupHttpContextClaim(ownerUserId.ToString());
            SetupGetById(profile);
            _patientProfileRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<PatientProfile>())).Returns(Task.CompletedTask);
            _patientProfileRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            //Act
            var result = await _sut.Process(profileId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            profile.IdentityNumber.Should().BeNull();
            profile.Address.Should().BeNull();
            profile.PhoneNumber.Should().BeNull();
            profile.BhytNumber.Should().BeNull();
            profile.BloodType.Should().BeNull();
            profile.Allergies.Should().BeNull();
            profile.MedicalHistory.Should().BeNull();
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_UpdateAsyncThrows_RollsBackAndReturns5001DatabaseError()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var profile = BuildExistingProfile(profileId, ownerUserId);
            var request = BuildValidRequest(identityNumber: null);

            //Arrange 2
            SetupHttpContextClaim(ownerUserId.ToString());
            SetupGetById(profile);
            _patientProfileRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<PatientProfile>()))
                .ThrowsAsync(new InvalidOperationException("db error"));

            //Act
            var result = await _sut.Process(profileId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
            result.Data.Should().BeNull();
            _transactionMock.Verify(x => x.RollbackAsync(default), Times.Once);
            _transactionMock.Verify(x => x.CommitAsync(default), Times.Never);
        }

        [Fact]
        public async Task Process_SaveChangesAsyncThrows_RollsBackAndReturns5001DatabaseError()
        {
            //Arrange 1
            var profileId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var profile = BuildExistingProfile(profileId, ownerUserId);
            var request = BuildValidRequest(identityNumber: null);

            //Arrange 2
            SetupHttpContextClaim(ownerUserId.ToString());
            SetupGetById(profile);
            _patientProfileRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<PatientProfile>())).Returns(Task.CompletedTask);
            _patientProfileRepositoryMock
                .Setup(x => x.SaveChangesAsync())
                .ThrowsAsync(new InvalidOperationException("db error"));

            //Act
            var result = await _sut.Process(profileId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
            result.Data.Should().BeNull();
            _transactionMock.Verify(x => x.RollbackAsync(default), Times.Once);
        }

        [Fact]
        public void MapToResponse_NullRelationshipLink_ReturnsNAAsRelationship()
        {
            //Arrange 1
            var profile = new PatientProfile
            {
                Id = Guid.NewGuid(),
                FullName = "Nguyen Van A",
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 5, 20),
                UpdatedAt = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc)
            };
            var link = new UserPatient
            {
                UserId = Guid.NewGuid(),
                PatientId = profile.Id,
                Relationship = null
            };

            //Arrange 2
            var method = typeof(UpdatePatientProfileService).GetMethod(
                "MapToResponse",
                BindingFlags.NonPublic | BindingFlags.Instance);

            //Act
            var response = (UpdatePatientProfileResponse)method!.Invoke(_sut, new object[] { profile, link })!;

            //Assert
            response.Relationship.Should().Be("N/A");
            response.PatientProfileId.Should().Be(profile.Id);
            response.FullName.Should().Be(profile.FullName);
            response.UpdatedAt.Should().Be(profile.UpdatedAt.ToString("dd/MM/yyyy HH:mm"));
        }
    }
}