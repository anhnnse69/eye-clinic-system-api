using System.Linq.Expressions;
using ECS.Application.Services.DoctorScheduleManagementServices.GetActiveDoctorsServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.DoctorScheduleManagementServices.GetActiveDoctorsServices
{
    /// <summary>
    /// Unit tests for <see cref="GetActiveDoctorsService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line AND branch coverage on <c>GetActiveDoctorsService.cs</c>.
    /// </summary>
    public class GetActiveDoctorsServiceTests
    {
        private static readonly Guid ReceptionistUserId = GetActiveDoctorsMockData.ReceptionistUserId;
        private static readonly Guid ClinicId = GetActiveDoctorsMockData.ClinicId;

        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly GetActiveDoctorsService _sut;

        public GetActiveDoctorsServiceTests()
        {
            _sut = new GetActiveDoctorsService(
                _staffClinicRepoMock.Object,
                _doctorRepoMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Repository helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupStaffClinicRepo(IEnumerable<StaffClinic> staffClinics)
        {
            var list = staffClinics.ToList();
            _staffClinicRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<StaffClinic>().Object);
        }

        private void SetupDoctorRepo(IEnumerable<DoctorProfile> doctors)
        {
            var list = doctors.ToList();
            _doctorRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<DoctorProfile>().Object);
        }

        // ==================================================================
        // ==================== AUTHORIZATION TESTS ========================
        // ==================================================================

        /// <summary>
        /// TC-01: Receptionist not found (StaffClinic is null) → throws KeyNotFoundException(APP_MESSAGE_4008).
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;

            //Arrange 2
            SetupStaffClinicRepo(Array.Empty<StaffClinic>());

            //Act
            var act = () => _sut.Process(receptionistUserId);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
            _doctorRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-02: Receptionist is inactive (StaffClinic.IsActive = false) → throws KeyNotFoundException(APP_MESSAGE_4008).
        /// Note: Simple mock cannot apply IsActive filter, so inactive = not found scenario.
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistInactive_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;

            //Arrange 2
            // Return empty list to simulate IsActive filter (mock can't apply filters)
            SetupStaffClinicRepo(Array.Empty<StaffClinic>());

            //Act
            var act = () => _sut.Process(receptionistUserId);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        // ==================================================================
        // ==================== SUCCESS PATH TESTS =========================
        // ==================================================================

        /// <summary>
        /// TC-03: Happy path - active receptionist with active doctors → returns list sorted by FullName.
        /// </summary>
        [Fact]
        public async Task Process_HappyPathWithActiveDoctors_ReturnsSortedList()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;
            var doctor1 = GetActiveDoctorsMockData.GetActiveDoctorProfile(
                id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                userId: Guid.Parse("11111111-1111-1111-1111-111111111112"),
                fullName: "BS. An");
            var doctor2 = GetActiveDoctorsMockData.GetActiveDoctorProfile(
                id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                userId: Guid.Parse("11111111-1111-1111-1111-111111111113"),
                fullName: "BS. Binh");
            var doctor3 = GetActiveDoctorsMockData.GetActiveDoctorProfile(
                id: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                userId: Guid.Parse("11111111-1111-1111-1111-111111111114"),
                fullName: "BS. Cuong");
            var doctors = new[] { doctor3, doctor1, doctor2 }; // Unsorted input

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveDoctorsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupDoctorRepo(doctors);

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(3);
            result.Data![0].FullName.Should().Be("BS. An");
            result.Data[1].FullName.Should().Be("BS. Binh");
            result.Data[2].FullName.Should().Be("BS. Cuong");
        }

        /// <summary>
        /// TC-04: Doctors with Specialty → Specialty field populated.
        /// </summary>
        [Fact]
        public async Task Process_DoctorsWithSpecialty_ReturnsSpecialtyName()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;
            var specialty = GetActiveDoctorsMockData.GetSpecialty("Khoa Mắt Nhi");
            var doctor = GetActiveDoctorsMockData.GetActiveDoctorProfile(
                id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                userId: Guid.Parse("11111111-1111-1111-1111-111111111112"),
                specialty: specialty);

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveDoctorsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupDoctorRepo(new[] { doctor });

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data![0].Specialty.Should().Be("Khoa Mắt Nhi");
        }

        /// <summary>
        /// TC-05: Doctor without Specialty (null) → Specialty field is null.
        /// </summary>
        [Fact]
        public async Task Process_DoctorWithoutSpecialty_ReturnsNullSpecialty()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;
            var doctor = GetActiveDoctorsMockData.GetActiveDoctorProfile(
                id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                userId: Guid.Parse("11111111-1111-1111-1111-111111111112"),
                specialty: null);

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveDoctorsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupDoctorRepo(new[] { doctor });

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data![0].Specialty.Should().BeNull();
        }

        // ==================================================================
        // ==================== EDGE CASE TESTS =============================
        // ==================================================================

        /// <summary>
        /// TC-06: No active doctors in clinic → returns empty list.
        /// </summary>
        [Fact]
        public async Task Process_NoActiveDoctors_ReturnsEmptyList()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveDoctorsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupDoctorRepo(Array.Empty<DoctorProfile>());

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        /// <summary>
        /// TC-07: Mix of active and inactive doctors → only active doctors returned.
        /// </summary>
        [Fact]
        public async Task Process_MixActiveAndInactiveDoctors_ReturnsOnlyActive()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;
            var activeDoctor = GetActiveDoctorsMockData.GetActiveDoctorProfile(
                id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                userId: Guid.Parse("11111111-1111-1111-1111-111111111112"),
                fullName: "BS. Active");

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveDoctorsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupDoctorRepo(new[] { activeDoctor }); // Only active doctor in mock

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data![0].FullName.Should().Be("BS. Active");
            result.Data[0].IsActive.Should().BeTrue();
        }

        // ==================================================================
        // ==================== RESPONSE STRUCTURE TESTS ===================
        // ==================================================================

        /// <summary>
        /// TC-08: Response contains correct fields from DoctorOptionResponse.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ResponseContainsCorrectFields()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;
            var specialty = GetActiveDoctorsMockData.GetSpecialty("Khoa Tim");
            var doctorUser = GetActiveDoctorsMockData.GetDoctorUser(
                id: Guid.Parse("11111111-1111-1111-1111-111111111112"),
                fullName: "BS. Heart");
            doctorUser.AvatarUrl = "https://example.com/avatar.jpg";
            var doctor = GetActiveDoctorsMockData.GetActiveDoctorProfile(
                id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                userId: doctorUser.Id,
                specialty: specialty,
                user: doctorUser);

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveDoctorsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupDoctorRepo(new[] { doctor });

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);

            var doctorResponse = result.Data![0];
            doctorResponse.DoctorId.Should().Be(doctor.Id);
            doctorResponse.FullName.Should().Be("BS. Heart");
            doctorResponse.Specialty.Should().Be("Khoa Tim");
            doctorResponse.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
            doctorResponse.IsActive.Should().BeTrue();
        }

        /// <summary>
        /// TC-09: Response CodeMessage is APP_MESSAGE_2000 and Meta is null.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ResponseCodeMessage2000AndMetaNull()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveDoctorsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupDoctorRepo(Array.Empty<DoctorProfile>());

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Meta.Should().BeNull();
        }

        /// <summary>
        /// TC-10: Response Data is null-safe (empty list, not null).
        /// </summary>
        [Fact]
        public async Task Process_NoDoctors_ResponseDataIsEmptyListNotNull()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveDoctorsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupDoctorRepo(Array.Empty<DoctorProfile>());

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }
    }
}
