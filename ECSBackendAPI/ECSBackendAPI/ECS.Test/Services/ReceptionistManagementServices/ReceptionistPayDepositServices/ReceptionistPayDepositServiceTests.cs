using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using Moq;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistPayDepositServices.Tests
{
    public class ReceptionistPayDepositServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<Appointment, Guid, AppDbContext>> _appointmentRepoMock;
        private readonly ReceptionistPayDepositService _sut;

        public ReceptionistPayDepositServiceTests()
        {
            _appointmentRepoMock = new Mock<IRepositoryBaseAsync<Appointment, Guid, AppDbContext>>();
            _sut = new ReceptionistPayDepositService(_appointmentRepoMock.Object);
        }

        [Fact]
        public async Task Process_AppointmentNotFound_ReturnsAppointmentNotFoundFailResponse()
        {
            // Arrange
            _appointmentRepoMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Appointment?)null);

            var request = ReceptionistPayDepositMockData.GetValidRequest();

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("APPOINTMENT_NOT_FOUND");
            response.Data.Should().BeNull();

            _appointmentRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
            _appointmentRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_AppointmentDateNotToday_ReturnsErrorNotTodayFailResponse()
        {
            // Arrange
            var appointment = ReceptionistPayDepositMockData.GetValidAppointment();
            appointment.AppointmentDate = ReceptionistPayDepositMockData.GetTodayVnDateTime().AddDays(-1);

            _appointmentRepoMock
                .Setup(r => r.GetByIdAsync(appointment.Id))
                .ReturnsAsync(appointment);

            var request = new ReceptionistPayDepositRequest { AppointmentId = appointment.Id };

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("ERROR_NOT_TODAY");
            response.Data.Should().BeNull();

            _appointmentRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
            _appointmentRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_DepositAlreadyPaid_ReturnsDepositAlreadyPaidFailResponse()
        {
            // Arrange
            var appointment = ReceptionistPayDepositMockData.GetValidAppointment();
            appointment.DepositPaid = true;

            _appointmentRepoMock
                .Setup(r => r.GetByIdAsync(appointment.Id))
                .ReturnsAsync(appointment);

            var request = new ReceptionistPayDepositRequest { AppointmentId = appointment.Id };

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("DEPOSIT_ALREADY_PAID");
            response.Data.Should().BeNull();

            _appointmentRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
            _appointmentRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Theory]
        [InlineData(AppointmentStatus.PENDING)]
        [InlineData(AppointmentStatus.IN_PROGRESS)]
        [InlineData(AppointmentStatus.COMPLETED)]
        [InlineData(AppointmentStatus.CANCELLED)]
        public async Task Process_InvalidStatusForDeposit_ReturnsInvalidStatusFailResponse(AppointmentStatus invalidStatus)
        {
            // Arrange
            var appointment = ReceptionistPayDepositMockData.GetValidAppointment();
            appointment.Status = invalidStatus;

            _appointmentRepoMock
                .Setup(r => r.GetByIdAsync(appointment.Id))
                .ReturnsAsync(appointment);

            var request = new ReceptionistPayDepositRequest { AppointmentId = appointment.Id };

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("INVALID_STATUS_FOR_DEPOSIT");
            response.Data.Should().BeNull();

            _appointmentRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
            _appointmentRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Theory]
        [InlineData(AppointmentStatus.CONFIRMED)]
        [InlineData(AppointmentStatus.BOOKED)]
        public async Task Process_ValidRequest_UpdatesDepositPaidAndReturnsSuccessResponse(AppointmentStatus validStatus)
        {
            // Arrange
            var appointment = ReceptionistPayDepositMockData.GetValidAppointment();
            appointment.Status = validStatus;

            _appointmentRepoMock
                .Setup(r => r.GetByIdAsync(appointment.Id))
                .ReturnsAsync(appointment);

            _appointmentRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<Appointment>()))
                .Returns(Task.CompletedTask);

            _appointmentRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            var request = new ReceptionistPayDepositRequest { AppointmentId = appointment.Id };

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("APP_MESSAGE_2000");
            response.Data.Should().NotBeNull();

            response.Data!.AppointmentId.Should().Be(appointment.Id.ToString());
            response.Data.DepositPaid.Should().BeTrue();
            response.Data.UpdatedAt.Should().NotBeNullOrEmpty();
            appointment.DepositPaid.Should().BeTrue();

            _appointmentRepoMock.Verify(r => r.UpdateAsync(appointment), Times.Once);
            _appointmentRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}