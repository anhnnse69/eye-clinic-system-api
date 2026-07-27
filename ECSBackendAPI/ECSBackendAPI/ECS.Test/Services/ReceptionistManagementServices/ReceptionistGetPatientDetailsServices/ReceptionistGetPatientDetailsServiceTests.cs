using System.Linq.Expressions;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientDetailsServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using Moq;

namespace ECS.Test.Services.ReceptionistManagementServices.ReceptionistGetPatientDetailsServices
{
    public class ReceptionistGetPatientDetailsServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>> _patientQueryRepoMock;
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffQueryRepoMock;
        private readonly Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>> _appointmentQueryRepoMock;
        private readonly ReceptionistGetPatientDetailsService _sut;

        public ReceptionistGetPatientDetailsServiceTests()
        {
            _patientQueryRepoMock = new Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>>();
            _staffQueryRepoMock = new Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>>();
            _appointmentQueryRepoMock = new Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>>();

            _sut = new ReceptionistGetPatientDetailsService(
                _patientQueryRepoMock.Object,
                _staffQueryRepoMock.Object,
                _appointmentQueryRepoMock.Object);
        }

        #region Helper Setup Methods

        private void SetupStaffRepository(List<StaffClinic> staffClinics)
        {
            _staffQueryRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>()
                ))
                .Returns((Expression<Func<StaffClinic, bool>> predicate, bool trackChanges) =>
                {
                    var compiled = predicate.Compile();
                    return staffClinics.Where(compiled).AsQueryable();
                });

            _staffQueryRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<StaffClinic, object>>[]>()
                ))
                .Returns((Expression<Func<StaffClinic, bool>> predicate, bool trackChanges, Expression<Func<StaffClinic, object>>[] includes) =>
                {
                    var compiled = predicate.Compile();
                    return staffClinics.Where(compiled).AsQueryable();
                });
        }

        private void SetupPatientRepository(PatientProfile? patient)
        {
            _patientQueryRepoMock
                .Setup(r => r.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Expression<Func<PatientProfile, object>>[]>()
                ))
                .ReturnsAsync((Guid id, Expression<Func<PatientProfile, object>>[] includes) =>
                    patient != null && patient.Id == id ? patient : null);
        }

        private void SetupAppointmentRepository(List<Appointment> appointments)
        {
            _appointmentQueryRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>()
                ))
                .Returns((Expression<Func<Appointment, bool>> predicate, bool trackChanges) =>
                {
                    var compiled = predicate.Compile();
                    return appointments.Where(compiled).AsQueryable();
                });

            _appointmentQueryRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<Appointment, object>>[]>()
                ))
                .Returns((Expression<Func<Appointment, bool>> predicate, bool trackChanges, Expression<Func<Appointment, object>>[] includes) =>
                {
                    var compiled = predicate.Compile();
                    return appointments.Where(compiled).AsQueryable();
                });
        }

        #endregion

        [Fact]
        public async Task Process_ValidPatientAndActiveClinic_ReturnsFullDetailsWithFilteredAppointments()
        {
            // Arrange
            var staffUserId = ReceptionistGetPatientDetailsMockData.ReceptionistUserId;
            var patientId = ReceptionistGetPatientDetailsMockData.PatientId;

            SetupStaffRepository(ReceptionistGetPatientDetailsMockData.GetStaffClinics(staffUserId));
            SetupPatientRepository(ReceptionistGetPatientDetailsMockData.GetPatientProfile(patientId));
            SetupAppointmentRepository(ReceptionistGetPatientDetailsMockData.GetAppointments(patientId));

            var request = ReceptionistGetPatientDetailsMockData.GetValidRequest();

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("APP_MESSAGE_2000");
            response.Data.Should().NotBeNull();

            var data = response.Data!;
            data.Id.Should().Be(patientId.ToString());
            data.FullName.Should().Be("Alice Smith");
            data.Email.Should().Be("alice@example.com");
            data.Appointments.Should().HaveCount(2);
            data.Appointments.Should().Contain(a => a.SpecialtyName == "Phòng khám chung");
            data.Appointments.Should().Contain(a => a.SpecialtyName == "Cardiology");
        }

        [Fact]
        public async Task Process_PatientNotFound_ReturnsFailResponse()
        {
            // Arrange
            var staffUserId = ReceptionistGetPatientDetailsMockData.ReceptionistUserId;
            var nonExistentPatientId = Guid.NewGuid();

            SetupStaffRepository(ReceptionistGetPatientDetailsMockData.GetStaffClinics(staffUserId));
            SetupPatientRepository(null); // Patient does not exist
            SetupAppointmentRepository(new List<Appointment>());

            var request = new ReceptionistGetPatientDetailsRequest
            {
                CurrentUserId = staffUserId,
                PatientId = nonExistentPatientId
            };

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("APP_MESSAGE_4004");
            response.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_ReceptionistHasNoActiveClinics_ReturnsPatientProfileWithEmptyAppointments()
        {
            // Arrange
            var staffUserId = ReceptionistGetPatientDetailsMockData.ReceptionistUserId;
            var patientId = ReceptionistGetPatientDetailsMockData.PatientId;
            SetupStaffRepository(new List<StaffClinic>());
            SetupPatientRepository(ReceptionistGetPatientDetailsMockData.GetPatientProfile(patientId));
            SetupAppointmentRepository(ReceptionistGetPatientDetailsMockData.GetAppointments(patientId));

            var request = ReceptionistGetPatientDetailsMockData.GetValidRequest();

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("APP_MESSAGE_2000");
            response.Data.Should().NotBeNull();
            response.Data!.FullName.Should().Be("Alice Smith");
            response.Data.Appointments.Should().BeEmpty();
        }
        [Fact]
        public async Task Process_PatientWithNullUser_ReturnsResponseWithNullEmailAndAvatar()
        {
            // Arrange
            var staffUserId = ReceptionistGetPatientDetailsMockData.ReceptionistUserId;
            var patientId = ReceptionistGetPatientDetailsMockData.PatientId;

            SetupStaffRepository(ReceptionistGetPatientDetailsMockData.GetStaffClinics(staffUserId));
            SetupPatientRepository(ReceptionistGetPatientDetailsMockData.GetPatientProfileWithNullUser(patientId));
            SetupAppointmentRepository(new List<Appointment>());

            var request = ReceptionistGetPatientDetailsMockData.GetValidRequest();

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("APP_MESSAGE_2000");
            response.Data.Should().NotBeNull();
            response.Data!.Email.Should().BeNull();
            response.Data.AvatarUrl.Should().BeNull();
        }
    }
}