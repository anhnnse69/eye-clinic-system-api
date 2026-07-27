using System.Linq.Expressions;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetDailyAppointmentsServices;
using ECS.Domain.Entities.Auth;
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

namespace ECS.Test.Services.ReceptionistManagementServices.ReceptionistGetDailyAppointmentsServices
{
    public class ReceptionistGetDailyAppointmentsServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffQueryRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>> _appointmentQueryRepoMock = new();

        private readonly ReceptionistGetDailyAppointmentsService _sut;

        public ReceptionistGetDailyAppointmentsServiceTests()
        {
            _sut = new ReceptionistGetDailyAppointmentsService(
                _staffQueryRepoMock.Object,
                _appointmentQueryRepoMock.Object);
        }

        private void SetupStaffRepository(List<StaffClinic> staffClinics)
        {
            var mockDbSet = staffClinics.BuildMockDbSet();

            _staffQueryRepoMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>()
                ))
                .Returns((Expression<Func<StaffClinic, bool>> predicate, bool _) =>
                    mockDbSet.Object.Where(predicate));
        }

        private void SetupAppointmentRepository(List<Appointment> appointments)
        {
            var mockDbSet = appointments.BuildMockDbSet();

            _appointmentQueryRepoMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<Appointment, object>>[]>()
                ))
                .Returns((Expression<Func<Appointment, bool>> predicate, bool _, Expression<Func<Appointment, object>>[] _) =>
                    mockDbSet.Object.Where(predicate));
        }

        [Fact]
        public async Task Process_BasicFlow_ReturnsPaginatedAppointmentsAndTelemetry()
        {
            // Arrange
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;

            SetupStaffRepository(ReceptionistGetDailyAppointmentsMockData.GetStaffClinics());
            SetupAppointmentRepository(ReceptionistGetDailyAppointmentsMockData.GetAppointments(today));

            var request = ReceptionistGetDailyAppointmentsMockData.GetValidRequest(today);

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("APP_MESSAGE_2000");
            response.Data.Should().HaveCount(4);
            response.Meta.Should().NotBeNull();

            var firstItem = response.Data!.First();
            firstItem.Queue.Should().NotBeNull();
            firstItem.Queue!.QueueNumber.Should().Be(1);
            firstItem.Doctor.ClinicRoomName.Should().Be("Room 101");
        }

        [Fact]
        public async Task Process_WithNullTargetDate_DefaultsToTodayVn()
        {
            // Arrange
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;

            SetupStaffRepository(ReceptionistGetDailyAppointmentsMockData.GetStaffClinics());
            SetupAppointmentRepository(ReceptionistGetDailyAppointmentsMockData.GetAppointments(today));

            var request = ReceptionistGetDailyAppointmentsMockData.GetValidRequest(targetDate: null);

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().NotBeNull();
            response.Data.Should().HaveCount(4);
        }

        [Fact]
        public async Task Process_WithShiftFilterAndSearchFilters_ReturnsFilteredResults()
        {
            // Arrange
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;

            SetupStaffRepository(ReceptionistGetDailyAppointmentsMockData.GetStaffClinics());
            SetupAppointmentRepository(ReceptionistGetDailyAppointmentsMockData.GetAppointments(today));

            var request = new ReceptionistGetDailyAppointmentsRequest
            {
                CurrentUserId = ReceptionistGetDailyAppointmentsMockData.ReceptionistUserId,
                TargetDate = today,
                ShiftFilter = ShiftType.MORNING,
                SearchPatient = " 0988 ",
                SearchDoctor = " house ",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(3);
            response.Data!.All(x => x.Patient.FullName == "Alice Smith").Should().BeTrue();
        }

        [Fact]
        public async Task Process_SearchPatientByNameAndFallbackRoomName_ReturnsMatchingAppointments()
        {
            // Arrange
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;

            SetupStaffRepository(ReceptionistGetDailyAppointmentsMockData.GetStaffClinics());
            SetupAppointmentRepository(ReceptionistGetDailyAppointmentsMockData.GetAppointments(today));

            var request = new ReceptionistGetDailyAppointmentsRequest
            {
                CurrentUserId = ReceptionistGetDailyAppointmentsMockData.ReceptionistUserId,
                TargetDate = today,
                SearchPatient = "bob",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(1);
            var item = response.Data!.First();
            item.Patient.FullName.Should().Be("Bob Johnson");
            item.Doctor.ClinicRoomName.Should().Be("Phòng khám chung");
        }

        [Fact]
        public async Task Process_SearchPatientWithNullPhoneNumber_ExecutesCorrectly()
        {
            // Arrange
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
            var doctor = new DoctorProfile { Id = ReceptionistGetDailyAppointmentsMockData.DoctorId, ClinicId = ReceptionistGetDailyAppointmentsMockData.ClinicId, User = new User { FullName = "Dr. House" } };
            var slot = new TimeSlot { Id = Guid.NewGuid(), Schedule = new DoctorSchedule { ShiftType = ShiftType.MORNING, Room = new FacilityRoom { RoomName = "Room A" } } };
            var patientWithNullPhone = new PatientProfile { Id = Guid.NewGuid(), FullName = "Null Phone Patient", PhoneNumber = null };

            var appointments = new List<Appointment>
    {
        new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patientWithNullPhone.Id,
            DoctorId = doctor.Id,
            SlotId = slot.Id,
            AppointmentDate = today,
            Status = AppointmentStatus.PENDING,
            Patient = patientWithNullPhone,
            Doctor = doctor,
            Slot = slot
        }
    };

            SetupStaffRepository(ReceptionistGetDailyAppointmentsMockData.GetStaffClinics());
            SetupAppointmentRepository(appointments);

            var request = new ReceptionistGetDailyAppointmentsRequest
            {
                CurrentUserId = ReceptionistGetDailyAppointmentsMockData.ReceptionistUserId,
                TargetDate = today,
                SearchPatient = "null phone",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(1);
            response.Data!.First().Patient.FullName.Should().Be("Null Phone Patient");
        }

        [Fact]
        public async Task Process_WithNullQueueCalledAt_MapsNullCalledAtString()
        {
            // Arrange
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;

            var doctor = new DoctorProfile { Id = ReceptionistGetDailyAppointmentsMockData.DoctorId, ClinicId = ReceptionistGetDailyAppointmentsMockData.ClinicId, User = new User { FullName = "Dr. House" } };
            var slot = new TimeSlot { Id = Guid.NewGuid(), Schedule = new DoctorSchedule { ShiftType = ShiftType.MORNING, Room = new FacilityRoom { RoomName = "Room A" } } };
            var patient = new PatientProfile { Id = Guid.NewGuid(), FullName = "Queue Test Patient" };

            var appointments = new List<Appointment>
    {
        new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            SlotId = slot.Id,
            AppointmentDate = today,
            Status = AppointmentStatus.ARRIVED,
            Patient = patient,
            Doctor = doctor,
            Slot = slot,
            Queue = new Queue
            {
                Id = Guid.NewGuid(),
                QueueNumber = 99,
                Status = QueueStatus.WAITING,
                CalledAt = null
            }
        }
    };

            SetupStaffRepository(ReceptionistGetDailyAppointmentsMockData.GetStaffClinics());
            SetupAppointmentRepository(appointments);

            var request = ReceptionistGetDailyAppointmentsMockData.GetValidRequest(today);

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(1);
            var item = response.Data!.First();
            item.Queue.Should().NotBeNull();
            item.Queue!.CalledAt.Should().BeNull();
        }
    }
}