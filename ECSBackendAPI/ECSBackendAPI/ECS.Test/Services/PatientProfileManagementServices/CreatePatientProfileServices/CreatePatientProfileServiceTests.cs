using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using ECS.Application.Services.PatientProfileManagementServices.CreatePatientProfileServices;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Storage;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.PatientProfileManagementServices.CreatePatientProfileServices
{
    public class CreatePatientProfileServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext>> _patientProfileRepositoryMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly Mock<IDbContextTransaction> _transactionMock = new();
        private readonly CreatePatientProfileService _sut;

        public CreatePatientProfileServiceTests()
        {
            _sut = new CreatePatientProfileService(
                _patientProfileRepositoryMock.Object,
                _httpContextAccessorMock.Object);

            _patientProfileRepositoryMock
                .Setup(x => x.BeginTransactionAsync())
                .ReturnsAsync(_transactionMock.Object);
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

        private void SetupExistingIdentityNumbers(List<PatientProfile> rows)
        {
            var queryable = rows.BuildMockDbSet();

            _patientProfileRepositoryMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<PatientProfile, bool>>>(),
                    It.IsAny<bool>()))
                .Returns((Expression<Func<PatientProfile, bool>> predicate, bool _) =>
                    queryable.Object.Where(predicate));
        }

        private static CreatePatientProfileRequest BuildValidRequest(
            string relationship = "Bản thân",
            string? identityNumber = "001090012345")
        {
            return new CreatePatientProfileRequest
            {
                FullName = "  Nguyen Van A  ",
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 5, 20),
                IdentityNumber = identityNumber,
                Address = "  123 Le Loi  ",
                PhoneNumber = "  0912345678  ",
                BhytNumber = "  BH123  ",
                BloodType = "  O+  ",
                Allergies = "  Penicillin  ",
                MedicalHistory = "  None  ",
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
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _patientProfileRepositoryMock.Verify(x => x.BeginTransactionAsync(), Times.Never);
            _patientProfileRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<PatientProfile>()), Times.Never);
        }

        [Fact]
        public async Task Process_MissingNameIdentifierClaim_Returns4033AuthenticationError()
        {
            //Arrange 1
            var request = BuildValidRequest();

            //Arrange 2
            SetupHttpContextClaim(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(x => x.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_MalformedNameIdentifierClaim_Returns4033AuthenticationError()
        {
            //Arrange 1
            var request = BuildValidRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(x => x.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_IdentityNumberNull_SkipsUniquenessCheckAndReturns2005()
        {
            //Arrange 1
            var request = BuildValidRequest(identityNumber: null);

            //Arrange 2
            SetupHttpContextClaim(Guid.NewGuid().ToString());
            _patientProfileRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<PatientProfile>())).ReturnsAsync(Guid.NewGuid());
            _patientProfileRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data.Should().NotBeNull();
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_IdentityNumberAlreadyExists_Returns4043ConflictError()
        {
            //Arrange 1
            var request = BuildValidRequest(identityNumber: "001090012345");
            var conflictingProfile = new PatientProfile
            {
                Id = Guid.NewGuid(),
                FullName = "Someone Else",
                Gender = Gender.MALE,
                Dob = new DateTime(1980, 1, 1),
                IdentityNumber = "001090012345",
                CreatedAt = DateTime.UtcNow
            };

            //Arrange 2
            SetupHttpContextClaim(Guid.NewGuid().ToString());
            SetupExistingIdentityNumbers(new List<PatientProfile> { conflictingProfile });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4043.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(x => x.BeginTransactionAsync(), Times.Never);
            _patientProfileRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<PatientProfile>()), Times.Never);
        }

        [Fact]
        public async Task Process_IdentityNumberUnique_ProceedsAndReturns2005()
        {
            //Arrange 1
            var request = BuildValidRequest(identityNumber: "001090012345");

            //Arrange 2
            SetupHttpContextClaim(Guid.NewGuid().ToString());
            SetupExistingIdentityNumbers(new List<PatientProfile>());
            _patientProfileRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<PatientProfile>())).ReturnsAsync(Guid.NewGuid());
            _patientProfileRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data.Should().NotBeNull();
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _patientProfileRepositoryMock.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _patientProfileRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<PatientProfile>()), Times.Once);
            _patientProfileRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
            _transactionMock.Verify(x => x.CommitAsync(default), Times.Once);
            _transactionMock.Verify(x => x.RollbackAsync(default), Times.Never);
        }

        [Fact]
        public async Task Process_RelationshipIsBanThan_SetsUserIdOnProfileAndReturnsMappedResponse()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            var request = BuildValidRequest(relationship: "Bản thân", identityNumber: null);
            PatientProfile? capturedProfile = null;

            //Arrange 2
            SetupHttpContextClaim(userId.ToString());
            _patientProfileRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<PatientProfile>()))
                .Callback<PatientProfile>(p => capturedProfile = p)
                .ReturnsAsync(Guid.NewGuid());
            _patientProfileRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            capturedProfile.Should().NotBeNull();
            capturedProfile!.UserId.Should().Be(userId);
            capturedProfile.FullName.Should().Be("Nguyen Van A"); // trimmed
            capturedProfile.UserPatients.Should().ContainSingle();
            capturedProfile.UserPatients!.First().Relationship.Should().Be("Bản thân");

            result.Data!.PatientProfileId.Should().Be(capturedProfile.Id);
            result.Data.FullName.Should().Be(capturedProfile.FullName);
            result.Data.Relationship.Should().Be("Bản thân");
            result.Data.CreatedAt.Should().Be(capturedProfile.CreatedAt.ToString("dd/MM/yyyy HH:mm"));
        }

        [Fact]
        public async Task Process_RelationshipIsNotBanThan_LeavesUserIdNullOnProfile()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            var request = BuildValidRequest(relationship: "Con", identityNumber: null);
            PatientProfile? capturedProfile = null;

            //Arrange 2
            SetupHttpContextClaim(userId.ToString());
            _patientProfileRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<PatientProfile>()))
                .Callback<PatientProfile>(p => capturedProfile = p)
                .ReturnsAsync(Guid.NewGuid());
            _patientProfileRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            capturedProfile.Should().NotBeNull();
            capturedProfile!.UserId.Should().BeNull();
            capturedProfile.UserPatients!.First().UserId.Should().Be(userId);
            result.Data!.Relationship.Should().Be("Con");
        }

        [Fact]
        public async Task Process_CreateAsyncThrows_RollsBackAndReturns5001DatabaseError()
        {
            //Arrange 1
            var request = BuildValidRequest(identityNumber: null);

            //Arrange 2
            SetupHttpContextClaim(Guid.NewGuid().ToString());
            _patientProfileRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<PatientProfile>()))
                .ThrowsAsync(new InvalidOperationException("db error"));

            //Act
            var result = await _sut.Process(request);

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
            var request = BuildValidRequest(identityNumber: null);

            //Arrange 2
            SetupHttpContextClaim(Guid.NewGuid().ToString());
            _patientProfileRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<PatientProfile>())).ReturnsAsync(Guid.NewGuid());
            _patientProfileRepositoryMock
                .Setup(x => x.SaveChangesAsync())
                .ThrowsAsync(new InvalidOperationException("db error"));

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
            result.Data.Should().BeNull();
            _transactionMock.Verify(x => x.RollbackAsync(default), Times.Once);
        }

        [Fact]
        public async Task Process_TrimsAllOptionalStringFieldsOnProfile()
        {
            //Arrange 1
            var request = BuildValidRequest(identityNumber: "  123456789012  ");
            PatientProfile? capturedProfile = null;

            //Arrange 2
            SetupHttpContextClaim(Guid.NewGuid().ToString());
            SetupExistingIdentityNumbers(new List<PatientProfile>());
            _patientProfileRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<PatientProfile>()))
                .Callback<PatientProfile>(p => capturedProfile = p)
                .ReturnsAsync(Guid.NewGuid());
            _patientProfileRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            capturedProfile.Should().NotBeNull();
            capturedProfile!.IdentityNumber.Should().Be("123456789012");
            capturedProfile.Address.Should().Be("123 Le Loi");
            capturedProfile.PhoneNumber.Should().Be("0912345678");
            capturedProfile.BhytNumber.Should().Be("BH123");
            capturedProfile.BloodType.Should().Be("O+");
            capturedProfile.Allergies.Should().Be("Penicillin");
            capturedProfile.MedicalHistory.Should().Be("None");
        }

        [Fact]
        public async Task Process_AllOptionalFieldsNull_LeavesCorrespondingProfileFieldsNull()
        {
            //Arrange 1
            var request = new CreatePatientProfileRequest
            {
                FullName = "Nguyen Van A",
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 5, 20),
                IdentityNumber = null,
                Address = null,
                PhoneNumber = null,
                BhytNumber = null,
                BloodType = null,
                Allergies = null,
                MedicalHistory = null,
                Relationship = "Bản thân"
            };
            PatientProfile? capturedProfile = null;

            //Arrange 2
            SetupHttpContextClaim(Guid.NewGuid().ToString());
            _patientProfileRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<PatientProfile>()))
                .Callback<PatientProfile>(p => capturedProfile = p)
                .ReturnsAsync(Guid.NewGuid());
            _patientProfileRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            capturedProfile.Should().NotBeNull();
            capturedProfile!.IdentityNumber.Should().BeNull();
            capturedProfile.Address.Should().BeNull();
            capturedProfile.PhoneNumber.Should().BeNull();
            capturedProfile.BhytNumber.Should().BeNull();
            capturedProfile.BloodType.Should().BeNull();
            capturedProfile.Allergies.Should().BeNull();
            capturedProfile.MedicalHistory.Should().BeNull();
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never); // IdentityNumber null -> bỏ qua bước kiểm tra trùng lặp
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
                CreatedAt = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc)
            };
            var link = new UserPatient
            {
                UserId = Guid.NewGuid(),
                PatientId = profile.Id,
                Relationship = null
            };

            //Arrange 2
            var method = typeof(CreatePatientProfileService).GetMethod(
                "MapToResponse",
                BindingFlags.NonPublic | BindingFlags.Instance);

            //Act
            var response = (CreatePatientProfileResponse)method!.Invoke(_sut, new object[] { profile, link })!;

            //Assert
            response.Relationship.Should().Be("N/A");
            response.PatientProfileId.Should().Be(profile.Id);
            response.FullName.Should().Be(profile.FullName);
            response.CreatedAt.Should().Be(profile.CreatedAt.ToString("dd/MM/yyyy HH:mm"));
        }
    }
}