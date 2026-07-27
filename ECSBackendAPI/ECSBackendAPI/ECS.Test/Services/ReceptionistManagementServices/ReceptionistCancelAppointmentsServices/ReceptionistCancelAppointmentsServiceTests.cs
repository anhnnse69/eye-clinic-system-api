using System.Linq.Expressions;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistCancelAppointmentsServices;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ReceptionistManagementServices.ReceptionistCancelAppointmentsServices
{
    public class ReceptionistCancelAppointmentsServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<Appointment, Guid, AppDbContext>> _appointmentRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext>> _slotRepoMock = new();
        private readonly ReceptionistCancelAppointmentsService _sut;

        public ReceptionistCancelAppointmentsServiceTests()
        {
            _sut = new ReceptionistCancelAppointmentsService(
                _appointmentRepoMock.Object,
                _slotRepoMock.Object);
        }

        private void SetupAppointmentRepository(List<Appointment> appointments)
        {
            var mockDbSet = appointments.BuildMockDbSet();

            _appointmentRepoMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>()))
                .Returns((Expression<Func<Appointment, bool>> predicate, bool _) =>
                    mockDbSet.Object.Where(predicate));
        }

        private void SetupSlotRepository(List<TimeSlot> slots)
        {
            var mockDbSet = slots.BuildMockDbSet();

            _slotRepoMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<TimeSlot, bool>>>(),
                    It.IsAny<bool>()))
                .Returns((Expression<Func<TimeSlot, bool>> predicate, bool _) =>
                    mockDbSet.Object.Where(predicate));
        }

        [Fact]
        public async Task Process_AppointmentNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var request = new ReceptionistCancelAppointmentsRequest
            {
                AppointmentId = ReceptionistCancelAppointmentsMockData.NonExistentAppointmentId,
                NoteReason = "Bệnh nhân báo bận đột xuất"
            };

            SetupAppointmentRepository(new List<Appointment>());

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be("APPOINTMENT_NOT_FOUND");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Process_EmptyOrNullNoteReason_ReturnsReasonRequiredError(string? invalidReason)
        {
            var appointment = ReceptionistCancelAppointmentsMockData.GetPendingAppointment();
            SetupAppointmentRepository(new List<Appointment> { appointment });

            var request = new ReceptionistCancelAppointmentsRequest
            {
                AppointmentId = appointment.Id,
                NoteReason = invalidReason!
            };

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be("CANCELLATION_REASON_REQUIRED");
        }

        [Fact]
        public async Task Process_AppointmentAlreadyCancelled_ReturnsAlreadyCancelledError()
        {
            // Arrange
            var cancelledAppointment = ReceptionistCancelAppointmentsMockData.GetCancelledAppointment();
            var request = new ReceptionistCancelAppointmentsRequest
            {
                AppointmentId = cancelledAppointment.Id,
                NoteReason = "Yêu cầu hủy lại"
            };

            SetupAppointmentRepository(new List<Appointment> { cancelledAppointment });

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be("APPOINTMENT_ALREADY_CANCELLED");
        }

        [Fact]
        public async Task Process_AppointmentInThePast_ReturnsCannotCancelPastAppointmentError()
        {
            var pastAppointment = ReceptionistCancelAppointmentsMockData.GetPastAppointment();
            var request = new ReceptionistCancelAppointmentsRequest
            {
                AppointmentId = pastAppointment.Id,
                NoteReason = "Muốn hủy lịch trong quá khứ"
            };

            SetupAppointmentRepository(new List<Appointment> { pastAppointment });

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be("CANNOT_CANCEL_PAST_APPOINTMENT");
        }

        [Theory]
        [InlineData(AppointmentStatus.COMPLETED)]
        [InlineData(AppointmentStatus.IN_PROGRESS)]
        [InlineData(AppointmentStatus.ARRIVED)]
        public async Task Process_AppointmentInInvalidStatus_ReturnsInvalidStatusError(AppointmentStatus invalidStatus)
        {
            var appointment = ReceptionistCancelAppointmentsMockData.GetPendingAppointment();
            appointment.Status = invalidStatus;

            var request = new ReceptionistCancelAppointmentsRequest
            {
                AppointmentId = appointment.Id,
                NoteReason = "Hủy khi không đúng trạng thái"
            };

            SetupAppointmentRepository(new List<Appointment> { appointment });

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be("INVALID_STATUS_FOR_CANCELLATION");
        }

        [Fact]
        public async Task Process_ValidRequest_SlotIsBooked_UpdatesSlotToAvailableAndCancelsAppointment()
        {
            var slot = ReceptionistCancelAppointmentsMockData.GetTimeSlot(currentPatients: 2, status: SlotStatus.BOOKED);
            slot.MaxPatients = 5;

            var appointment = ReceptionistCancelAppointmentsMockData.GetPendingAppointment(slot);

            SetupAppointmentRepository(new List<Appointment> { appointment });
            SetupSlotRepository(new List<TimeSlot> { slot });

            var request = new ReceptionistCancelAppointmentsRequest
            {
                AppointmentId = appointment.Id,
                NoteReason = "Bệnh nhân bận việc gia đình"
            };

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Data.Should().NotBeNull();
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data!.AppointmentId.Should().Be(appointment.Id.ToString());
            result.Data.Status.Should().Be(AppointmentStatus.CANCELLED.ToString());

            // Verify appointment updated
            _appointmentRepoMock.Verify(x => x.UpdateAsync(It.Is<Appointment>(a =>
                a.Status == AppointmentStatus.CANCELLED &&
                a.NoteReason == request.NoteReason)), Times.Once);

            _slotRepoMock.Verify(x => x.UpdateAsync(It.Is<TimeSlot>(s =>
                s.CurrentPatients == 1 &&
                s.Status == SlotStatus.AVAILABLE)), Times.Once);

            _appointmentRepoMock.Verify(x => x.EndTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_ValidRequest_SlotIsNotBooked_DoesNotChangeSlotStatusToAvailable()
        {
            var slot = ReceptionistCancelAppointmentsMockData.GetTimeSlot(currentPatients: 2, status: SlotStatus.BLOCKED);
            slot.MaxPatients = 5;

            var appointment = ReceptionistCancelAppointmentsMockData.GetPendingAppointment(slot);

            SetupAppointmentRepository(new List<Appointment> { appointment });
            SetupSlotRepository(new List<TimeSlot> { slot });

            var request = new ReceptionistCancelAppointmentsRequest
            {
                AppointmentId = appointment.Id,
                NoteReason = "Bệnh nhân bận việc gia đình"
            };

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Data.Should().NotBeNull();

            _slotRepoMock.Verify(x => x.UpdateAsync(It.Is<TimeSlot>(s =>
                s.CurrentPatients == 1 &&
                s.Status == SlotStatus.BLOCKED)), Times.Once);
        }

        [Fact]
        public async Task Process_ExceptionOccurs_RollsBackTransactionAndRethrows()
        {
            // Arrange (Cover line 77-80: catch Exception & Rollback)
            var slot = ReceptionistCancelAppointmentsMockData.GetTimeSlot();
            var appointment = ReceptionistCancelAppointmentsMockData.GetPendingAppointment(slot);

            SetupAppointmentRepository(new List<Appointment> { appointment });
            SetupSlotRepository(new List<TimeSlot> { slot });

            _appointmentRepoMock
                .Setup(x => x.UpdateAsync(It.IsAny<Appointment>()))
                .ThrowsAsync(new InvalidOperationException("Database connection error"));

            var request = new ReceptionistCancelAppointmentsRequest
            {
                AppointmentId = appointment.Id,
                NoteReason = "Bệnh nhân bận việc gia đình"
            };

            // Act
            Func<Task> act = async () => await _sut.Process(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Database connection error");

            _appointmentRepoMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }
    }
}