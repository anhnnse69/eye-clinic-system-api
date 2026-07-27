using ECS.Application.Services.AuthServices.ViewPersonalProfileServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.AuthServices
{
    /// <summary>
    /// Unit tests for <see cref="GetPersonalProfileService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on GetPersonalProfileService.cs.
    /// </summary>
    public class GetPersonalProfileServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<User, Guid, AppDbContext>> _userRepoMock;
        private readonly GetPersonalProfileService _sut;

        public GetPersonalProfileServiceTests()
        {
            _userRepoMock = new Mock<IRepositoryQueryBase<User, Guid, AppDbContext>>();
            _sut = new GetPersonalProfileService(_userRepoMock.Object);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// IMPORTANT: Must use AsyncQueryable wrapping so FirstOrDefaultAsync resolves an
        /// IAsyncQueryProvider; a plain List.AsQueryable() throws InvalidOperationException.
        /// </summary>
        private void SetupUserRepo(User? returnUser)
        {
            var users = returnUser != null ? new List<User> { returnUser } : new List<User>();
            var mockQueryable = users.BuildMockDbSet<User>();

            _userRepoMock
                .Setup(r => r.FindAll(It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// TC-VPP-01: User id does not match any user → throws KeyNotFoundException with code
        /// "APP_MESSAGE_404_USER_NOT_FOUND".
        /// Covers: FetchUserWithProfilesOrThrow (user == null branch).
        /// </summary>
        [Fact]
        public async Task Process_UserNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var currentUserId = Guid.NewGuid();

            //Arrange 2
            SetupUserRepo(null);

            //Act
            var act = async () => await _sut.Process(currentUserId);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("APP_MESSAGE_404_USER_NOT_FOUND");
            _userRepoMock.Verify(r => r.FindAll(It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// TC-VPP-02: Patient user → every simple property is mapped, clinic stays at the
        /// default placeholder, DoctorProfile remains null.
        /// Covers: MapToProfileResponse (all property assignments), ExtractClinicAndProfessionalDetails
        /// (Role fallthrough — PATIENT), CreateApiResponse (Success wrapping).
        /// </summary>
        [Fact]
        public async Task Process_PatientUser_ReturnsDefaultClinicPlaceholder()
        {
            //Arrange 1
            var user = UserMockData.GetValidActiveUser();
            user.DoctorProfiles = new List<Domain.Entities.Clinics.DoctorProfile>();
            user.StaffClinics = new List<Domain.Entities.Clinics.StaffClinic>();

            //Arrange 2
            SetupUserRepo(user);

            //Act
            var result = await _sut.Process(user.Id);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(user.Id.ToString());
            result.Data.FullName.Should().Be(user.FullName);
            result.Data.Phone.Should().Be(user.Phone);
            result.Data.Email.Should().Be(user.Email);
            result.Data.Role.Should().Be(user.Role.ToString());
            result.Data.IsActive.Should().Be(user.IsActive);
            result.Data.AvatarUrl.Should().Be(user.AvatarUrl);
            result.Data.DoctorProfile.Should().BeNull();
            result.Data.Clinic.Name.Should().Be("Chưa phân bổ");
            result.Data.Clinic.Address.Should().Be("Chưa cập nhật");

            _userRepoMock.Verify(r => r.FindAll(It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// TC-VPP-03: Doctor user with one active DoctorProfile referencing a known clinic
        /// and known specialty → Clinic is overwritten with profile's clinic, DoctorProfile
        /// is fully populated.
        /// Covers: ExtractClinicAndProfessionalDetails (Role == DOCTOR AND doctorProfile != null).
        /// </summary>
        [Fact]
        public async Task Process_DoctorWithActiveDoctorProfile_PopulatesBothClinicAndProfile()
        {
            //Arrange 1
            var user = UserMockData.GetDoctorUser();
            var clinic = UserMockData.GetTestClinic();
            var specialty = UserMockData.GetTestSpecialty();
            var profile = UserMockData.GetActiveDoctorProfile(user, clinic, specialty);
            user.DoctorProfiles = new List<Domain.Entities.Clinics.DoctorProfile> { profile };
            user.StaffClinics = new List<Domain.Entities.Clinics.StaffClinic>();

            //Arrange 2
            SetupUserRepo(user);

            //Act
            var result = await _sut.Process(user.Id);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Role.Should().Be(UserRole.DOCTOR.ToString());
            result.Data.Clinic.Name.Should().Be(clinic.Name);
            result.Data.Clinic.Address.Should().Be(clinic.Address);
            result.Data.DoctorProfile.Should().NotBeNull();
            result.Data.DoctorProfile!.Title.Should().Be(profile.Title);
            result.Data.DoctorProfile.ExperienceYears.Should().Be(profile.ExperienceYears);
            result.Data.DoctorProfile.Bio.Should().Be(profile.Bio);
            result.Data.DoctorProfile.SpecialtyName.Should().Be(specialty.Name);

            _userRepoMock.Verify(r => r.FindAll(It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// TC-VPP-04: Doctor user with active DoctorProfile but Specialty == null →
        /// SpecialtyName falls back to "Mắt tổng quát".
        /// Covers: ExtractClinicAndProfessionalDetails
        /// (Role == DOCTOR AND doctorProfile != null AND Specialty?.Name ?? "Mắt tổng quát").
        /// </summary>
        [Fact]
        public async Task Process_DoctorWithActiveProfileButNullSpecialty_FallsBackToDefaultSpecialtyName()
        {
            //Arrange 1
            var user = UserMockData.GetDoctorUser();
            var clinic = UserMockData.GetTestClinic();
            var profile = UserMockData.GetActiveDoctorProfile(user, clinic, specialty: null);
            user.DoctorProfiles = new List<Domain.Entities.Clinics.DoctorProfile> { profile };
            user.StaffClinics = new List<Domain.Entities.Clinics.StaffClinic>();

            //Arrange 2
            SetupUserRepo(user);

            //Act
            var result = await _sut.Process(user.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Clinic.Name.Should().Be(clinic.Name);
            result.Data.Clinic.Address.Should().Be(clinic.Address);
            result.Data.DoctorProfile.Should().NotBeNull();
            result.Data.DoctorProfile!.SpecialtyName.Should().Be("Mắt tổng quát");

            _userRepoMock.Verify(r => r.FindAll(It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// TC-VPP-05: Doctor user with only INACTIVE DoctorProfiles → FirstOrDefault(dp => dp.IsActive)
        /// returns null → clinic stays default and DoctorProfile remains null.
        /// Covers: ExtractClinicAndProfessionalDetails (Role == DOCTOR AND FirstOrDefault == null).
        /// </summary>
        [Fact]
        public async Task Process_DoctorWithOnlyInactiveDoctorProfiles_KeepsDefaultClinicAndNullProfile()
        {
            //Arrange 1
            var user = UserMockData.GetDoctorUser();
            var clinic = UserMockData.GetTestClinic();
            var inactiveProfile = UserMockData.GetInactiveDoctorProfile(user, clinic);
            user.DoctorProfiles = new List<Domain.Entities.Clinics.DoctorProfile> { inactiveProfile };
            user.StaffClinics = new List<Domain.Entities.Clinics.StaffClinic>();

            //Arrange 2
            SetupUserRepo(user);

            //Act
            var result = await _sut.Process(user.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Role.Should().Be(UserRole.DOCTOR.ToString());
            result.Data.DoctorProfile.Should().BeNull();
            result.Data.Clinic.Name.Should().Be("Chưa phân bổ");
            result.Data.Clinic.Address.Should().Be("Chưa cập nhật");

            _userRepoMock.Verify(r => r.FindAll(It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// TC-VPP-06: Doctor user with DoctorProfiles == null → null-conditional short-circuits,
        /// clinic stays default and DoctorProfile remains null.
        /// Covers: ExtractClinicAndProfessionalDetails (Role == DOCTOR AND DoctorProfiles?.FirstOrDefault path).
        /// </summary>
        [Fact]
        public async Task Process_DoctorWithNullDoctorProfiles_KeepsDefaultClinicAndNullProfile()
        {
            //Arrange 1
            var user = UserMockData.GetDoctorUser();
            user.DoctorProfiles = null;
            user.StaffClinics = new List<Domain.Entities.Clinics.StaffClinic>();

            //Arrange 2
            SetupUserRepo(user);

            //Act
            var result = await _sut.Process(user.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Role.Should().Be(UserRole.DOCTOR.ToString());
            result.Data.DoctorProfile.Should().BeNull();
            result.Data.Clinic.Name.Should().Be("Chưa phân bổ");
            result.Data.Clinic.Address.Should().Be("Chưa cập nhật");

            _userRepoMock.Verify(r => r.FindAll(It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// TC-VPP-07: Receptionist user with one active StaffClinic → Clinic is overwritten
        /// with staff clinic's clinic, DoctorProfile remains null.
        /// Covers: ExtractClinicAndProfessionalDetails (Role == RECEPTIONIST AND staffClinic != null).
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistWithActiveStaffClinic_OverwritesClinicInfo()
        {
            //Arrange 1
            var user = UserMockData.GetReceptionistUser();
            var clinic = UserMockData.GetTestClinic();
            var staffClinic = UserMockData.GetActiveStaffClinic(user, clinic);
            user.DoctorProfiles = new List<Domain.Entities.Clinics.DoctorProfile>();
            user.StaffClinics = new List<Domain.Entities.Clinics.StaffClinic> { staffClinic };

            //Arrange 2
            SetupUserRepo(user);

            //Act
            var result = await _sut.Process(user.Id);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Role.Should().Be(UserRole.RECEPTIONIST.ToString());
            result.Data.Clinic.Name.Should().Be(clinic.Name);
            result.Data.Clinic.Address.Should().Be(clinic.Address);
            result.Data.DoctorProfile.Should().BeNull();

            _userRepoMock.Verify(r => r.FindAll(It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// TC-VPP-08: Receptionist user with only INACTIVE StaffClinics → FirstOrDefault(sc => sc.IsActive)
        /// returns null → clinic stays default.
        /// Covers: ExtractClinicAndProfessionalDetails (Role == RECEPTIONIST AND FirstOrDefault == null).
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistWithOnlyInactiveStaffClinics_KeepsDefaultClinic()
        {
            //Arrange 1
            var user = UserMockData.GetReceptionistUser();
            var clinic = UserMockData.GetTestClinic();
            var inactiveStaffClinic = UserMockData.GetInactiveStaffClinic(user, clinic);
            user.DoctorProfiles = new List<Domain.Entities.Clinics.DoctorProfile>();
            user.StaffClinics = new List<Domain.Entities.Clinics.StaffClinic> { inactiveStaffClinic };

            //Arrange 2
            SetupUserRepo(user);

            //Act
            var result = await _sut.Process(user.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Role.Should().Be(UserRole.RECEPTIONIST.ToString());
            result.Data.DoctorProfile.Should().BeNull();
            result.Data.Clinic.Name.Should().Be("Chưa phân bổ");
            result.Data.Clinic.Address.Should().Be("Chưa cập nhật");

            _userRepoMock.Verify(r => r.FindAll(It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// TC-VPP-09: Receptionist user with StaffClinics == null → null-conditional short-circuits,
        /// clinic stays default.
        /// Covers: ExtractClinicAndProfessionalDetails (Role == RECEPTIONIST AND StaffClinics?.FirstOrDefault path).
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistWithNullStaffClinics_KeepsDefaultClinic()
        {
            //Arrange 1
            var user = UserMockData.GetReceptionistUser();
            user.DoctorProfiles = new List<Domain.Entities.Clinics.DoctorProfile>();
            user.StaffClinics = null;

            //Arrange 2
            SetupUserRepo(user);

            //Act
            var result = await _sut.Process(user.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Role.Should().Be(UserRole.RECEPTIONIST.ToString());
            result.Data.DoctorProfile.Should().BeNull();
            result.Data.Clinic.Name.Should().Be("Chưa phân bổ");
            result.Data.Clinic.Address.Should().Be("Chưa cập nhật");

            _userRepoMock.Verify(r => r.FindAll(It.IsAny<bool>()), Times.Once);
        }
    }
}
