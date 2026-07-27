using System.Linq.Expressions;
using System.Reflection;
using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorAppointmentsServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.DoctorAppointmentPatientManagementServices.ViewDoctorAppointmentsServices
{
    public class ViewDoctorAppointmentsServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepositoryMock;
        private readonly Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>> _appointmentRepositoryMock;
        private readonly ViewDoctorAppointmentsService _service;

        public ViewDoctorAppointmentsServiceTests()
        {
            _doctorRepositoryMock = new Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>>();
            _appointmentRepositoryMock = new Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>>();
            _service = new ViewDoctorAppointmentsService(_doctorRepositoryMock.Object, _appointmentRepositoryMock.Object);
        }

        private void SetupDoctorProfileRepo(DoctorProfile? profile)
        {
            var list = profile != null ? new List<DoctorProfile> { profile } : new List<DoctorProfile>();
            var mockQueryable = list.BuildMockDbSet();
            _doctorRepositoryMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        private void SetupAppointmentRepo(IEnumerable<Appointment> appointments)
        {
            var mockQueryable = appointments.ToList().BuildMockDbSet();
            _appointmentRepositoryMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        [Fact]
        public async Task Process_DoctorProfileNotFoundOrInactive_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            var request = new ViewDoctorAppointmentsRequest();

            //Arrange 2
            SetupDoctorProfileRepo(null);

            //Act
            Func<Task> act = async () => await _service.Process(userId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
            _doctorRepositoryMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()), Times.Once);
            _appointmentRepositoryMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Process_ValidDoctorWithNoAppointments_ReturnsSuccessWithEmptyAppointmentsAndZeroCounts()
        {
            //Arrange 1
            var doctor = ViewDoctorAppointmentsMockData.GetDoctorProfile();
            var request = new ViewDoctorAppointmentsRequest { PageNumber = 1, PageSize = 10 };

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
            result.Data.Appointments.Should().BeEmpty();
            _doctorRepositoryMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()), Times.Once);
            _appointmentRepositoryMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Process_InvalidPagingParameters_NormalizesPagingParameters()
        {
            //Arrange 1
            var doctor = ViewDoctorAppointmentsMockData.GetDoctorProfile();
            var request = new ViewDoctorAppointmentsRequest { PageNumber = -1, PageSize = 0 };

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
            var doctor = ViewDoctorAppointmentsMockData.GetDoctorProfile();
            var appointments = new List<Appointment>();
            for (int i = 1; i <= 7; i++)
            {
                appointments.Add(ViewDoctorAppointmentsMockData.GetAppointment(
                    doctorId: doctor.Id,
                    appointmentDate: DateTime.Today.AddDays(-i)));
            }
            var request = new ViewDoctorAppointmentsRequest { PageNumber = 2, PageSize = 5 };

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
            result.Data.Appointments.Should().HaveCount(2);
        }

        [Fact]
        public async Task Process_FilterByStatus_ReturnsFilteredAppointments()
        {
            //Arrange 1
            var doctor = ViewDoctorAppointmentsMockData.GetDoctorProfile();
            var apptPending = ViewDoctorAppointmentsMockData.GetAppointment(doctorId: doctor.Id, status: AppointmentStatus.PENDING);
            var apptConfirmed = ViewDoctorAppointmentsMockData.GetAppointment(doctorId: doctor.Id, status: AppointmentStatus.CONFIRMED);
            var request = new ViewDoctorAppointmentsRequest { Status = AppointmentStatus.PENDING };

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupAppointmentRepo(new[] { apptPending, apptConfirmed });

            //Act
            var result = await _service.Process(doctor.UserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(1);
            result.Data.Appointments.Should().HaveCount(1);
            result.Data.Appointments[0].AppointmentId.Should().Be(apptPending.Id);
        }

        [Fact]
        public async Task Process_FilterByDate_ReturnsFilteredAppointments()
        {
            //Arrange 1
            var doctor = ViewDoctorAppointmentsMockData.GetDoctorProfile();
            var targetDate = new DateOnly(2026, 6, 15);
            var apptTarget = ViewDoctorAppointmentsMockData.GetAppointment(doctorId: doctor.Id, appointmentDate: targetDate.ToDateTime(TimeOnly.MinValue));
            var apptOther = ViewDoctorAppointmentsMockData.GetAppointment(doctorId: doctor.Id, appointmentDate: new DateTime(2026, 6, 16));
            var request = new ViewDoctorAppointmentsRequest { Date = targetDate };

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupAppointmentRepo(new[] { apptTarget, apptOther });

            //Act
            var result = await _service.Process(doctor.UserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(1);
            result.Data.Appointments[0].AppointmentId.Should().Be(apptTarget.Id);
        }

        [Fact]
        public async Task Process_FilterBySearchKeywordMatchingName_ReturnsFilteredAppointments()
        {
            //Arrange 1
            var doctor = ViewDoctorAppointmentsMockData.GetDoctorProfile();
            var patient1 = ViewDoctorAppointmentsMockData.GetPatientProfile(fullName: "Nguyen Van Alpha", phone: "0901111111");
            var patient2 = ViewDoctorAppointmentsMockData.GetPatientProfile(fullName: "Tran Thi Beta", phone: "0902222222");
            var appt1 = ViewDoctorAppointmentsMockData.GetAppointment(doctorId: doctor.Id, patient: patient1);
            var appt2 = ViewDoctorAppointmentsMockData.GetAppointment(doctorId: doctor.Id, patient: patient2);
            var request = new ViewDoctorAppointmentsRequest { Search = "  ALPHA  " };

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupAppointmentRepo(new[] { appt1, appt2 });

            //Act
            var result = await _service.Process(doctor.UserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(1);
            result.Data.Appointments[0].PatientName.Should().Be("Nguyen Van Alpha");
        }

        [Fact]
        public async Task Process_FilterBySearchKeywordMatchingPhone_ReturnsFilteredAppointments()
        {
            //Arrange 1
            var doctor = ViewDoctorAppointmentsMockData.GetDoctorProfile();
            var patient1 = ViewDoctorAppointmentsMockData.GetPatientProfile(fullName: "Nguyen Van Alpha", phone: "0901111111");
            var patient2 = ViewDoctorAppointmentsMockData.GetPatientProfile(fullName: "Tran Thi Beta", phone: "0902222222");
            var appt1 = ViewDoctorAppointmentsMockData.GetAppointment(doctorId: doctor.Id, patient: patient1);
            var appt2 = ViewDoctorAppointmentsMockData.GetAppointment(doctorId: doctor.Id, patient: patient2);
            var request = new ViewDoctorAppointmentsRequest { Search = "222222" };

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupAppointmentRepo(new[] { appt1, appt2 });

            //Act
            var result = await _service.Process(doctor.UserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(1);
            result.Data.Appointments[0].PatientPhone.Should().Be("0902222222");
        }

        [Fact]
        public async Task Process_SearchWithNullPatientProperties_HandlesNullGracefully()
        {
            //Arrange 1
            var doctor = ViewDoctorAppointmentsMockData.GetDoctorProfile();
            var patientWithNulls = new PatientProfile
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                User = null,
                FullName = null!,
                PhoneNumber = null
            };
            var appt = ViewDoctorAppointmentsMockData.GetAppointment(doctorId: doctor.Id, patient: patientWithNulls);
            var request = new ViewDoctorAppointmentsRequest { Search = "nonmatching" };

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupAppointmentRepo(new[] { appt });

            //Act
            var result = await _service.Process(doctor.UserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(0);
            result.Data.Appointments.Should().BeEmpty();
        }

        [Fact]
        public async Task Process_AppointmentsWithFullAndNullNavigationProperties_MapsResponsePropertiesCorrectly()
        {
            //Arrange 1
            var doctor = ViewDoctorAppointmentsMockData.GetDoctorProfile();

            // Item 1: Complete navigation properties
            var user1 = ViewDoctorAppointmentsMockData.GetUser(avatarUrl: "https://example.com/avatar1.jpg");
            var patient1 = ViewDoctorAppointmentsMockData.GetPatientProfile(user: user1);
            var service1 = ViewDoctorAppointmentsMockData.GetService(serviceName: "Kham Tong Quat", price: 200000m);
            var slot1 = ViewDoctorAppointmentsMockData.GetSlot();
            var medicalRecord1 = ViewDoctorAppointmentsMockData.GetMedicalRecord();
            var appt1 = ViewDoctorAppointmentsMockData.GetAppointment(
                doctorId: doctor.Id,
                patient: patient1,
                service: service1,
                slot: slot1,
                medicalRecord: medicalRecord1);

            // Item 2: Null optional navigation properties
            var patient2 = new PatientProfile
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                User = null,
                FullName = "Tran Thi Bare",
                PhoneNumber = null,
                Gender = Gender.FEMALE,
                Dob = default(DateTime)
            };
            var slot2 = ViewDoctorAppointmentsMockData.GetSlot();
            var appt2 = ViewDoctorAppointmentsMockData.GetAppointment(
                doctorId: doctor.Id,
                patient: patient2,
                service: null,
                slot: slot2,
                medicalRecord: null);

            var request = new ViewDoctorAppointmentsRequest();

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupAppointmentRepo(new[] { appt1, appt2 });

            //Act
            var result = await _service.Process(doctor.UserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(2);
            result.Data.Appointments.Should().HaveCount(2);

            var item1 = result.Data.Appointments.First(a => a.AppointmentId == appt1.Id);
            item1.PatientAvatarUrl.Should().Be("https://example.com/avatar1.jpg");
            item1.ServiceId.Should().Be(service1.Id);
            item1.ServiceName.Should().Be("Kham Tong Quat");
            item1.ServicePrice.Should().Be(200000m);
            item1.HasMedicalRecord.Should().BeTrue();
            item1.MedicalRecordId.Should().Be(medicalRecord1.Id);

            var item2 = result.Data.Appointments.First(a => a.AppointmentId == appt2.Id);
            item2.PatientAvatarUrl.Should().BeNull();
            item2.ServiceId.Should().BeNull();
            item2.ServiceName.Should().BeNull();
            item2.ServicePrice.Should().BeNull();
            item2.HasMedicalRecord.Should().BeFalse();
            item2.MedicalRecordId.Should().BeNull();
        }

        [Fact]
        public void CalculateTotalPages_PageSizeIsZero_ReturnsZero()
        {
            //Arrange 1
            var method = typeof(ViewDoctorAppointmentsService).GetMethod(
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
