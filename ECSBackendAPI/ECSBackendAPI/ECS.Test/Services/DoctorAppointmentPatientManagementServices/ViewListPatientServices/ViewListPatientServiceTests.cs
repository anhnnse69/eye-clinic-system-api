using System.Linq.Expressions;
using System.Reflection;
using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewListPatientServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.DoctorAppointmentPatientManagementServices.ViewListPatientServices
{
    public class ViewListPatientServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock;
        private readonly Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>> _appointmentRepoMock;
        private readonly ViewPatientListService _service;

        public ViewListPatientServiceTests()
        {
            _doctorRepoMock = new Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>>();
            _appointmentRepoMock = new Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>>();
            _service = new ViewPatientListService(_doctorRepoMock.Object, _appointmentRepoMock.Object);
        }

        private void SetupDoctorProfileRepo(DoctorProfile? profile)
        {
            var list = profile != null ? new List<DoctorProfile> { profile } : new List<DoctorProfile>();
            _doctorRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<DoctorProfile, bool>> expr, bool track) =>
                    list.AsQueryable().Where(expr).ToList().BuildMockDbSet().Object);
        }

        private void SetupAppointmentRepo(IEnumerable<Appointment> appointments)
        {
            var list = appointments.ToList();
            _appointmentRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<Appointment, bool>> expr, bool track) =>
                    list.AsQueryable().Where(expr).ToList().BuildMockDbSet().Object);
        }

        [Fact]
        public async Task Process_DoctorProfileNotFoundOrInactive_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            var request = new ViewListPatientRequest();

            //Arrange 2
            SetupDoctorProfileRepo(null);

            //Act
            Func<Task> act = async () => await _service.Process(userId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4011.ToString());
            _doctorRepoMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()), Times.Once);
            _appointmentRepoMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Process_ValidDoctorWithNoAppointments_ReturnsSuccessWithEmptyPatientsAndZeroCounts()
        {
            //Arrange 1
            var doctor = ViewListPatientMockData.GetDoctorProfile();
            var request = new ViewListPatientRequest { PageNumber = 1, PageSize = 10 };

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupAppointmentRepo(Enumerable.Empty<Appointment>());

            //Act
            var result = await _service.Process(doctor.UserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(0);
            result.Data.TotalPages.Should().Be(0);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
            result.Data.Patients.Should().BeEmpty();
            _doctorRepoMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()), Times.Once);
            _appointmentRepoMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Process_InvalidPagingParameters_NormalizesPagingParameters()
        {
            //Arrange 1
            var doctor = ViewListPatientMockData.GetDoctorProfile();
            var request = new ViewListPatientRequest { PageNumber = -1, PageSize = 0 };

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupAppointmentRepo(Enumerable.Empty<Appointment>());

            //Act
            var result = await _service.Process(doctor.UserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task Process_CustomValidPagingParameters_ReturnsCorrectPageAndPageSize()
        {
            //Arrange 1
            var doctor = ViewListPatientMockData.GetDoctorProfile();
            var appointments = new List<Appointment>();
            for (int i = 1; i <= 7; i++)
            {
                appointments.Add(ViewListPatientMockData.GetAppointment(
                    doctorId: doctor.Id,
                    appointmentDate: DateTime.Today.AddDays(-i)));
            }
            var request = new ViewListPatientRequest { PageNumber = 2, PageSize = 5 };

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupAppointmentRepo(appointments);

            //Act
            var result = await _service.Process(doctor.UserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(7);
            result.Data.TotalPages.Should().Be(2);
            result.Data.PageNumber.Should().Be(2);
            result.Data.PageSize.Should().Be(5);
            result.Data.Patients.Should().HaveCount(2);
        }

        [Fact]
        public async Task Process_FilterByStatus_ReturnsFilteredAppointments()
        {
            //Arrange 1
            var doctor = ViewListPatientMockData.GetDoctorProfile();
            var apptPending = ViewListPatientMockData.GetAppointment(doctorId: doctor.Id, status: AppointmentStatus.PENDING);
            var apptConfirmed = ViewListPatientMockData.GetAppointment(doctorId: doctor.Id, status: AppointmentStatus.CONFIRMED);
            var request = new ViewListPatientRequest { Status = AppointmentStatus.CONFIRMED };

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupAppointmentRepo(new[] { apptPending, apptConfirmed });

            //Act
            var result = await _service.Process(doctor.UserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(1);
            result.Data.Patients.Should().HaveCount(1);
            result.Data.Patients[0].AppointmentId.Should().Be(apptConfirmed.Id);
            result.Data.Patients[0].Status.Should().Be(AppointmentStatus.CONFIRMED.ToString());
        }

        [Fact]
        public async Task Process_AppointmentsWithUserAndNullUser_MapsResponsePropertiesCorrectly()
        {
            //Arrange 1
            var doctor = ViewListPatientMockData.GetDoctorProfile();

            // Item 1: Patient with User (AvatarUrl present)
            var user1 = ViewListPatientMockData.GetUser(avatarUrl: "https://example.com/patient1.jpg");
            var patient1 = ViewListPatientMockData.GetPatientProfile(fullName: "Nguyen Van A", phone: "0901111111", user: user1);
            var appt1 = ViewListPatientMockData.GetAppointment(doctorId: doctor.Id, patient: patient1);

            // Item 2: Patient without User
            var patient2 = new PatientProfile
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                User = null,
                FullName = "Tran Thi Bare",
                PhoneNumber = "0902222222",
                Gender = Gender.FEMALE,
                Dob = new DateTime(1995, 5, 20)
            };
            var appt2 = ViewListPatientMockData.GetAppointment(doctorId: doctor.Id, patient: patient2);

            var request = new ViewListPatientRequest();

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupAppointmentRepo(new[] { appt1, appt2 });

            //Act
            var result = await _service.Process(doctor.UserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(2);
            result.Data.Patients.Should().HaveCount(2);

            var item1 = result.Data.Patients.First(p => p.AppointmentId == appt1.Id);
            item1.PatientId.Should().Be(patient1.Id);
            item1.PatientName.Should().Be("Nguyen Van A");
            item1.PatientAvatarUrl.Should().Be("https://example.com/patient1.jpg");
            item1.PatientPhone.Should().Be("0901111111");

            var item2 = result.Data.Patients.First(p => p.AppointmentId == appt2.Id);
            item2.PatientId.Should().Be(patient2.Id);
            item2.PatientName.Should().Be("Tran Thi Bare");
            item2.PatientAvatarUrl.Should().BeNull();
            item2.PatientPhone.Should().Be("0902222222");
        }

        [Fact]
        public void CalculateTotalPages_PageSizeIsZero_ReturnsZero()
        {
            //Arrange 1
            var method = typeof(ViewPatientListService).GetMethod(
                "CalculateTotalPages",
                BindingFlags.NonPublic | BindingFlags.Static);

            //Arrange 2
            method.Should().NotBeNull();

            //Act
            var result = (int)method!.Invoke(null, new object[] { 10, 0 })!;

            //Assert
            result.Should().Be(0);
        }
    }
}
