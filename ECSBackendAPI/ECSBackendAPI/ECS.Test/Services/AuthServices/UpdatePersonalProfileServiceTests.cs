using ECS.Application.Services.AuthServices.UpdatePersonalProfileServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;

namespace ECS.Test.Services.AuthServices
{
    /// <summary>
    /// Unit tests for <see cref="UpdatePersonalProfileService"/>.
    /// Covers: user not found (KeyNotFoundException), patient happy path, doctor with active profile,
    /// doctor with no active profile, doctor with null DoctorProfiles, whitespace trimming,
    /// null email/title handling, and response shape.
    /// Goal: 100% line coverage on UpdatePersonalProfileService.cs.
    /// </summary>
    public class UpdatePersonalProfileServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>> _userRepoMock;
        private readonly UpdatePersonalProfileService _sut;

        public UpdatePersonalProfileServiceTests()
        {
            _userRepoMock = new Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>>();
            _sut = new UpdatePersonalProfileService(_userRepoMock.Object);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// IMPORTANT: Must use AsyncQueryable wrapping for the async FindAll(...).Include(...).FirstOrDefaultAsync(...) chain.
        /// </summary>
        private void SetupUserRepo(User? returnUser)
        {
            var users = returnUser != null ? new List<User> { returnUser } : new List<User>();
            var mockQueryable = users.BuildMockDbSet<User>();

            _userRepoMock
                .Setup(r => r.FindAll(It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        private void SetupUpdateAndSave()
        {
            _userRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);
            _userRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// TC-UPP-01: User not found → throws KeyNotFoundException with code "APP_MESSAGE_404_USER_NOT_FOUND".
        /// Covers: FetchUserWithRelatedProfilesOrThrow (user == null branch).
        /// </summary>
        [Fact]
        public async Task Process_UserNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var currentUserId = Guid.NewGuid();
            var request = new UpdatePersonalProfileRequest
            {
                FullName = "Nguyen Van A",
                Phone = "0901234567",
                Email = "user@ECS.vn"
            };

            //Arrange 2
            SetupUserRepo(null);

            //Act
            var act = async () => await _sut.Process(currentUserId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("APP_MESSAGE_404_USER_NOT_FOUND");
            _userRepoMock.Verify(
                r => r.FindAll(It.IsAny<bool>()),
                Times.Once);
            _userRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<User>()),
                Times.Never);
            _userRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-UPP-02: Patient user found → updates fields, saves, returns success.
        /// Covers: FetchUserWithRelatedProfilesOrThrow (user != null), MapAndMutateProperties (full path),
        /// MutateProfessionalProfileIfDoctor (Role != DOCTOR branch, skipped),
        /// SaveDataChanges, BuildResponseDto, CreateApiResponse.
        /// </summary>
        [Fact]
        public async Task Process_PatientUserFound_UpdatesUserFieldsAndReturnsSuccess()
        {
            //Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var currentUserId = user.Id;
            var request = new UpdatePersonalProfileRequest
            {
                FullName = "Nguyen Van B Updated",
                Phone = "0912345678",
                Email = "updated@ECS.vn",
                AvatarUrl = "https://cdn.example.com/avatar.png",
                Title = "Should Be Ignored",
                ExperienceYears = 5,
                Bio = "Should Be Ignored",
                SpecialtyId = Guid.NewGuid()
            };

            //Arrange 2
            SetupUserRepo(user);
            SetupUpdateAndSave();

            //Act
            var result = await _sut.Process(currentUserId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(user.Id.ToString());
            result.Data.FullName.Should().Be("Nguyen Van B Updated");
            result.Data.Role.Should().Be("PATIENT");
            result.Data.UpdatedAt.Should().NotBeNullOrWhiteSpace();
            DateTime.TryParseExact(
                result.Data.UpdatedAt,
                "dd/MM/yyyy HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out _).Should().BeTrue();

            user.FullName.Should().Be("Nguyen Van B Updated");
            user.Phone.Should().Be("0912345678");
            user.Email.Should().Be("updated@ECS.vn");
            user.AvatarUrl.Should().Be("https://cdn.example.com/avatar.png");
            user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            _userRepoMock.Verify(
                r => r.UpdateAsync(user),
                Times.Once);
            _userRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// TC-UPP-03: Doctor user with active DoctorProfile → mutates profile fields.
        /// Covers: MutateProfessionalProfileIfDoctor (Role == DOCTOR AND activeDoctorProfile != null branch).
        /// </summary>
        [Fact]
        public async Task Process_DoctorUserWithActiveProfile_UpdatesDoctorProfileFields()
        {
            //Arrange 1
            var user = UserMockData.GetDoctorUser();
            var activeProfile = new DoctorProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ClinicId = Guid.NewGuid(),
                Title = "Old Title",
                ExperienceYears = 1,
                Bio = "Old Bio",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1),
                UpdatedAt = DateTime.UtcNow.AddYears(-1)
            };
            user.DoctorProfiles = new List<DoctorProfile> { activeProfile };
            var newSpecialtyId = Guid.NewGuid();
            var request = new UpdatePersonalProfileRequest
            {
                FullName = "BS. Le Van C Updated",
                Phone = "0923456789",
                Email = "levanc-updated@ECS.vn",
                AvatarUrl = null,
                Title = "Senior Ophthalmologist",
                ExperienceYears = 10,
                Bio = "Updated bio with 10 years of experience.",
                SpecialtyId = newSpecialtyId
            };

            //Arrange 2
            SetupUserRepo(user);
            SetupUpdateAndSave();

            //Act
            var result = await _sut.Process(user.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Role.Should().Be("DOCTOR");

            activeProfile.Title.Should().Be("Senior Ophthalmologist");
            activeProfile.ExperienceYears.Should().Be(10);
            activeProfile.Bio.Should().Be("Updated bio with 10 years of experience.");
            activeProfile.SpecialtyId.Should().Be(newSpecialtyId);
            activeProfile.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            user.FullName.Should().Be("BS. Le Van C Updated");
            user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// TC-UPP-04: Doctor user with only inactive DoctorProfiles → does not mutate profile.
        /// Covers: MutateProfessionalProfileIfDoctor (Role == DOCTOR AND activeDoctorProfile == null branch, skipped).
        /// </summary>
        [Fact]
        public async Task Process_DoctorUserWithNoActiveProfile_DoesNotMutateProfile()
        {
            //Arrange 1
            var user = UserMockData.GetDoctorUser();
            var inactiveProfile = new DoctorProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ClinicId = Guid.NewGuid(),
                Title = "Original Title",
                ExperienceYears = 3,
                Bio = "Original Bio",
                IsActive = false,
                CreatedAt = DateTime.UtcNow.AddYears(-2),
                UpdatedAt = DateTime.UtcNow.AddYears(-2)
            };
            user.DoctorProfiles = new List<DoctorProfile> { inactiveProfile };
            var oldTitle = inactiveProfile.Title;
            var oldBio = inactiveProfile.Bio;
            var oldExperienceYears = inactiveProfile.ExperienceYears;
            var oldSpecialtyId = inactiveProfile.SpecialtyId;
            var oldUpdatedAt = inactiveProfile.UpdatedAt;

            var request = new UpdatePersonalProfileRequest
            {
                FullName = "BS. Le Van C Updated",
                Phone = "0923456789",
                Email = "levanc-updated@ECS.vn",
                AvatarUrl = null,
                Title = "Should Not Be Applied",
                ExperienceYears = 99,
                Bio = "Should Not Be Applied",
                SpecialtyId = Guid.NewGuid()
            };

            //Arrange 2
            SetupUserRepo(user);
            SetupUpdateAndSave();

            //Act
            var result = await _sut.Process(user.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());

            inactiveProfile.Title.Should().Be(oldTitle);
            inactiveProfile.Bio.Should().Be(oldBio);
            inactiveProfile.ExperienceYears.Should().Be(oldExperienceYears);
            inactiveProfile.SpecialtyId.Should().Be(oldSpecialtyId);
            inactiveProfile.UpdatedAt.Should().Be(oldUpdatedAt);

            // User-level fields ARE still updated.
            user.FullName.Should().Be("BS. Le Van C Updated");
            user.Phone.Should().Be("0923456789");

            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// TC-UPP-05: Doctor user with null DoctorProfiles → does not throw, profile-mutation branch skipped.
        /// Covers: MutateProfessionalProfileIfDoctor (DoctorProfiles?.FirstOrDefault(...) null-conditional path).
        /// </summary>
        [Fact]
        public async Task Process_DoctorUserWithNullDoctorProfiles_DoesNotThrow()
        {
            //Arrange 1
            var user = UserMockData.GetDoctorUser();
            user.DoctorProfiles = null;
            var request = new UpdatePersonalProfileRequest
            {
                FullName = "BS. Le Van C Updated",
                Phone = "0923456789",
                Email = "levanc-updated@ECS.vn",
                Title = "Senior",
                ExperienceYears = 7,
                Bio = "Bio",
                SpecialtyId = Guid.NewGuid()
            };

            //Arrange 2
            SetupUserRepo(user);
            SetupUpdateAndSave();

            //Act
            var result = await _sut.Process(user.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            user.FullName.Should().Be("BS. Le Van C Updated");
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// TC-UPP-06: Request with leading/trailing whitespace is trimmed on FullName, Phone, Email.
        /// Covers: MapAndMutateProperties (.Trim() calls on FullName, Phone, Email).
        /// </summary>
        [Fact]
        public async Task Process_TrimLeadingTrailingWhitespace_OnStringFields()
        {
            //Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var request = new UpdatePersonalProfileRequest
            {
                FullName = "  Nguyen Van A  ",
                Phone = "  0901234567  ",
                Email = "  user@ECS.vn  ",
                AvatarUrl = null
            };

            //Arrange 2
            SetupUserRepo(user);
            SetupUpdateAndSave();

            //Act
            var result = await _sut.Process(user.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            user.FullName.Should().Be("Nguyen Van A");
            user.Phone.Should().Be("0901234567");
            user.Email.Should().Be("user@ECS.vn");
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        /// <summary>
        /// TC-UPP-07: Request with null Email → assigns null to user.Email (?.Trim() short-circuit).
        /// Covers: MapAndMutateProperties (user.Email = request.Email?.Trim() null-conditional path).
        /// </summary>
        [Fact]
        public async Task Process_NullEmailRequest_AssignsNullToUserEmail()
        {
            //Arrange 1
            var user = UserMockData.GetValidActiveUser();
            var request = new UpdatePersonalProfileRequest
            {
                FullName = "Nguyen Van A",
                Phone = "0901234567",
                Email = null,
                AvatarUrl = null
            };

            //Arrange 2
            SetupUserRepo(user);
            SetupUpdateAndSave();

            //Act
            var result = await _sut.Process(user.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            user.Email.Should().BeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        /// <summary>
        /// TC-UPP-08: Doctor user with active profile, request has null Title and Bio → both assigned to null.
        /// Covers: MutateProfessionalProfileIfDoctor (request.Title?.Trim() and request.Bio?.Trim() null-conditional paths).
        /// </summary>
        [Fact]
        public async Task Process_NullTitleRequest_DoctorProfileWithActiveProfile_AssignsNullToTitle()
        {
            //Arrange 1
            var user = UserMockData.GetDoctorUser();
            var activeProfile = new DoctorProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ClinicId = Guid.NewGuid(),
                Title = "Old Title",
                ExperienceYears = 1,
                Bio = "Old Bio",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1),
                UpdatedAt = DateTime.UtcNow.AddYears(-1)
            };
            user.DoctorProfiles = new List<DoctorProfile> { activeProfile };
            var request = new UpdatePersonalProfileRequest
            {
                FullName = "BS. Le Van C",
                Phone = "0923456789",
                Email = null,
                AvatarUrl = null,
                Title = null,
                ExperienceYears = 5,
                Bio = null,
                SpecialtyId = null
            };

            //Arrange 2
            SetupUserRepo(user);
            SetupUpdateAndSave();

            //Act
            var result = await _sut.Process(user.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            activeProfile.Title.Should().BeNull();
            activeProfile.Bio.Should().BeNull();
            activeProfile.ExperienceYears.Should().Be(5);
            activeProfile.SpecialtyId.Should().BeNull();
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        /// <summary>
        /// TC-UPP-09: Response shape — every field of UpdatePersonalProfileResponse is populated correctly.
        /// Covers: BuildResponseDto (Id, FullName, Role.ToString(), UpdatedAt formatted as dd/MM/yyyy HH:mm:ss).
        /// Note: The service overwrites user.UpdatedAt = DateTime.UtcNow in MapAndMutateProperties,
        /// so the response's UpdatedAt always reflects the run-time, not the pre-existing value.
        /// </summary>
        [Fact]
        public async Task Process_ResponseShapeOnSuccess_ContainsAllExpectedFields()
        {
            //Arrange 1
            var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var user = new User
            {
                Id = userId,
                FullName = "Shape Tester Original",
                Phone = "0901234567",
                Email = "shape@ECS.vn",
                Role = UserRole.PATIENT,
                IsActive = true,
                UpdatedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            var request = new UpdatePersonalProfileRequest
            {
                FullName = "Shape Tester",
                Phone = "0901234567",
                Email = "shape@ECS.vn"
            };

            //Arrange 2
            SetupUserRepo(user);
            SetupUpdateAndSave();

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(userId.ToString());
            result.Data.FullName.Should().Be("Shape Tester");
            result.Data.Role.Should().Be("PATIENT");
            // The service sets user.UpdatedAt = DateTime.UtcNow inside MapAndMutateProperties,
            // so the response UpdatedAt must reflect "now" — verify by parsing the formatted string.
            DateTime.TryParseExact(
                result.Data.UpdatedAt,
                "dd/MM/yyyy HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsedUpdatedAt).Should().BeTrue();
            parsedUpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
