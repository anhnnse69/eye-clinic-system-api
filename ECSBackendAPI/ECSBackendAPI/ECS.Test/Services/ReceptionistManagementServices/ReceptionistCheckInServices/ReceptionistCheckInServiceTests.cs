using System.Linq.Expressions;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistCheckInServices;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ReceptionistManagementServices.ReceptionistCheckInServices
{
    public class ReceptionistCheckInServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<Appointment, Guid, AppDbContext>> _appointmentRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<Queue, Guid, AppDbContext>> _queueRepoMock = new();
        private readonly ReceptionistCheckInService _sut;

        public ReceptionistCheckInServiceTests()
        {
            _sut = new ReceptionistCheckInService(
                _appointmentRepoMock.Object,
                _queueRepoMock.Object);
        }

        private void SetupAppointmentRepository(List<Appointment> appointments)
        {
            var mockDbSet = appointments.BuildMockDbSet();

            _appointmentRepoMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<Appointment, object>>[]>()
                ))
                .Returns((Expression<Func<Appointment, bool>> predicate, bool _, Expression<Func<Appointment, object>>[] _) =>
                    mockDbSet.Object.Where(predicate));
        }

        private void SetupQueueRepository(List<Queue> queues)
        {
            var mockDbSet = queues.BuildMockDbSet();

            _queueRepoMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<Queue, bool>>>(),
                    It.IsAny<bool>()
                ))
                .Returns((Expression<Func<Queue, bool>> predicate, bool _) =>
                    mockDbSet.Object.Where(predicate));
        }

        [Fact]
        public async Task Process_AppointmentNotFound_ReturnsNotFoundError()
        {
            SetupAppointmentRepository(new List<Appointment>());
            SetupQueueRepository(new List<Queue>());

            var request = new ReceptionistCheckInRequest
            {
                AppointmentId = ReceptionistCheckInMockData.NonExistentAppointmentId
            };

            var result = await _sut.Process(request);

            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be("APPOINTMENT_NOT_FOUND");
        }

        [Fact]
        public async Task Process_AppointmentNotToday_ReturnsErrorNotToday()
        {
            var notTodayDate = ReceptionistCheckInMockData.GetTodayVnDate().AddDays(1);
            var appointment = ReceptionistCheckInMockData.GetValidCheckInAppointment(appointmentDate: notTodayDate);

            SetupAppointmentRepository(new List<Appointment> { appointment });
            SetupQueueRepository(new List<Queue>());

            var request = new ReceptionistCheckInRequest { AppointmentId = appointment.Id };

            var result = await _sut.Process(request);

            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be("ERROR_NOT_TODAY");
        }

        [Theory]
        [InlineData(AppointmentStatus.PENDING)]
        [InlineData(AppointmentStatus.ARRIVED)]
        [InlineData(AppointmentStatus.IN_PROGRESS)]
        [InlineData(AppointmentStatus.COMPLETED)]
        [InlineData(AppointmentStatus.CANCELLED)]
        public async Task Process_InvalidStatus_ReturnsInvalidStatusError(AppointmentStatus invalidStatus)
        {
            var appointment = ReceptionistCheckInMockData.GetValidCheckInAppointment(status: invalidStatus);

            SetupAppointmentRepository(new List<Appointment> { appointment });
            SetupQueueRepository(new List<Queue>());

            var request = new ReceptionistCheckInRequest { AppointmentId = appointment.Id };

            var result = await _sut.Process(request);

            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be("INVALID_STATUS_FOR_CHECKIN");
        }

        [Fact]
        public async Task Process_DepositNotPaid_ReturnsDepositMustBePaidError()
        {
            var appointment = ReceptionistCheckInMockData.GetValidCheckInAppointment(depositPaid: false);

            SetupAppointmentRepository(new List<Appointment> { appointment });
            SetupQueueRepository(new List<Queue>());

            var request = new ReceptionistCheckInRequest { AppointmentId = appointment.Id };

            var result = await _sut.Process(request);

            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be("DEPOSIT_MUST_BE_PAID_FIRST");
        }

        [Fact]
        public async Task Process_RoomNotConfigured_RoomIsNull_ReturnsDoctorRoomNotConfiguredError()
        {
            var appointment = ReceptionistCheckInMockData.GetValidCheckInAppointment(hasRoom: false);

            SetupAppointmentRepository(new List<Appointment> { appointment });
            SetupQueueRepository(new List<Queue>());

            var request = new ReceptionistCheckInRequest { AppointmentId = appointment.Id };

            var result = await _sut.Process(request);

            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be("DOCTOR_ROOM_NOT_CONFIGURED");
        }

        [Fact]
        public async Task Process_RoomNotConfigured_SlotIsNull_ReturnsDoctorRoomNotConfiguredError()
        {
            var appointment = ReceptionistCheckInMockData.GetValidCheckInAppointment(hasSlot: false);

            SetupAppointmentRepository(new List<Appointment> { appointment });
            SetupQueueRepository(new List<Queue>());

            var request = new ReceptionistCheckInRequest { AppointmentId = appointment.Id };

            var result = await _sut.Process(request);

            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be("DOCTOR_ROOM_NOT_CONFIGURED");
        }

        [Fact]
        public async Task Process_RoomNotConfigured_ScheduleIsNull_ReturnsDoctorRoomNotConfiguredError()
        {
            var appointment = ReceptionistCheckInMockData.GetValidCheckInAppointment(hasSchedule: false);

            SetupAppointmentRepository(new List<Appointment> { appointment });
            SetupQueueRepository(new List<Queue>());

            var request = new ReceptionistCheckInRequest { AppointmentId = appointment.Id };

            var result = await _sut.Process(request);

            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be("DOCTOR_ROOM_NOT_CONFIGURED");
        }

        [Theory]
        [InlineData(AppointmentStatus.CONFIRMED)]
        [InlineData(AppointmentStatus.BOOKED)]
        [InlineData(AppointmentStatus.NOSHOW)]
        public async Task Process_ValidRequest_NoExistingQueue_CreatesFirstQueueAndReturnsSuccess(AppointmentStatus validStatus)
        {
            var appointment = ReceptionistCheckInMockData.GetValidCheckInAppointment(status: validStatus);

            SetupAppointmentRepository(new List<Appointment> { appointment });
            SetupQueueRepository(new List<Queue>());

            var request = new ReceptionistCheckInRequest { AppointmentId = appointment.Id };

            var result = await _sut.Process(request);

            result.Data.Should().NotBeNull();
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data!.AppointmentId.Should().Be(appointment.Id.ToString());
            result.Data.Status.Should().Be(AppointmentStatus.ARRIVED.ToString());
            result.Data.Queue.Should().NotBeNull();
            result.Data.Queue.QueueNumber.Should().Be(1);
            result.Data.Queue.Status.Should().Be("WAITING");

            _appointmentRepoMock.Verify(x => x.UpdateAsync(It.Is<Appointment>(a =>
                a.Status == AppointmentStatus.ARRIVED)), Times.Once);

            _queueRepoMock.Verify(x => x.CreateAsync(It.Is<Queue>(q =>
                q.AppointmentId == appointment.Id &&
                q.QueueNumber == 1 &&
                q.Status == QueueStatus.WAITING)), Times.Once);

            _appointmentRepoMock.Verify(x => x.EndTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_ValidRequest_HasExistingQueues_IncrementsQueueNumberAndReturnsSuccess()
        {
            var appointment = ReceptionistCheckInMockData.GetValidCheckInAppointment();
            SetupAppointmentRepository(new List<Appointment> { appointment });

            DateTime todayVn = ReceptionistCheckInMockData.GetTodayVnDate();
            DateTime startOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(todayVn, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            var existingQueue1 = new Queue
            {
                Id = Guid.NewGuid(),
                RoomId = ReceptionistCheckInMockData.RoomId,
                QueueNumber = 3,
                CreatedAt = startOfDayUtc.AddHours(1)
            };
            var existingQueue2 = new Queue
            {
                Id = Guid.NewGuid(),
                RoomId = ReceptionistCheckInMockData.RoomId,
                QueueNumber = 5,
                CreatedAt = startOfDayUtc.AddHours(2)
            };

            SetupQueueRepository(new List<Queue> { existingQueue1, existingQueue2 });

            var request = new ReceptionistCheckInRequest { AppointmentId = appointment.Id };

            var result = await _sut.Process(request);

            result.Data.Should().NotBeNull();
            result.Data!.Queue.QueueNumber.Should().Be(6);

            _queueRepoMock.Verify(x => x.CreateAsync(It.Is<Queue>(q =>
                q.QueueNumber == 6)), Times.Once);
        }

        [Fact]
        public async Task Process_ExceptionOccurs_RollsBackTransactionAndRethrows()
        {
            var appointment = ReceptionistCheckInMockData.GetValidCheckInAppointment();
            SetupAppointmentRepository(new List<Appointment> { appointment });
            SetupQueueRepository(new List<Queue>());

            _queueRepoMock
                .Setup(x => x.CreateAsync(It.IsAny<Queue>()))
                .ThrowsAsync(new InvalidOperationException("Database insertion error"));

            var request = new ReceptionistCheckInRequest { AppointmentId = appointment.Id };

            Func<Task> act = async () => await _sut.Process(request);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Database insertion error");

            _appointmentRepoMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }
    }
}